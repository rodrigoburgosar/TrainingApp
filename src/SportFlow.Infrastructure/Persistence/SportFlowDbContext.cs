using Microsoft.EntityFrameworkCore;
using SportFlow.Application.Abstractions;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Tenants;

namespace SportFlow.Infrastructure.Persistence;

public class SportFlowDbContext(
    DbContextOptions<SportFlowDbContext> options,
    ITenantContext? tenantContext = null) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserTenantRole> UserTenantRoles => Set<UserTenantRole>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Location> Locations => Set<Location>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SportFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);
}
