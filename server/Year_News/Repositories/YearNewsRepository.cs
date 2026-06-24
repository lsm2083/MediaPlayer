using Microsoft.Data.Sqlite;
using WorldBeat.Api.Contracts;
using WorldBeat.Api.Infrastructure;
using WorldBeat.Api.Models;

namespace WorldBeat.Api.Repositories
{
    public sealed class YearNewsRepository : IYearNewsRepository
    {
        private readonly ISqliteConnectionFactory _factory;

        public YearNewsRepository(ISqliteConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<YearNewsEntity>> GetByYearAsync(int year, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT NewsId, Year, Month, Headline, Description, Category, ImagePath, CreatedAt
                FROM YearNews
                WHERE Year = $year
                ORDER BY NewsId ASC;
            ";
            cmd.Parameters.AddWithValue("$year", year);

            var result = new List<YearNewsEntity>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                result.Add(MapNews(reader));

            return result;
        }

        public async Task<YearNewsEntity?> GetByIdAsync(int newsId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT NewsId, Year, Month, Headline, Description, Category, ImagePath, CreatedAt
                FROM YearNews
                WHERE NewsId = $newsId
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("$newsId", newsId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return MapNews(reader);
        }

        public async Task<int> AddAsync(YearNewsCreateRequest request, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO YearNews (Year, Month, Headline, Description, Category)
                VALUES ($year, $month, $headline, $description, $category);
                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("$year", request.Year);
            cmd.Parameters.AddWithValue("$month", request.Month);
            cmd.Parameters.AddWithValue("$headline", request.Headline ?? "");
            cmd.Parameters.AddWithValue("$description", request.Description ?? "");
            cmd.Parameters.AddWithValue("$category", request.Category ?? "");

            object id = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(id);
        }

        public async Task<bool> UpdateAsync(int newsId, YearNewsUpdateRequest request, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE YearNews
                SET Month       = $month,
                    Headline    = $headline,
                    Description = $description,
                    Category    = $category
                WHERE NewsId = $newsId;
            ";
            cmd.Parameters.AddWithValue("$month", request.Month);
            cmd.Parameters.AddWithValue("$headline", request.Headline ?? "");
            cmd.Parameters.AddWithValue("$description", request.Description ?? "");
            cmd.Parameters.AddWithValue("$category", request.Category ?? "");
            cmd.Parameters.AddWithValue("$newsId", newsId);

            int affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int newsId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM YearNews WHERE NewsId = $newsId;";
            cmd.Parameters.AddWithValue("$newsId", newsId);

            int affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }

        public async Task<bool> UpdateImagePathAsync(int newsId, string imagePath, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE YearNews
                SET ImagePath = $imagePath
                WHERE NewsId = $newsId;
            ";
            cmd.Parameters.AddWithValue("$imagePath", imagePath ?? "");
            cmd.Parameters.AddWithValue("$newsId", newsId);

            int affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }

        private static YearNewsEntity MapNews(SqliteDataReader reader)
        {
            return new YearNewsEntity
            {
                NewsId      = reader.GetInt32(0),
                Year        = reader.GetInt32(1),
                Month       = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),   // [추가] Month
                Headline    = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Category    = reader.IsDBNull(5) ? "" : reader.GetString(5),
                ImagePath   = reader.IsDBNull(6) ? "" : reader.GetString(6),
                CreatedAt   = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7)
            };
        }
    }
}
