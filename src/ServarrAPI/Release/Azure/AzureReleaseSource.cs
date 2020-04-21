using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ServarrAPI.Database;
using ServarrAPI.Database.Models;
using ServarrAPI.Extensions;
using ServarrAPI.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Octokit;

namespace ServarrAPI.Release.Azure
{
    public class AzureReleaseSource : ReleaseSourceBase
    {
        private const string PackageArtifactName = "Packages";
        private readonly int[] BuildPipelines = new int[] { 1 };

        private static int? _lastBuildId;

        private readonly Config _config;
        private readonly DatabaseContext _database;
        private readonly GitHubClient _githubClient;
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

            _connection = new VssConnection(new Uri($"https://dev.azure.com/{_config.Project}"), new VssBasicCredential());
            _githubClient = new GitHubClient(new ProductHeaderValue("ServarrAPI"));
            _httpClient = new HttpClient();

            _logger = logger;
        }

        protected override async Task<bool> DoFetchReleasesAsync()
        {
            var hasNewRelease = false;

            var buildClient = _connection.GetClient<BuildHttpClient>();
            var nightlyHistory = await buildClient.GetBuildsAsync(project: _config.Project,
                                                                  definitions: BuildPipelines,
                                                                  branchName: "refs/heads/develop",
                                                                  reasonFilter: BuildReason.IndividualCI | BuildReason.Manual,
                                                                  statusFilter: BuildStatus.Completed,
                                                                  resultFilter: BuildResult.Succeeded,
                                                                  queryOrder: BuildQueryOrder.StartTimeDescending,
                                                                  top: 5);

            var branchHistory = await buildClient.GetBuildsAsync(project: _config.Project,
                                                             definitions: BuildPipelines,
                                                             reasonFilter: BuildReason.PullRequest | BuildReason.Manual | BuildReason.IndividualCI,
                                                             statusFilter: BuildStatus.Completed,
                                                             resultFilter: BuildResult.Succeeded,
                                                             queryOrder: BuildQueryOrder.StartTimeDescending,
                                                             top: 10);

            var history = nightlyHistory.Concat(branchHistory).DistinctBy(x => x.Id).OrderByDescending(x => x.Id);

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

                // Get the branch - either PR source branch or the actual brach
                string branch = null;
                if (build.SourceBranch.StartsWith("refs/heads/"))
                {
                    branch = build.SourceBranch.Replace("refs/heads/", string.Empty);
                }
                else if (build.SourceBranch.StartsWith("refs/pull/"))
                {
                    var success = int.TryParse(build.SourceBranch.Split("/")[2], out var prNum);
                    if (!success)
                    {
                        continue;
                    }

                    var pr = await _githubClient.PullRequest.Get(_config.Project, _config.Project, prNum);

                    if (pr.Head.Repository.Fork)
                    {
                        continue;
                    }

                    branch = pr.Head.Ref;
                }
                else
                {
                    continue;
                }

                // If the branch is call develop (conflicts with daily develop builds)
                // or branch is called master (will get picked up when the github release goes up)
                // then skip
                if (branch == "nightly" || branch == "master")
                {
                    _logger.LogInformation($"Skipping azure build with branch {branch}");
                    continue;
                }

                // On azure, develop -> nightly
                if (branch == "develop")
                {
                    branch = "nightly";
                }

                _logger.LogInformation($"Found branch for version {build.BuildNumber}: {branch}");

                // Get build changes
                var changesTask = buildClient.GetBuildChangesAsync(_config.Project, build.Id);

                // Grab artifacts
                var artifacts = await buildClient.GetArtifactsAsync(_config.Project, build.Id);

                // there should be a single artifact called 'Packages' we parse for packages
                var artifact = artifacts.FirstOrDefault(x => x.Name == PackageArtifactName);
                if (artifact == null)
                {
                    continue;
                }

                var artifactClient = _connection.GetClient<ArtifactHttpClient>();
                var files = await artifactClient.GetArtifactFiles(_config.Project, build.Id, artifact);

                // Get an updateEntity
                var updateEntity = _database.UpdateEntities
                    .Include(x => x.UpdateFiles)
                    .FirstOrDefault(x => x.Version.Equals(build.BuildNumber) && x.Branch.Equals(branch));

                if (updateEntity == null)
                {
                    // Create update object
                    updateEntity = new UpdateEntity
                    {
                        Version = build.BuildNumber,
                        ReleaseDate = build.StartTime.Value,
                        Branch = branch,
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
                    var releaseZip = Path.Combine(_config.DataDirectory, branch, releaseFileName);
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
