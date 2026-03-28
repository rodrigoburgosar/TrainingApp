using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Infrastructure.Services;

namespace SportFlow.Application.Tests.Identity;

public class JwtServiceTests
{
    private const string SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123!";
    private const string Issuer = "SportFlow";
    private const string Audience = "SportFlow";

    private readonly JwtService _service;
    private readonly User _user;
    private readonly TenantId _tenantId;

    public JwtServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = SecretKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:ExpiresMinutes"] = "15"
            })
            .Build();

        _service = new JwtService(config);
        _user = User.Create("coach@gym.com", "hash", SystemRoles.Coach);
        _tenantId = TenantId.New();
    }

    [Theory]
    [InlineData(SystemRoles.SuperAdmin)]
    [InlineData(SystemRoles.TenantOwner)]
    [InlineData(SystemRoles.Coach)]
    [InlineData(SystemRoles.Member)]
    public void GenerateAccessToken_ContainsCorrectRoleClaim(string role)
    {
        var token = _service.GenerateAccessToken(_user, _tenantId, "demo", role);

        var principal = ValidateToken(token);
        principal.FindFirst("role")?.Value.ShouldBe(role);
    }

    [Fact]
    public void GenerateAccessToken_ContainsTenantIdClaim()
    {
        var token = _service.GenerateAccessToken(_user, _tenantId, "demo", SystemRoles.Coach);

        var principal = ValidateToken(token);
        principal.FindFirst("tenant_id")?.Value.ShouldBe(_tenantId.Value.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ContainsSubClaim()
    {
        var token = _service.GenerateAccessToken(_user, _tenantId, "demo", SystemRoles.Coach);

        var principal = ValidateToken(token);
        principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.ShouldBe(_user.Id.Value.ToString());
    }

    [Fact]
    public void GenerateAccessToken_SuperAdmin_HasNoTenantClaim()
    {
        var token = _service.GenerateAccessToken(_user, null, null, SystemRoles.SuperAdmin);

        var principal = ValidateToken(token);
        principal.FindFirst("tenant_id").ShouldBeNull();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();

        token1.ShouldNotBeNullOrWhiteSpace();
        token2.ShouldNotBeNullOrWhiteSpace();
        token1.ShouldNotBe(token2);
    }

    [Fact]
    public void HashToken_SameInput_ReturnsSameHash()
    {
        var hash1 = _service.HashToken("test-token");
        var hash2 = _service.HashToken("test-token");

        hash1.ShouldBe(hash2);
    }

    [Fact]
    public void HashToken_DifferentInput_ReturnsDifferentHash()
    {
        var hash1 = _service.HashToken("token-a");
        var hash2 = _service.HashToken("token-b");

        hash1.ShouldNotBe(hash2);
    }

    private ClaimsPrincipal ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        }, out _);
    }
}
