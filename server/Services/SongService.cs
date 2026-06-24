using WorldBeat.Api.Contracts;
using WorldBeat.Api.Models;
using WorldBeat.Api.Repositories;

namespace WorldBeat.Api.Services
{
    public sealed class SongService : ISongService
    {
        private readonly ISongRepository _songRepository;
        private readonly IFileStorageService _fileStorageService;

        public SongService(ISongRepository songRepository, IFileStorageService fileStorageService)
        {
            _songRepository = songRepository;
            _fileStorageService = fileStorageService;
        }

        public Task<List<string>> GetGenresAsync(CancellationToken cancellationToken)
        {
            return _songRepository.GetGenresAsync(cancellationToken);
        }

        public async Task<List<SongResponse>> GetSongsAsync(string genreName, HttpContext httpContext, CancellationToken cancellationToken)
        {
            var songs = await _songRepository.GetSongsAsync(genreName, cancellationToken);
            return songs.Select(song => Map(song, httpContext)).ToList();
        }

        public async Task<List<SongResponse>> GetRecentAsync(int limit, HttpContext httpContext, CancellationToken cancellationToken)
        {
            var songs = await _songRepository.GetRecentAsync(limit, cancellationToken);
            return songs.Select(song => Map(song, httpContext)).ToList();
        }

        public async Task<List<SongResponse>> GetTopAsync(int limit, HttpContext httpContext, CancellationToken cancellationToken)
        {
            var songs = await _songRepository.GetTopAsync(limit, cancellationToken);
            return songs.Select(song => Map(song, httpContext)).ToList();
        }

        public async Task<SongResponse> GetSongAsync(int songId, HttpContext httpContext, CancellationToken cancellationToken)
        {
            var song = await _songRepository.GetByIdAsync(songId, cancellationToken);
            if (song == null)
                return null;

            return Map(song, httpContext);
        }

        public async Task<SongResponse> UploadSongAsync(IFormFile file, SongCreateCommand command, HttpContext httpContext, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("업로드할 파일이 없습니다.");

            command ??= new SongCreateCommand();

            string storedSongName = await _fileStorageService.SaveSongAsync(file, cancellationToken);
            string storedSongFullPath = _fileStorageService.GetSongFullPath(storedSongName);
            string storedAlbumArtName = "";

            try
            {
                string autoTitle = SafeFileNameWithoutExtension(file.FileName);
                string autoArtist = "";
                string autoAlbum = "";
                string autoGenre = "기타";
                int autoYear = 0;
                int autoDuration = 0;

                try
                {
                    using var tagFile = TagLib.File.Create(storedSongFullPath);

                    autoTitle = PickText(
                        tagFile.Tag.Title,
                        autoTitle);

                    autoArtist = PickText(
                        tagFile.Tag.JoinedPerformers,
                        tagFile.Tag.FirstPerformer,
                        autoArtist);

                    autoAlbum = PickText(
                        tagFile.Tag.Album,
                        autoAlbum);

                    autoGenre = PickGenre(
                        tagFile.Tag.JoinedGenres,
                        tagFile.Tag.FirstGenre,
                        autoGenre);

                    if (tagFile.Tag.Year > 0 && tagFile.Tag.Year <= 9999)
                        autoYear = (int)tagFile.Tag.Year;

                    if (tagFile.Properties != null && tagFile.Properties.Duration.TotalSeconds > 0)
                        autoDuration = (int)Math.Round(tagFile.Properties.Duration.TotalSeconds);

                    if (tagFile.Tag.Pictures != null && tagFile.Tag.Pictures.Length > 0)
                    {
                        var picture = tagFile.Tag.Pictures[0];
                        byte[] bytes = picture.Data.Data;
                        string imageExt = GuessImageExtension(picture.MimeType);

                        if (bytes != null && bytes.Length > 0)
                            storedAlbumArtName = await _fileStorageService.SaveAlbumArtAsync(bytes, imageExt, cancellationToken);
                    }
                }
                catch
                {
                }

                var createCommand = new SongCreateCommand
                {
                    Title = PickText(command.Title, autoTitle, SafeFileNameWithoutExtension(file.FileName)),
                    Artist = PickText(command.Artist, autoArtist),
                    Album = PickText(command.Album, autoAlbum),
                    Genre = PickGenre(command.Genre, autoGenre, "기타"),
                    Year = command.Year > 0 ? command.Year : autoYear,
                    Duration = command.Duration > 0 ? command.Duration : autoDuration,
                    FilePath = storedSongName,
                    AlbumArtPath = storedAlbumArtName,
                    Lyrics = command.Lyrics  // [추가] 업로드 시 입력한 가사 전달
                };

                bool exists = await _songRepository.ExistsDuplicateAsync(
                    createCommand.Title,
                    createCommand.Artist,
                    createCommand.Duration,
                    cancellationToken);

                if (exists)
                    throw new InvalidOperationException("이미 등록된 곡입니다.");

                int songId = await _songRepository.AddAsync(createCommand, cancellationToken);
                var createdSong = await _songRepository.GetByIdAsync(songId, cancellationToken);

                return Map(createdSong, httpContext);
            }
            catch
            {
                await _fileStorageService.DeleteSongAsync(storedSongName, cancellationToken);

                if (!string.IsNullOrWhiteSpace(storedAlbumArtName))
                    await _fileStorageService.DeleteAlbumArtAsync(storedAlbumArtName, cancellationToken);

                throw;
            }
        }

