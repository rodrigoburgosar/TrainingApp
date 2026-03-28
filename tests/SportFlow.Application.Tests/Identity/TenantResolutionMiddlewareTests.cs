using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shouldly;
using SportFlow.API.Middleware;
using SportFlow.Domain.Identity;
using SportFlow.Infrastructure.Services;

namespace SportFlow.Application.Tests.Identity;

public class TenantResolutionMiddlewareTests
{
    private static readonly Guid UserGuid = Guid.NewGuid();
    private static readonly Guid TenantGuid = Guid.NewGuid();

    private static HttpContext CreateHttpContext(IEnumerable<Claim>? claims = null)
    {
        var context = new DefaultHttpContext();

        if (claims is not null)
        {
            var identity = new ClaimsIdentity(claims, "TestAuth");
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }

    private static TenantContext CreateTenantContext() => new();

    [Fact]
    public async Task Invoke_AuthenticatedUserWithTenant_PopulatesTenantContext()
    {
        var tenantContext = CreateTenantContext();
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        var claims = new[]
        {
            new Claim("sub", UserGuid.ToString()),
            new Claim("role", SystemRoles.Coach),
            new Claim("tenant_id", TenantGuid.ToString()),
            new Claim("tenant_slug", "demo")
        };

        var context = CreateHttpContext(claims);

        await middleware.InvokeAsync(context, tenantContext);

        tenantContext.UserId.Value.ShouldBe(UserGuid);
        tenantContext.Role.ShouldBe(SystemRoles.Coach);
        tenantContext.TenantId!.Value.Value.ShouldBe(TenantGuid);
        tenantContext.TenantSlug.ShouldBe("demo");
    }

    [Fact]
    public async Task Invoke_SuperAdminWithoutTenant_PopulatesContextWithNoTenant()
    {
        var tenantContext = CreateTenantContext();
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        var claims = new[]
        {
            new Claim("sub", UserGuid.ToString()),
            new Claim("role", SystemRoles.SuperAdmin)
        };

        var context = CreateHttpContext(claims);

        await middleware.InvokeAsync(context, tenantContext);

        tenantContext.UserId.Value.ShouldBe(UserGuid);
        tenantContext.Role.ShouldBe(SystemRoles.SuperAdmin);
        tenantContext.TenantId.ShouldBeNull();
        tenantContext.IsSuperAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task Invoke_UnauthenticatedRequest_DoesNotPopulateContext()
    {
        var tenantContext = CreateTenantContext();
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask);

        var context = CreateHttpContext(); // No claims

        await middleware.InvokeAsync(context, tenantContext);

        tenantContext.Role.ShouldBeEmpty();
    }
}
