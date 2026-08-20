using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for anti-gaming fraud detection entities
/// </summary>

public class AntiGamingAlertConfiguration : IEntityTypeConfiguration<AntiGamingAlert>
{
    public void Configure(EntityTypeBuilder<AntiGamingAlert> builder)
    {
        builder.ToTable("AntiGamingAlerts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.AlertType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Severity)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Evidence)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(AlertStatus.Open)
            .HasSentinel(AlertStatus.Open);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.ResolvedAt);

        builder.Property(x => x.ResolvedBy);

        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(2000);

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ResolvedByUser)
            .WithMany()
            .HasForeignKey(x => x.ResolvedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_AntiGamingAlerts_UserId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_AntiGamingAlerts_Status");

        builder.HasIndex(x => x.Severity)
            .HasDatabaseName("IX_AntiGamingAlerts_Severity");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_AntiGamingAlerts_CreatedAt");

        builder.HasIndex(x => x.AlertType)
            .HasDatabaseName("IX_AntiGamingAlerts_AlertType");
    }
}

public class UserBehaviorMetricConfiguration : IEntityTypeConfiguration<UserBehaviorMetric>
{
    public void Configure(EntityTypeBuilder<UserBehaviorMetric> builder)
    {
        builder.ToTable("UserBehaviorMetrics");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.MetricName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.MetricValue)
            .IsRequired()
            .HasColumnType("decimal(18,6)");

        builder.Property(x => x.CalculationWindow)
            .HasMaxLength(50);

        builder.Property(x => x.CalculatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.IsAnomaly)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserBehaviorMetrics_UserId");

        builder.HasIndex(x => x.MetricName)
            .HasDatabaseName("IX_UserBehaviorMetrics_MetricName");

        builder.HasIndex(x => x.CalculatedAt)
            .HasDatabaseName("IX_UserBehaviorMetrics_CalculatedAt");

        builder.HasIndex(x => x.IsAnomaly)
            .HasDatabaseName("IX_UserBehaviorMetrics_IsAnomaly");

        builder.HasIndex(x => new { x.UserId, x.MetricName, x.CalculatedAt })
            .HasDatabaseName("IX_UserBehaviorMetrics_User_Metric_Date");
    }
}

public class UserNetworkConnectionConfiguration : IEntityTypeConfiguration<UserNetworkConnection>
{
    public void Configure(EntityTypeBuilder<UserNetworkConnection> builder)
    {
        builder.ToTable("UserNetworkConnections", t =>
            t.HasCheckConstraint("CK_UserNetworkConnections_DifferentUsers", "[User1Id] != [User2Id]"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.User1Id)
            .IsRequired();

        builder.Property(x => x.User2Id)
            .IsRequired();

        builder.Property(x => x.ConnectionType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ConnectionStrength)
            .IsRequired()
            .HasColumnType("decimal(18,6)");

        builder.Property(x => x.InteractionCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastInteractionAt)
            .IsRequired(false);

        builder.Property(x => x.Metadata)
            .IsRequired(false);

        builder.Property(x => x.DetectedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.IsValidated)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(x => x.User1)
            .WithMany()
            .HasForeignKey(x => x.User1Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User2)
            .WithMany()
            .HasForeignKey(x => x.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.User1Id)
            .HasDatabaseName("IX_UserNetworkConnections_User1Id");

        builder.HasIndex(x => x.User2Id)
            .HasDatabaseName("IX_UserNetworkConnections_User2Id");

        builder.HasIndex(x => x.ConnectionType)
            .HasDatabaseName("IX_UserNetworkConnections_ConnectionType");

        builder.HasIndex(x => x.DetectedAt)
            .HasDatabaseName("IX_UserNetworkConnections_DetectedAt");

        builder.HasIndex(x => x.ConnectionStrength)
            .HasDatabaseName("IX_UserNetworkConnections_ConnectionStrength");

        builder.HasIndex(x => x.LastInteractionAt)
            .HasDatabaseName("IX_UserNetworkConnections_LastInteractionAt");

        builder.HasIndex(x => new { x.User1Id, x.User2Id })
            .HasDatabaseName("IX_UserNetworkConnections_UserPair")
            .IsUnique();
    }
}

public class UserSanctionConfiguration : IEntityTypeConfiguration<UserSanction>
{
    public void Configure(EntityTypeBuilder<UserSanction> builder)
    {
        builder.ToTable("UserSanctions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.SanctionType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Severity)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Evidence)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.IssuedBy);

        builder.Property(x => x.IssuedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.ExpiresAt);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(SanctionStatus.Active)
            .HasSentinel(SanctionStatus.Active);

        builder.Property(x => x.AppealNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.AppealSubmittedAt);

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.IssuedByUser)
            .WithMany()
            .HasForeignKey(x => x.IssuedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserSanctions_UserId");

        builder.HasIndex(x => x.SanctionType)
            .HasDatabaseName("IX_UserSanctions_SanctionType");

        builder.HasIndex(x => x.Severity)
            .HasDatabaseName("IX_UserSanctions_Severity");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_UserSanctions_Status");

        builder.HasIndex(x => x.IssuedAt)
            .HasDatabaseName("IX_UserSanctions_IssuedAt");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_UserSanctions_ExpiresAt");
    }
}

public class GamingRiskAssessmentConfiguration : IEntityTypeConfiguration<GamingRiskAssessment>
{
    public void Configure(EntityTypeBuilder<GamingRiskAssessment> builder)
    {
        builder.ToTable("GamingRiskAssessments");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.RiskScore)
            .IsRequired()
            .HasColumnType("decimal(18,6)");

        builder.Property(x => x.RiskFactors)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.DetectedPatterns)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.AnalyzedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(x => x.ModelVersion)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("1.0");

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_GamingRiskAssessments_UserId");

        builder.HasIndex(x => x.RiskScore)
            .HasDatabaseName("IX_GamingRiskAssessments_RiskScore");

        builder.HasIndex(x => x.AnalyzedAt)
            .HasDatabaseName("IX_GamingRiskAssessments_AnalyzedAt");

        builder.HasIndex(x => x.ModelVersion)
            .HasDatabaseName("IX_GamingRiskAssessments_ModelVersion");
    }
}