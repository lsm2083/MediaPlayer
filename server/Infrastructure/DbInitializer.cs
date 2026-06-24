using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using WorldBeat.Api.Models;

namespace WorldBeat.Api.Infrastructure
{
    public sealed class DbInitializer
    {
        private readonly ISqliteConnectionFactory _factory;
        private readonly PasswordHasher<UserEntity> _hasher = new PasswordHasher<UserEntity>();

        public DbInitializer(ISqliteConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS YearNews (
                    NewsId      INTEGER PRIMARY KEY AUTOINCREMENT,
                    Year        INTEGER NOT NULL,
                    Month       INTEGER NOT NULL DEFAULT 0,
                    Headline    TEXT NOT NULL,
                    Description TEXT,
                    Category    TEXT,
                    CreatedAt   DATETIME DEFAULT CURRENT_TIMESTAMP,
                    ImagePath   TEXT DEFAULT '',
                    UNIQUE (Year, Headline)
                );

                CREATE TABLE IF NOT EXISTS Users (
                    UserId          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username        TEXT NOT NULL UNIQUE,
                    Password        TEXT NOT NULL,
                    PasswordVersion INTEGER NOT NULL DEFAULT 1,
                    Name            TEXT,
                    Phone           TEXT,
                    Region          TEXT,
                    City            TEXT,
                    DetailAddr      TEXT,
                    IsPaid          TEXT NOT NULL DEFAULT '미결제',
                    PlanType        TEXT,
                    CreatedAt       DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Genres (
                    GenreId   INTEGER PRIMARY KEY AUTOINCREMENT,
                    GenreName TEXT NOT NULL UNIQUE
                );

                CREATE TABLE IF NOT EXISTS Songs (
                    SongId        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title         TEXT NOT NULL,
                    Artist        TEXT,
                    Album         TEXT,
                    GenreId       INTEGER,
                    Year          INTEGER,
                    Duration      INTEGER,
                    FilePath      TEXT UNIQUE,
                    AlbumArtPath  TEXT,
                    PlayCount     INTEGER DEFAULT 0,
                    LastPlayedAt  DATETIME,
                    AddedAt       DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (GenreId) REFERENCES Genres(GenreId)
                );

                CREATE TABLE IF NOT EXISTS Capsules (
                    CapsuleId   INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId      INTEGER NOT NULL,
                    Title       TEXT NOT NULL,
                    OpenDate    DATETIME NOT NULL,
                    NoticeShown INTEGER NOT NULL DEFAULT 0,
                    CreatedAt   DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId)
                );

                CREATE TABLE IF NOT EXISTS CapsuleSongLinks (
                    CapsuleSongId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CapsuleId     INTEGER NOT NULL,
                    SongId        INTEGER NOT NULL,
                    FOREIGN KEY (CapsuleId) REFERENCES Capsules(CapsuleId),
                    FOREIGN KEY (SongId) REFERENCES Songs(SongId)
                );

                CREATE INDEX IF NOT EXISTS IX_Songs_GenreId ON Songs(GenreId);
                CREATE INDEX IF NOT EXISTS IX_Songs_Title ON Songs(Title);
                CREATE INDEX IF NOT EXISTS IX_Songs_Artist ON Songs(Artist);
                CREATE INDEX IF NOT EXISTS IX_YearNews_Year ON YearNews(Year);
                CREATE INDEX IF NOT EXISTS IX_Capsules_UserId_OpenDate ON Capsules(UserId, OpenDate);
                CREATE INDEX IF NOT EXISTS IX_CapsuleSongLinks_CapsuleId ON CapsuleSongLinks(CapsuleId);
                CREATE UNIQUE INDEX IF NOT EXISTS UX_CapsuleSongLinks_CapsuleId_SongId ON CapsuleSongLinks(CapsuleId, SongId);

                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('전체');
                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('팝송');
                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('발라드');
                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('랩/힙합');
                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('댄스');
                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('R&B');
                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('록');
                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('트로트');
                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('인디');
                INSERT OR IGNORE INTO Genres (GenreName) VALUES ('기타');

                INSERT OR IGNORE INTO YearNews (Year, Headline, Description, Category) VALUES
                (1992, '바르셀로나 올림픽', '스페인 바르셀로나에서 제25회 하계 올림픽이 개최됐습니다.', '스포츠'),
                (1993, '김영삼 대통령 취임', '문민정부 출범, 금융실명제 시행 등 개혁이 추진됐습니다.', '사회'),
                (1994, '성수대교 붕괴', '서울 성수대교가 붕괴하며 32명이 사망한 대형 참사였습니다.', '사회'),
                (1995, '삼풍백화점 붕괴', '서울 삼풍백화점이 무너져 502명이 사망한 최악의 사고였습니다.', '사회'),
                (1996, '아틀란타 올림픽', '미국 아틀란타에서 제26회 하계 올림픽이 개최됐습니다.', '스포츠'),
                (1997, 'IMF 외환위기', '한국이 외환위기로 IMF 구제금융을 신청, 국가 경제가 흔들렸습니다.', '경제'),
                (1998, '김대중 대통령 취임', '국민의 정부 출범과 함께 IMF 위기 극복이 시작됐습니다.', '사회'),
                (1999, 'Y2K 밀레니엄 버그', '세기 전환을 앞두고 컴퓨터 오류 공포가 전 세계를 휩쓸었습니다.', '사회'),
                (2000, '남북정상회담', '김대중-김정일 역사적 첫 남북 정상회담이 평양에서 열렸습니다.', '사회'),
                (2001, '9·11 테러', '미국 뉴욕 세계무역센터가 테러 공격을 받아 3,000여 명이 숨졌습니다.', '국제'),
                (2002, '한일 FIFA 월드컵', '한국이 4강 신화를 이루며 전국이 붉은악마 열기로 가득했습니다.', '스포츠'),
                (2003, '대구 지하철 화재', '대구 지하철 중앙로역 화재로 192명이 사망한 참사였습니다.', '사회'),
                (2004, 'KTX 개통', '경부고속철도 KTX가 개통해 서울-부산 2시간 18분 시대가 열렸습니다.', '사회'),
                (2005, '한류 열풍 본격화', '드라마·K-팝이 아시아를 강타하며 한류가 세계로 뻗어나갔습니다.', '문화'),
                (2006, '이탈리아 FIFA 월드컵', '이탈리아가 독일 월드컵에서 우승했습니다.', '스포츠'),
                (2007, '아이폰 출시', '애플이 첫 아이폰을 발표하며 스마트폰 시대가 열렸습니다.', '기술'),
                (2008, '베이징 올림픽', '중국 베이징에서 제29회 하계 올림픽이 화려하게 개최됐습니다.', '스포츠'),
                (2009, '마이클 잭슨 사망', '팝의 황제 마이클 잭슨이 50세로 세상을 떠났습니다.', '문화'),
                (2010, '남아공 FIFA 월드컵', '아프리카 최초의 월드컵, 스페인이 첫 우승을 차지했습니다.', '스포츠'),
                (2011, '스티브 잡스 사망', '애플 공동 창업자 스티브 잡스가 췌장암으로 사망했습니다.', '기술'),
                (2012, '런던 올림픽', '영국 런던에서 제30회 하계 올림픽이 개최됐습니다.', '스포츠'),
                (2013, '방탄소년단 데뷔', 'BTS가 데뷔하며 K-팝 역사의 새 장을 열기 시작했습니다.', '문화'),
                (2014, '세월호 침몰', '304명의 희생자를 낸 세월호 참사가 온 나라를 슬픔에 빠뜨렸습니다.', '사회'),
                (2015, '메르스 사태', '중동호흡기증후군(MERS)이 한국에서 확산돼 큰 혼란이 빚어졌습니다.', '사회'),
                (2016, '리우 올림픽', '브라질 리우데자네이루에서 제31회 하계 올림픽이 개최됐습니다.', '스포츠'),
                (2017, '촛불혁명·박근혜 탄핵', '대규모 촛불집회 끝에 박근혜 대통령이 헌정 사상 첫 탄핵됐습니다.', '사회'),
                (2018, '평창 동계올림픽', '한국 평창에서 제23회 동계올림픽이 개최됐습니다.', '스포츠'),
                (2019, '일본 수출 규제', '일본의 반도체 소재 수출 규제로 한일 갈등이 격화됐습니다.', '경제'),
                (2020, 'COVID-19 팬데믹', '코로나19 바이러스가 전 세계를 덮쳐 일상이 완전히 바뀌었습니다.', '사회'),
                (2021, '도쿄 올림픽', '코로나19로 1년 연기된 뒤 무관중으로 개최된 올림픽이었습니다.', '스포츠'),
                (2022, '카타르 FIFA 월드컵', '중동 최초의 월드컵, 아르헨티나가 36년 만에 우승했습니다.', '스포츠'),
                (2023, 'K-팝·K-콘텐츠 세계 석권', 'BTS·블랙핑크·뉴진스 등 K-팝이 전 세계 음악 차트를 장악했습니다.', '문화'),
                (2024, '파리 올림픽', '프랑스 파리에서 제33회 하계 올림픽이 개최됐습니다.', '스포츠');
            ";

            await cmd.ExecuteNonQueryAsync(cancellationToken);

            if (!await HasColumnAsync(conn, "Users", "PasswordVersion", cancellationToken))
            {
                var addUserColumn = conn.CreateCommand();
                addUserColumn.CommandText = "ALTER TABLE Users ADD COLUMN PasswordVersion INTEGER NOT NULL DEFAULT 0;";
                await addUserColumn.ExecuteNonQueryAsync(cancellationToken);
            }

            if (!await HasColumnAsync(conn, "Songs", "AlbumArtPath", cancellationToken))
            {
                var addSongColumn = conn.CreateCommand();
                addSongColumn.CommandText = "ALTER TABLE Songs ADD COLUMN AlbumArtPath TEXT;";
                await addSongColumn.ExecuteNonQueryAsync(cancellationToken);
            }

            if (!await HasColumnAsync(conn, "YearNews", "Month", cancellationToken))
            {
                var addNewsColumn = conn.CreateCommand();
                addNewsColumn.CommandText = "ALTER TABLE YearNews ADD COLUMN Month INTEGER NOT NULL DEFAULT 0;";
                await addNewsColumn.ExecuteNonQueryAsync(cancellationToken);
            }

            if (!await HasColumnAsync(conn, "Capsules", "NoticeShown", cancellationToken))
            {
                var addCapsuleColumn = conn.CreateCommand();
                addCapsuleColumn.CommandText = "ALTER TABLE Capsules ADD COLUMN NoticeShown INTEGER NOT NULL DEFAULT 0;";
                await addCapsuleColumn.ExecuteNonQueryAsync(cancellationToken);
            }

            if (!await HasColumnAsync(conn, "Users", "IsPaid", cancellationToken))
            {
                var addIsPaid = conn.CreateCommand();
                addIsPaid.CommandText = "ALTER TABLE Users ADD COLUMN IsPaid TEXT NOT NULL DEFAULT '미결제';";
                await addIsPaid.ExecuteNonQueryAsync(cancellationToken);
            }

            if (!await HasColumnAsync(conn, "Users", "PlanType", cancellationToken))
            {
                var addPlanType = conn.CreateCommand();
                addPlanType.CommandText = "ALTER TABLE Users ADD COLUMN PlanType TEXT;";
                await addPlanType.ExecuteNonQueryAsync(cancellationToken);
            }

            // 기존 DB 에는 LastPlayedAt 컬럼이 없으므로 먼저 컬럼을 추가한다.
            // (초기 CREATE 배치에서 인덱스를 만들면 기존 테이블엔 컬럼이 없어 실패하므로,
            //  인덱스는 컬럼이 보장된 이 시점에서 만든다.)
            if (!await HasColumnAsync(conn, "Songs", "LastPlayedAt", cancellationToken))
            {
                var addLastPlayedAt = conn.CreateCommand();
                addLastPlayedAt.CommandText = "ALTER TABLE Songs ADD COLUMN LastPlayedAt DATETIME;";
                await addLastPlayedAt.ExecuteNonQueryAsync(cancellationToken);
            }

            // 신규/기존 DB 공통으로 인덱스를 보장한다.
            var addLastPlayedIndex = conn.CreateCommand();
            addLastPlayedIndex.CommandText = "CREATE INDEX IF NOT EXISTS IX_Songs_LastPlayedAt ON Songs(LastPlayedAt DESC);";
            await addLastPlayedIndex.ExecuteNonQueryAsync(cancellationToken);

            // ──────────────────────────────────────────────────────
            // [추가] AdminRole 컬럼 마이그레이션
            //
            // 기존에 이미 만들어진 DB 파일에는 AdminRole 컬럼이 없어.
            // 서버가 시작될 때 HasColumnAsync 로 컬럼 존재 여부를 확인하고,
            // 없으면 ALTER TABLE 로 컬럼을 추가해줘.
            //
            // DEFAULT NULL → 기존 유저들은 자동으로 일반 고객(null)으로 처리돼.
            // 관리자로 만들고 싶은 유저는 DB에서 직접 AdminRole = 1 로 바꿔줘.
            // ──────────────────────────────────────────────────────
            if (!await HasColumnAsync(conn, "Users", "AdminRole", cancellationToken))
            {
                var addAdminRole = conn.CreateCommand();
                addAdminRole.CommandText = "ALTER TABLE Users ADD COLUMN AdminRole INTEGER DEFAULT NULL;";
                await addAdminRole.ExecuteNonQueryAsync(cancellationToken);
            }

            // ──────────────────────────────────────────────────────
            // [추가] Songs 테이블에 Lyrics 컬럼 마이그레이션
            //
            // 기존 DB 에는 Lyrics 컬럼이 없으므로 서버 시작 시 자동으로 추가해줘.
            // DEFAULT NULL → 기존 곡들은 가사 없음으로 처리됨.
            // 가사는 Swagger AdminSongs 업로드/수정 폼에서 입력할 수 있어.
            // ──────────────────────────────────────────────────────
            if (!await HasColumnAsync(conn, "Songs", "Lyrics", cancellationToken))
            {
                var addLyrics = conn.CreateCommand();
                addLyrics.CommandText = "ALTER TABLE Songs ADD COLUMN Lyrics TEXT DEFAULT NULL;";
                await addLyrics.ExecuteNonQueryAsync(cancellationToken);
            }

            await MigratePlainPasswordsAsync(conn, cancellationToken);
            await MigrateLegacyCapsuleSongsAsync(conn, cancellationToken);
        }

