using Microsoft.AspNetCore.Identity;
using SkillLedger.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class User : IdentityUser<Guid>
{
    public User()
    {
        Id = Guid.NewGuid();
        SecurityStamp = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// User's first name
    /// </summary>
    [MaxLength(50)]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [MaxLength(50)]
    public string? LastName { get; set; }

    /// <summary>
    /// User's current verification/compliance status
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;


    /// <summary>
    /// Whether the user has completed tax compliance setup
    /// </summary>
    public bool TaxCompliant { get; set; } = false;

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which the account was created (for audit purposes)
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? CreatedFromIP { get; set; }

    /// <summary>
    /// When the user account was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which the last update was made
    /// </summary>
    [MaxLength(45)]
    public string? UpdatedFromIP { get; set; }

    /// <summary>
    /// Number of failed login attempts (for rate limiting)
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// When the account was last locked out (if applicable)
    /// </summary>
    public DateTime? LastLockoutAt { get; set; }

    /// <summary>
    /// External customer ID from payment provider (e.g., Stripe)
    /// </summary>
    [MaxLength(200)]
    public string? ExternalCustomerId { get; set; }

    /// <summary>
    /// Navigation property for audit logs
    /// </summary>
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    /// <summary>
    /// Navigation property for user profile
    /// </summary>
    public virtual Profile? Profile { get; set; }

    /// <summary>
    /// Navigation property for user skills
    /// </summary>
    public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();

    /// <summary>
    /// Navigation property for user experiences
    /// </summary>
    public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();

    /// <summary>
    /// Navigation property for endorsements given by this user
    /// </summary>
    public virtual ICollection<SkillEndorsement> GivenEndorsements { get; set; } = new List<SkillEndorsement>();

    /// <summary>
    /// Navigation property for projects created by this user (as client)
    /// </summary>
    public virtual ICollection<Project> ClientProjects { get; set; } = new List<Project>();

    /// <summary>
    /// Navigation property for badges earned by this user
    /// </summary>
    public virtual ICollection<UserBadge> Badges { get; set; } = new List<UserBadge>();

    /// <summary>
    /// Navigation property for badges verified by this user
    /// </summary>
    public virtual ICollection<UserBadge> VerifiedBadges { get; set; } = new List<UserBadge>();

    /// <summary>
    /// Navigation property for verification requests made by this user
    /// </summary>
    public virtual ICollection<VerificationRequest> VerificationRequests { get; set; } = new List<VerificationRequest>();

    /// <summary>
    /// Navigation property for verification requests reviewed by this user
    /// </summary>
    public virtual ICollection<VerificationRequest> ReviewedVerificationRequests { get; set; } = new List<VerificationRequest>();

    /// <summary>
    /// Navigation property for badge earning history for this user
    /// </summary>
    public virtual ICollection<BadgeEarningHistory> BadgeHistory { get; set; } = new List<BadgeEarningHistory>();

    /// <summary>
    /// Navigation property for user subscriptions
    /// </summary>
    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();

    /// <summary>
    /// Navigation property for user payment methods
    /// </summary>
    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
}