using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Infrastructure.Persistence.Configurations;

public class TenantsConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Plan)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.Slug)
            .IsUnique()
            .HasDatabaseName("UX_Tenants_Slug");

        builder.HasQueryFilter(t => t.Status != "cancelled");
    }
}
