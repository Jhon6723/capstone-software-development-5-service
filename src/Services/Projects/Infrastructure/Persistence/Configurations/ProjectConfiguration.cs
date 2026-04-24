using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixPro.Services.Projects.Domain.Entities;
using PixPro.Services.Projects.Domain.ValueObjects;

namespace PixPro.Services.Projects.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasColumnType($"varchar({ProjectName.MaxLength})")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.HasIndex(p => p.OwnerId);
    }
}
