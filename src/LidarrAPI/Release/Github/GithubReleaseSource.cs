using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LidarrAPI.Database;
using LidarrAPI.Database.Models;
using LidarrAPI.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Octokit;

namespace LidarrAPI.Release.Github
{
    public class GithubReleaseSource : ReleaseSourceBase
    {
        private readonly Config _config;
        private readonly DatabaseContext _database;

        private readonly GitHubClient _gitHubClient;

        private readonly HttpClient _httpClient;

        private static readonly Regex ReleaseFeaturesGroup = new Regex(@"\*\s+[0-9a-f]{40}\s+New:\s*(?<text>.*?)\r*$", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex ReleaseFixesGroup = new Regex(@"\*\s+[0-9a-f]{40}\s+Fixed:\s*(?<text>.*?)\r*$", RegexOptions.Compiled | RegexOptions.Multiline);

        private enum Branch
        {
            Master,
            Develop
        }

        public GithubReleaseSource(DatabaseContext database, IOptions<Config> config)
        {
            _database = database;
            _config = config.Value;
            _gitHubClient = new GitHubClient(new ProductHeaderValue("LidarrAPI"));
            _httpClient = new HttpClient();
        }

        protected override async Task<bool> DoFetchReleasesAsync()
        {
            var hasNewRelease = false;

            var releases = (await _gitHubClient.Repository.Release.GetAll("Lidarr", "Lidarr")).ToArray();
            var validReleases = releases
                .Where(r => r.TagName.StartsWith("v") && VersionUtil.IsValid(r.TagName.Substring(1)))
                .Take(3)
                .Reverse();

            foreach (var release in validReleases)
            {
                // Check if release has been published.
                if (!release.PublishedAt.HasValue) continue;

                var version = release.TagName.Substring(1);

                // determine the branch
                var branch = release.Assets.Any(a => a.Name.StartsWith("Lidarr.master")) ? Branch.Master : Branch.Develop;

                hasNewRelease |= await ProcessRelease(release, branch, version);

                // releases on master should also appear on develop
                if (branch == Branch.Master)
                {
                    hasNewRelease |= await ProcessRelease(release, Branch.Develop, version);
                }
            }

            return hasNewRelease;
        }

        private async Task<bool> ProcessRelease(Octokit.Release release, Branch branch, string version)
        {
            bool isNewRelease = false;

            // Get an updateEntity
            var updateEntity = _database.UpdateEntities
                .Include(x => x.UpdateFiles)
                .FirstOrDefault(x => x.Version.Equals(version) && x.Branch.Equals(branch.ToString().ToLower()));

            if (updateEntity == null)
            {
                // Create update object
                updateEntity = new UpdateEntity
                {
                    Version = version,
                    ReleaseDate = release.PublishedAt.Value.UtcDateTime,
                    Branch = branch.ToString().ToLower()
                };

                // Start tracking this object
                await _database.AddAsync(updateEntity);

                // Set new release to true.
                isNewRelease = true;
            }

            // Parse changes
            var releaseBody = release.Body;
                
            var features = ReleaseFeaturesGroup.Matches(releaseBody);
            if (features.Any())
            {
                updateEntity.New.Clear();

                foreach (Match match in features)
                {
                    updateEntity.New.Add(match.Groups["text"].Value);
                }
            }

            var fixes = ReleaseFixesGroup.Matches(releaseBody);
            if (fixes.Any())
            {
                updateEntity.Fixed.Clear();

                foreach (Match match in fixes)
                {
                    updateEntity.Fixed.Add(match.Groups["text"].Value);
                }
            }

            // Process release files.
            foreach (var releaseAsset in release.Assets)
            {
                var operatingSystem = Parser.ParseOS(releaseAsset.Name);
                if (!operatingSystem.HasValue)
                {
                    continue;
                }

                var runtime = Parser.ParseRuntime(releaseAsset.Name);
                var arch = Parser.ParseArchitecture(releaseAsset.Name);

                // Check if exists in database.
                var updateFileEntity = _database.UpdateFileEntities
                    .FirstOrDefault(x =>
                                    x.UpdateEntityId == updateEntity.UpdateEntityId &&
                                    x.OperatingSystem == operatingSystem.Value &&
                                    x.Runtime == runtime &&
                                    x.Architecture == arch);

                if (updateFileEntity != null) continue;

                // Calculate the hash of the zip file.
                var releaseZip = Path.Combine(_config.DataDirectory, branch.ToString().ToLower(), releaseAsset.Name);
                string releaseHash;

                if (!File.Exists(releaseZip))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(releaseZip));

                    using (var fileStream = File.OpenWrite(releaseZip))
                    using (var artifactStream = await _httpClient.GetStreamAsync(releaseAsset.BrowserDownloadUrl))
                    {
                        await artifactStream.CopyToAsync(fileStream);
                    }
                }

                using (var stream = File.OpenRead(releaseZip))
                using (var sha = SHA256.Create())
                {
                    releaseHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLower();
                }

                File.Delete(releaseZip);

                // Add to database.
                updateEntity.UpdateFiles.Add(new UpdateFileEntity
                    {
                        OperatingSystem = operatingSystem.Value,
                        Architecture = arch,
                        Runtime = runtime,
                        Filename = releaseAsset.Name,
                        Url = releaseAsset.BrowserDownloadUrl,
                        Hash = releaseHash
                    });
            }

            // Save all changes to the database.
            await _database.SaveChangesAsync();

            return isNewRelease;
        }
    }
}
