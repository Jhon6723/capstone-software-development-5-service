using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixPro.Services.Projects.Domain.Entities;

namespace PixPro.Services.Projects.Infrastructure.Persistence.Configurations;

public sealed class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("images");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(i => i.FileName)
            .HasColumnName("file_name")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(i => i.ContentType)
            .HasColumnName("content_type")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(i => i.FilePath)
            .HasColumnName("file_path")
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(i => i.CloudinaryPublicId)
            .HasColumnName("cloudinary_public_id")
            .HasColumnType("varchar(512)")
            .IsRequired();

        builder.Property(i => i.SecureUrl)
            .HasColumnName("secure_url")
            .HasColumnType("varchar(1024)")
            .IsRequired();

        builder.Property(i => i.Format)
            .HasColumnName("format")
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(i => i.SizeInBytes)
            .HasColumnName("size_in_bytes")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(i => i.Width)
            .HasColumnName("width")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(i => i.Height)
            .HasColumnName("height")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(20)")
            .HasDefaultValue("Pending")
            .IsRequired();

        builder.Property(i => i.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(i => i.OwnerId);
    }
}
