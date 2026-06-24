using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WorldBeat.Api.Configuration;
using WorldBeat.Api.Contracts;
using WorldBeat.Api.Services;

namespace WorldBeat.Api.Controllers
{
    [ApiController]
    [Route("api/admin/songs")]
    public class AdminSongsController : ControllerBase
    {
        private readonly ISongService _songService;
        private readonly ApiOptions _options;

        public AdminSongsController(
            ISongService songService,
            IOptions<ApiOptions> options)
        {
            _songService = songService;
            _options = options.Value;
        }

        /// <summary>[관리자] 곡 업로드 (노래 추가)</summary>
        /// <remarks>음원 파일과 제목·가수·앨범·장르·가사 등을 함께 업로드해 새 곡을 등록합니다. X-Admin-Key 헤더가 필요합니다.</remarks>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Upload(
            [FromHeader(Name = "X-Admin-Key")] string adminKey,
            [FromForm] SongUploadForm form,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.AdminKey))
                return StatusCode(500, new { message = "AdminKey 설정이 필요합니다." });

            if (!string.Equals(adminKey, _options.AdminKey, StringComparison.Ordinal))
                return Unauthorized(new { message = "X-Admin-Key가 올바르지 않습니다." });

            if (form == null || form.File == null || form.File.Length == 0)
                return BadRequest(new { message = "업로드할 파일이 없습니다." });

            try
            {
                var command = new SongCreateCommand
                {
                    Title = form.Title,
                    Artist = form.Artist,
                    Album = form.Album,
                    Genre = form.Genre,
                    Year = form.Year,
                    Duration = form.Duration,
                    FilePath = "",
                    AlbumArtPath = "",
                    // [수정] 업로드 폼에서 \n 을 문자 그대로 입력하면 실제 줄바꿈으로 변환
                    Lyrics = string.IsNullOrWhiteSpace(form.Lyrics)
                        ? null
                        : form.Lyrics.Replace("\\n", "\n")
                };

                var created = await _songService.UploadSongAsync(
                    form.File,
                    command,
                    HttpContext,
                    cancellationToken);

                return Created($"/api/songs/{created.SongId}", created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}