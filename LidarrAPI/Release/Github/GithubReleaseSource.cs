using System;
using System.Collections.Generic;
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
using Branch = LidarrAPI.Update.Branch;
using OperatingSystem = LidarrAPI.Update.OperatingSystem;

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


        public GithubReleaseSource(DatabaseContext database, IOptions<Config> config)
        {
            _database = database;
            _config = config.Value;
            _gitHubClient = new GitHubClient(new ProductHeaderValue("LidarrAPI"));
            _httpClient = new HttpClient();
        }

        protected override async Task<bool> DoFetchReleasesAsync()
        {
            if (ReleaseBranch == Branch.Unknown)
            {
                throw new ArgumentException("ReleaseBranch must not be unknown when fetching releases.");
            }

            List<string> allowedNames;

            if (ReleaseBranch == Branch.Master)
            {
                allowedNames = new List<string> { "Lidarr.master" };
            }
            else if (ReleaseBranch == Branch.Develop)
            {
                allowedNames = new List<string> { "Lidarr.master", "Lidarr.develop" };
            }
            else
            {
                throw new ArgumentException("Branch {0} cannot be used with GitHubReleaseSource", ReleaseBranch.ToString());
            }

            var hasNewRelease = false;

            var releases = (await _gitHubClient.Repository.Release.GetAll("Lidarr", "Lidarr")).ToArray();
            var validReleases = releases
                .Where(r =>
                       r.Assets.Any(asset => allowedNames.Any(name => asset.Name.StartsWith(name))) &&
                       r.TagName.StartsWith("v") && VersionUtil.IsValid(r.TagName.Substring(1))
                    )
                .Take(3)
                .Reverse();

            foreach (var release in validReleases)
            {
                // Check if release has been published.
                if (!release.PublishedAt.HasValue) continue;

                var version = release.TagName.Substring(1);

                // Get an updateEntity
                var updateEntity = _database.UpdateEntities
                    .Include(x => x.UpdateFiles)
                    .FirstOrDefault(x => x.Version.Equals(version) && x.Branch.Equals(ReleaseBranch));

                if (updateEntity == null)
                {
                    // Create update object
                    updateEntity = new UpdateEntity
                    {
                        Version = version,
                        ReleaseDate = release.PublishedAt.Value.UtcDateTime,
                        Branch = ReleaseBranch
                    };

                    // Start tracking this object
                    await _database.AddAsync(updateEntity);

                    // Set new release to true.
                    hasNewRelease = true;
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
                    // Detect target operating system.
                    OperatingSystem operatingSystem;

                    if (releaseAsset.Name.Contains("windows."))
                    {
                        operatingSystem = OperatingSystem.Windows;
                    }
                    else if (releaseAsset.Name.Contains("linux."))
                    {
                        operatingSystem = OperatingSystem.Linux;
                    }
                    else if (releaseAsset.Name.Contains("osx."))
                    {
                        operatingSystem = OperatingSystem.Osx;
                    }
                    else
                    {
                        continue;
                    }

                    // Check if exists in database.
                    var updateFileEntity = _database.UpdateFileEntities
                        .FirstOrDefault(x =>
                            x.UpdateEntityId == updateEntity.UpdateEntityId &&
                            x.OperatingSystem == operatingSystem);

                    if (updateFileEntity != null) continue;

                    // Calculate the hash of the zip file.
                    var releaseZip = Path.Combine(_config.DataDirectory, ReleaseBranch.ToString(), releaseAsset.Name);
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
                    {
                        using (var sha = SHA256.Create())
                        {
                            releaseHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLower();
                        }
                    }

                    File.Delete(releaseZip);

                    // Add to database.
                    updateEntity.UpdateFiles.Add(new UpdateFileEntity
                    {
                        OperatingSystem = operatingSystem,
                        Filename = releaseAsset.Name,
                        Url = releaseAsset.BrowserDownloadUrl,
                        Hash = releaseHash
                    });
                }

                // Save all changes to the database.
                await _database.SaveChangesAsync();
            }

            return hasNewRelease;
        }
    }
}
