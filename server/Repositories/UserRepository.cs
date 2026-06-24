using Microsoft.Data.Sqlite;
using WorldBeat.Api.Infrastructure;
using WorldBeat.Api.Models;

namespace WorldBeat.Api.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly ISqliteConnectionFactory _factory;

        public UserRepository(ISqliteConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = $username;";
            cmd.Parameters.AddWithValue("$username", username);

            long count = (long)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0L);
            return count > 0;
        }

        public async Task<int> CreateAsync(UserEntity user, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();

            // 신규 회원가입은 AdminRole 을 지정하지 않아 → DB 기본값(NULL = 일반 고객) 으로 저장됨
            cmd.CommandText = @"
                INSERT INTO Users
                    (Username, Password, PasswordVersion, Name, Phone, Region, City, DetailAddr, IsPaid, PlanType)
                VALUES
                    ($username, $password, $passwordVersion, $name, $phone, $region, $city, $detailAddr, '미결제', NULL);

                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("$username", user.Username ?? "");
            cmd.Parameters.AddWithValue("$password", user.PasswordHash ?? "");
            cmd.Parameters.AddWithValue("$passwordVersion", 1);
            cmd.Parameters.AddWithValue("$name", user.Name ?? "");
            cmd.Parameters.AddWithValue("$phone", user.Phone ?? "");
            cmd.Parameters.AddWithValue("$region", user.Region ?? "");
            cmd.Parameters.AddWithValue("$city", user.City ?? "");
            cmd.Parameters.AddWithValue("$detailAddr", user.DetailAddr ?? "");

            object id = await cmd.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(id);
        }

        public async Task<UserEntity> GetByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();

            // ──────────────────────────────────────────────────────
            // [수정] SELECT 쿼리에 AdminRole, PlanType 컬럼 추가
            //
            // 기존에는 AdminRole 만 읽고 있었고
            // PlanType 을 SELECT 하지 않아서 로그인 후 윈폼에서
            // 현재 사용자의 요금제 상태를 알 수 없었어.
            //
            // 컬럼 순서 (reader.GetXxx(인덱스) 와 맞춰야 해):
            //   0: UserId
            //   1: Username
            //   2: Password
            //   3: PasswordVersion
            //   4: Name
            //   5: Phone
            //   6: Region
            //   7: City
            //   8: DetailAddr
            //   9: AdminRole
            //  10: PlanType  ← 추가
            // ──────────────────────────────────────────────────────
            cmd.CommandText = @"
                SELECT UserId, Username, Password, COALESCE(PasswordVersion, 1), Name, Phone, Region, City, DetailAddr, AdminRole, PlanType
                FROM Users
                WHERE Username = $username
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("$username", username);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return new UserEntity
            {
                UserId = reader.GetInt32(0),
                Username = reader.IsDBNull(1) ? "" : reader.GetString(1),
                PasswordHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                PasswordVersion = reader.IsDBNull(3) ? 1 : reader.GetInt32(3),
                Name = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Phone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Region = reader.IsDBNull(6) ? "" : reader.GetString(6),
                City = reader.IsDBNull(7) ? "" : reader.GetString(7),
                DetailAddr = reader.IsDBNull(8) ? "" : reader.GetString(8),

                // [추가] 인덱스 9번: AdminRole (NULL 이면 null, 값이 있으면 int 로 읽음)
                AdminRole = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),

                // [추가] 인덱스 10번: PlanType (NULL 이면 빈 문자열 처리)
                PlanType = reader.IsDBNull(10) ? "" : reader.GetString(10)
            };
        }

        public async Task UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Users
                SET Password = $password,
                    PasswordVersion = 1
                WHERE UserId = $userId;
            ";
            cmd.Parameters.AddWithValue("$password", passwordHash);
            cmd.Parameters.AddWithValue("$userId", userId);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task UpdatePaymentAsync(int userId, string planType, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Users
                SET IsPaid = '결제',
                    PlanType = $planType
                WHERE UserId = $userId;
            ";
            cmd.Parameters.AddWithValue("$planType", planType);
            cmd.Parameters.AddWithValue("$userId", userId);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // ──────────────────────────────────────────────────────
        // [추가] 전체 유저 목록 조회
        // AdminManager 에서 유저 목록 + 관리자 여부 표시에 사용
        // ──────────────────────────────────────────────────────
        public async Task<List<UserEntity>> GetAllAsync(CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT UserId, Username, Password, COALESCE(PasswordVersion, 1), Name, Phone, Region, City, DetailAddr, AdminRole, PlanType
                FROM Users
                ORDER BY UserId;
            ";

            var list = new List<UserEntity>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new UserEntity
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    PasswordHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    PasswordVersion = reader.IsDBNull(3) ? 1 : reader.GetInt32(3),
                    Name = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Phone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Region = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    City = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    DetailAddr = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    AdminRole = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                    PlanType = reader.IsDBNull(10) ? "" : reader.GetString(10)
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────────────
        // [추가] 관리자 권한 변경
        // adminRole: 1 = 관리자로 지정, null = 일반 고객으로 해제
        // 반환값: true = 변경 성공, false = 해당 userId 없음
        // ──────────────────────────────────────────────────────
        public async Task<bool> UpdateAdminRoleAsync(int userId, int? adminRole, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Users
                SET AdminRole = $adminRole
                WHERE UserId = $userId;
            ";

            // adminRole이 null이면 DB에 NULL 로 저장 (일반 고객으로 해제)
            // adminRole이 1이면 DB에 1 로 저장 (관리자로 지정)
            if (adminRole.HasValue)
                cmd.Parameters.AddWithValue("$adminRole", adminRole.Value);
            else
                cmd.Parameters.AddWithValue("$adminRole", DBNull.Value);

            cmd.Parameters.AddWithValue("$userId", userId);

            int affected = await cmd.ExecuteNonQueryAsync(cancellationToken);

            // 변경된 행이 0이면 해당 userId 가 없는 것
            return affected > 0;
        }

        // ──────────────────────────────────────────────────────
        // [추가] userId 로 유저 단건 조회
        // Program.cs 에서 관리자 지정 전 유저 존재 여부 확인에 사용
        // ──────────────────────────────────────────────────────
        public async Task<UserEntity> GetByIdAsync(int userId, CancellationToken cancellationToken)
        {
            await using var conn = _factory.Create();
            await conn.OpenAsync(cancellationToken);

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT UserId, Username, Password, COALESCE(PasswordVersion, 1), Name, Phone, Region, City, DetailAddr, AdminRole, PlanType
                FROM Users
                WHERE UserId = $userId
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("$userId", userId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            return new UserEntity
            {
                UserId = reader.GetInt32(0),
                Username = reader.IsDBNull(1) ? "" : reader.GetString(1),
                PasswordHash = reader.IsDBNull(2) ? "" : reader.GetString(2),
                PasswordVersion = reader.IsDBNull(3) ? 1 : reader.GetInt32(3),
                Name = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Phone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Region = reader.IsDBNull(6) ? "" : reader.GetString(6),
                City = reader.IsDBNull(7) ? "" : reader.GetString(7),
                DetailAddr = reader.IsDBNull(8) ? "" : reader.GetString(8),
                AdminRole = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                PlanType = reader.IsDBNull(10) ? "" : reader.GetString(10)
            };
        }
    }
}