using WorldBeat.Api.Models;

namespace WorldBeat.Api.Repositories
{
    public interface IUserRepository
    {
        Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken);
        Task<int> CreateAsync(UserEntity user, CancellationToken cancellationToken);
        Task<UserEntity> GetByUsernameAsync(string username, CancellationToken cancellationToken);
        Task<UserEntity> GetByIdAsync(int userId, CancellationToken cancellationToken);
        Task UpdatePasswordHashAsync(int userId, string passwordHash, CancellationToken cancellationToken);
        Task UpdatePaymentAsync(int userId, string planType, CancellationToken cancellationToken);

        // [추가] 전체 유저 목록 조회 (AdminManager 에서 유저 목록 표시용)
        Task<List<UserEntity>> GetAllAsync(CancellationToken cancellationToken);

        // [추가] 관리자 권한 변경 (1 = 관리자, null = 일반 고객)
        Task<bool> UpdateAdminRoleAsync(int userId, int? adminRole, CancellationToken cancellationToken);
    }
}
