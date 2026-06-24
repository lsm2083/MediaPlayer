using Microsoft.Extensions.Options;
using WorldBeat.Api.Configuration;

namespace WorldBeat.Api.Filters
{
    public sealed class AdminKeyFilter : IEndpointFilter
    {
        private readonly ApiOptions _options;

        public AdminKeyFilter(IOptions<ApiOptions> options)
        {
            _options = options.Value;
        }

        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            if (string.IsNullOrWhiteSpace(_options.AdminKey))
                return Results.Problem("AdminKey 설정이 필요합니다.", statusCode: 500);

            string requestKey = context.HttpContext.Request.Headers["X-Admin-Key"].ToString();
            if (!string.Equals(requestKey, _options.AdminKey, StringComparison.Ordinal))
                return Results.Unauthorized();

            return await next(context);
        }
    }
}