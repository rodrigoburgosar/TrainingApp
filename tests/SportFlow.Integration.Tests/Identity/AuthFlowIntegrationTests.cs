using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SportFlow.Application.Identity.Commands;
using SportFlow.Application.Identity.DTOs;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Infrastructure.Persistence;

namespace SportFlow.Integration.Tests.Identity;

public class AuthFlowIntegrationTests : IClassFixture<SportFlowWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly SportFlowWebAppFactory _factory;
    private const string TenantSlug = "demo";

    public AuthFlowIntegrationTests(SportFlowWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        var request = new LoginRequest("member@demo.com", "Password1!", TenantSlug);
        var response = await _client.PostAsJsonAsync("/v1/auth/login", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body.ShouldNotBeNull();
        body.AccessToken.ShouldNotBeNullOrWhiteSpace();
        body.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        body.Me.Email.ShouldBe("member@demo.com");
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns400()
    {
        var request = new LoginRequest("member@demo.com", "WrongPass!", TenantSlug);
        var response = await _client.PostAsJsonAsync("/v1/auth/login", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserInfo()
    {
        var loginResp = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest("member@demo.com", "Password1!", TenantSlug));
        var tokens = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var meResp = await _client.GetAsync("/v1/auth/me");

        meResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var me = await meResp.Content.ReadFromJsonAsync<MeResponse>();
        me!.Email.ShouldBe("member@demo.com");
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        var loginResp = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest("member@demo.com", "Password1!", TenantSlug));
        var tokens = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();

        var refreshResp = await _client.PostAsJsonAsync("/v1/auth/refresh",
            new RefreshTokenRequest(tokens!.RefreshToken));

        refreshResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newTokens = await refreshResp.Content.ReadFromJsonAsync<TokenResponse>();
        newTokens!.AccessToken.ShouldNotBe(tokens.AccessToken);
        newTokens.RefreshToken.ShouldNotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task Logout_ThenRefresh_ReturnsUnauthorized()
    {
        var loginResp = await _client.PostAsJsonAsync("/v1/auth/login",
            new LoginRequest("member@demo.com", "Password1!", TenantSlug));
        var tokens = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        await _client.PostAsJsonAsync("/v1/auth/logout", new LogoutRequest(tokens.RefreshToken));

        var refreshResp = await _client.PostAsJsonAsync("/v1/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));

        refreshResp.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

public class SportFlowWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace SQL Server with InMemory
            services.RemoveAll<DbContextOptions<SportFlowDbContext>>();
            services.RemoveAll<SportFlowDbContext>();

            services.AddDbContext<SportFlowDbContext>(options =>
                options.UseInMemoryDatabase("SportFlowIntegrationTest_" + Guid.NewGuid()));

            // Seed test data after DB is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SportFlowDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedTestData(SportFlowDbContext db)
    {
        var tenantId = TenantId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var hasher = new PasswordHasher<User>();

        // Create user with placeholder hash, then update with real hash
        var user = User.Create("member@demo.com", "placeholder", SystemRoles.Member);
        var hash = hasher.HashPassword(user, "Password1!");
        user.UpdatePasswordHash(hash);

        var role = UserTenantRole.Create(user.Id, tenantId, SystemRoles.Member);

        db.Users.Add(user);
        db.UserTenantRoles.Add(role);
        db.SaveChanges();
    }
}
