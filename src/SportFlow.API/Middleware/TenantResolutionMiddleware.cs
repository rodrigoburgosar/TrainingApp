using System.IdentityModel.Tokens.Jwt;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Infrastructure.Services;

namespace SportFlow.API.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var principal = context.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? principal.FindFirst("sub")?.Value;

            var role = principal.FindFirst("role")?.Value;

            if (sub is not null && role is not null && Guid.TryParse(sub, out var userId))
            {
                var tenantIdClaim = principal.FindFirst("tenant_id")?.Value;
                var tenantSlug = principal.FindFirst("tenant_slug")?.Value;

                TenantId? tenantId = tenantIdClaim is not null && Guid.TryParse(tenantIdClaim, out var tid)
                    ? TenantId.From(tid)
                    : null;

                tenantContext.Initialize(
                    UserId.From(userId),
                    role,
                    tenantId,
                    tenantSlug);
            }
        }

        await next(context);
    }
}
