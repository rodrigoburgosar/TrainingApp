using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SportFlow.Application.Abstractions;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Infrastructure.Services;

public class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateAccessToken(User user, TenantId? tenantId, string? tenantSlug, string role)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (tenantId.HasValue)
            claims.Add(new Claim("tenant_id", tenantId.Value.Value.ToString()));

        if (tenantSlug is not null)
            claims.Add(new Claim("tenant_slug", tenantSlug));

        var scopes = SystemRoles.GetScopesForRole(role);
        foreach (var scope in scopes)
            claims.Add(new Claim("scope", scope));

        var expiresMinutes = jwtSettings.GetValue("ExpiresMinutes", 15);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
