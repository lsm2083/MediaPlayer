using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using WorldBeat.Api.Configuration;

namespace WorldBeat.Api.Services
{
    public sealed class LocalFileStorageService : IFileStorageService
    {
        private static readonly HashSet<string> AllowedSongExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".aac", ".flac", ".m4a"
        };

        // [추가] 뉴스 이미지 허용 확장자
        private static readonly HashSet<string> AllowedImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
        };

        private readonly string _songRootPath;
        private readonly string _albumArtRootPath;
        private readonly string _newsImageRootPath; // [추가] 뉴스 이미지 저장 폴더
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new FileExtensionContentTypeProvider();

        public LocalFileStorageService(IOptions<ApiOptions> options)
        {
            string root = options.Value.MusicStorageRoot;
            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "WorldBeat",
                    "MusicStorage");
            }

            _songRootPath      = Path.Combine(root, "songs");
            _albumArtRootPath  = Path.Combine(root, "albumart");
            _newsImageRootPath = Path.Combine(root, "newsimages"); // [추가]

            Directory.CreateDirectory(_songRootPath);
            Directory.CreateDirectory(_albumArtRootPath);
            Directory.CreateDirectory(_newsImageRootPath); // [추가]
        }

        public async Task<string> SaveSongAsync(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("업로드할 파일이 없습니다.");

            string ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedSongExtensions.Contains(ext))
                throw new InvalidOperationException("지원하지 않는 음원 형식입니다.");

            string storedName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            string fullPath = Path.Combine(_songRootPath, storedName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream, cancellationToken);

            return storedName;
        }

        public async Task<string> SaveAlbumArtAsync(byte[] bytes, string extension, CancellationToken cancellationToken)
        {
            if (bytes == null || bytes.Length == 0)
                return "";

            string ext = NormalizeImageExtension(extension);
            string storedName = $"{Guid.NewGuid():N}{ext}";
            string fullPath = Path.Combine(_albumArtRootPath, storedName);

            await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
            return storedName;
        }

        // [추가] 뉴스 이미지 파일 저장
        public async Task<string> SaveNewsImageAsync(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("업로드할 이미지 파일이 없습니다.");

            string ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedImageExtensions.Contains(ext))
                throw new InvalidOperationException("지원하지 않는 이미지 형식입니다. (jpg, png, gif, bmp, webp 허용)");

            string storedName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            string fullPath = Path.Combine(_newsImageRootPath, storedName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream, cancellationToken);

            return storedName;
        }

        // [추가] 뉴스 이미지 파일 삭제
        public Task DeleteNewsImageAsync(string storedFileName, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(storedFileName))
            {
                string fullPath = Path.Combine(_newsImageRootPath, storedFileName);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }

        // [추가] 뉴스 이미지 파일 스트림 열기
        public Task<Stream> OpenNewsImageReadAsync(string storedFileName, CancellationToken cancellationToken)
        {
            string fullPath = Path.Combine(_newsImageRootPath, storedFileName);
            if (!File.Exists(fullPath))
                return Task.FromResult<Stream>(null);

            Stream stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        // [추가] 뉴스 이미지 Content-Type 반환
        public string GetNewsImageContentType(string storedFileName)
        {
            if (_contentTypeProvider.TryGetContentType(storedFileName, out string contentType))
                return contentType;

            return "image/jpeg";
        }

        public Task DeleteSongAsync(string storedFileName, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(storedFileName))
            {
                string fullPath = Path.Combine(_songRootPath, storedFileName);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAlbumArtAsync(string storedFileName, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(storedFileName))
            {
                string fullPath = Path.Combine(_albumArtRootPath, storedFileName);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }

        public Task<Stream> OpenSongReadAsync(string storedFileName, CancellationToken cancellationToken)
        {
            string fullPath = Path.Combine(_songRootPath, storedFileName);
            if (!File.Exists(fullPath))
                return Task.FromResult<Stream>(null);

            Stream stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        public Task<Stream> OpenAlbumArtReadAsync(string storedFileName, CancellationToken cancellationToken)
        {
            string fullPath = Path.Combine(_albumArtRootPath, storedFileName);
            if (!File.Exists(fullPath))
                return Task.FromResult<Stream>(null);

            Stream stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        public string GetSongFullPath(string storedFileName)
        {
            return Path.Combine(_songRootPath, storedFileName ?? "");
        }

        public string GetSongContentType(string storedFileName)
        {
            if (_contentTypeProvider.TryGetContentType(storedFileName, out string contentType))
                return contentType;

            return "application/octet-stream";
        }

        public string GetAlbumArtContentType(string storedFileName)
        {
            if (_contentTypeProvider.TryGetContentType(storedFileName, out string contentType))
                return contentType;

            return "image/jpeg";
        }

        private static string NormalizeImageExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return ".jpg";

            string ext = extension.Trim().ToLowerInvariant();
            if (!ext.StartsWith("."))
                ext = "." + ext;

            if (ext == ".jpeg")
                return ".jpg";

            if (ext == ".jpg" || ext == ".png" || ext == ".gif" || ext == ".bmp" || ext == ".webp")
                return ext;

            return ".jpg";
        }
    }
}
