using System;
using System.Configuration;
using System.IO;
using InfluxDB.Collector;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Octokit;
using Serilog;
using ServarrAPI.Cloudflare;
using ServarrAPI.Datastore;
using ServarrAPI.Datastore.Migration.Framework;
using ServarrAPI.Model;
using ServarrAPI.Release;
using ServarrAPI.Release.Azure;
using ServarrAPI.Release.Github;
using ServarrAPI.TaskQueue;

namespace ServarrAPI
{
    public class Startup
    {
        public Startup(IHostEnvironment env)
        {
            // Loading .NetCore style of config variables from json and environment
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Config = builder.Build();
            ConfigServarr = Config.GetSection("Update").Get<Config>();

            if (ConfigServarr == null)
            {
                throw new Exception("Update config not found");
            }

            Log.Debug($@"Config Variables
            ----------------
            Database       : {Config.GetConnectionString("Database")}
            Project        : {ConfigServarr.Project}
            DataDirectory  : {ConfigServarr.DataDirectory}
            APIKey         : {ConfigServarr.ApiKey}");

            SetupDataDirectory();

            SetupTelegraf();
        }

        public IConfiguration Config { get; }

        public Config ConfigServarr { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Config>(Config.GetSection("Update"));

            services.AddSingleton<IMigrationController, MigrationController>();
            services.AddSingleton<IDatabase, Database>();
            services.AddSingleton<IUpdateRepository, UpdateRepository>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IUpdateFileRepository, UpdateFileRepository>();
            services.AddSingleton<IUpdateFileService, UpdateFileService>();
            services.AddSingleton<ICloudflareProxy, CloudflareProxy>();
            services.AddSingleton(new GitHubClient(new ProductHeaderValue("ServarrAPI")));
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            services.AddTransient<ReleaseService>();
            services.AddTransient<GithubReleaseSource>();
            services.AddTransient<AzureReleaseSource>();

            services.AddHostedService<QueuedHostedService>();

            services
                .AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.IgnoreNullValues = true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            SqlBuilderExtensions.LogSql = ConfigServarr.LogSql;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void SetupDataDirectory()
        {
            // Check data path
            if (!Path.IsPathRooted(ConfigServarr.DataDirectory))
            {
                throw new Exception($"DataDirectory path must be absolute.\nDataDirectory: {ConfigServarr.DataDirectory}");
            }

            // Create
            Directory.CreateDirectory(ConfigServarr.DataDirectory);
        }

        private void SetupTelegraf()
        {
            var server = Config.GetSection("Stats")["Server"];
            var port = Config.GetSection("Stats").GetValue<int>("Port");

            if (string.IsNullOrWhiteSpace(server) || port == 0)
            {
                return;
            }

            Metrics.Collector = new CollectorConfiguration()
                .Tag.With("application_path", Environment.GetEnvironmentVariable("SERVICE_HOSTNAME"))
                .Batch.AtInterval(TimeSpan.FromSeconds(2))
                .WriteTo.InfluxDB(string.Format("http://{0}:{1}", server, port), "telegraf")
                .CreateCollector();
        }
    }
}
