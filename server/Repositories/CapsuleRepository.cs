using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using WorldBeat.Api.Infrastructure;
using WorldBeat.Api.Models;

namespace WorldBeat.Api.Repositories
{
    public sealed class CapsuleRepository : ICapsuleRepository
    {
        private readonly ISqliteConnectionFactory _factory;

        public CapsuleRepository(ISqliteConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<int> AddAsync(int userId, string title, DateTime openDate, IReadOnlyCollection<int> songIds, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            await using var dbTx = await conn.BeginTransactionAsync(cancellationToken);
            var tx = (SqliteTransaction)dbTx;

            var insertCapsule = conn.CreateCommand();
            insertCapsule.Transaction = tx;
            insertCapsule.CommandText = @"
                INSERT INTO Capsules (UserId, Title, OpenDate, NoticeShown)
                VALUES ($userId, $title, $openDate, 0);
                SELECT last_insert_rowid();
            ";
            insertCapsule.Parameters.AddWithValue("$userId", userId);
            insertCapsule.Parameters.AddWithValue("$title", title ?? "");
            insertCapsule.Parameters.AddWithValue("$openDate", openDate.Date);

            object result = await insertCapsule.ExecuteScalarAsync(cancellationToken);
            int capsuleId = Convert.ToInt32(result);

            foreach (int songId in songIds)
            {
                var insertSong = conn.CreateCommand();
                insertSong.Transaction = tx;
                insertSong.CommandText = @"
                    INSERT INTO CapsuleSongLinks (CapsuleId, SongId)
                    VALUES ($capsuleId, $songId);
                ";
                insertSong.Parameters.AddWithValue("$capsuleId", capsuleId);
                insertSong.Parameters.AddWithValue("$songId", songId);

                await insertSong.ExecuteNonQueryAsync(cancellationToken);
            }

            await dbTx.CommitAsync(cancellationToken);
            return capsuleId;
        }

        public async Task<CapsuleEntity> GetByIdAsync(int capsuleId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT c.CapsuleId,
                       c.UserId,
                       c.Title,
                       c.OpenDate,
                       COALESCE(c.NoticeShown, 0),
                       c.CreatedAt,
                       s.SongId,
                       s.Title,
                       s.Artist,
                       s.Album,
                       COALESCE(s.GenreId, 0),
                       COALESCE(g.GenreName, '기타'),
                       COALESCE(s.Year, 0),
                       COALESCE(s.Duration, 0),
                       COALESCE(s.FilePath, ''),
                       COALESCE(s.AlbumArtPath, ''),
                       COALESCE(s.PlayCount, 0)
                FROM Capsules c
                LEFT JOIN CapsuleSongLinks l ON c.CapsuleId = l.CapsuleId
                LEFT JOIN Songs s ON l.SongId = s.SongId
                LEFT JOIN Genres g ON s.GenreId = g.GenreId
                WHERE c.CapsuleId = $capsuleId
                ORDER BY l.CapsuleSongId ASC;
            ";
            cmd.Parameters.AddWithValue("$capsuleId", capsuleId);

            var list = await ReadCapsulesAsync(cmd, cancellationToken);
            return list.FirstOrDefault();
        }

        public async Task<List<CapsuleEntity>> GetByUserAsync(int userId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT c.CapsuleId,
                       c.UserId,
                       c.Title,
                       c.OpenDate,
                       COALESCE(c.NoticeShown, 0),
                       c.CreatedAt,
                       s.SongId,
                       s.Title,
                       s.Artist,
                       s.Album,
                       COALESCE(s.GenreId, 0),
                       COALESCE(g.GenreName, '기타'),
                       COALESCE(s.Year, 0),
                       COALESCE(s.Duration, 0),
                       COALESCE(s.FilePath, ''),
                       COALESCE(s.AlbumArtPath, ''),
                       COALESCE(s.PlayCount, 0)
                FROM Capsules c
                LEFT JOIN CapsuleSongLinks l ON c.CapsuleId = l.CapsuleId
                LEFT JOIN Songs s ON l.SongId = s.SongId
                LEFT JOIN Genres g ON s.GenreId = g.GenreId
                WHERE c.UserId = $userId
                ORDER BY c.OpenDate ASC, c.CapsuleId ASC, l.CapsuleSongId ASC;
            ";
            cmd.Parameters.AddWithValue("$userId", userId);

            return await ReadCapsulesAsync(cmd, cancellationToken);
        }

        public async Task<List<CapsuleEntity>> GetPendingOpenNoticesAsync(int userId, DateTime today, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT c.CapsuleId,
                       c.UserId,
                       c.Title,
                       c.OpenDate,
                       COALESCE(c.NoticeShown, 0),
                       c.CreatedAt,
                       s.SongId,
                       s.Title,
                       s.Artist,
                       s.Album,
                       COALESCE(s.GenreId, 0),
                       COALESCE(g.GenreName, '기타'),
                       COALESCE(s.Year, 0),
                       COALESCE(s.Duration, 0),
                       COALESCE(s.FilePath, ''),
                       COALESCE(s.AlbumArtPath, ''),
                       COALESCE(s.PlayCount, 0)
                FROM Capsules c
                LEFT JOIN CapsuleSongLinks l ON c.CapsuleId = l.CapsuleId
                LEFT JOIN Songs s ON l.SongId = s.SongId
                LEFT JOIN Genres g ON s.GenreId = g.GenreId
                WHERE c.UserId = $userId
                  AND date(c.OpenDate) <= date($today)
                  AND COALESCE(c.NoticeShown, 0) = 0
                ORDER BY c.OpenDate ASC, c.CapsuleId ASC, l.CapsuleSongId ASC;
            ";
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$today", today.Date);

            return await ReadCapsulesAsync(cmd, cancellationToken);
        }

        public async Task<bool> MarkNoticeShownAsync(int capsuleId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Capsules
                SET NoticeShown = 1
                WHERE CapsuleId = $capsuleId;
            ";
            cmd.Parameters.AddWithValue("$capsuleId", capsuleId);

            int affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }

        public async Task<bool> UpdateAsync(int capsuleId, int userId, string title, DateTime openDate, IReadOnlyCollection<int> songIds, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            await using var dbTx = await conn.BeginTransactionAsync(cancellationToken);
            var tx = (SqliteTransaction)dbTx;

            var updateCapsule = conn.CreateCommand();
            updateCapsule.Transaction = tx;
            updateCapsule.CommandText = @"
                UPDATE Capsules
                SET Title = $title,
                    OpenDate = $openDate
                WHERE CapsuleId = $capsuleId
                  AND UserId = $userId;
            ";
            updateCapsule.Parameters.AddWithValue("$capsuleId", capsuleId);
            updateCapsule.Parameters.AddWithValue("$userId", userId);
            updateCapsule.Parameters.AddWithValue("$title", title ?? "");
            updateCapsule.Parameters.AddWithValue("$openDate", openDate.Date);

            int updated = await updateCapsule.ExecuteNonQueryAsync(cancellationToken);
            if (updated <= 0)
            {
                await dbTx.RollbackAsync(cancellationToken);
                return false;
            }

            var deleteLinks = conn.CreateCommand();
            deleteLinks.Transaction = tx;
            deleteLinks.CommandText = @"
                DELETE FROM CapsuleSongLinks
                WHERE CapsuleId = $capsuleId;
            ";
            deleteLinks.Parameters.AddWithValue("$capsuleId", capsuleId);
            await deleteLinks.ExecuteNonQueryAsync(cancellationToken);

            foreach (int songId in songIds)
            {
                var insertSong = conn.CreateCommand();
                insertSong.Transaction = tx;
                insertSong.CommandText = @"
                    INSERT INTO CapsuleSongLinks (CapsuleId, SongId)
                    VALUES ($capsuleId, $songId);
                ";
                insertSong.Parameters.AddWithValue("$capsuleId", capsuleId);
                insertSong.Parameters.AddWithValue("$songId", songId);

                await insertSong.ExecuteNonQueryAsync(cancellationToken);
            }

            await dbTx.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(int capsuleId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            await using var dbTx = await conn.BeginTransactionAsync(cancellationToken);
            var tx = (SqliteTransaction)dbTx;

            var deleteLinks = conn.CreateCommand();
            deleteLinks.Transaction = tx;
            deleteLinks.CommandText = @"
                DELETE FROM CapsuleSongLinks
                WHERE CapsuleId = $capsuleId;
            ";
            deleteLinks.Parameters.AddWithValue("$capsuleId", capsuleId);
            await deleteLinks.ExecuteNonQueryAsync(cancellationToken);

            var deleteCapsule = conn.CreateCommand();
            deleteCapsule.Transaction = tx;
            deleteCapsule.CommandText = @"
                DELETE FROM Capsules
                WHERE CapsuleId = $capsuleId;
            ";
            deleteCapsule.Parameters.AddWithValue("$capsuleId", capsuleId);

            int deleted = await deleteCapsule.ExecuteNonQueryAsync(cancellationToken);

            await dbTx.CommitAsync(cancellationToken);
            return deleted > 0;
        }

        private static async Task<List<CapsuleEntity>> ReadCapsulesAsync(SqliteCommand cmd, CancellationToken cancellationToken)
        {
            var map = new Dictionary<int, CapsuleEntity>();

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                int capsuleId = reader.GetInt32(0);

                if (!map.TryGetValue(capsuleId, out var capsule))
                {
                    capsule = new CapsuleEntity
                    {
                        CapsuleId = capsuleId,
                        UserId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        Title = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        OpenDate = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                        NoticeShown = !reader.IsDBNull(4) && reader.GetInt32(4) == 1,
                        CreatedAt = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5)
                    };

                    map[capsuleId] = capsule;
                }

                if (!reader.IsDBNull(6))
                {
                    capsule.Songs.Add(new SongEntity
                    {
                        SongId = reader.GetInt32(6),
                        Title = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        Artist = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        Album = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        GenreId = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                        Genre = reader.IsDBNull(11) ? "기타" : reader.GetString(11),
                        Year = reader.IsDBNull(12) ? 0 : reader.GetInt32(12),
                        Duration = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                        FilePath = reader.IsDBNull(14) ? "" : reader.GetString(14),
                        AlbumArtPath = reader.IsDBNull(15) ? "" : reader.GetString(15),
                        PlayCount = reader.IsDBNull(16) ? 0 : reader.GetInt32(16)
                    });
                }
            }

            return map.Values.ToList();
        }
    }
}