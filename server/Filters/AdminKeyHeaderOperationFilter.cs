using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WorldBeat.Api.Filters
{
    public sealed class AdminKeyHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            string path = context.ApiDescription.RelativePath ?? "";

            if (!path.StartsWith("api/admin/", StringComparison.OrdinalIgnoreCase))
                return;

            operation.Parameters ??= new List<OpenApiParameter>();

            bool exists = operation.Parameters.Any(p =>
                string.Equals(p.Name, "X-Admin-Key", StringComparison.OrdinalIgnoreCase) &&
                p.In == ParameterLocation.Header);

            if (exists)
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Admin-Key",
                In = ParameterLocation.Header,
                Required = true,
                Description = "관리자 키",
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });
        }
    }
}