using System;
using System.Diagnostics;
using System.Reflection;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServarrAPI.Datastore.Migration.Framework
{
    public interface IMigrationController
    {
        void Migrate(string connectionString, MigrationContext migrationContext);
    }

    public class MigrationController : IMigrationController
    {
        private readonly ILogger _logger;

        public MigrationController(ILogger<MigrationController> logger)
        {
            _logger = logger;
        }

        public void Migrate(string connectionString, MigrationContext migrationContext)
        {
            var sw = Stopwatch.StartNew();

            _logger.LogInformation("*** Migrating {0} ***", connectionString);

            var serviceProvider = new ServiceCollection()
                .AddLogging(lb => lb.AddProvider(new MigrationLoggerProvider(_logger)))
                .AddFluentMigratorCore()
                .ConfigureRunner(
                    builder => builder
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .WithMigrationsIn(Assembly.GetExecutingAssembly()))
                .Configure<TypeFilterOptions>(opt => opt.Namespace = "ServarrAPI.Datastore.Migration")
                .Configure<ProcessorOptions>(opt =>
                {
                    opt.PreviewOnly = false;
                    opt.Timeout = TimeSpan.FromSeconds(60);
                })
                .BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

                MigrationContext.Current = migrationContext;

                if (migrationContext.DesiredVersion.HasValue)
                {
                    runner.MigrateUp(migrationContext.DesiredVersion.Value);
                }
                else
                {
                    runner.MigrateUp();
                }

                MigrationContext.Current = null;
            }

            sw.Stop();

            _logger.LogDebug("Took: {0}", sw.Elapsed);
        }
    }
}
