namespace WorldBeat.Api.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveSongAsync(IFormFile file, CancellationToken cancellationToken);
        Task<string> SaveAlbumArtAsync(byte[] bytes, string extension, CancellationToken cancellationToken);
        Task DeleteSongAsync(string storedFileName, CancellationToken cancellationToken);
        Task DeleteAlbumArtAsync(string storedFileName, CancellationToken cancellationToken);
        Task<Stream> OpenSongReadAsync(string storedFileName, CancellationToken cancellationToken);
        Task<Stream> OpenAlbumArtReadAsync(string storedFileName, CancellationToken cancellationToken);
        string GetSongFullPath(string storedFileName);
        string GetSongContentType(string storedFileName);
        string GetAlbumArtContentType(string storedFileName);

        // [추가] 뉴스 이미지 저장/삭제/읽기
        Task<string> SaveNewsImageAsync(IFormFile file, CancellationToken cancellationToken);
        Task DeleteNewsImageAsync(string storedFileName, CancellationToken cancellationToken);
        Task<Stream> OpenNewsImageReadAsync(string storedFileName, CancellationToken cancellationToken);
        string GetNewsImageContentType(string storedFileName);
    }
}
