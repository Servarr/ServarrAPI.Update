using System;
using System.Collections.Generic;
using System.IO;
using LidarrAPI.Database;
using LidarrAPI.Release;
using LidarrAPI.Release.AppVeyor;
using LidarrAPI.Release.Github;
using LidarrAPI.Update;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Targets;
using NLog.Web;
using Octokit;
using StatsdClient;
using TraktApiSharp;

namespace LidarrAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Loading .NetCore style of config variables from json and environment
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Config = builder.Build();
            ConfigLidarr = Config.GetSection("Lidarr").Get<Config>();

            env.ConfigureNLog("nlog.config");
            SetupDataDirectory();
            SetupDatadog();

            var triggersString = "";
            if (ConfigLidarr.Triggers != null)
            {
                triggersString += "Triggers\t>\n";
                foreach (KeyValuePair<Update.Branch, List<String>> entry in ConfigLidarr.Triggers)
                {
                    var combined = String.Join(", ", entry.Value);
                    triggersString += $"\t\t{entry.Key}: {combined}\n";
                }
            }
            else
            {
                triggersString += "No triggers registered";
            }

            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Debug($@"Config Variables
            ----------------
            DataDirectory  : {ConfigLidarr.DataDirectory}
            Database       : {ConfigLidarr.Database}
            APIKey         : {ConfigLidarr.ApiKey}
            AppVeyorApiKey : {ConfigLidarr.AppVeyorApiKey}
            {triggersString}");
        }

        public IConfiguration Config { get; }
        
        public Config ConfigLidarr { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Config>(Config.GetSection("Lidarr"));
            services.AddDbContextPool<DatabaseContext>(o => o.UseMySql(ConfigLidarr.Database));
            services.AddSingleton(new GitHubClient(new ProductHeaderValue("LidarrAPI")));
            
            services.AddTransient<ReleaseService>();
            services.AddTransient<GithubReleaseSource>();
            services.AddTransient<AppVeyorReleaseSource>();
            
            services.AddSingleton(new TraktClient(Config.GetSection("Trakt")["ClientId"], Config.GetSection("Trakt")["ClientSecret"]));
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime applicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            
            applicationLifetime.ApplicationStarted.Register(() => DogStatsd.Event("LidarrAPI", "LidarrAPI just started."));
            applicationLifetime.ApplicationStopped.Register(() => DogStatsd.Event("LidarrAPI", "LidarrAPI just stopped."));
        }

        private void SetupDataDirectory()
        {
            // Check data path
            if (!Path.IsPathRooted(ConfigLidarr.DataDirectory))
            {
                throw new Exception($"DataDirectory path must be absolute.\nDataDirectory: {ConfigLidarr.DataDirectory}");
            }

            // Create
            Directory.CreateDirectory(ConfigLidarr.DataDirectory);
        }

        private void SetupDatadog()
        {
            var server = Config.GetSection("DataDog")["Server"];
            var port = Config.GetSection("DataDog").GetValue<int>("Port");
            var prefix = Config.GetSection("DataDog")["Prefix"];

            if (string.IsNullOrWhiteSpace(server) || port == 0) return;

            DogStatsd.Configure(new StatsdConfig
            {
                StatsdServerName = server,
                StatsdPort = port,
                Prefix = prefix
            });
        }
    }
}