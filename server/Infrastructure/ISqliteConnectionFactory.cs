using Microsoft.Data.Sqlite;

namespace WorldBeat.Api.Infrastructure
{
    public interface ISqliteConnectionFactory
    {
        SqliteConnection Create();
    }
}