using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Entities;
using SkillLedger.Infrastructure.Configurations;

namespace SkillLedger.Infrastructure.Data;

public class SkillLedgerDbContext : IdentityDbContext<User, Role, Guid>
{
    public SkillLedgerDbContext(DbContextOptions<SkillLedgerDbContext> options) : base(options)
    {
    }

    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<PrivacyRequest> PrivacyRequests { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<UserSkill> UserSkills { get; set; }
    public DbSet<Experience> Experiences { get; set; }
    public DbSet<ExperienceSkill> ExperienceSkills { get; set; }
    public DbSet<SkillEndorsement> SkillEndorsements { get; set; }

    // Project management entities
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectDeliverable> ProjectDeliverables { get; set; }
    public DbSet<ProjectSkill> ProjectSkills { get; set; }
    public DbSet<SavedSearch> SavedSearches { get; set; }

    // Project application entities
    public DbSet<ProjectApplication> ProjectApplications { get; set; }
    public DbSet<ProjectApplicationAttachment> ProjectApplicationAttachments { get; set; }

    // Provider selection entities
    public DbSet<ProviderSelection> ProviderSelections { get; set; }

    // Security and fraud detection entities
    public DbSet<DeviceFingerprint> DeviceFingerprints { get; set; }
    public DbSet<IpGeolocation> IpGeolocations { get; set; }

    // Content moderation entities
    public DbSet<ContentModerationLog> ContentModerationLogs { get; set; }
    public DbSet<CustomBlocklistTerm> CustomBlocklistTerms { get; set; }
    public DbSet<ContentReviewQueue> ContentReviewQueues { get; set; }

    // Media upload entities
    public DbSet<UploadedFile> UploadedFiles { get; set; }

    // Credit wallet entities
    public DbSet<CreditWallet> CreditWallets { get; set; }
    public DbSet<CreditTransaction> CreditTransactions { get; set; }
    public DbSet<CreditTransfer> CreditTransfers { get; set; }
    public DbSet<UserCreditReport> UserCreditReports { get; set; }

    // Subscription entities
    public DbSet<SubscriptionTier> SubscriptionTiers { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<SubscriptionTransaction> SubscriptionTransactions { get; set; }
    public DbSet<ProcessedStripeWebhookEvent> ProcessedStripeWebhookEvents { get; set; }

    // Project escrow entities
    public DbSet<ProjectEscrow> ProjectEscrows { get; set; }
    public DbSet<EscrowMilestone> EscrowMilestones { get; set; }

    // Project workspace entities
    public DbSet<ProjectWorkspace> ProjectWorkspaces { get; set; }

    // Messaging entities
    public DbSet<WorkspaceMessage> WorkspaceMessages { get; set; }
    public DbSet<MessageReaction> MessageReactions { get; set; }
    public DbSet<TypingIndicator> TypingIndicators { get; set; }

    // Document management entities
    public DbSet<WorkspaceDocument> WorkspaceDocuments { get; set; }
    public DbSet<DocumentFolder> DocumentFolders { get; set; }
    public DbSet<DocumentAccess> DocumentAccesses { get; set; }
    public DbSet<DocumentShare> DocumentShares { get; set; }

    // Milestone tracking entities
    public DbSet<ProjectMilestone> ProjectMilestones { get; set; }
    public DbSet<DeliverableSubmission> DeliverableSubmissions { get; set; }

    // Project review entities
    public DbSet<ProjectReview> ProjectReviews { get; set; }

    // Reputation system entities
    public DbSet<UserReputationScore> UserReputationScores { get; set; }
    public DbSet<CategoryReputationScore> CategoryReputationScores { get; set; }
    public DbSet<ReputationHistory> ReputationHistories { get; set; }

    // Anti-gaming fraud detection entities
    public DbSet<AntiGamingAlert> AntiGamingAlerts { get; set; }
    public DbSet<UserBehaviorMetric> UserBehaviorMetrics { get; set; }
    public DbSet<UserNetworkConnection> UserNetworkConnections { get; set; }
    public DbSet<UserSanction> UserSanctions { get; set; }
    public DbSet<GamingRiskAssessment> GamingRiskAssessments { get; set; }

    // Badge system entities
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<BadgeDefinition> BadgeDefinitions { get; set; }
    public DbSet<BadgeCriteria> BadgeCriteria { get; set; }
    public DbSet<BadgeEarningHistory> BadgeEarningHistory { get; set; }
    public DbSet<VerificationRequest> VerificationRequests { get; set; }

    // Questionnaire system entities
    public DbSet<Questionnaire> Questionnaires { get; set; }
    public DbSet<QuestionnaireQuestion> QuestionnaireQuestions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<QuestionnaireResponse> QuestionnaireResponses { get; set; }
    public DbSet<QuestionResponse> QuestionResponses { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply entity configurations
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new PasswordResetConfiguration());
        builder.ApplyConfiguration(new PrivacyRequestConfiguration());
        builder.ApplyConfiguration(new AuditLogConfiguration());

        // Apply RBAC configurations
        builder.ApplyConfiguration(new PermissionConfiguration());
        builder.ApplyConfiguration(new RolePermissionConfiguration());

        // Apply profile configuration
        builder.ApplyConfiguration(new ProfileConfiguration());

        // Apply skill and experience configurations
        builder.ApplyConfiguration(new SkillConfiguration());
        builder.ApplyConfiguration(new UserSkillConfiguration());
        builder.ApplyConfiguration(new ExperienceConfiguration());
        builder.ApplyConfiguration(new ExperienceSkillConfiguration());
        builder.ApplyConfiguration(new SkillEndorsementConfiguration());

        // Apply project management configurations
        builder.ApplyConfiguration(new ProjectConfiguration());
        builder.ApplyConfiguration(new ProjectDeliverableConfiguration());
        builder.ApplyConfiguration(new ProjectSkillConfiguration());

        // Apply project application configurations
        builder.ApplyConfiguration(new ProjectApplicationConfiguration());
        builder.ApplyConfiguration(new ProjectApplicationAttachmentConfiguration());

        // Apply provider selection configurations
        builder.ApplyConfiguration(new ProviderSelectionConfiguration());

        // Apply security and fraud detection configurations
        builder.ApplyConfiguration(new DeviceFingerprintConfiguration());
        builder.ApplyConfiguration(new IpGeolocationConfiguration());

        // Apply content moderation configurations
        builder.ApplyConfiguration(new ContentModerationLogConfiguration());
        builder.ApplyConfiguration(new CustomBlocklistTermConfiguration());
        builder.ApplyConfiguration(new ContentReviewQueueConfiguration());

        // Apply media upload configurations
        builder.ApplyConfiguration(new UploadedFileConfiguration());

        // Apply credit wallet configurations
        builder.ApplyConfiguration(new CreditWalletConfiguration());
        builder.ApplyConfiguration(new CreditTransactionConfiguration());
        builder.ApplyConfiguration(new CreditTransferConfiguration());
        builder.ApplyConfiguration(new UserCreditReportConfiguration());

        // Apply subscription configurations
        builder.ApplyConfiguration(new SubscriptionTierConfiguration());
        builder.ApplyConfiguration(new UserSubscriptionConfiguration());
        builder.ApplyConfiguration(new PaymentMethodConfiguration());
        builder.ApplyConfiguration(new SubscriptionTransactionConfiguration());
        builder.ApplyConfiguration(new ProcessedStripeWebhookEventConfiguration());

        // Apply project escrow configurations
        builder.ApplyConfiguration(new ProjectEscrowConfiguration());
        builder.ApplyConfiguration(new EscrowMilestoneConfiguration());

        // Apply project workspace configurations
        builder.ApplyConfiguration(new ProjectWorkspaceConfiguration());

        // Apply messaging configurations
        builder.ApplyConfiguration(new WorkspaceMessageConfiguration());
        builder.ApplyConfiguration(new MessageReactionConfiguration());
        builder.ApplyConfiguration(new TypingIndicatorConfiguration());

        // Apply document management configurations
        builder.ApplyConfiguration(new WorkspaceDocumentConfiguration());
        builder.ApplyConfiguration(new DocumentFolderConfiguration());
        builder.ApplyConfiguration(new DocumentAccessConfiguration());
        builder.ApplyConfiguration(new DocumentShareConfiguration());

        // Apply milestone tracking configurations
        builder.ApplyConfiguration(new ProjectMilestoneConfiguration());
        builder.ApplyConfiguration(new DeliverableSubmissionConfiguration());

        // Apply project review configurations
        builder.ApplyConfiguration(new ProjectReviewConfiguration());

        // Apply reputation system configurations
        builder.ApplyConfiguration(new UserReputationScoresConfiguration());
        builder.ApplyConfiguration(new CategoryReputationScoresConfiguration());
        builder.ApplyConfiguration(new ReputationHistoryConfiguration());

        // Apply anti-gaming fraud detection configurations
        builder.ApplyConfiguration(new AntiGamingAlertConfiguration());
        builder.ApplyConfiguration(new UserBehaviorMetricConfiguration());
        builder.ApplyConfiguration(new UserNetworkConnectionConfiguration());
        builder.ApplyConfiguration(new UserSanctionConfiguration());
        builder.ApplyConfiguration(new GamingRiskAssessmentConfiguration());

        // Apply badge system configurations
        builder.ApplyConfiguration(new UserBadgeConfiguration());
        builder.ApplyConfiguration(new BadgeDefinitionConfiguration());
        builder.ApplyConfiguration(new BadgeCriteriaConfiguration());
        builder.ApplyConfiguration(new BadgeEarningHistoryConfiguration());
        builder.ApplyConfiguration(new VerificationRequestConfiguration());

        // Apply questionnaire system configurations
        builder.ApplyConfiguration(new QuestionnaireConfiguration());
        builder.ApplyConfiguration(new QuestionnaireQuestionConfiguration());
        builder.ApplyConfiguration(new QuestionOptionConfiguration());
        builder.ApplyConfiguration(new QuestionnaireResponseConfiguration());
        builder.ApplyConfiguration(new QuestionResponseConfiguration());



        // Customize Identity table names
        builder.Entity<User>().ToTable("Users");
        builder.Entity<Role>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
    }
}
