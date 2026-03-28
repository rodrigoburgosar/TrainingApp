using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportFlow.Domain.Identity;
using SportFlow.Domain.Shared.ValueObjects;

namespace SportFlow.Infrastructure.Persistence.Configurations;

public class UsersConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.SystemRole)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.EmailVerificationToken)
            .HasMaxLength(100);

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(100);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("UX_Users_Email");

        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
