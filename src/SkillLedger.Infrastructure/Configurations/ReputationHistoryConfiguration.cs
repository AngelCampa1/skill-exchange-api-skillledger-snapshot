using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class ReputationHistoryConfiguration : IEntityTypeConfiguration<ReputationHistory>
{
    public void Configure(EntityTypeBuilder<ReputationHistory> builder)
    {
        builder.ToTable("ReputationHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(h => h.UserId)
            .IsRequired();

        builder.Property(h => h.Date)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(h => h.Score)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(h => h.ChangeReason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(h => h.ProjectId)
            .IsRequired(false);

        builder.Property(h => h.ReviewId)
            .IsRequired(false);

        // Relationships
        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Project)
            .WithMany()
            .HasForeignKey(h => h.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(h => h.Review)
            .WithMany()
            .HasForeignKey(h => h.ReviewId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(h => h.UserId)
            .HasDatabaseName("IX_ReputationHistories_UserId");

        builder.HasIndex(h => h.Date)
            .HasDatabaseName("IX_ReputationHistories_Date");

        builder.HasIndex(h => new { h.UserId, h.Date })
            .HasDatabaseName("IX_ReputationHistories_UserDate");

        builder.HasIndex(h => h.ProjectId)
            .HasDatabaseName("IX_ReputationHistories_ProjectId");

        builder.HasIndex(h => h.ReviewId)
            .HasDatabaseName("IX_ReputationHistories_ReviewId");
    }
}