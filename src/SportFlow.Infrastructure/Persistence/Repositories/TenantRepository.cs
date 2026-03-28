using Microsoft.EntityFrameworkCore;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Infrastructure.Persistence.Repositories;

public class TenantRepository(SportFlowDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct = default)
        => db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant(), ct);
}
