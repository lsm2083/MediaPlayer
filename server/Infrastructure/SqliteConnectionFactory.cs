using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using WorldBeat.Api.Configuration;

namespace WorldBeat.Api.Infrastructure
{
    public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
    {
        private readonly string _connectionString;

        public SqliteConnectionFactory(IOptions<ApiOptions> options)
        {
            var opt = options.Value;

            string dbPath = opt.DatabasePath;
            if (string.IsNullOrWhiteSpace(dbPath))
            {
                string commonRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "WorldBeat");

                Directory.CreateDirectory(commonRoot);
                dbPath = Path.Combine(commonRoot, "worldbeat.db");
            }

            string dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            _connectionString = $"Data Source={dbPath}";
        }

        public SqliteConnection Create()
        {
            return new SqliteConnection(_connectionString);
        }
    }
}