using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Domain.Tenants;

public interface ILocationRepository
{
    Task<IReadOnlyList<Location>> GetByTenantAsync(TenantId tenantId, CancellationToken ct = default);
    Task<Location?> GetByIdAsync(LocationId id, CancellationToken ct = default);
    Task AddAsync(Location location, CancellationToken ct = default);
    Task UpdateAsync(Location location, CancellationToken ct = default);
}
