using Microsoft.Data.Sqlite;
using WorldBeat.Api.Contracts;
using WorldBeat.Api.Infrastructure;
using WorldBeat.Api.Models;

namespace WorldBeat.Api.Repositories
{
    public sealed class SongRepository : ISongRepository
    {
        private readonly ISqliteConnectionFactory _factory;

        public SongRepository(ISqliteConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<List<SongEntity>> GetSongsAsync(string genreName, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var songs = new List<SongEntity>();
            var cmd = conn.CreateCommand();

            if (string.IsNullOrWhiteSpace(genreName) || genreName == "전체")
            {
                cmd.CommandText = @"
                    SELECT s.SongId, s.Title, s.Artist, s.Album,
                           COALESCE(s.GenreId, 0),
                           COALESCE(g.GenreName, '기타'),
                           COALESCE(s.Year, 0),
                           COALESCE(s.Duration, 0),
                           COALESCE(s.FilePath, ''),
                           COALESCE(s.AlbumArtPath, ''),
                           COALESCE(s.PlayCount, 0),
                           s.Lyrics
                    FROM Songs s
                    LEFT JOIN Genres g ON s.GenreId = g.GenreId
                    ORDER BY s.AddedAt DESC, s.SongId DESC;
                ";
            }
            else
            {
                cmd.CommandText = @"
                    SELECT s.SongId, s.Title, s.Artist, s.Album,
                           COALESCE(s.GenreId, 0),
                           COALESCE(g.GenreName, '기타'),
                           COALESCE(s.Year, 0),
                           COALESCE(s.Duration, 0),
                           COALESCE(s.FilePath, ''),
                           COALESCE(s.AlbumArtPath, ''),
                           COALESCE(s.PlayCount, 0),
                           s.Lyrics
                    FROM Songs s
                    LEFT JOIN Genres g ON s.GenreId = g.GenreId
                    WHERE g.GenreName = $genre COLLATE NOCASE
                    ORDER BY s.AddedAt DESC, s.SongId DESC;
                ";
                cmd.Parameters.AddWithValue("$genre", genreName);
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                songs.Add(MapSong(reader));

            return songs;
        }

        public async Task<List<SongEntity>> GetRecentAsync(int limit, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var songs = new List<SongEntity>();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.SongId, s.Title, s.Artist, s.Album,
                       COALESCE(s.GenreId, 0),
                       COALESCE(g.GenreName, '기타'),
                       COALESCE(s.Year, 0),
                       COALESCE(s.Duration, 0),
                       COALESCE(s.FilePath, ''),
                       COALESCE(s.AlbumArtPath, ''),
                       COALESCE(s.PlayCount, 0),
                       s.Lyrics
                FROM Songs s
                LEFT JOIN Genres g ON s.GenreId = g.GenreId
                WHERE COALESCE(s.PlayCount, 0) > 0
                ORDER BY s.LastPlayedAt DESC, s.SongId DESC
                LIMIT $limit;
            ";
            cmd.Parameters.AddWithValue("$limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                songs.Add(MapSong(reader));

            return songs;
        }

        public async Task<List<SongEntity>> GetTopAsync(int limit, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var songs = new List<SongEntity>();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.SongId, s.Title, s.Artist, s.Album,
                       COALESCE(s.GenreId, 0),
                       COALESCE(g.GenreName, '기타'),
                       COALESCE(s.Year, 0),
                       COALESCE(s.Duration, 0),
                       COALESCE(s.FilePath, ''),
                       COALESCE(s.AlbumArtPath, ''),
                       COALESCE(s.PlayCount, 0),
                       s.Lyrics
                FROM Songs s
                LEFT JOIN Genres g ON s.GenreId = g.GenreId
                WHERE COALESCE(s.PlayCount, 0) > 0
                ORDER BY s.PlayCount DESC, s.SongId DESC
                LIMIT $limit;
            ";
            cmd.Parameters.AddWithValue("$limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                songs.Add(MapSong(reader));

            return songs;
        }

        public async Task<SongEntity> GetByIdAsync(int songId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT s.SongId, s.Title, s.Artist, s.Album,
                       COALESCE(s.GenreId, 0),
                       COALESCE(g.GenreName, '기타'),
                       COALESCE(s.Year, 0),
                       COALESCE(s.Duration, 0),
                       COALESCE(s.FilePath, ''),
                       COALESCE(s.AlbumArtPath, ''),
                       COALESCE(s.PlayCount, 0),
                       s.Lyrics
                FROM Songs s
                LEFT JOIN Genres g ON s.GenreId = g.GenreId
                WHERE s.SongId = $songId
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("$songId", songId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return MapSong(reader);
        }

        public async Task<List<string>> GetGenresAsync(CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var list = new List<string>();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT '전체'
                UNION
                SELECT DISTINCT g.GenreName
                FROM Songs s
                JOIN Genres g ON s.GenreId = g.GenreId
                ORDER BY 1;
            ";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                list.Add(reader.GetString(0));

            return list;
        }

        public async Task<bool> ExistsDuplicateAsync(string title, string artist, int duration, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(title))
                return false;

            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*)
                FROM Songs
                WHERE lower(trim(COALESCE(Title, ''))) = lower(trim($title))
                  AND (
                        $artist = ''
                        OR lower(trim(COALESCE(Artist, ''))) = lower(trim($artist))
                  )
                  AND (
                        $duration <= 0
                        OR COALESCE(Duration, 0) = $duration
                  );
            ";
            cmd.Parameters.AddWithValue("$title", title.Trim());
            cmd.Parameters.AddWithValue("$artist", (artist ?? "").Trim());
            cmd.Parameters.AddWithValue("$duration", duration);

            long count = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0L);
            return count > 0;
        }

        public async Task<int> AddAsync(SongCreateCommand command, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            int genreId = await GetOrCreateGenreIdAsync(conn, command.Genre, cancellationToken);

            var cmd = conn.CreateCommand();

            // [수정] INSERT 에 Lyrics 컬럼 추가
            cmd.CommandText = @"
                INSERT INTO Songs
                    (Title, Artist, Album, GenreId, Year, Duration, FilePath, AlbumArtPath, Lyrics)
                VALUES
                    ($title, $artist, $album, $genreId, $year, $duration, $filePath, $albumArtPath, $lyrics);

                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("$title", command.Title ?? "");
            cmd.Parameters.AddWithValue("$artist", command.Artist ?? "");
            cmd.Parameters.AddWithValue("$album", command.Album ?? "");
            cmd.Parameters.AddWithValue("$genreId", genreId);
            cmd.Parameters.AddWithValue("$year", command.Year);
            cmd.Parameters.AddWithValue("$duration", command.Duration);
            cmd.Parameters.AddWithValue("$filePath", command.FilePath ?? "");
            cmd.Parameters.AddWithValue("$albumArtPath", command.AlbumArtPath ?? "");

            // Lyrics 가 null 이면 DB 에 NULL 로 저장
            if (string.IsNullOrWhiteSpace(command.Lyrics))
                cmd.Parameters.AddWithValue("$lyrics", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("$lyrics", command.Lyrics);

            object id = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(id);
        }

        public async Task<bool> UpdateAsync(int songId, SongUpdateRequest request, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            int genreId = await GetOrCreateGenreIdAsync(conn, request.Genre, cancellationToken);

            var cmd = conn.CreateCommand();

            // [수정] UPDATE 에 Lyrics 컬럼 추가
            cmd.CommandText = @"
                UPDATE Songs
                SET Title    = $title,
                    Artist   = $artist,
                    Album    = $album,
                    GenreId  = $genreId,
                    Year     = $year,
                    Duration = $duration,
                    Lyrics   = $lyrics
                WHERE SongId = $songId;
            ";
            cmd.Parameters.AddWithValue("$title", request.Title ?? "");
            cmd.Parameters.AddWithValue("$artist", request.Artist ?? "");
            cmd.Parameters.AddWithValue("$album", request.Album ?? "");
            cmd.Parameters.AddWithValue("$genreId", genreId);
            cmd.Parameters.AddWithValue("$year", request.Year);
            cmd.Parameters.AddWithValue("$duration", request.Duration);
            cmd.Parameters.AddWithValue("$songId", songId);

            // Lyrics 가 null 이면 DB 에 NULL 로 저장
            if (string.IsNullOrWhiteSpace(request.Lyrics))
                cmd.Parameters.AddWithValue("$lyrics", DBNull.Value);
            else
                cmd.Parameters.AddWithValue("$lyrics", request.Lyrics);

            int affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int songId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Songs WHERE SongId = $songId;";
            cmd.Parameters.AddWithValue("$songId", songId);

            int affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }

        public async Task<bool> IncrementPlayCountAsync(int songId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Songs
                SET PlayCount    = COALESCE(PlayCount, 0) + 1,
                    LastPlayedAt = CURRENT_TIMESTAMP
                WHERE SongId = $songId;
            ";
            cmd.Parameters.AddWithValue("$songId", songId);

            int affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }

        // ──────────────────────────────────────────────────────
        // [수정] MapSong: 인덱스 11번에 Lyrics 추가
        //
        // 컬럼 순서:
        //   0: SongId
        //   1: Title
        //   2: Artist
        //   3: Album
        //   4: GenreId
        //   5: Genre
        //   6: Year
        //   7: Duration
        //   8: FilePath
        //   9: AlbumArtPath
        //  10: PlayCount
        //  11: Lyrics  ← 추가
        // ──────────────────────────────────────────────────────
        private static SongEntity MapSong(SqliteDataReader reader)
        {
            return new SongEntity
            {
                SongId       = reader.GetInt32(0),
                Title        = reader.IsDBNull(1)  ? "" : reader.GetString(1),
                Artist       = reader.IsDBNull(2)  ? "" : reader.GetString(2),
                Album        = reader.IsDBNull(3)  ? "" : reader.GetString(3),
                GenreId      = reader.IsDBNull(4)  ? 0  : reader.GetInt32(4),
                Genre        = reader.IsDBNull(5)  ? "기타" : reader.GetString(5),
                Year         = reader.IsDBNull(6)  ? 0  : reader.GetInt32(6),
                Duration     = reader.IsDBNull(7)  ? 0  : reader.GetInt32(7),
                FilePath     = reader.IsDBNull(8)  ? "" : reader.GetString(8),
                AlbumArtPath = reader.IsDBNull(9)  ? "" : reader.GetString(9),
                PlayCount    = reader.IsDBNull(10) ? 0  : reader.GetInt32(10),
                Lyrics       = reader.IsDBNull(11) ? null : reader.GetString(11) // [추가]
            };
        }

        private static async Task<int> GetOrCreateGenreIdAsync(SqliteConnection conn, string genreName, CancellationToken cancellationToken)
        {
            string normalized = string.IsNullOrWhiteSpace(genreName) ? "기타" : genreName.Trim();

            var select = conn.CreateCommand();
            select.CommandText = "SELECT GenreId FROM Genres WHERE GenreName = $genre COLLATE NOCASE LIMIT 1;";
            select.Parameters.AddWithValue("$genre", normalized);

            object result = await select.ExecuteScalarAsync(cancellationToken);
            if (result != null && result != DBNull.Value)
                return Convert.ToInt32(result);

            var insert = conn.CreateCommand();
            insert.CommandText = @"
                INSERT INTO Genres (GenreName) VALUES ($genre);
                SELECT last_insert_rowid();
            ";
            insert.Parameters.AddWithValue("$genre", normalized);

            object newId = await insert.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(newId);
        }
    }
}
