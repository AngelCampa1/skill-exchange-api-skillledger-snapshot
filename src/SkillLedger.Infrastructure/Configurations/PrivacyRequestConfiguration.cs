using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class PrivacyRequestConfiguration : IEntityTypeConfiguration<PrivacyRequest>
{
    public void Configure(EntityTypeBuilder<PrivacyRequest> builder)
    {
        builder.ToTable("PrivacyRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.RequestedFromIp)
            .HasMaxLength(45);

        builder.Property(r => r.UserAgent)
            .HasMaxLength(500);

        builder.Property(r => r.ConfirmationTokenHash)
            .HasMaxLength(128);

        builder.HasIndex(r => new { r.UserId, r.RequestType, r.Status })
            .HasDatabaseName("IX_PrivacyRequests_User_Type_Status");

        builder.HasIndex(r => r.RequestedAt)
            .HasDatabaseName("IX_PrivacyRequests_RequestedAt");

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
