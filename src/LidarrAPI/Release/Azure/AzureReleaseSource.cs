using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LidarrAPI.Database;
using LidarrAPI.Database.Models;
using LidarrAPI.Update;
using LidarrAPI.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace LidarrAPI.Release.Azure
{
    public class AzureReleaseSource : ReleaseSourceBase
    {
        private const string AccountName = "Lidarr";
        private const string ProjectSlug = "Lidarr";
        private const string PackageArtifactName = "Packages";
        private readonly int[] BuildPipelines = new int[] { 1 };

        private static int? _lastBuildId;

        private readonly Config _config;
        private readonly DatabaseContext _database;
        private readonly HttpClient _httpClient;
        private readonly VssConnection _connection;

        private readonly ILogger<AzureReleaseSource> _logger;

        private static readonly Regex ReleaseFeaturesGroup = new Regex(@"^New:\s*(?<text>.*?)\r*$", RegexOptions.Compiled);

        private static readonly Regex ReleaseFixesGroup = new Regex(@"^Fixed:\s*(?<text>.*?)\r*$", RegexOptions.Compiled);

        public AzureReleaseSource(DatabaseContext database,
                                  IOptions<Config> config,
                                  ILogger<AzureReleaseSource> logger)
        {
            _database = database;
            _config = config.Value;

            _connection = new VssConnection(new Uri($"https://dev.azure.com/{AccountName}"), new VssBasicCredential());

            _httpClient = new HttpClient();

            _logger = logger;
        }

        protected override async Task<bool> DoFetchReleasesAsync()
        {
            if (ReleaseBranch == Branch.Unknown)
            {
                throw new ArgumentException("ReleaseBranch must not be unknown when fetching releases.");
            }

            string branchName;
            if (ReleaseBranch == Branch.Nightly)
            {
                branchName = "develop";
            }
            else if (ReleaseBranch == Branch.NetCore)
            {
                branchName = "dotnet-core-2";
            }
            else
            {
                throw new ArgumentException($"ReleaseBranch {ReleaseBranch} not supported for Azure");
            }

            var hasNewRelease = false;

            var buildClient = _connection.GetClient<BuildHttpClient>();
            var history = await buildClient.GetBuildsAsync(project: ProjectSlug,
                                                           definitions: BuildPipelines,
                                                           branchName: $"refs/heads/{branchName}",
                                                           reasonFilter: BuildReason.IndividualCI | BuildReason.Manual,
                                                           statusFilter: BuildStatus.Completed,
                                                           resultFilter: BuildResult.Succeeded,
                                                           queryOrder: BuildQueryOrder.StartTimeDescending,
                                                           top: 5);

            // var history = JsonSerializer.Deserialize<AzureList<AzureProjectBuild>>(historyData).Value;

            // Store here temporarily so we don't break on not processed builds.
            var lastBuild = _lastBuildId;

            // URL query has filtered to most recent 5 successful, completed builds
            foreach (var build in history)
            {
                if (lastBuild.HasValue && lastBuild.Value >= build.Id)
                {
                    break;
                }

                // Extract the build version
                _logger.LogInformation($"Found version: {build.BuildNumber}");

                // Get build changes
                var changesTask = buildClient.GetBuildChangesAsync(ProjectSlug, build.Id);

                // Grab artifacts
                var artifacts = await buildClient.GetArtifactsAsync(ProjectSlug, build.Id);

                // there should be a single artifact called 'Packages' we parse for packages
                var artifact = artifacts.FirstOrDefault(x => x.Name == PackageArtifactName);
                if (artifact == null)
                {
                    continue;
                }

                var artifactClient = _connection.GetClient<ArtifactHttpClient>();
                var files = await artifactClient.GetArtifactFiles(ProjectSlug, build.Id, artifact);

                // Get an updateEntity
                var updateEntity = _database.UpdateEntities
                    .Include(x => x.UpdateFiles)
                    .FirstOrDefault(x => x.Version.Equals(build.BuildNumber) && x.Branch.Equals(ReleaseBranch));

                if (updateEntity == null)
                {
                    // Create update object
                    updateEntity = new UpdateEntity
                    {
                        Version = build.BuildNumber,
                        ReleaseDate = build.StartTime.Value,
                        Branch = ReleaseBranch,
                        Status = build.Status.ToString()
                    };

                    // Start tracking this object
                    await _database.AddAsync(updateEntity);

                    // Set new release to true.
                    hasNewRelease = true;
                }

                // Parse changes
                var changes = await changesTask;
                var features = changes.Select(x => ReleaseFeaturesGroup.Match(x.Message));
                if (features.Any(x => x.Success))
                {
                    updateEntity.New.Clear();

                    foreach (Match match in features.Where(x => x.Success))
                    {
                        updateEntity.New.Add(match.Groups["text"].Value);
                    }
                }

                var fixes = changes.Select(x => ReleaseFixesGroup.Match(x.Message));
                if (fixes.Any(x => x.Success))
                {
                    updateEntity.Fixed.Clear();

                    foreach (Match match in fixes.Where(x => x.Success))
                    {
                        updateEntity.Fixed.Add(match.Groups["text"].Value);
                    }
                }

                // Process artifacts
                foreach (var file in files)
                {
                    _logger.LogDebug("Processing {0}", file.Path);

                    // Detect target operating system.
                    var operatingSystem = Parser.ParseOS(file.Path);
                    if (!operatingSystem.HasValue)
                    {
                        continue;
                    }

                    _logger.LogDebug("Got os {0}", operatingSystem);

                    // Detect runtime / arch
                    var runtime = Parser.ParseRuntime(file.Path);
                    _logger.LogDebug("Got runtime {0}", runtime);

                    var arch = Parser.ParseArchitecture(file.Path);
                    _logger.LogDebug("Got arch {0}", arch);

                    // Check if exists in database.
                    var updateFileEntity = _database.UpdateFileEntities
                        .FirstOrDefault(x =>
                            x.UpdateEntityId == updateEntity.UpdateEntityId &&
                            x.OperatingSystem == operatingSystem.Value &&
                            x.Runtime == runtime &&
                            x.Architecture == arch);

                    if (updateFileEntity != null) continue;

                    // Calculate the hash of the zip file.
                    var releaseFileName = Path.GetFileName(file.Path);
                    var releaseZip = Path.Combine(_config.DataDirectory, ReleaseBranch.ToString(), releaseFileName);
                    string releaseHash;

                    if (!File.Exists(releaseZip))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(releaseZip));

                        using (var fileStream = File.OpenWrite(releaseZip))
                        using (var artifactStream = await _httpClient.GetStreamAsync(file.Url))
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
                        OperatingSystem = operatingSystem.Value,
                        Architecture = arch,
                        Runtime = runtime,
                        Filename = releaseFileName,
                        Url = file.Url,
                        Hash = releaseHash
                    });
                }

                // Save all changes to the database.
                await _database.SaveChangesAsync();

                // Make sure we atleast skip this build next time.
                if (_lastBuildId == null ||
                    _lastBuildId.Value < build.Id)
                {
                    _lastBuildId = build.Id;
                }
            }

            return hasNewRelease;
        }
    }
}
