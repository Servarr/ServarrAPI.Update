using System;
using FluentMigrator;
using Microsoft.Extensions.Logging;

namespace ServarrAPI.Datastore.Migration.Framework
{
    public abstract class NzbDroneMigrationBase : FluentMigrator.Migration
    {
        protected readonly ILogger _logger;

        protected NzbDroneMigrationBase(ILogger<NzbDroneMigrationBase> logger)
        {
            _logger = logger;
        }

        protected virtual void MainDbUpgrade()
        {
        }

        protected virtual void LogDbUpgrade()
        {
        }

        public int Version
        {
            get
            {
                var migrationAttribute = (MigrationAttribute)Attribute.GetCustomAttribute(GetType(), typeof(MigrationAttribute));
                return (int)migrationAttribute.Version;
            }
        }

        public override void Up()
        {
            if (MigrationContext.Current.BeforeMigration != null)
            {
                MigrationContext.Current.BeforeMigration(this);
            }

            MainDbUpgrade();
            return;
        }

        public override void Down()
        {
            throw new NotImplementedException();
        }
    }
}
