using SportFlow.Application.Abstractions;
using SportFlow.Application.Tenants.DTOs;
using SportFlow.Domain.Tenants;
using SportFlow.Shared.Results;

namespace SportFlow.Application.Tenants.Queries;

public record GetTenantMeQuery : IQuery<TenantMeResponse>;

public class GetTenantMeQueryHandler(
    ITenantRepository tenantRepository,
    ITenantContext tenantContext) : IQueryHandler<GetTenantMeQuery, TenantMeResponse>
{
    public async Task<Result<TenantMeResponse>> Handle(GetTenantMeQuery query, CancellationToken ct = default)
    {
        if (!tenantContext.TenantId.HasValue)
            return Result.Failure<TenantMeResponse>("NO_TENANT_CONTEXT", "This endpoint requires a tenant context.");

        var tenant = await tenantRepository.GetByIdAsync(tenantContext.TenantId.Value, ct);
        if (tenant is null)
            return Result.Failure<TenantMeResponse>("TENANT_NOT_FOUND", "Tenant not found.");

        return Result.Success(new TenantMeResponse(
            tenant.Id.Value,
            tenant.Name,
            tenant.Slug,
            tenant.Status,
            tenant.Plan,
            tenant.CreatedAt));
    }
}
