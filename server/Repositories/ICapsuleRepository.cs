using WorldBeat.Api.Models;

namespace WorldBeat.Api.Repositories
{
    public interface ICapsuleRepository
    {
        Task<int> AddAsync(int userId, string title, DateTime openDate, IReadOnlyCollection<int> songIds, CancellationToken cancellationToken);
        Task<CapsuleEntity> GetByIdAsync(int capsuleId, CancellationToken cancellationToken);
        Task<List<CapsuleEntity>> GetByUserAsync(int userId, CancellationToken cancellationToken);
        Task<List<CapsuleEntity>> GetPendingOpenNoticesAsync(int userId, DateTime today, CancellationToken cancellationToken);
        Task<bool> MarkNoticeShownAsync(int capsuleId, CancellationToken cancellationToken);

        //캡슐
        Task<bool> DeleteAsync(int capsuleId, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(int capsuleId, int userId, string title, DateTime openDate, IReadOnlyCollection<int> songIds, CancellationToken cancellationToken);
    }
}