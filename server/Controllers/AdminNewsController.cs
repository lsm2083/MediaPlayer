using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WorldBeat.Api.Configuration;
using WorldBeat.Api.Contracts;
using WorldBeat.Api.Repositories;
using WorldBeat.Api.Services;
using System.ComponentModel.DataAnnotations;

namespace WorldBeat.Api.Controllers
{
    [ApiController]
    [Route("api/admin/news")]
    public class AdminNewsController : ControllerBase
    {
        private readonly IYearNewsRepository _newsRepo;
        private readonly IFileStorageService _storage;
        private readonly ApiOptions _options;

        public AdminNewsController(
            IYearNewsRepository newsRepo,
            IFileStorageService storage,
            IOptions<ApiOptions> options)
        {
            _newsRepo = newsRepo;
            _storage = storage;
            _options = options.Value;
        }

        private IActionResult CheckAdminKey(string adminKey)
        {
            if (string.IsNullOrWhiteSpace(_options.AdminKey))
                return StatusCode(500, new { message = "AdminKey 설정이 필요합니다." });

            if (!string.Equals(adminKey, _options.AdminKey, StringComparison.Ordinal))
                return Unauthorized(new { message = "X-Admin-Key가 올바르지 않습니다." });

            return null;
        }

        /// <summary>[관리자] 연도별 뉴스 조회</summary>
        [HttpGet("{year:int}")]
        public async Task<IActionResult> GetByYear(
            [FromHeader(Name = "X-Admin-Key")] string adminKey,
            int year,
            CancellationToken cancellationToken)
        {
            var err = CheckAdminKey(adminKey);
            if (err != null) return err;

            var items = await _newsRepo.GetByYearAsync(year, cancellationToken);
            var response = items.Select(n => new YearNewsResponse
            {
                NewsId = n.NewsId,
                Year = n.Year,
                Month = n.Month,
                Headline = n.Headline,
                Description = n.Description,
                Category = n.Category,
                ImageUrl = string.IsNullOrWhiteSpace(n.ImagePath)
                    ? ""
                    : $"{Request.Scheme}://{Request.Host}/api/news/{n.NewsId}/image"
            });

            return Ok(response);
        }

        /// <summary>[관리자] 뉴스 추가</summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Create(
            [FromHeader(Name = "X-Admin-Key")] string adminKey,
            [FromForm] NewsCreateForm form,
            CancellationToken cancellationToken)
        {
            var err = CheckAdminKey(adminKey);
            if (err != null) return err;

            if (form.Year == 0)
                return BadRequest(new { message = "Year는 필수입니다." });

            if (string.IsNullOrWhiteSpace(form.Headline))
                return BadRequest(new { message = "Headline은 필수입니다." });

            var request = new YearNewsCreateRequest
            {
                Year = form.Year,
                Month = form.Month ?? 0,
                Headline = form.Headline,
                Description = form.Description,
                Category = form.Category
            };

            int newId = await _newsRepo.AddAsync(request, cancellationToken);

            if (form.Image != null && form.Image.Length > 0)
            {
                string imagePath = await _storage.SaveNewsImageAsync(form.Image, cancellationToken);
                await _newsRepo.UpdateImagePathAsync(newId, imagePath, cancellationToken);
            }

            var created = await _newsRepo.GetByIdAsync(newId, cancellationToken);
            return Created($"/api/admin/news/{form.Year}", new YearNewsResponse
            {
                NewsId = created!.NewsId,
                Year = created.Year,
                Month = created.Month,
                Headline = created.Headline,
                Description = created.Description,
                Category = created.Category,
                ImageUrl = string.IsNullOrWhiteSpace(created.ImagePath)
                    ? ""
                    : $"{Request.Scheme}://{Request.Host}/api/news/{created.NewsId}/image"
            });
        }

        /// <summary>[관리자] 뉴스 수정</summary>
        [HttpPut("{newsId:int}")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Update(
            [FromHeader(Name = "X-Admin-Key")] string adminKey,
            int newsId,
            [FromForm] NewsUpdateForm form,
            CancellationToken cancellationToken)
        {
            var err = CheckAdminKey(adminKey);
            if (err != null) return err;

            var request = new YearNewsUpdateRequest
            {
                Month = form.Month ?? 0,
                Headline = form.Headline,
                Description = form.Description,
                Category = form.Category
            };

            bool updated = await _newsRepo.UpdateAsync(newsId, request, cancellationToken);
            if (!updated)
                return NotFound(new { message = "뉴스 항목을 찾을 수 없습니다." });

            if (form.Image != null && form.Image.Length > 0)
            {
                var existing = await _newsRepo.GetByIdAsync(newsId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(existing?.ImagePath))
                    await _storage.DeleteNewsImageAsync(existing.ImagePath, cancellationToken);

                string imagePath = await _storage.SaveNewsImageAsync(form.Image, cancellationToken);
                await _newsRepo.UpdateImagePathAsync(newsId, imagePath, cancellationToken);
            }

            var item = await _newsRepo.GetByIdAsync(newsId, cancellationToken);
            return Ok(new YearNewsResponse
            {
                NewsId = item!.NewsId,
                Year = item.Year,
                Month = item.Month,
                Headline = item.Headline,
                Description = item.Description,
                Category = item.Category,
                ImageUrl = string.IsNullOrWhiteSpace(item.ImagePath)
                    ? ""
                    : $"{Request.Scheme}://{Request.Host}/api/news/{item.NewsId}/image"
            });
        }

        /// <summary>[관리자] 뉴스 삭제</summary>
        [HttpDelete("{newsId:int}")]
        public async Task<IActionResult> Delete(
            [FromHeader(Name = "X-Admin-Key")] string adminKey,
            int newsId,
            CancellationToken cancellationToken)
        {
            var err = CheckAdminKey(adminKey);
            if (err != null) return err;

            var existing = await _newsRepo.GetByIdAsync(newsId, cancellationToken);
            if (existing == null)
                return NotFound(new { message = "뉴스 항목을 찾을 수 없습니다." });

            if (!string.IsNullOrWhiteSpace(existing.ImagePath))
                await _storage.DeleteNewsImageAsync(existing.ImagePath, cancellationToken);

            bool deleted = await _newsRepo.DeleteAsync(newsId, cancellationToken);
            return deleted
                ? Ok(new { success = true, message = "삭제 완료" })
                : NotFound(new { message = "뉴스 항목을 찾을 수 없습니다." });
        }
    }

    public sealed class NewsCreateForm
    {
        [Display(Name = "년도")]
        public int Year { get; set; }

        [Display(Name = "월")]
        public int? Month { get; set; }

        [Display(Name = "제목")]
        public string Headline { get; set; }

        [Display(Name = "내용")]
        public string Description { get; set; }

        [Display(Name = "카테고리")]
        public string Category { get; set; }

        [Display(Name = "이미지")]
        public IFormFile Image { get; set; }
    }

    public sealed class NewsUpdateForm
    {
        [Display(Name = "년도")]
        public int Year { get; set; }

        [Display(Name = "월")]
        public int? Month { get; set; }

        [Display(Name = "제목")]
        public string Headline { get; set; }

        [Display(Name = "내용")]
        public string Description { get; set; }

        [Display(Name = "카테고리")]
        public string Category { get; set; }

        [Display(Name = "이미지")]
        public IFormFile Image { get; set; }
    }
}