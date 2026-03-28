using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportFlow.Domain.Shared.ValueObjects;
using SportFlow.Domain.Tenants;

namespace SportFlow.Infrastructure.Persistence.Configurations;

public class LocationsConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, value => LocationId.From(value))
            .ValueGeneratedNever();

        builder.Property(l => l.TenantId)
            .HasConversion(id => id.Value, value => TenantId.From(value))
            .IsRequired();

        builder.Property(l => l.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Address)
            .HasMaxLength(500);

        builder.Property(l => l.Timezone)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(l => new { l.TenantId, l.Name })
            .HasDatabaseName("IX_Locations_TenantId_Name");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(l => l.IsActive);
    }
}
