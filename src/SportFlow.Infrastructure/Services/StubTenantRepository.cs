using Microsoft.Extensions.Configuration;
using SportFlow.Application.Identity.Commands;

namespace SportFlow.Infrastructure.Services;

/// <summary>
/// Stub implementation for development. Replace with proper repository when Tenants domain is implemented.
/// Reads tenants from appsettings.json under "Tenants" section.
/// </summary>
public class StubTenantRepository(IConfiguration configuration) : ITenantRepository
{
    public Task<TenantInfo?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var tenants = configuration.GetSection("Tenants").Get<List<TenantConfig>>();
        var match = tenants?.FirstOrDefault(t =>
            string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return Task.FromResult<TenantInfo?>(null);

        return Task.FromResult<TenantInfo?>(
            new TenantInfo(match.Id, match.Name, match.Slug, match.Status, match.Plan));
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
