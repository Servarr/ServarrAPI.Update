using System;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Logging;
using Microsoft.Extensions.Logging;

namespace ServarrAPI.Datastore.Migration.Framework
{
    public class MigrationLogger : FluentMigratorLogger
    {
        private readonly ILogger _logger;

        public MigrationLogger(ILogger logger,
                               FluentMigratorLoggerOptions options)
        : base(options)
        {
            _logger = logger;
        }

        protected override void WriteHeading(string message)
        {
            _logger.LogInformation("*** {0} ***", message);
        }

        protected override void WriteSay(string message)
        {
            _logger.LogDebug(message);
        }

        protected override void WriteEmphasize(string message)
        {
            _logger.LogWarning(message);
        }

        protected override void WriteSql(string sql)
        {
            _logger.LogDebug(sql);
        }

        protected override void WriteEmptySql()
        {
            _logger.LogDebug(@"No SQL statement executed.");
        }

        protected override void WriteElapsedTime(TimeSpan timeSpan)
        {
            _logger.LogDebug("Took: {0}", timeSpan);
        }

        protected override void WriteError(string message)
        {
            _logger.LogError(message);
        }

        protected override void WriteError(Exception exception)
        {
            _logger.LogInformation(exception, "Error");
        }
    }
}