        private static async Task<bool> HasColumnAsync(SqliteConnection conn, string tableName, string columnName, CancellationToken cancellationToken)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static async Task<bool> TableExistsAsync(SqliteConnection conn, string tableName, CancellationToken cancellationToken)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = $tableName;
            ";
            cmd.Parameters.AddWithValue("$tableName", tableName);

            long count = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0L);
            return count > 0;
        }

        private async Task MigratePlainPasswordsAsync(SqliteConnection conn, CancellationToken cancellationToken)
        {
            var users = new List<UserEntity>();

            var select = conn.CreateCommand();
            select.CommandText = @"
                SELECT UserId, Username, Password, Name, COALESCE(PasswordVersion, 0)
                FROM Users
                WHERE COALESCE(PasswordVersion, 0) = 0
            ";

            await using (var reader = await select.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    users.Add(new UserEntity
                    {
                        UserId = reader.GetInt32(0),
                        Username = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        PasswordHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Name = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        PasswordVersion = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
                    });
                }
            }

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.PasswordHash))
                    continue;

                string plainPassword = user.PasswordHash;
                string hashed = _hasher.HashPassword(user, plainPassword);

                var update = conn.CreateCommand();
                update.CommandText = @"
                    UPDATE Users
                    SET Password = $password,
                        PasswordVersion = 1
                    WHERE UserId = $userId
                ";
                update.Parameters.AddWithValue("$password", hashed);
                update.Parameters.AddWithValue("$userId", user.UserId);

                await update.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private static async Task MigrateLegacyCapsuleSongsAsync(SqliteConnection conn, CancellationToken cancellationToken)
        {
            bool hasLegacyTable = await TableExistsAsync(conn, "CapsuleSongs", cancellationToken);
            if (!hasLegacyTable)
                return;

            var migrate = conn.CreateCommand();
            migrate.CommandText = @"
                INSERT OR IGNORE INTO CapsuleSongLinks (CapsuleId, SongId)
                SELECT
                    cs.CapsuleId,
                    (
                        SELECT s.SongId
                        FROM Songs s
                        WHERE lower(trim(COALESCE(s.Title, ''))) = lower(trim(COALESCE(cs.SongTitle, '')))
                        ORDER BY s.SongId
                        LIMIT 1
                    ) AS SongId
                FROM CapsuleSongs cs
                WHERE trim(COALESCE(cs.SongTitle, '')) <> ''
                  AND EXISTS (
                        SELECT 1
                        FROM Songs s2
                        WHERE lower(trim(COALESCE(s2.Title, ''))) = lower(trim(COALESCE(cs.SongTitle, '')))
                  );
            ";

            await migrate.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
