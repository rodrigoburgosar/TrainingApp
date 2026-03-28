using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Application.Abstractions;

public interface IJwtService
{
    string GenerateAccessToken(User user, TenantId? tenantId, string? tenantSlug, string role);
    string GenerateRefreshToken();
    string HashToken(string token);
}
