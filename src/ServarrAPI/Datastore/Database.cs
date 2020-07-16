using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using ServarrAPI.Datastore.Migration.Framework;

namespace ServarrAPI.Datastore
{
    public interface IDatabase
    {
        Task<IDbConnection> OpenConnection();
        Task<int> MigrationVersion();
    }

    public class Database : IDatabase
    {
        private readonly string _connectionString;

        private readonly ILogger _logger;

        public Database(IConfiguration config,
                        IMigrationController migrationController,
                        ILogger<Database> logger)
        {
            _connectionString = config.GetConnectionString("Database");
            _logger = logger;

            var context = new MigrationContext();
            migrationController.Migrate(_connectionString, context);

            TableMapping.Map();
        }

        public async Task<IDbConnection> OpenConnection()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }

        public async Task<int> MigrationVersion()
        {
            using var db = await OpenConnection();
            return await db.QueryFirstOrDefaultAsync<int>("SELECT version from VersionInfo ORDER BY version DESC LIMIT 1");
        }
    }
}
