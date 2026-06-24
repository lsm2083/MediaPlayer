using WorldBeat.Api.Contracts;
using WorldBeat.Api.Models;

namespace WorldBeat.Api.Repositories
{
    public interface IYearNewsRepository
    {
        Task<List<YearNewsEntity>> GetByYearAsync(int year, CancellationToken cancellationToken);
        Task<YearNewsEntity?> GetByIdAsync(int newsId, CancellationToken cancellationToken);
        Task<int> AddAsync(YearNewsCreateRequest request, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(int newsId, YearNewsUpdateRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(int newsId, CancellationToken cancellationToken);
        Task<bool> UpdateImagePathAsync(int newsId, string imagePath, CancellationToken cancellationToken);
    }
}
