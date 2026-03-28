using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Domain.Tenants;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default);
}
