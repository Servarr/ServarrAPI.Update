using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Octokit;
using Serilog;
using ServarrAPI.Database;
using ServarrAPI.Release;
using ServarrAPI.Release.Azure;
using ServarrAPI.Release.Github;

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
            ConfigServarr = Config.GetSection("Servarr").Get<Config>();

            Log.Debug($@"Config Variables
            ----------------
            DataDirectory  : {ConfigServarr.DataDirectory}
            Database       : {ConfigServarr.Database}
            APIKey         : {ConfigServarr.ApiKey}");

            SetupDataDirectory();
        }

        public IConfiguration Config { get; }

        public Config ConfigServarr { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Config>(Config.GetSection("Servarr"));
            services.AddDbContextPool<DatabaseContext>(o => o.UseNpgsql(ConfigServarr.Database));
            services.AddSingleton(new GitHubClient(new ProductHeaderValue("ServarrAPI")));

            services.AddTransient<ReleaseService>();
            services.AddTransient<GithubReleaseSource>();
            services.AddTransient<AzureReleaseSource>();

            services
                .AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.IgnoreNullValues = true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            UpdateDatabase(app);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
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

        private static void UpdateDatabase(IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices
                   .GetRequiredService<IServiceScopeFactory>()
                   .CreateScope();
            using (var context = serviceScope.ServiceProvider.GetService<DatabaseContext>())
            {
                context.Database.Migrate();
            }
        }
    }
}
