using Microsoft.AspNetCore.Mvc;
using WorldBeat.Api.Contracts;
using WorldBeat.Api.Services;

namespace WorldBeat.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class CapsulesController : ControllerBase
    {
        private readonly ICapsuleService _capsuleService;

        public CapsulesController(ICapsuleService capsuleService)
        {
            _capsuleService = capsuleService;
        }

        /// <summary>타임캡슐 생성</summary>
        [HttpPost]
        public async Task<ActionResult<CapsuleResponse>> Create(
            [FromBody] CapsuleCreateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _capsuleService.CreateAsync(
                    request,
                    HttpContext,
                    cancellationToken);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>사용자 타임캡슐 목록</summary>
        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<List<CapsuleResponse>>> GetByUser(
            int userId,
            CancellationToken cancellationToken)
        {
            var result = await _capsuleService.GetByUserAsync(
                userId,
                HttpContext,
                cancellationToken);

            return Ok(result);
        }

        /// <summary>열린 미확인 캡슐 조회</summary>
        [HttpGet("user/{userId:int}/opened-pending")]
        public async Task<ActionResult<List<CapsuleOpenNoticeResponse>>> GetOpenedPending(
            int userId,
            CancellationToken cancellationToken)
        {
            var result = await _capsuleService.GetPendingOpenNoticesAsync(
                userId,
                cancellationToken);

            return Ok(result);
        }

        /// <summary>캡슐 열림 확인 처리</summary>
        [HttpPost("{capsuleId:int}/ack-open")]
        public async Task<IActionResult> AckOpen(
            int capsuleId,
            CancellationToken cancellationToken)
        {
            bool ok = await _capsuleService.MarkNoticeShownAsync(
                capsuleId,
                cancellationToken);

            if (!ok)
                return NotFound(new { Message = "캡슐을 찾을 수 없습니다." });

            return Ok(new { Message = "확인 완료" });
        }

        /// <summary>타임캡슐 수정</summary>
        [HttpPut("{capsuleId:int}")]
        public async Task<ActionResult<CapsuleResponse>> Update(
            int capsuleId,
            [FromBody] CapsuleCreateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _capsuleService.UpdateAsync(
                    capsuleId,
                    request,
                    HttpContext,
                    cancellationToken);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>타임캡슐 삭제</summary>
        [HttpDelete("{capsuleId:int}")]
        public async Task<IActionResult> Delete(
            int capsuleId,
            CancellationToken cancellationToken)
        {
            bool ok = await _capsuleService.DeleteAsync(
                capsuleId,
                cancellationToken);

            if (!ok)
                return NotFound(new { Message = "삭제할 캡슐을 찾을 수 없습니다." });

            return Ok(new { Message = "캡슐 삭제 완료" });
        }
    }
}