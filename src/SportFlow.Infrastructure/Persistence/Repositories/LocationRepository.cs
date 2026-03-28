using Microsoft.EntityFrameworkCore;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Infrastructure.Persistence.Repositories;

public class LocationRepository(SportFlowDbContext db) : ILocationRepository
{
    public async Task<IReadOnlyList<Location>> GetByTenantAsync(TenantId tenantId, CancellationToken ct = default)
        => await db.Locations
            .Where(l => l.TenantId == tenantId)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    public Task<Location?> GetByIdAsync(LocationId id, CancellationToken ct = default)
        => db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task AddAsync(Location location, CancellationToken ct = default)
        => await db.Locations.AddAsync(location, ct);

    public Task UpdateAsync(Location location, CancellationToken ct = default)
    {
        db.Locations.Update(location);
        return Task.CompletedTask;
    }
}
