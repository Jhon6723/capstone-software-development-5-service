using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixPro.Services.Auth.Domain.Entities;
using PixPro.Services.Auth.Domain.Enums;

namespace PixPro.Services.Auth.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(u => u.Auth0Id)
            .HasColumnName("auth0_id")
            .HasColumnType("varchar(128)")
            .IsRequired(false);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(256)")
            .IsRequired();

        builder.Property(u => u.Password)
            .HasColumnName("Password")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasColumnType("int")
            .IsRequired()
            .HasDefaultValue(UserRole.User);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(u => u.Auth0Id)
            .IsUnique();

        builder.HasIndex(u => u.Email)
            .IsUnique();
    }
}
