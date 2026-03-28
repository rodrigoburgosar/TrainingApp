using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Infrastructure.Persistence.Configurations;

public class RefreshTokensConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(r => r.TenantId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? TenantId.From(value.Value) : null);

        builder.Property(r => r.TokenHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(r => r.IpAddress)
            .HasMaxLength(45);

        builder.Property(r => r.UserAgent)
            .HasMaxLength(512);

        builder.HasIndex(r => r.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_TokenHash");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(r => r.IsExpired);
        builder.Ignore(r => r.IsRevoked);
        builder.Ignore(r => r.IsActive);
    }
}
