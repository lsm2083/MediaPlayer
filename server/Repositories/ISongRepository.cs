using WorldBeat.Api.Contracts;
using WorldBeat.Api.Models;

namespace WorldBeat.Api.Repositories
{
    public interface ISongRepository
    {
        Task<List<SongEntity>> GetSongsAsync(string genreName, CancellationToken cancellationToken);
        Task<List<SongEntity>> GetRecentAsync(int limit, CancellationToken cancellationToken);
        Task<List<SongEntity>> GetTopAsync(int limit, CancellationToken cancellationToken);
        Task<SongEntity> GetByIdAsync(int songId, CancellationToken cancellationToken);
        Task<List<string>> GetGenresAsync(CancellationToken cancellationToken);
        Task<bool> ExistsDuplicateAsync(string title, string artist, int duration, CancellationToken cancellationToken);
        Task<int> AddAsync(SongCreateCommand command, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(int songId, SongUpdateRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(int songId, CancellationToken cancellationToken);
        Task<bool> IncrementPlayCountAsync(int songId, CancellationToken cancellationToken);
    }
}