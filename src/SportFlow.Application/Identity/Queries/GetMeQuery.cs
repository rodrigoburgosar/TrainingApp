using SportFlow.Application.Abstractions;
using SportFlow.Application.Identity.Commands;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Tenants;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Identity.Queries;

public record GetMeQuery : IQuery<MeResponse>;

public class GetMeQueryHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ITenantContext tenantContext) : IQueryHandler<GetMeQuery, MeResponse>
{
    public async Task<Result<MeResponse>> Handle(GetMeQuery query, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(tenantContext.UserId, ct);
        if (user is null)
            return Result.Failure<MeResponse>("NOT_FOUND", "User not found.");

        TenantRef? tenantRef = null;
        if (tenantContext.TenantId.HasValue)
        {
            var tenant = await tenantRepository.GetBySlugAsync(tenantContext.TenantSlug ?? "", ct);
            if (tenant is not null)
                tenantRef = new TenantRef(tenant.Id.Value, tenant.Name, tenant.Slug, tenant.Plan);
        }

        var scopes = SystemRoles.GetScopesForRole(tenantContext.Role);
        var me = new MeResponse(user.Id.Value, user.Email, tenantContext.Role, scopes, tenantRef);

        return Result.Success(me);
    }
}
