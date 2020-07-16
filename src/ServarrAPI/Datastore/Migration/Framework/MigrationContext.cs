using System;

namespace ServarrAPI.Datastore.Migration.Framework
{
    public class MigrationContext
    {
        public static MigrationContext Current { get; set; }
        public long? DesiredVersion { get; set; }
        public Action<NzbDroneMigrationBase> BeforeMigration { get; set; }

        public MigrationContext(long? desiredVersion = null)
        {
            DesiredVersion = desiredVersion;
        }
    }
}
