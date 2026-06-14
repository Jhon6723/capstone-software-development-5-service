using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixPro.Services.Projects.Domain.Entities;

namespace PixPro.Services.Projects.Infrastructure.Persistence.Configurations;

public sealed class UserCreditConfiguration : IEntityTypeConfiguration<UserCredit>
{
    public void Configure(EntityTypeBuilder<UserCredit> builder)
    {
        builder.ToTable("user_credits");

        builder.HasKey(uc => uc.Id);

        builder.Property(uc => uc.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(uc => uc.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(uc => uc.ModelTier)
            .HasColumnName("model_tier")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(uc => uc.CreditsRemaining)
            .HasColumnName("credits_remaining")
            .IsRequired();

        builder.Property(uc => uc.CreditsTotal)
            .HasColumnName("credits_total")
            .IsRequired();

        builder.Property(uc => uc.SubscriptionTier)
            .HasColumnName("subscription_tier")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(uc => uc.ResetAt)
            .HasColumnName("reset_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(uc => uc.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(uc => uc.FluxDailyCount)
            .HasColumnName("flux_daily_count")
            .HasColumnType("int")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(uc => uc.FluxDailyResetAt)
            .HasColumnName("flux_daily_reset_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.HasIndex(uc => new { uc.UserId, uc.ModelTier })
            .IsUnique();

        builder.HasIndex(uc => uc.UserId);

        // Use PostgreSQL's system column "xmin" as an optimistic concurrency token.
        // EF Core checks it on every UPDATE: if another transaction modified the row
        // in between, SaveChanges throws DbUpdateConcurrencyException. "xmin" is a
        // hidden system column, so no extra column or migration is required.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
