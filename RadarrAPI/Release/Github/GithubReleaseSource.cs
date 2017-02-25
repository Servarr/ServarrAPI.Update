using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using RadarrAPI.Database;
using RadarrAPI.Database.Models;
using RadarrAPI.Update;
using RadarrAPI.Util;
using Branch = RadarrAPI.Update.Branch;

namespace RadarrAPI.Release.Github
{
    public class GithubReleaseSource : ReleaseSourceBase
    {
        private readonly GitHubClient _gitHubClient;
        private readonly HttpClient _httpClient;

        public GithubReleaseSource(IServiceProvider serviceProvider, Branch branch) : base(serviceProvider, branch)
        {
            _gitHubClient = new GitHubClient(new ProductHeaderValue("RadarrAPI"));
            _httpClient = new HttpClient();
        }

        protected override async Task DoFetchReleasesAsync()
        {
            var releases = await _gitHubClient.Repository.Release.GetAll("Radarr", "Radarr");
            var validReleases = releases.Where(r =>
                r.TagName.StartsWith("v") &&
                VersionUtil.IsValid(r.TagName.Substring(1)) &&
                r.Prerelease == (ReleaseBranch == Branch.Develop))
                .Reverse();

            var database = ServiceProvider.GetService<DatabaseContext>();

            foreach (var release in validReleases)
            {
                // Check if release has been published.
                if (!release.PublishedAt.HasValue) continue;

                var version = release.TagName.Substring(1);

                // Get an updateEntity
                var updateEntity = database.UpdateEntities
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

                    // Parse changes
                    var releaseBody = release.Body;

                    var features = RegexUtil.ReleaseFeaturesGroup.Match(releaseBody);
                    if (features.Success)
                    {
                        foreach (Match match in RegexUtil.ReleaseChange.Matches(features.Groups["features"].Value))
                        {
                            if (match.Success) updateEntity.New.Add(match.Groups["text"].Value);
                        }
                    }

                    var fixes = RegexUtil.ReleaseFixesGroup.Match(releaseBody);
                    if (fixes.Success)
                    {
                        foreach (Match match in RegexUtil.ReleaseChange.Matches(fixes.Groups["fixes"].Value))
                        {
                            if (match.Success) updateEntity.Fixed.Add(match.Groups["text"].Value);
                        }
                    }

                    // Start tracking this object
                    await database.AddAsync(updateEntity);
                }

                // Process releases
                foreach (var releaseAsset in release.Assets)
                {
                    // Detect target operating system.
                    OperatingSystem operatingSystem;

                    if (releaseAsset.Name.Contains("windows."))
                    {
                        operatingSystem = OperatingSystem.Windows;
                    }
                    else if(releaseAsset.Name.Contains("linux."))
                    {
                        operatingSystem = OperatingSystem.Linux;
                    }
                    else if(releaseAsset.Name.Contains("osx."))
                    {
                        operatingSystem = OperatingSystem.Osx;
                    }
                    else
                    {
                        continue;
                    }

                    // Check if exists in database.
                    var updateFileEntity = database.UpdateFileEntities
                        .FirstOrDefault(x => 
                            x.UpdateEntityId == updateEntity.UpdateEntityId && 
                            x.OperatingSystem == operatingSystem);

                    if (updateFileEntity != null) continue;

                    // Calculate the hash of the zip file.
                    var releaseZip = Path.Combine(Config.DataDirectory, ReleaseBranch.ToString(), releaseAsset.Name);
                    string releaseHash;

                    if (!File.Exists(releaseZip))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(releaseZip));
                        File.WriteAllBytes(releaseZip, await _httpClient.GetByteArrayAsync(releaseAsset.BrowserDownloadUrl));
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
                await database.SaveChangesAsync();
            }
        }
    }
}
