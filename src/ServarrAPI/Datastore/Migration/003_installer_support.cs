using FluentMigrator;
using Microsoft.Extensions.Logging;
using ServarrAPI.Datastore.Migration.Framework;

namespace ServarrAPI.Datastore.Migration
{
    [Migration(3)]
    public class InstallerSupport : NzbDroneMigrationBase
    {
        public InstallerSupport(ILogger<InitialSetup> logger)
            : base(logger)
        {
        }

        protected override void MainDbUpgrade()
        {
            Create.Column("installer").OnTable("updatefile").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
