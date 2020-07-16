using System;
using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;

namespace ServarrAPI.Datastore.Migration.Framework
{
    public class MigrationLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;

        public MigrationLoggerProvider(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new MigrationLogger(_logger, new FluentMigratorLoggerOptions() { ShowElapsedTime = true, ShowSql = true });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Nothing to clean up
        }
    }
}
