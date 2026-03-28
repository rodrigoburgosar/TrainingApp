using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Domain.Identity;

public class User
{
    public UserId Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string SystemRole { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetExpiresAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private User() { }

    public static User Create(string email, string passwordHash, string systemRole)
    {
        return new User
        {
            Id = UserId.New(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            SystemRole = systemRole,
            IsActive = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePasswordHash(string newHash)
    {
        PasswordHash = newHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordResetToken(string token, DateTime expiresAt)
    {
        PasswordResetToken = token;
        PasswordResetExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
