using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Primary key
        builder.HasKey(al => al.Id);

        // Properties
        builder.Property(al => al.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.Details)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(al => al.IPAddress)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(al => al.Timestamp)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(al => al.Success)
            .IsRequired();

        builder.Property(al => al.ErrorMessage)
            .HasMaxLength(1000)
            .IsRequired(false);

        // Indexes for performance
        builder.HasIndex(al => al.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(al => al.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(al => al.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(al => al.IPAddress)
            .HasDatabaseName("IX_AuditLogs_IPAddress");

        // Composite index for common queries
        builder.HasIndex(al => new { al.IPAddress, al.Timestamp, al.Success })
            .HasDatabaseName("IX_AuditLogs_IPAddress_Timestamp_Success");

        // Performance index for user activity history queries
        builder.HasIndex(al => new { al.UserId, al.Timestamp })
            .IsDescending(false, true)  // UserId ASC, Timestamp DESC
            .HasDatabaseName("IX_AuditLogs_UserId_Timestamp");

        // Relationships
        builder.HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}