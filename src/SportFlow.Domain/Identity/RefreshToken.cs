using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Domain.Identity;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public UserId UserId { get; private set; }
    public TenantId? TenantId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken() { }

    public static RefreshToken Create(
        UserId userId,
        TenantId? tenantId,
        string tokenHash,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}
