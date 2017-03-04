using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Octokit;
using RadarrAPI.Database;
using RadarrAPI.Release;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using StatsdClient;
using TraktApiSharp;

namespace RadarrAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            env.ConfigureNLog("nlog.config");

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            
            SetupDataDirectory();
            SetupDatadog();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Config>(Configuration);
            services.AddSingleton(new GitHubClient(new ProductHeaderValue("RadarrAPI")));
            services.AddSingleton<ReleaseService>();
            services.AddSingleton(new TraktClient(Configuration.GetSection("Trakt")["ClientId"], Configuration.GetSection("Trakt")["ClientSecret"]));
            services.AddMvc();

            // Add database
            services.AddDbContext<DatabaseContext>(o => o.UseMySql(Configuration["Database"]));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime applicationLifetime)
        {
            loggerFactory.AddNLog();
            app.AddNLogWeb();
            app.UseMvc();

            applicationLifetime.ApplicationStarted.Register(() => DogStatsd.Event("RadarrAPI", "RadarrAPI just started."));
        }

        private void SetupDataDirectory()
        {
            // Check data path
            if (!Path.IsPathRooted(Configuration["DataDirectory"]))
            {
                throw new Exception("DataDirectory path must be absolute.");
            }

            // Create
            Directory.CreateDirectory(Configuration["DataDirectory"]);
        }

        private void SetupDatadog()
        {
            var server = Configuration.GetSection("DataDog")["Server"];
            var port = Configuration.GetSection("DataDog").GetValue<int>("Port");
            var prefix = Configuration.GetSection("DataDog")["Prefix"];

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
