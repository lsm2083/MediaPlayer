using WorldBeat.Api.Contracts;

namespace WorldBeat.Api.Services
{
    public interface ICapsuleService
    {
        Task<CapsuleResponse> CreateAsync(CapsuleCreateRequest request, HttpContext httpContext, CancellationToken cancellationToken);
        Task<List<CapsuleResponse>> GetByUserAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken);
        Task<List<CapsuleOpenNoticeResponse>> GetPendingOpenNoticesAsync(int userId, CancellationToken cancellationToken);
        Task<bool> MarkNoticeShownAsync(int capsuleId, CancellationToken cancellationToken);


        //캡슐
        Task<CapsuleResponse> UpdateAsync(int capsuleId, CapsuleCreateRequest request, HttpContext httpContext, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(int capsuleId, CancellationToken cancellationToken);
    }
}