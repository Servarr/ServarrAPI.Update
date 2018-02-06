using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LidarrAPI.Database;
using LidarrAPI.Database.Models;
using LidarrAPI.Release.AppVeyor.Responses;
using LidarrAPI.Update;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OperatingSystem = LidarrAPI.Update.OperatingSystem;

namespace LidarrAPI.Release.AppVeyor
{
    public class AppVeyorReleaseSource : ReleaseSourceBase
    {
        private const string AccountName = "lidarr";
        private const string ProjectSlug = "lidarr";

        private static int? _lastBuildId;

        private readonly Config _config;

        private readonly DatabaseContext _database;

        private readonly HttpClient _downloadHttpClient;

        private readonly HttpClient _httpClient;

        public AppVeyorReleaseSource(DatabaseContext database, IOptions<Config> config)
        {
            _database = database;
            _config = config.Value;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.AppVeyorApiKey);

            _downloadHttpClient = new HttpClient();
        }

        protected override async Task DoFetchReleasesAsync()
        {
            if (ReleaseBranch == Branch.Unknown)
            {
                throw new ArgumentException("ReleaseBranch must not be unknown when fetching releases.");
            }

            var historyUrl =
                $"https://ci.appveyor.com/api/projects/{AccountName}/{ProjectSlug}/history?recordsNumber=10&branch=develop";

            var historyData = await _httpClient.GetStringAsync(historyUrl);
            var history = JsonConvert.DeserializeObject<AppVeyorProjectHistory>(historyData);

            // Store here temporarily so we don't break on not processed builds.
            var lastBuild = _lastBuildId;

            foreach (var build in history.Builds.Where(b => b.Status.ToLower().Equals("success")).Take(5).ToList()) // Only take sucessful builds, and only the last 5
            {
                if (lastBuild.HasValue &&
                    lastBuild.Value >= build.BuildId) break;

                // Make sure we dont distribute;
                // - pull requests,
                // - unsuccesful builds,
                // - tagged builds (duplicate).
                if (build.PullRequestId.HasValue ||
                    build.IsTag) continue;

                var buildExtendedData = await _httpClient.GetStringAsync(
                    $"https://ci.appveyor.com/api/projects/{AccountName}/{ProjectSlug}/build/{build.Version}");
                var buildExtended = JsonConvert.DeserializeObject<AppVeyorProjectLastBuild>(buildExtendedData).Build;

                // Filter out incomplete builds
                var buildJob = buildExtended.Jobs.FirstOrDefault();
                if (buildJob == null ||
                    buildJob.ArtifactsCount == 0 ||
                    !buildExtended.Started.HasValue) continue;

                // Grab artifacts
                var artifactsPath = $"https://ci.appveyor.com/api/buildjobs/{buildJob.JobId}/artifacts";
                var artifactsData = await _httpClient.GetStringAsync(artifactsPath);
                var artifacts = JsonConvert.DeserializeObject<AppVeyorArtifact[]>(artifactsData);

                // Get an updateEntity
                var updateEntity = _database.UpdateEntities
                    .Include(x => x.UpdateFiles)
                    .FirstOrDefault(x => x.Version.Equals(buildExtended.Version) && x.Branch.Equals(ReleaseBranch));

                if (updateEntity == null)
                {
                    // Create update object
                    updateEntity = new UpdateEntity
                    {
                        Version = buildExtended.Version,
                        ReleaseDate = buildExtended.Started.Value.UtcDateTime,
                        Branch = ReleaseBranch,
                        Status = build.Status,
                        New = new List<string>
                        {
                            build.Message
                        }
                    };

                    // Add extra message
                    if (!string.IsNullOrWhiteSpace(build.MessageExtended))
                    {
                        updateEntity.New.Add(build.MessageExtended);
                    }

                    // Start tracking this object
                    await _database.AddAsync(updateEntity);
                }

                // Process artifacts
                foreach (var artifact in artifacts)
                {
                    // Detect target operating system.
                    OperatingSystem operatingSystem;

                    // NB: Added this because our "artifatcs incliude a Lidarr...windows.exe, which really shouldn't be added
                    if (artifact.FileName.Contains("windows.") && artifact.FileName.ToLower().Contains(".zip"))
                    {
                        operatingSystem = OperatingSystem.Windows;
                    }
                    else if (artifact.FileName.Contains("linux."))
                    {
                        operatingSystem = OperatingSystem.Linux;
                    }
                    else if (artifact.FileName.Contains("osx."))
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
                    var releaseDownloadUrl = $"{artifactsPath}/{artifact.FileName}";
                    var releaseFileName = artifact.FileName.Split('/').Last();
                    var releaseZip = Path.Combine(_config.DataDirectory, ReleaseBranch.ToString(), releaseFileName);
                    string releaseHash;

                    if (!File.Exists(releaseZip))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(releaseZip));
                        File.WriteAllBytes(releaseZip, await _downloadHttpClient.GetByteArrayAsync(releaseDownloadUrl));
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
                        Filename = releaseFileName,
                        Url = releaseDownloadUrl,
                        Hash = releaseHash
                    });
                }

                // Save all changes to the database.
                await _database.SaveChangesAsync();

                // Make sure we atleast skip this build next time.
                if (_lastBuildId == null ||
                    _lastBuildId.Value < build.BuildId)
                {
                    _lastBuildId = build.BuildId;
                }
            }
        }
    }
}