using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Application.Abstractions;

public interface ITenantContext
{
    TenantId? TenantId { get; }
    string? TenantSlug { get; }
    UserId UserId { get; }
    string Role { get; }
    bool IsSuperAdmin { get; }
}
