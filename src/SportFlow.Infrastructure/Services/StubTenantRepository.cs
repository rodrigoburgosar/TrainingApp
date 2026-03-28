using Microsoft.Extensions.Configuration;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Infrastructure.Services;

/// <summary>
/// Configuration-backed stub for development. Used when running without a database.
/// Reads tenants from appsettings.json under "Tenants" section.
/// </summary>
public class StubTenantRepository(IConfiguration configuration) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct = default)
    {
        var tenants = configuration.GetSection("Tenants").Get<List<TenantConfig>>();
        var match = tenants?.FirstOrDefault(t => t.Id == id.Value);
        return Task.FromResult(match is null ? null : ToTenant(match));
    }

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var tenants = configuration.GetSection("Tenants").Get<List<TenantConfig>>();
        var match = tenants?.FirstOrDefault(t =>
            string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match is null ? null : ToTenant(match));
    }

    private static Tenant? ToTenant(TenantConfig config)
    {
        var tenant = Tenant.Create(config.Name, config.Slug, config.Plan);
        if (config.Status == TenantStatus.Suspended) tenant.Suspend();
        return tenant;
    }

    private sealed class TenantConfig
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
        public string Plan { get; set; } = "basic";
    }
}
