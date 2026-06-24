using WorldBeat.Api.Contracts;
using WorldBeat.Api.Models;
using WorldBeat.Api.Repositories;

namespace WorldBeat.Api.Services
{
    public sealed class CapsuleService : ICapsuleService
    {
        private readonly ICapsuleRepository _capsuleRepository;
        private readonly ISongRepository _songRepository;

        public CapsuleService(ICapsuleRepository capsuleRepository, ISongRepository songRepository)
        {
            _capsuleRepository = capsuleRepository;
            _songRepository = songRepository;
        }

        public async Task<CapsuleResponse> CreateAsync(CapsuleCreateRequest request, HttpContext httpContext, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            var validSongIds = await GetValidSongIdsAsync(request.SongIds, cancellationToken);
            if (validSongIds.Count == 0)
                throw new InvalidOperationException("유효한 곡이 없습니다.");

            int capsuleId = await _capsuleRepository.AddAsync(
                request.UserId,
                request.Title.Trim(),
                request.OpenDate.Date,
                validSongIds,
                cancellationToken);

            var created = await _capsuleRepository.GetByIdAsync(capsuleId, cancellationToken);
            if (created == null)
                throw new InvalidOperationException("생성된 캡슐을 찾을 수 없습니다.");

            return Map(created, httpContext);
        }

        public async Task<CapsuleResponse> UpdateAsync(int capsuleId, CapsuleCreateRequest request, HttpContext httpContext, CancellationToken cancellationToken)
        {
            if (capsuleId <= 0)
                throw new InvalidOperationException("수정할 캡슐 정보가 올바르지 않습니다.");

            ValidateRequest(request);

            var validSongIds = await GetValidSongIdsAsync(request.SongIds, cancellationToken);
            if (validSongIds.Count == 0)
                throw new InvalidOperationException("유효한 곡이 없습니다.");

            bool updated = await _capsuleRepository.UpdateAsync(
                capsuleId,
                request.UserId,
                request.Title.Trim(),
                request.OpenDate.Date,
                validSongIds,
                cancellationToken);

            if (!updated)
                throw new InvalidOperationException("수정할 캡슐을 찾을 수 없습니다.");

            var item = await _capsuleRepository.GetByIdAsync(capsuleId, cancellationToken);
            if (item == null)
                throw new InvalidOperationException("수정된 캡슐을 찾을 수 없습니다.");

            return Map(item, httpContext);
        }

        public Task<bool> DeleteAsync(int capsuleId, CancellationToken cancellationToken)
        {
            if (capsuleId <= 0)
                return Task.FromResult(false);

            return _capsuleRepository.DeleteAsync(capsuleId, cancellationToken);
        }

        public async Task<List<CapsuleResponse>> GetByUserAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken)
        {
            var items = await _capsuleRepository.GetByUserAsync(userId, cancellationToken);
            return items.Select(x => Map(x, httpContext)).ToList();
        }

        public async Task<List<CapsuleOpenNoticeResponse>> GetPendingOpenNoticesAsync(int userId, CancellationToken cancellationToken)
        {
            var items = await _capsuleRepository.GetPendingOpenNoticesAsync(userId, DateTime.Now.Date, cancellationToken);

            return items.Select(x => new CapsuleOpenNoticeResponse
            {
                CapsuleId = x.CapsuleId,
                Title = x.Title,
                OpenDate = x.OpenDate
            }).ToList();
        }

        public Task<bool> MarkNoticeShownAsync(int capsuleId, CancellationToken cancellationToken)
        {
            return _capsuleRepository.MarkNoticeShownAsync(capsuleId, cancellationToken);
        }

        private async Task<List<int>> GetValidSongIdsAsync(List<int> songIds, CancellationToken cancellationToken)
        {
            var requestedSongIds = (songIds ?? new List<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (requestedSongIds.Count == 0)
                throw new InvalidOperationException("최소 1곡 이상 선택하세요.");

            var validSongIds = new List<int>();

            foreach (int songId in requestedSongIds)
            {
                var song = await _songRepository.GetByIdAsync(songId, cancellationToken);
                if (song != null)
                    validSongIds.Add(songId);
            }

            return validSongIds;
        }

        private static void ValidateRequest(CapsuleCreateRequest request)
        {
            if (request == null)
                throw new InvalidOperationException("잘못된 요청입니다.");

            if (request.UserId <= 0)
                throw new InvalidOperationException("로그인 정보가 없습니다.");

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new InvalidOperationException("캡슐 이름을 입력하세요.");

            if (request.OpenDate.Date <= DateTime.Now.Date)
                throw new InvalidOperationException("개봉일은 오늘 이후만 가능합니다.");
        }

        private static CapsuleResponse Map(CapsuleEntity capsule, HttpContext httpContext)
        {
            string baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            return new CapsuleResponse
            {
                CapsuleId = capsule.CapsuleId,
                UserId = capsule.UserId,
                Title = capsule.Title ?? "",
                OpenDate = capsule.OpenDate,
                NoticeShown = capsule.NoticeShown,
                Songs = capsule.Songs.Select(song => new SongResponse
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
                        : $"{baseUrl}/api/songs/{song.SongId}/art"
                }).ToList()
            };
        }
    }
}