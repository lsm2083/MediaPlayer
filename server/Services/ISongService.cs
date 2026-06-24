using WorldBeat.Api.Contracts;

namespace WorldBeat.Api.Services
{
    public interface ISongService
    {
        Task<List<string>> GetGenresAsync(CancellationToken cancellationToken);
        Task<List<SongResponse>> GetSongsAsync(string genreName, HttpContext httpContext, CancellationToken cancellationToken);
        Task<List<SongResponse>> GetRecentAsync(int limit, HttpContext httpContext, CancellationToken cancellationToken);
        Task<List<SongResponse>> GetTopAsync(int limit, HttpContext httpContext, CancellationToken cancellationToken);
        Task<SongResponse> GetSongAsync(int songId, HttpContext httpContext, CancellationToken cancellationToken);
        Task<SongResponse> UploadSongAsync(IFormFile file, SongCreateCommand command, HttpContext httpContext, CancellationToken cancellationToken);
        Task<bool> UpdateSongAsync(int songId, SongUpdateRequest request, HttpContext httpContext, CancellationToken cancellationToken);
        Task<bool> DeleteSongAsync(int songId, CancellationToken cancellationToken);
        Task<bool> IncrementPlayCountAsync(int songId, CancellationToken cancellationToken);
        Task<SongStreamInfo> OpenSongStreamAsync(int songId, CancellationToken cancellationToken);
        Task<SongStreamInfo> OpenAlbumArtStreamAsync(int songId, CancellationToken cancellationToken);
    }
}