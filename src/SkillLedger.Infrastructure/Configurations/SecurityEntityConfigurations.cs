using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for DeviceFingerprint
/// </summary>
public class DeviceFingerprintConfiguration : IEntityTypeConfiguration<DeviceFingerprint>
{
    public void Configure(EntityTypeBuilder<DeviceFingerprint> builder)
    {
        builder.ToTable("DeviceFingerprints");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FingerprintHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(d => d.UserAgent)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.CountryCode)
            .HasMaxLength(2);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.LastSeenAt)
            .IsRequired();

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => d.FingerprintHash);
        builder.HasIndex(d => d.UserId);
        builder.HasIndex(d => d.CreatedAt);
    }
}

/// <summary>
/// Entity configuration for IpGeolocation
/// </summary>
public class IpGeolocationConfiguration : IEntityTypeConfiguration<IpGeolocation>
{
    public void Configure(EntityTypeBuilder<IpGeolocation> builder)
    {
        builder.ToTable("IpGeolocations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.IpAddressHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(i => i.CountryName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.City)
            .HasMaxLength(100);

        builder.Property(i => i.Region)
            .HasMaxLength(100);

        builder.Property(i => i.Timezone)
            .HasMaxLength(50);

        builder.Property(i => i.Isp)
            .HasMaxLength(200);

        builder.HasIndex(i => i.IpAddressHash)
            .IsUnique();

        builder.HasIndex(i => i.ExpiresAt);
    }
}