        public async Task<bool> UpdateSongAsync(int songId, SongUpdateRequest request, HttpContext httpContext, CancellationToken cancellationToken)
        {
            return await _songRepository.UpdateAsync(songId, request, cancellationToken);
        }

        public async Task<bool> DeleteSongAsync(int songId, CancellationToken cancellationToken)
        {
            var song = await _songRepository.GetByIdAsync(songId, cancellationToken);
            if (song == null)
                return false;

            bool deleted = await _songRepository.DeleteAsync(songId, cancellationToken);
            if (!deleted)
                return false;

            await _fileStorageService.DeleteSongAsync(song.FilePath, cancellationToken);

            if (!string.IsNullOrWhiteSpace(song.AlbumArtPath))
                await _fileStorageService.DeleteAlbumArtAsync(song.AlbumArtPath, cancellationToken);

            return true;
        }

        public Task<bool> IncrementPlayCountAsync(int songId, CancellationToken cancellationToken)
        {
            return _songRepository.IncrementPlayCountAsync(songId, cancellationToken);
        }

        public async Task<SongStreamInfo> OpenSongStreamAsync(int songId, CancellationToken cancellationToken)
        {
            var song = await _songRepository.GetByIdAsync(songId, cancellationToken);
            if (song == null || string.IsNullOrWhiteSpace(song.FilePath))
                return null;

            var stream = await _fileStorageService.OpenSongReadAsync(song.FilePath, cancellationToken);
            if (stream == null)
                return null;

            return new SongStreamInfo
            {
                Stream = stream,
                ContentType = _fileStorageService.GetSongContentType(song.FilePath),
                DownloadName = $"{song.Title}{Path.GetExtension(song.FilePath)}"
            };
        }

        public async Task<SongStreamInfo> OpenAlbumArtStreamAsync(int songId, CancellationToken cancellationToken)
        {
            var song = await _songRepository.GetByIdAsync(songId, cancellationToken);
            if (song == null || string.IsNullOrWhiteSpace(song.AlbumArtPath))
                return null;

            var stream = await _fileStorageService.OpenAlbumArtReadAsync(song.AlbumArtPath, cancellationToken);
            if (stream == null)
                return null;

            return new SongStreamInfo
            {
                Stream = stream,
                ContentType = _fileStorageService.GetAlbumArtContentType(song.AlbumArtPath),
                DownloadName = song.AlbumArtPath
            };
        }

        private static SongResponse Map(SongEntity song, HttpContext httpContext)
        {
            string baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            return new SongResponse
            {
                SongId = song.SongId,
                Title = song.Title ?? "",
                Artist = song.Artist ?? "",
                Album = song.Album ?? "",
                Genre = song.Genre ?? "기타",
                Year = song.Year,
                Duration = song.Duration,
                PlayCount = song.PlayCount,
                FileUrl = $"{baseUrl}/api/songs/{song.SongId}/stream",
                AlbumArtUrl = string.IsNullOrWhiteSpace(song.AlbumArtPath)
                    ? ""
                    : $"{baseUrl}/api/songs/{song.SongId}/art",
                Lyrics = song.Lyrics  // [추가] 가사를 응답에 포함
            };
        }

        private static string GuessImageExtension(string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
                return ".jpg";

            string mime = mimeType.Trim().ToLowerInvariant();

            if (mime.Contains("png")) return ".png";
            if (mime.Contains("gif")) return ".gif";
            if (mime.Contains("bmp")) return ".bmp";
            if (mime.Contains("webp")) return ".webp";
            return ".jpg";
        }

        private static string SafeFileNameWithoutExtension(string fileName)
        {
            return NormalizeText(Path.GetFileNameWithoutExtension(fileName));
        }

        private static string PickText(params string[] values)
        {
            foreach (var value in values)
            {
                string normalized = NormalizeText(value);
                if (!string.IsNullOrWhiteSpace(normalized))
                    return normalized;
            }

            return "";
        }

        private static string PickGenre(params string[] values)
        {
            foreach (var value in values)
            {
                string normalized = NormalizeGenre(value);
                if (!string.IsNullOrWhiteSpace(normalized))
                    return normalized;
            }

            return "기타";
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            string text = value.Trim();

            if (IsPlaceholderText(text))
                return "";

            return text;
        }

        private static string NormalizeGenre(string value)
        {
            string genre = NormalizeText(value);

            if (string.Equals(genre, "전체", StringComparison.OrdinalIgnoreCase))
                return "";

            return genre;
        }

        private static bool IsPlaceholderText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            string text = value.Trim();

            return string.Equals(text, "string", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "null", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "(null)", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "undefined", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "none", StringComparison.OrdinalIgnoreCase);
        }
    }
}