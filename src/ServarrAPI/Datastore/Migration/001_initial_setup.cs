using FluentMigrator;
using Microsoft.Extensions.Logging;
using ServarrAPI.Datastore.Migration.Framework;

namespace ServarrAPI.Datastore.Migration
{
    [Migration(1)]
    public class InitialSetup : NzbDroneMigrationBase
    {
        public InitialSetup(ILogger<InitialSetup> logger)
            : base(logger)
        {
        }

        protected override void MainDbUpgrade()
        {
            Create.Table("update")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("version").AsString().Indexed()
                .WithColumn("releasedate").AsDateTime().Indexed()
                .WithColumn("branch").AsString().Indexed()
                .WithColumn("new").AsString()
                .WithColumn("fixed").AsString();

            Create.Table("updatefile")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("updateid").AsInt32().Indexed()
                .WithColumn("operatingsystem").AsInt32()
                .WithColumn("runtime").AsInt32()
                .WithColumn("architecture").AsInt32()
                .WithColumn("filename").AsString()
                .WithColumn("url").AsString()
                .WithColumn("hash").AsString();
        }
    }
}
