using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class BadgeEarningHistoryConfiguration : IEntityTypeConfiguration<BadgeEarningHistory>
{
    public void Configure(EntityTypeBuilder<BadgeEarningHistory> builder)
    {
        builder.ToTable("BadgeEarningHistory");

        builder.HasKey(beh => beh.Id);

        builder.Property(beh => beh.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(beh => beh.UserId)
            .IsRequired();

        builder.Property(beh => beh.BadgeId)
            .IsRequired();

        builder.Property(beh => beh.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(beh => beh.Reason)
            .HasMaxLength(500);

        builder.Property(beh => beh.Evidence)
            .HasColumnType("nvarchar(max)");

        builder.Property(beh => beh.ActionBy);

        builder.Property(beh => beh.ActionAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(beh => beh.User)
            .WithMany(u => u.BadgeHistory)
            .HasForeignKey(beh => beh.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(beh => beh.Badge)
            .WithMany()
            .HasForeignKey(beh => beh.BadgeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(beh => beh.ActionByUser)
            .WithMany()
            .HasForeignKey(beh => beh.ActionBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(beh => beh.UserId)
            .HasDatabaseName("IX_BadgeEarningHistory_UserId");

        builder.HasIndex(beh => beh.BadgeId)
            .HasDatabaseName("IX_BadgeEarningHistory_BadgeId");

        builder.HasIndex(beh => beh.Action)
            .HasDatabaseName("IX_BadgeEarningHistory_Action");

        builder.HasIndex(beh => beh.ActionAt)
            .HasDatabaseName("IX_BadgeEarningHistory_ActionAt");

        builder.HasIndex(beh => new { beh.UserId, beh.ActionAt })
            .HasDatabaseName("IX_BadgeEarningHistory_UserId_ActionAt");
    }
}