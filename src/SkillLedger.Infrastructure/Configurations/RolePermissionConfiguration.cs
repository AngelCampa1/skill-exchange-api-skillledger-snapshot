using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillLedger.Core.Entities;

namespace SkillLedger.Infrastructure.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.RoleId)
            .IsRequired();

        builder.Property(rp => rp.PermissionId)
            .IsRequired();

        builder.Property(rp => rp.GrantedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(rp => rp.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.GrantedByUser)
            .WithMany()
            .HasForeignKey(rp => rp.GrantedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique constraint - one permission per role
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_RolePermissions_RoleId_PermissionId");

        // Index on IsActive for filtering
        builder.HasIndex(rp => rp.IsActive)
            .HasDatabaseName("IX_RolePermissions_IsActive");

        // Index on GrantedAt for audit queries
        builder.HasIndex(rp => rp.GrantedAt)
            .HasDatabaseName("IX_RolePermissions_GrantedAt");
    }
}