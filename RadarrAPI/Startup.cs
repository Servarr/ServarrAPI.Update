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
            
            // Check data path
            if (!Path.IsPathRooted(Configuration["DataDirectory"]))
            {
                throw new Exception("DataDirectory path must be absolute.");
            }

            // Create
            Directory.CreateDirectory(Configuration["DataDirectory"]);
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Config>(Configuration);
            services.AddSingleton(new GitHubClient(new ProductHeaderValue("RadarrAPI")));
            services.AddSingleton<ReleaseService>();
            services.AddMvc();

            // Add database
            services.AddDbContext<DatabaseContext>(o => o.UseMySql(Configuration["Database"]));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddNLog();
            app.AddNLogWeb();
            app.UseMvc();
        }
    }
}
