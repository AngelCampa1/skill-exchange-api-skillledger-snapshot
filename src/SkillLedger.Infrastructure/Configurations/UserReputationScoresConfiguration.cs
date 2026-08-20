using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class UserReputationScoresConfiguration : IEntityTypeConfiguration<UserReputationScore>
{
    public void Configure(EntityTypeBuilder<UserReputationScore> builder)
    {
        builder.ToTable("UserReputationScores");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(u => u.UserId)
            .IsRequired();

        builder.Property(u => u.OverallScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(u => u.ProjectCompletionRate)
            .HasPrecision(5, 4)
            .IsRequired();

        builder.Property(u => u.AverageResponseTime)
            .IsRequired();

        builder.Property(u => u.TotalProjectsCompleted)
            .IsRequired();

        builder.Property(u => u.LastUpdated)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(u => u.User)
            .WithOne()
            .HasForeignKey<UserReputationScore>(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(u => u.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserReputationScores_UserId");

        builder.HasIndex(u => u.OverallScore)
            .HasDatabaseName("IX_UserReputationScores_OverallScore");

        builder.HasIndex(u => u.LastUpdated)
            .HasDatabaseName("IX_UserReputationScores_LastUpdated");
    }
}