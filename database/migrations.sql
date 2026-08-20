IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [BadgeDefinitions] (
        [Id] uniqueidentifier NOT NULL,
        [BadgeType] nvarchar(100) NOT NULL,
        [Category] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [IconUrl] nvarchar(500) NULL,
        [RequiredVerification] nvarchar(max) NOT NULL,
        [ExpirationPeriod] time NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [DisplayPriority] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_BadgeDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_BadgeDefinitions_BadgeType] UNIQUE ([BadgeType])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [CarrierInfos] (
        [Id] uniqueidentifier NOT NULL,
        [PhonePrefix] nvarchar(20) NOT NULL,
        [CarrierName] nvarchar(100) NOT NULL,
        [NetworkCode] nvarchar(10) NULL,
        [CountryCode] nvarchar(3) NOT NULL,
        [PhoneType] int NOT NULL,
        [IsVoip] bit NOT NULL,
        [IsPrepaid] bit NOT NULL,
        [CarrierRiskScore] int NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CarrierInfos] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [IpGeolocations] (
        [Id] uniqueidentifier NOT NULL,
        [IpAddressHash] nvarchar(256) NOT NULL,
        [CountryCode] nvarchar(2) NOT NULL,
        [CountryName] nvarchar(100) NOT NULL,
        [City] nvarchar(100) NULL,
        [Region] nvarchar(100) NULL,
        [Timezone] nvarchar(50) NULL,
        [Isp] nvarchar(200) NULL,
        [IsVpn] bit NOT NULL,
        [IsProxy] bit NOT NULL,
        [IsDataCenter] bit NOT NULL,
        [IsRestricted] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        CONSTRAINT [PK_IpGeolocations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Category] nvarchar(100) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [RevokedTokens] (
        [Id] uniqueidentifier NOT NULL,
        [TokenId] nvarchar(256) NOT NULL,
        [RevokedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ExpiresAt] datetime2 NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [RevokedFromIP] nvarchar(45) NULL,
        CONSTRAINT [PK_RevokedTokens] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsSystemRole] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Priority] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [Skills] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Category] nvarchar(50) NOT NULL,
        [IsSystemManaged] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Skills] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [EmailVerified] bit NOT NULL DEFAULT CAST(0 AS bit),
        [PhoneVerified] bit NOT NULL DEFAULT CAST(0 AS bit),
        [TaxCompliant] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedFromIP] nvarchar(45) NULL,
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedFromIP] nvarchar(45) NULL,
        [FailedLoginAttempts] int NOT NULL DEFAULT 0,
        [LastLockoutAt] datetime2 NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [BadgeCriteria] (
        [Id] uniqueidentifier NOT NULL,
        [BadgeType] nvarchar(100) NOT NULL,
        [CriteriaName] nvarchar(200) NOT NULL,
        [CriteriaValue] nvarchar(500) NOT NULL,
        [CriteriaExpression] nvarchar(max) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [Priority] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_BadgeCriteria] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BadgeCriteria_BadgeDefinitions_BadgeType] FOREIGN KEY ([BadgeType]) REFERENCES [BadgeDefinitions] ([BadgeType]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [AntiGamingAlerts] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [AlertType] nvarchar(100) NOT NULL,
        [Severity] nvarchar(450) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Evidence] nvarchar(max) NULL,
        [Status] nvarchar(450) NOT NULL DEFAULT N'Open',
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ResolvedAt] datetime2 NULL,
        [ResolvedBy] uniqueidentifier NULL,
        [ResolutionNotes] nvarchar(2000) NULL,
        CONSTRAINT [PK_AntiGamingAlerts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AntiGamingAlerts_Users_ResolvedBy] FOREIGN KEY ([ResolvedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AntiGamingAlerts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [CategoryReputationScores] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SkillId] uniqueidentifier NOT NULL,
        [Score] decimal(5,2) NOT NULL,
        [ProjectCount] int NOT NULL,
        [LastProjectAt] datetime2 NULL,
        CONSTRAINT [PK_CategoryReputationScores] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CategoryReputationScores_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CategoryReputationScores_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ContentModerationLogs] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ContentType] int NOT NULL,
        [WasApproved] bit NOT NULL,
        [RiskLevel] int NOT NULL,
        [RequiredHumanReview] bit NOT NULL,
        [FlaggedCategories] nvarchar(max) NULL,
        [ModerationScores] nvarchar(max) NULL,
        [BlockedTerms] nvarchar(max) NULL,
        [ReasonForRejection] nvarchar(500) NULL,
        [AnalysisId] nvarchar(100) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContentModerationLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContentModerationLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ContentReviewQueues] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ContentType] int NOT NULL,
        [ContentText] nvarchar(max) NULL,
        [ContentUrl] nvarchar(500) NULL,
        [ModerationResult] nvarchar(max) NULL,
        [ReviewPriority] int NOT NULL,
        [Status] int NOT NULL,
        [AssignedReviewerId] uniqueidentifier NULL,
        [Decision] int NULL,
        [ReviewComments] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ReviewedAt] datetime2 NULL,
        CONSTRAINT [PK_ContentReviewQueues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContentReviewQueues_Users_AssignedReviewerId] FOREIGN KEY ([AssignedReviewerId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ContentReviewQueues_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [CreditWallets] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [UserId] uniqueidentifier NOT NULL,
        [EncryptedBalance] NVARCHAR(512) NOT NULL,
        [EncryptedPendingBalance] NVARCHAR(512) NOT NULL,
        [EncryptedTotalEarned] NVARCHAR(512) NOT NULL,
        [EncryptedTotalSpent] NVARCHAR(512) NOT NULL,
        [LastTransactionAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [RowVersion] rowversion NOT NULL,
        [KeyIdentifier] nvarchar(128) NOT NULL,
        [IsBlocked] bit NOT NULL,
        [BlockedReason] nvarchar(500) NULL,
        [BlockedAt] datetime2 NULL,
        CONSTRAINT [PK_CreditWallets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CreditWallets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [CustomBlocklistTerms] (
        [Id] uniqueidentifier NOT NULL,
        [Term] nvarchar(200) NOT NULL,
        [AddedByUserId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        CONSTRAINT [PK_CustomBlocklistTerms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomBlocklistTerms_Users_AddedByUserId] FOREIGN KEY ([AddedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [DeviceFingerprints] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [FingerprintHash] nvarchar(256) NOT NULL,
        [IpAddress] nvarchar(45) NOT NULL,
        [UserAgent] nvarchar(500) NOT NULL,
        [CountryCode] nvarchar(2) NULL,
        [UsedForRegistration] bit NOT NULL,
        [IsSuspicious] bit NOT NULL,
        [RiskLevel] int NOT NULL,
        [RiskFactors] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastSeenAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DeviceFingerprints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeviceFingerprints_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [EmailVerifications] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [Token] nvarchar(255) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IsUsed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [VerifiedAt] datetime2 NULL,
        [RequestedFromIP] nvarchar(45) NULL,
        [VerifiedFromIP] nvarchar(45) NULL,
        CONSTRAINT [PK_EmailVerifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmailVerifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [Experiences] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL DEFAULT 1,
        [Title] nvarchar(100) NOT NULL,
        [Organization] nvarchar(100) NOT NULL,
        [Location] nvarchar(100) NULL,
        [Description] nvarchar(2000) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [IsCurrent] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsVisible] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsFeatured] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DisplayOrder] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Experiences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Experiences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [GamingRiskAssessments] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RiskScore] decimal(18,6) NOT NULL,
        [RiskFactors] nvarchar(max) NULL,
        [DetectedPatterns] nvarchar(max) NULL,
        [AnalyzedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ModelVersion] nvarchar(20) NOT NULL DEFAULT N'1.0',
        CONSTRAINT [PK_GamingRiskAssessments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GamingRiskAssessments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [PasswordResets] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Token] nvarchar(256) NOT NULL,
        [TokenHash] nvarchar(512) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [IsUsed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [UsedAt] datetime2 NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(1000) NULL,
        [AttemptCount] int NOT NULL DEFAULT 0,
        [LastAttemptAt] datetime2 NULL,
        CONSTRAINT [PK_PasswordResets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [PhoneFraudLogs] (
        [Id] uniqueidentifier NOT NULL,
        [PhoneNumberHash] nvarchar(256) NOT NULL,
        [UserId] uniqueidentifier NULL,
        [IpAddress] nvarchar(45) NOT NULL,
        [CountryCode] nvarchar(3) NULL,
        [CarrierInfo] nvarchar(max) NULL,
        [RiskLevel] int NOT NULL,
        [RiskFactors] nvarchar(max) NULL,
        [WasBlocked] bit NOT NULL,
        [RequiredManualReview] bit NOT NULL,
        [VerificationSuccessful] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PhoneFraudLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhoneFraudLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [PhoneVerifications] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [VerificationCode] nvarchar(6) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IsUsed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [UsedAt] datetime2 NULL,
        [RequestedFromIP] nvarchar(45) NULL,
        [VerifiedFromIP] nvarchar(45) NULL,
        [AttemptCount] int NOT NULL DEFAULT 0,
        [IsVerified] bit NOT NULL,
        CONSTRAINT [PK_PhoneVerifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhoneVerifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [Profiles] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(50) NULL,
        [LastName] nvarchar(50) NULL,
        [ProfileSlug] nvarchar(100) NULL,
        [Title] nvarchar(100) NULL,
        [Company] nvarchar(100) NULL,
        [Location] nvarchar(100) NULL,
        [Bio] nvarchar(2000) NULL,
        [Summary] nvarchar(500) NULL,
        [WebsiteUrl] nvarchar(255) NULL,
        [LinkedInUrl] nvarchar(255) NULL,
        [GitHubUrl] nvarchar(255) NULL,
        [TwitterUrl] nvarchar(200) NULL,
        [AvatarUrl] nvarchar(500) NULL,
        [TimeZone] nvarchar(50) NULL,
        [Visibility] int NOT NULL,
        [IsPublic] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsComplete] bit NOT NULL DEFAULT CAST(0 AS bit),
        [ViewCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Profiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Profiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [Projects] (
        [Id] uniqueidentifier NOT NULL,
        [ClientId] uniqueidentifier NOT NULL,
        [ProviderId] uniqueidentifier NULL,
        [Title] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [CreditBudget] int NOT NULL DEFAULT 50,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [ModerationStatus] int NOT NULL DEFAULT 0,
        [ModerationNotes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CompletedAt] datetime2 NULL,
        [CancelledAt] datetime2 NULL,
        [CancellationReason] nvarchar(500) NULL,
        [DisputeReason] nvarchar(500) NULL,
        [CreatedFromIP] nvarchar(45) NULL,
        [LocationLatitude] float NULL,
        [LocationLongitude] float NULL,
        [LocationCity] nvarchar(100) NULL,
        [LocationState] nvarchar(100) NULL,
        [LocationCountry] nvarchar(100) NULL,
        [IsRemoteWork] bit NOT NULL,
        [SearchText] nvarchar(max) NULL,
        [ComplexityScore] int NOT NULL,
        [IsUrgent] bit NOT NULL,
        [IsFeatured] bit NOT NULL,
        [Visibility] int NOT NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Projects_CreditBudget] CHECK ([CreditBudget] >= 50 AND [CreditBudget] <= 5000),
        CONSTRAINT [CK_Projects_Timeline] CHECK ([EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] > [StartDate]),
        CONSTRAINT [FK_Projects_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Projects_Users_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [Questionnaires] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsTemplate] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RequiresReview] bit NOT NULL DEFAULT CAST(0 AS bit),
        [MaxResponses] int NULL,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [Version] int NOT NULL DEFAULT 1,
        [Metadata] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Questionnaires] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Questionnaires_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [Token] nvarchar(256) NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ExpiresAt] datetime2 NOT NULL,
        [IsRevoked] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RevokedAt] datetime2 NULL,
        [RevokedByIp] nvarchar(45) NULL,
        [CreatedByIp] nvarchar(45) NULL,
        [UserAgent] nvarchar(512) NULL,
        [LastUsedAt] datetime2 NULL,
        [LastUsedByIp] nvarchar(45) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [Id] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [PermissionId] uniqueidentifier NOT NULL,
        [GrantedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [GrantedByUserId] uniqueidentifier NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RolePermissions_Users_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [SavedSearches] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [SearchCriteriaJson] nvarchar(max) NOT NULL,
        [SearchCriteria] nvarchar(max) NOT NULL,
        [NotificationsEnabled] bit NOT NULL,
        [NotificationFrequency] int NOT NULL,
        [LastNotificationSentAt] datetime2 NULL,
        [ExecutionCount] int NOT NULL,
        [UsageCount] int NOT NULL,
        [LastExecutedAt] datetime2 NULL,
        [LastUsedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SavedSearches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SavedSearches_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [SuspiciousPhones] (
        [Id] uniqueidentifier NOT NULL,
        [PhoneNumberHash] nvarchar(256) NOT NULL,
        [ReportedByUserId] uniqueidentifier NULL,
        [Reason] nvarchar(500) NOT NULL,
        [ReportCount] int NOT NULL,
        [IsBlocked] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastReportedAt] datetime2 NOT NULL,
        [BlockExpiresAt] datetime2 NULL,
        CONSTRAINT [PK_SuspiciousPhones] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SuspiciousPhones_Users_ReportedByUserId] FOREIGN KEY ([ReportedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserBadges] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [BadgeType] nvarchar(100) NOT NULL,
        [BadgeName] nvarchar(200) NOT NULL,
        [BadgeDescription] nvarchar(500) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [IconUrl] nvarchar(500) NULL,
        [EarnedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ExpiresAt] datetime2 NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [VerificationLevel] nvarchar(max) NOT NULL,
        [VerificationEvidence] nvarchar(max) NULL,
        [VerifiedBy] uniqueidentifier NULL,
        [VerifiedAt] datetime2 NULL,
        [IntegrityHash] nvarchar(256) NULL,
        CONSTRAINT [PK_UserBadges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserBadges_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserBadges_Users_VerifiedBy] FOREIGN KEY ([VerifiedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserBehaviorMetrics] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [MetricName] nvarchar(100) NOT NULL,
        [MetricValue] decimal(18,6) NOT NULL,
        [CalculationWindow] nvarchar(50) NULL,
        [CalculatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IsAnomaly] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_UserBehaviorMetrics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserBehaviorMetrics_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserCreditReports] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ReportMonth] int NOT NULL,
        [TotalEarned] int NOT NULL,
        [TotalSpent] int NOT NULL,
        [TransactionCount] int NOT NULL,
        [AverageTransactionSize] decimal(18,2) NOT NULL,
        [GeneratedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [EarningsByType] nvarchar(2000) NULL,
        [SpendingByType] nvarchar(2000) NULL,
        [ProjectEarnings] nvarchar(2000) NULL,
        [PeakBalance] int NOT NULL,
        [LowestBalance] int NOT NULL,
        [StartingBalance] int NOT NULL,
        [EndingBalance] int NOT NULL,
        [UniqueProjectsCount] int NOT NULL,
        [CompletedProjectsCount] int NOT NULL,
        [LargestIncomingTransaction] int NOT NULL,
        [LargestOutgoingTransaction] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [IsFinalized] bit NOT NULL,
        [FinalizedAt] datetime2 NULL,
        CONSTRAINT [PK_UserCreditReports] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UserCreditReports_ValidReportMonth] CHECK ([ReportMonth] >= 190001 AND [ReportMonth] <= 999912),
        CONSTRAINT [CK_UserCreditReports_ValidTotals] CHECK ([TotalEarned] >= 0 AND [TotalSpent] >= 0),
        CONSTRAINT [CK_UserCreditReports_ValidTransactionCount] CHECK ([TransactionCount] >= 0),
        CONSTRAINT [FK_UserCreditReports_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
    DECLARE @defaultSchema AS sysname;
    SET @defaultSchema = SCHEMA_NAME();
    DECLARE @description AS sql_variant;
    SET @description = N'Report month in YYYYMM format';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'ReportMonth';
    SET @description = N'Total credits earned during the month';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'TotalEarned';
    SET @description = N'Total credits spent during the month';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'TotalSpent';
    SET @description = N'Number of transactions during the month';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'TransactionCount';
    SET @description = N'Average transaction amount (calculated field)';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'AverageTransactionSize';
    SET @description = N'When the report was generated';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'GeneratedAt';
    SET @description = N'When the report was last updated';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'UpdatedAt';
    SET @description = N'JSON data of earnings breakdown by transaction type';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'EarningsByType';
    SET @description = N'JSON data of spending breakdown by transaction type';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'SpendingByType';
    SET @description = N'JSON data of project-related earnings';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'UserCreditReports', 'COLUMN', N'ProjectEarnings';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserNetworkConnections] (
        [Id] uniqueidentifier NOT NULL,
        [User1Id] uniqueidentifier NOT NULL,
        [User2Id] uniqueidentifier NOT NULL,
        [ConnectionType] nvarchar(100) NOT NULL,
        [ConnectionStrength] decimal(18,6) NOT NULL,
        [DetectedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IsValidated] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_UserNetworkConnections] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UserNetworkConnections_DifferentUsers] CHECK ([User1Id] != [User2Id]),
        CONSTRAINT [FK_UserNetworkConnections_Users_User1Id] FOREIGN KEY ([User1Id]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserNetworkConnections_Users_User2Id] FOREIGN KEY ([User2Id]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserReputationScores] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [OverallScore] decimal(5,2) NOT NULL,
        [ProjectCompletionRate] decimal(5,4) NOT NULL,
        [AverageResponseTime] int NOT NULL,
        [TotalProjectsCompleted] int NOT NULL,
        [LastUpdated] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_UserReputationScores] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserReputationScores_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserSanctions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SanctionType] nvarchar(100) NOT NULL,
        [Severity] nvarchar(450) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Evidence] nvarchar(max) NULL,
        [IssuedBy] uniqueidentifier NULL,
        [IssuedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ExpiresAt] datetime2 NULL,
        [Status] nvarchar(450) NOT NULL DEFAULT N'Active',
        [AppealNotes] nvarchar(2000) NULL,
        [AppealSubmittedAt] datetime2 NULL,
        CONSTRAINT [PK_UserSanctions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSanctions_Users_IssuedBy] FOREIGN KEY ([IssuedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserSanctions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserSkills] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SkillId] uniqueidentifier NOT NULL,
        [Proficiency] int NOT NULL DEFAULT 1,
        [YearsOfExperience] int NOT NULL DEFAULT 0,
        [Notes] nvarchar(1000) NULL,
        [IsFeatured] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsVisible] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_UserSkills] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSkills_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserSkills_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [VerificationRequests] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [BadgeType] nvarchar(100) NOT NULL,
        [RequestedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [Status] nvarchar(50) NOT NULL DEFAULT N'Pending',
        [SubmittedEvidence] nvarchar(max) NULL,
        [ReviewedBy] uniqueidentifier NULL,
        [ReviewedAt] datetime2 NULL,
        [ReviewNotes] nvarchar(2000) NULL,
        CONSTRAINT [PK_VerificationRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VerificationRequests_Users_ReviewedBy] FOREIGN KEY ([ReviewedBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_VerificationRequests_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ExperienceSkills] (
        [Id] uniqueidentifier NOT NULL,
        [ExperienceId] uniqueidentifier NOT NULL,
        [SkillId] uniqueidentifier NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_ExperienceSkills] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExperienceSkills_Experiences_ExperienceId] FOREIGN KEY ([ExperienceId]) REFERENCES [Experiences] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExperienceSkills_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ProjectApplications] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [ProjectId] uniqueidentifier NOT NULL,
        [ProviderId] uniqueidentifier NOT NULL,
        [CoverLetter] nvarchar(2000) NOT NULL,
        [ProposedTimeline] int NULL,
        [SkillMatchScore] decimal(3,2) NULL,
        [Status] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ReviewedAt] datetime2 NULL,
        [ClientFeedback] nvarchar(1000) NULL,
        [SubmittedFromIP] nvarchar(45) NULL,
        [IsAvailableImmediately] bit NOT NULL DEFAULT CAST(0 AS bit),
        [ProposedBudget] int NULL,
        CONSTRAINT [PK_ProjectApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectApplications_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectApplications_Users_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ProjectDeliverables] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [OrderIndex] int NOT NULL DEFAULT 0,
        [IsRequired] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsCompleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_ProjectDeliverables] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProjectDeliverables_OrderIndex] CHECK ([OrderIndex] >= 0 AND [OrderIndex] <= 100),
        CONSTRAINT [FK_ProjectDeliverables_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ProjectEscrows] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [ClientId] uniqueidentifier NOT NULL,
        [ProviderId] uniqueidentifier NOT NULL,
        [TotalAmount] int NOT NULL,
        [ReleasedAmount] int NOT NULL DEFAULT 0,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CompletedAt] datetime2 NULL,
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedFromIP] nvarchar(45) NULL,
        [Notes] nvarchar(1000) NULL,
        [DisputeReason] nvarchar(1000) NULL,
        [DisputedAt] datetime2 NULL,
        [DisputeResolvedByUserId] uniqueidentifier NULL,
        [DisputeResolvedAt] datetime2 NULL,
        [DisputeResolutionNotes] nvarchar(1000) NULL,
        [RequiresMultiSignature] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_ProjectEscrows] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProjectEscrows_ReleasedAmount_LTE_TotalAmount] CHECK ([ReleasedAmount] <= [TotalAmount]),
        CONSTRAINT [CK_ProjectEscrows_ReleasedAmount_NonNegative] CHECK ([ReleasedAmount] >= 0),
        CONSTRAINT [CK_ProjectEscrows_TotalAmount_Positive] CHECK ([TotalAmount] > 0),
        CONSTRAINT [FK_ProjectEscrows_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectEscrows_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectEscrows_Users_DisputeResolvedByUserId] FOREIGN KEY ([DisputeResolvedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ProjectEscrows_Users_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
    DECLARE @defaultSchema AS sysname;
    SET @defaultSchema = SCHEMA_NAME();
    DECLARE @description AS sql_variant;
    SET @description = N'Total amount of credits in escrow';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'TotalAmount';
    SET @description = N'Amount released to provider so far';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'ReleasedAmount';
    SET @description = N'Current status of escrow account';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'Status';
    SET @description = N'When escrow account was created';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'CreatedAt';
    SET @description = N'When escrow was last updated';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'UpdatedAt';
    SET @description = N'IP address where escrow was created';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'CreatedFromIP';
    SET @description = N'Optional notes about the escrow';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'Notes';
    SET @description = N'Reason for dispute if status is Disputed';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'DisputeReason';
    SET @description = N'Admin notes for dispute resolution';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'DisputeResolutionNotes';
    SET @description = N'Whether escrow requires multi-signature approval';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'ProjectEscrows', 'COLUMN', N'RequiresMultiSignature';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ProjectReviews] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [ReviewerId] uniqueidentifier NOT NULL,
        [RevieweeId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [OverallRating] int NOT NULL,
        [QualityRating] int NULL,
        [CommunicationRating] int NULL,
        [TimelinessRating] int NULL,
        [ProfessionalismRating] int NULL,
        [ReviewText] nvarchar(2000) NOT NULL,
        [ResponseText] nvarchar(1000) NULL,
        [Status] int NOT NULL DEFAULT 0,
        [ModerationStatus] int NOT NULL DEFAULT 0,
        [ModerationNotes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [SubmittedAt] datetime2 NULL,
        [PublishedAt] datetime2 NULL,
        [SubmittedFromIP] nvarchar(45) NULL,
        [HasPhotoAttachments] bit NOT NULL DEFAULT CAST(0 AS bit),
        [PhotoAttachmentCount] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_ProjectReviews] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProjectReviews_CommunicationRating] CHECK ([CommunicationRating] IS NULL OR ([CommunicationRating] >= 1 AND [CommunicationRating] <= 10)),
        CONSTRAINT [CK_ProjectReviews_NoSelfReview] CHECK ([ReviewerId] != [RevieweeId]),
        CONSTRAINT [CK_ProjectReviews_OverallRating] CHECK ([OverallRating] >= 1 AND [OverallRating] <= 10),
        CONSTRAINT [CK_ProjectReviews_PhotoAttachmentCount] CHECK ([PhotoAttachmentCount] >= 0 AND ([HasPhotoAttachments] = 0 OR [PhotoAttachmentCount] > 0)),
        CONSTRAINT [CK_ProjectReviews_ProfessionalismRating] CHECK ([ProfessionalismRating] IS NULL OR ([ProfessionalismRating] >= 1 AND [ProfessionalismRating] <= 10)),
        CONSTRAINT [CK_ProjectReviews_QualityRating] CHECK ([QualityRating] IS NULL OR ([QualityRating] >= 1 AND [QualityRating] <= 10)),
        CONSTRAINT [CK_ProjectReviews_ReviewTextLength] CHECK (LEN(LTRIM(RTRIM([ReviewText]))) >= 25),
        CONSTRAINT [CK_ProjectReviews_TimelinessRating] CHECK ([TimelinessRating] IS NULL OR ([TimelinessRating] >= 1 AND [TimelinessRating] <= 10)),
        CONSTRAINT [FK_ProjectReviews_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectReviews_Users_RevieweeId] FOREIGN KEY ([RevieweeId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectReviews_Users_ReviewerId] FOREIGN KEY ([ReviewerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ProjectSkills] (
        [ProjectId] uniqueidentifier NOT NULL,
        [SkillId] uniqueidentifier NOT NULL,
        [ProficiencyRequired] int NOT NULL DEFAULT 2,
        [Weight] int NOT NULL DEFAULT 3,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_ProjectSkills] PRIMARY KEY ([ProjectId], [SkillId]),
        CONSTRAINT [CK_ProjectSkills_ProficiencyRequired] CHECK ([ProficiencyRequired] >= 1 AND [ProficiencyRequired] <= 5),
        CONSTRAINT [CK_ProjectSkills_Weight] CHECK ([Weight] >= 1 AND [Weight] <= 5),
        CONSTRAINT [FK_ProjectSkills_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectSkills_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ProjectWorkspaces] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [ClientId] uniqueidentifier NOT NULL,
        [ProviderId] uniqueidentifier NOT NULL,
        [WorkspaceKey] nvarchar(256) NOT NULL,
        [Status] int NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ArchivedAt] datetime2 NULL,
        [TimelineData] nvarchar(max) NULL,
        [MilestoneData] nvarchar(max) NULL,
        [LastSyncedAt] datetime2 NULL,
        [IntegrationStatus] nvarchar(100) NULL,
        CONSTRAINT [PK_ProjectWorkspaces] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectWorkspaces_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectWorkspaces_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectWorkspaces_Users_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [QuestionnaireQuestions] (
        [Id] uniqueidentifier NOT NULL,
        [QuestionnaireId] uniqueidentifier NOT NULL,
        [QuestionText] nvarchar(500) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Type] int NOT NULL,
        [IsRequired] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DisplayOrder] int NOT NULL,
        [Configuration] nvarchar(max) NULL,
        [DefaultValue] nvarchar(1000) NULL,
        [PlaceholderText] nvarchar(200) NULL,
        [ValidationRegex] nvarchar(500) NULL,
        [ValidationMessage] nvarchar(200) NULL,
        [MinValue] int NULL,
        [MaxValue] int NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_QuestionnaireQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuestionnaireQuestions_Questionnaires_QuestionnaireId] FOREIGN KEY ([QuestionnaireId]) REFERENCES [Questionnaires] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [QuestionnaireResponses] (
        [Id] uniqueidentifier NOT NULL,
        [QuestionnaireId] uniqueidentifier NOT NULL,
        [RespondentUserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [IsSubmitted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsComplete] bit NOT NULL DEFAULT CAST(0 AS bit),
        [StartedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [SubmittedAt] datetime2 NULL,
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [SubmittedFromIP] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Metadata] nvarchar(max) NULL,
        [ReviewNotes] nvarchar(2000) NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [ReviewedAt] datetime2 NULL,
        CONSTRAINT [PK_QuestionnaireResponses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuestionnaireResponses_Questionnaires_QuestionnaireId] FOREIGN KEY ([QuestionnaireId]) REFERENCES [Questionnaires] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuestionnaireResponses_Users_RespondentUserId] FOREIGN KEY ([RespondentUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuestionnaireResponses_Users_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [BadgeEarningHistory] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [BadgeId] uniqueidentifier NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [Evidence] nvarchar(max) NULL,
        [ActionBy] uniqueidentifier NULL,
        [ActionAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_BadgeEarningHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BadgeEarningHistory_UserBadges_BadgeId] FOREIGN KEY ([BadgeId]) REFERENCES [UserBadges] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BadgeEarningHistory_Users_ActionBy] FOREIGN KEY ([ActionBy]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_BadgeEarningHistory_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [SkillEndorsements] (
        [Id] uniqueidentifier NOT NULL,
        [UserSkillId] uniqueidentifier NOT NULL,
        [EndorsedByUserId] uniqueidentifier NOT NULL,
        [Comment] nvarchar(500) NULL,
        [ReviewText] nvarchar(max) NULL,
        [IsVisible] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [SkillId] uniqueidentifier NULL,
        CONSTRAINT [PK_SkillEndorsements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SkillEndorsements_Skills_SkillId] FOREIGN KEY ([SkillId]) REFERENCES [Skills] ([Id]),
        CONSTRAINT [FK_SkillEndorsements_UserSkills_UserSkillId] FOREIGN KEY ([UserSkillId]) REFERENCES [UserSkills] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SkillEndorsements_Users_EndorsedByUserId] FOREIGN KEY ([EndorsedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ProjectApplicationAttachments] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [ProjectApplicationId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [FileSize] bigint NOT NULL,
        [StorageUrl] nvarchar(500) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsVirusScanned] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsSafe] bit NOT NULL DEFAULT CAST(0 AS bit),
        [UploadedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_ProjectApplicationAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectApplicationAttachments_ProjectApplications_ProjectApplicationId] FOREIGN KEY ([ProjectApplicationId]) REFERENCES [ProjectApplications] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ProviderSelections] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [ProjectId] uniqueidentifier NOT NULL,
        [SelectedProviderId] uniqueidentifier NOT NULL,
        [SelectedApplicationId] uniqueidentifier NOT NULL,
        [SelectionReason] nvarchar(1000) NOT NULL,
        [ContractTerms] nvarchar(max) NULL,
        [EscrowAmount] int NOT NULL,
        [SelectedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ExpectedStartDate] datetime2 NULL,
        [ExpectedCompletionDate] datetime2 NULL,
        [Status] int NOT NULL DEFAULT 0,
        [NegotiationNotes] nvarchar(2000) NULL,
        [SelectedFromIP] nvarchar(45) NULL,
        [IsEscrowFunded] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsContractSigned] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_ProviderSelections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProviderSelections_ProjectApplications_SelectedApplicationId] FOREIGN KEY ([SelectedApplicationId]) REFERENCES [ProjectApplications] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProviderSelections_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProviderSelections_Users_SelectedProviderId] FOREIGN KEY ([SelectedProviderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [CreditTransactions] (
        [Id] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [FromUserId] uniqueidentifier NULL,
        [ToUserId] uniqueidentifier NULL,
        [Amount] int NOT NULL,
        [Type] int NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [ProjectId] uniqueidentifier NULL,
        [Description] nvarchar(500) NOT NULL,
        [TransactionHash] nvarchar(128) NOT NULL,
        [PreviousTransactionHash] nvarchar(128) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CompletedAt] datetime2 NULL,
        [FailedAt] datetime2 NULL,
        [FailureReason] nvarchar(500) NULL,
        [InitiatedFromIP] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Metadata] nvarchar(2000) NULL,
        [IsFlagged] bit NOT NULL,
        [FlaggedReason] nvarchar(500) NULL,
        [FlaggedAt] datetime2 NULL,
        [ProjectEscrowId] uniqueidentifier NULL,
        CONSTRAINT [PK_CreditTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CreditTransactions_Amount_Positive] CHECK (Amount > 0),
        CONSTRAINT [FK_CreditTransactions_ProjectEscrows_ProjectEscrowId] FOREIGN KEY ([ProjectEscrowId]) REFERENCES [ProjectEscrows] ([Id]),
        CONSTRAINT [FK_CreditTransactions_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_CreditTransactions_Users_FromUserId] FOREIGN KEY ([FromUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CreditTransactions_Users_ToUserId] FOREIGN KEY ([ToUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [EscrowMilestones] (
        [Id] uniqueidentifier NOT NULL,
        [EscrowId] uniqueidentifier NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Amount] int NOT NULL,
        [IsReleased] bit NOT NULL DEFAULT CAST(0 AS bit),
        [ReleasedAt] datetime2 NULL,
        [ReleasedByUserId] uniqueidentifier NULL,
        [ReleaseNotes] nvarchar(1000) NULL,
        [ExpectedCompletionDate] datetime2 NULL,
        [ActualCompletionDate] datetime2 NULL,
        [SequenceOrder] int NOT NULL DEFAULT 1,
        [IsBlocking] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedFromIP] nvarchar(45) NULL,
        [LinkedDeliverableId] uniqueidentifier NULL,
        CONSTRAINT [PK_EscrowMilestones] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EscrowMilestones_ActualCompletion_After_Created] CHECK ([ActualCompletionDate] IS NULL OR [ActualCompletionDate] >= [CreatedAt]),
        CONSTRAINT [CK_EscrowMilestones_Amount_Positive] CHECK ([Amount] > 0),
        CONSTRAINT [CK_EscrowMilestones_SequenceOrder_Positive] CHECK ([SequenceOrder] > 0),
        CONSTRAINT [FK_EscrowMilestones_ProjectDeliverables_LinkedDeliverableId] FOREIGN KEY ([LinkedDeliverableId]) REFERENCES [ProjectDeliverables] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_EscrowMilestones_ProjectEscrows_EscrowId] FOREIGN KEY ([EscrowId]) REFERENCES [ProjectEscrows] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EscrowMilestones_Users_ReleasedByUserId] FOREIGN KEY ([ReleasedByUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
    DECLARE @defaultSchema AS sysname;
    SET @defaultSchema = SCHEMA_NAME();
    DECLARE @description AS sql_variant;
    SET @description = N'Human-readable milestone description';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'EscrowMilestones', 'COLUMN', N'Description';
    SET @description = N'Credits to release for this milestone';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'EscrowMilestones', 'COLUMN', N'Amount';
    SET @description = N'Whether milestone has been released';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'EscrowMilestones', 'COLUMN', N'IsReleased';
    SET @description = N'Notes about milestone release';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'EscrowMilestones', 'COLUMN', N'ReleaseNotes';
    SET @description = N'Display order for milestones';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'EscrowMilestones', 'COLUMN', N'SequenceOrder';
    SET @description = N'Whether milestone blocks subsequent releases';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'EscrowMilestones', 'COLUMN', N'IsBlocking';
    SET @description = N'When milestone was created';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'EscrowMilestones', 'COLUMN', N'CreatedAt';
    SET @description = N'When milestone was last updated';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'EscrowMilestones', 'COLUMN', N'UpdatedAt';
    SET @description = N'IP address where milestone was created';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'EscrowMilestones', 'COLUMN', N'CreatedFromIP';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ReputationHistories] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Date] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [Score] decimal(5,2) NOT NULL,
        [ChangeReason] nvarchar(500) NOT NULL,
        [ProjectId] uniqueidentifier NULL,
        [ReviewId] uniqueidentifier NULL,
        CONSTRAINT [PK_ReputationHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReputationHistories_ProjectReviews_ReviewId] FOREIGN KEY ([ReviewId]) REFERENCES [ProjectReviews] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ReputationHistories_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ReputationHistories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [UploadedFiles] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [BlobName] nvarchar(500) NOT NULL,
        [ContainerName] nvarchar(100) NOT NULL,
        [FileType] int NOT NULL,
        [IsApproved] bit NOT NULL,
        [RequiresHumanReview] bit NOT NULL,
        [SecurityScanPassed] bit NOT NULL,
        [ModerationResult] nvarchar(max) NULL,
        [SecurityScanResult] nvarchar(max) NULL,
        [ImageVariants] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastAccessedAt] datetime2 NULL,
        [ProjectReviewId] uniqueidentifier NULL,
        CONSTRAINT [PK_UploadedFiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UploadedFiles_ProjectReviews_ProjectReviewId] FOREIGN KEY ([ProjectReviewId]) REFERENCES [ProjectReviews] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UploadedFiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [DocumentFolders] (
        [Id] uniqueidentifier NOT NULL,
        [WorkspaceId] uniqueidentifier NOT NULL,
        [FolderName] nvarchar(200) NOT NULL,
        [ParentFolderId] uniqueidentifier NULL,
        [CreatedBy] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [DeletedAt] datetime2 NULL,
        [DeletedBy] uniqueidentifier NULL,
        [Description] nvarchar(1000) NULL,
        [SortOrder] int NOT NULL DEFAULT 0,
        CONSTRAINT [PK_DocumentFolders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentFolders_DocumentFolders_ParentFolderId] FOREIGN KEY ([ParentFolderId]) REFERENCES [DocumentFolders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentFolders_ProjectWorkspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [ProjectWorkspaces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentFolders_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentFolders_Users_DeletedBy] FOREIGN KEY ([DeletedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [TypingIndicators] (
        [Id] uniqueidentifier NOT NULL,
        [WorkspaceId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [LastTypingAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ConnectionId] nvarchar(100) NULL,
        CONSTRAINT [PK_TypingIndicators] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TypingIndicators_ProjectWorkspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [ProjectWorkspaces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TypingIndicators_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [WorkspaceMessages] (
        [Id] uniqueidentifier NOT NULL,
        [WorkspaceId] uniqueidentifier NOT NULL,
        [SenderId] uniqueidentifier NOT NULL,
        [MessageText] nvarchar(4000) NULL,
        [MessageType] int NOT NULL,
        [Status] int NOT NULL,
        [AttachmentUrl] nvarchar(500) NULL,
        [AttachmentFileName] nvarchar(255) NULL,
        [AttachmentSize] bigint NULL,
        [AttachmentMimeType] nvarchar(100) NULL,
        [IsEdited] bit NOT NULL DEFAULT CAST(0 AS bit),
        [ReplyToMessageId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [EditedAt] datetime2 NULL,
        [ReadAt] datetime2 NULL,
        [SenderIpAddress] nvarchar(45) NULL,
        [SenderUserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_WorkspaceMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WorkspaceMessages_AttachmentSize] CHECK ([AttachmentSize] IS NULL OR [AttachmentSize] > 0),
        CONSTRAINT [CK_WorkspaceMessages_FileContent] CHECK ([MessageType] NOT IN (1, 4, 5) OR [AttachmentUrl] IS NOT NULL),
        CONSTRAINT [CK_WorkspaceMessages_MessageType] CHECK ([MessageType] IN (0, 1, 2, 3, 4, 5)),
        CONSTRAINT [CK_WorkspaceMessages_Status] CHECK ([Status] IN (0, 1, 2, 3, 4)),
        CONSTRAINT [CK_WorkspaceMessages_TextContent] CHECK ([MessageType] != 0 OR [MessageText] IS NOT NULL),
        CONSTRAINT [FK_WorkspaceMessages_ProjectWorkspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [ProjectWorkspaces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkspaceMessages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkspaceMessages_WorkspaceMessages_ReplyToMessageId] FOREIGN KEY ([ReplyToMessageId]) REFERENCES [WorkspaceMessages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [QuestionOptions] (
        [Id] uniqueidentifier NOT NULL,
        [QuestionId] uniqueidentifier NOT NULL,
        [OptionText] nvarchar(200) NOT NULL,
        [OptionValue] nvarchar(100) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Metadata] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_QuestionOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuestionOptions_QuestionnaireQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [QuestionnaireQuestions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [QuestionResponses] (
        [Id] uniqueidentifier NOT NULL,
        [QuestionnaireResponseId] uniqueidentifier NOT NULL,
        [QuestionId] uniqueidentifier NOT NULL,
        [ResponseValue] nvarchar(max) NULL,
        [SelectedOptionIds] nvarchar(max) NULL,
        [FileAttachments] nvarchar(max) NULL,
        [Metadata] nvarchar(max) NULL,
        [IsValid] bit NOT NULL DEFAULT CAST(1 AS bit),
        [ValidationError] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_QuestionResponses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuestionResponses_QuestionnaireQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [QuestionnaireQuestions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_QuestionResponses_QuestionnaireResponses_QuestionnaireResponseId] FOREIGN KEY ([QuestionnaireResponseId]) REFERENCES [QuestionnaireResponses] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [CreditTransfers] (
        [Id] uniqueidentifier NOT NULL,
        [FromUserId] uniqueidentifier NOT NULL,
        [ToUserId] uniqueidentifier NOT NULL,
        [Amount] int NOT NULL,
        [TransferFee] int NOT NULL DEFAULT 0,
        [Message] nvarchar(500) NULL,
        [Status] int NOT NULL DEFAULT 0,
        [TransactionHash] varchar(64) NOT NULL,
        [CreditTransactionId] uniqueidentifier NULL,
        [BatchId] uniqueidentifier NULL,
        [InitiatedFromIP] varchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CompletedAt] datetime2 NULL,
        [ReversedAt] datetime2 NULL,
        [ReversalReason] nvarchar(500) NULL,
        [ReversedByUserId] uniqueidentifier NULL,
        [ReceiptSignature] nvarchar(512) NULL,
        [Metadata] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CreditTransfers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CreditTransfers_Amount_Positive] CHECK ([Amount] > 0),
        CONSTRAINT [CK_CreditTransfers_CompletedAt_Logic] CHECK (([Status] = 1 AND [CompletedAt] IS NOT NULL) OR ([Status] != 1)),
        CONSTRAINT [CK_CreditTransfers_NotSelfTransfer] CHECK ([FromUserId] != [ToUserId]),
        CONSTRAINT [CK_CreditTransfers_ReversedAt_Logic] CHECK (([Status] = 3 AND [ReversedAt] IS NOT NULL) OR ([Status] != 3)),
        CONSTRAINT [CK_CreditTransfers_TransferFee_NonNegative] CHECK ([TransferFee] >= 0),
        CONSTRAINT [FK_CreditTransfers_CreditTransactions_CreditTransactionId] FOREIGN KEY ([CreditTransactionId]) REFERENCES [CreditTransactions] ([Id]),
        CONSTRAINT [FK_CreditTransfers_FromUser] FOREIGN KEY ([FromUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CreditTransfers_ToUser] FOREIGN KEY ([ToUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CreditTransfers_Users_ReversedByUserId] FOREIGN KEY ([ReversedByUserId]) REFERENCES [Users] ([Id])
    );
    DECLARE @defaultSchema AS sysname;
    SET @defaultSchema = SCHEMA_NAME();
    DECLARE @description AS sql_variant;
    SET @description = N'Direct credit transfers between users with comprehensive audit trail and fraud prevention';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'CreditTransfers';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Action] nvarchar(100) NOT NULL,
        [Details] nvarchar(max) NULL,
        [IPAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Timestamp] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [Success] bit NOT NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [EntityId] uniqueidentifier NULL,
        [ProjectId] uniqueidentifier NULL,
        [ProjectReviewId] uniqueidentifier NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_EscrowMilestones_EntityId] FOREIGN KEY ([EntityId]) REFERENCES [EscrowMilestones] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditLogs_ProjectEscrows_EntityId] FOREIGN KEY ([EntityId]) REFERENCES [ProjectEscrows] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditLogs_ProjectReviews_ProjectReviewId] FOREIGN KEY ([ProjectReviewId]) REFERENCES [ProjectReviews] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditLogs_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [ProjectMilestones] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [EscrowMilestoneId] uniqueidentifier NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Priority] nvarchar(50) NOT NULL,
        [DueDate] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [SequenceOrder] int NOT NULL,
        [WeightPercentage] decimal(5,2) NOT NULL DEFAULT 0.0,
        [AcceptanceCriteria] nvarchar(3000) NULL,
        [ReviewNotes] nvarchar(2000) NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [AssignedToUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedFromIP] nvarchar(45) NULL,
        CONSTRAINT [PK_ProjectMilestones] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProjectMilestones_WeightPercentage] CHECK ([WeightPercentage] >= 0 AND [WeightPercentage] <= 100),
        CONSTRAINT [FK_ProjectMilestones_EscrowMilestones_EscrowMilestoneId] FOREIGN KEY ([EscrowMilestoneId]) REFERENCES [EscrowMilestones] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ProjectMilestones_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectMilestones_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ProjectMilestones_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [WorkspaceDocuments] (
        [Id] uniqueidentifier NOT NULL,
        [WorkspaceId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(500) NOT NULL,
        [FilePath] nvarchar(1000) NOT NULL,
        [FileSize] bigint NOT NULL,
        [MimeType] nvarchar(100) NOT NULL,
        [UploadedBy] uniqueidentifier NOT NULL,
        [FolderId] uniqueidentifier NULL,
        [VersionNumber] int NOT NULL DEFAULT 1,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [LastAccessedAt] datetime2 NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] uniqueidentifier NULL,
        [SecurityScanResult] nvarchar(max) NULL,
        [SecurityScanPassed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [ParentDocumentId] uniqueidentifier NULL,
        CONSTRAINT [PK_WorkspaceDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkspaceDocuments_DocumentFolders_FolderId] FOREIGN KEY ([FolderId]) REFERENCES [DocumentFolders] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_WorkspaceDocuments_ProjectWorkspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [ProjectWorkspaces] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkspaceDocuments_Users_DeletedBy] FOREIGN KEY ([DeletedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkspaceDocuments_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkspaceDocuments_WorkspaceDocuments_ParentDocumentId] FOREIGN KEY ([ParentDocumentId]) REFERENCES [WorkspaceDocuments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [MessageReactions] (
        [Id] uniqueidentifier NOT NULL,
        [MessageId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Emoji] nvarchar(10) NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [IpAddress] nvarchar(45) NULL,
        CONSTRAINT [PK_MessageReactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MessageReactions_EmojiLength] CHECK (LEN([Emoji]) > 0),
        CONSTRAINT [FK_MessageReactions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MessageReactions_WorkspaceMessages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [WorkspaceMessages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [DeliverableSubmissions] (
        [Id] uniqueidentifier NOT NULL,
        [MilestoneId] uniqueidentifier NOT NULL,
        [SubmittedByUserId] uniqueidentifier NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Description] nvarchar(max) NULL,
        [SubmissionUrl] nvarchar(2000) NULL,
        [TextContent] nvarchar(max) NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [SubmittedFromIP] nvarchar(45) NULL,
        [SubmissionNotes] nvarchar(2000) NULL,
        [IsReviewed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsApproved] bit NOT NULL DEFAULT CAST(0 AS bit),
        [ReviewedAt] datetime2 NULL,
        [ReviewedByUserId] uniqueidentifier NULL,
        [ReviewFeedback] nvarchar(3000) NULL,
        CONSTRAINT [PK_DeliverableSubmissions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DeliverableSubmissions_Approval_Logic] CHECK (([IsApproved] = 0) OR ([IsApproved] = 1 AND [IsReviewed] = 1)),
        CONSTRAINT [CK_DeliverableSubmissions_Review_Logic] CHECK (([IsReviewed] = 0) OR ([IsReviewed] = 1 AND [ReviewedAt] IS NOT NULL AND [ReviewedByUserId] IS NOT NULL)),
        CONSTRAINT [CK_DeliverableSubmissions_ReviewedAt_After_SubmittedAt] CHECK ([ReviewedAt] IS NULL OR [ReviewedAt] >= [SubmittedAt]),
        CONSTRAINT [CK_DeliverableSubmissions_Title_NotEmpty] CHECK (LEN(TRIM([Title])) > 0),
        CONSTRAINT [FK_DeliverableSubmissions_ProjectMilestones] FOREIGN KEY ([MilestoneId]) REFERENCES [ProjectMilestones] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DeliverableSubmissions_Users_ReviewedBy] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DeliverableSubmissions_Users_SubmittedBy] FOREIGN KEY ([SubmittedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
    DECLARE @defaultSchema AS sysname;
    SET @defaultSchema = SCHEMA_NAME();
    DECLARE @description AS sql_variant;
    SET @description = N'Unique identifier for the deliverable submission';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'Id';
    SET @description = N'Foreign key to the associated milestone';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'MilestoneId';
    SET @description = N'User who submitted this deliverable';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'SubmittedByUserId';
    SET @description = N'Type of deliverable submission';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'Type';
    SET @description = N'Title or summary of the submission';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'Title';
    SET @description = N'Detailed description of submitted work';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'Description';
    SET @description = N'URL for link or repository submissions';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'SubmissionUrl';
    SET @description = N'Text content for text-type submissions';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'TextContent';
    SET @description = N'When the submission was created';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'SubmittedAt';
    SET @description = N'IP address from which submission was made';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'SubmittedFromIP';
    SET @description = N'Optional notes from the submitter';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'SubmissionNotes';
    SET @description = N'Whether this submission has been reviewed';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'IsReviewed';
    SET @description = N'Whether this submission was approved';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'IsApproved';
    SET @description = N'When this submission was reviewed';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'ReviewedAt';
    SET @description = N'User who reviewed this submission';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'ReviewedByUserId';
    SET @description = N'Feedback from the reviewer';
    EXEC sp_addextendedproperty 'MS_Description', @description, 'SCHEMA', @defaultSchema, 'TABLE', N'DeliverableSubmissions', 'COLUMN', N'ReviewFeedback';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [DocumentAccesses] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [AccessedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [AccessType] nvarchar(50) NOT NULL DEFAULT N'view',
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Metadata] nvarchar(max) NULL,
        CONSTRAINT [PK_DocumentAccesses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentAccesses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentAccesses_WorkspaceDocuments_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [WorkspaceDocuments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [DocumentShares] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SharedBy] uniqueidentifier NOT NULL,
        [Permission] int NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ExpiresAt] datetime2 NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [RevokedAt] datetime2 NULL,
        [RevokedBy] uniqueidentifier NULL,
        [ShareMessage] nvarchar(1000) NULL,
        [AccessToken] nvarchar(256) NULL,
        CONSTRAINT [PK_DocumentShares] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentShares_Users_RevokedBy] FOREIGN KEY ([RevokedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentShares_Users_SharedBy] FOREIGN KEY ([SharedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentShares_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentShares_WorkspaceDocuments_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [WorkspaceDocuments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE TABLE [DeliverableSubmissionFiles] (
        [SubmissionId] uniqueidentifier NOT NULL,
        [FileId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_DeliverableSubmissionFiles] PRIMARY KEY ([SubmissionId], [FileId]),
        CONSTRAINT [FK_DeliverableSubmissionFiles_DeliverableSubmissions_SubmissionId] FOREIGN KEY ([SubmissionId]) REFERENCES [DeliverableSubmissions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DeliverableSubmissionFiles_UploadedFiles_FileId] FOREIGN KEY ([FileId]) REFERENCES [UploadedFiles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AntiGamingAlerts_AlertType] ON [AntiGamingAlerts] ([AlertType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AntiGamingAlerts_CreatedAt] ON [AntiGamingAlerts] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AntiGamingAlerts_ResolvedBy] ON [AntiGamingAlerts] ([ResolvedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AntiGamingAlerts_Severity] ON [AntiGamingAlerts] ([Severity]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AntiGamingAlerts_Status] ON [AntiGamingAlerts] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AntiGamingAlerts_UserId] ON [AntiGamingAlerts] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Action] ON [AuditLogs] ([Action]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityId] ON [AuditLogs] ([EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_IPAddress] ON [AuditLogs] ([IPAddress]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_IPAddress_Timestamp_Success] ON [AuditLogs] ([IPAddress], [Timestamp], [Success]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ProjectId] ON [AuditLogs] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ProjectReviewId] ON [AuditLogs] ([ProjectReviewId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeCriteria_BadgeType] ON [BadgeCriteria] ([BadgeType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeCriteria_BadgeType_Priority] ON [BadgeCriteria] ([BadgeType], [Priority]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeCriteria_IsActive] ON [BadgeCriteria] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BadgeDefinitions_BadgeType] ON [BadgeDefinitions] ([BadgeType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeDefinitions_Category] ON [BadgeDefinitions] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeDefinitions_DisplayPriority] ON [BadgeDefinitions] ([DisplayPriority]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeDefinitions_IsActive] ON [BadgeDefinitions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeEarningHistory_Action] ON [BadgeEarningHistory] ([Action]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeEarningHistory_ActionAt] ON [BadgeEarningHistory] ([ActionAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeEarningHistory_ActionBy] ON [BadgeEarningHistory] ([ActionBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeEarningHistory_BadgeId] ON [BadgeEarningHistory] ([BadgeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeEarningHistory_UserId] ON [BadgeEarningHistory] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_BadgeEarningHistory_UserId_ActionAt] ON [BadgeEarningHistory] ([UserId], [ActionAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CarrierInfos_ExpiresAt] ON [CarrierInfos] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CarrierInfos_PhonePrefix] ON [CarrierInfos] ([PhonePrefix]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CategoryReputationScores_Score] ON [CategoryReputationScores] ([Score]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CategoryReputationScores_SkillId] ON [CategoryReputationScores] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CategoryReputationScores_UserId] ON [CategoryReputationScores] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CategoryReputationScores_UserSkill] ON [CategoryReputationScores] ([UserId], [SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ContentModerationLogs_CreatedAt] ON [ContentModerationLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ContentModerationLogs_UserId] ON [ContentModerationLogs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ContentModerationLogs_WasApproved] ON [ContentModerationLogs] ([WasApproved]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ContentReviewQueues_AssignedReviewerId] ON [ContentReviewQueues] ([AssignedReviewerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ContentReviewQueues_CreatedAt] ON [ContentReviewQueues] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ContentReviewQueues_ReviewPriority] ON [ContentReviewQueues] ([ReviewPriority]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ContentReviewQueues_Status] ON [ContentReviewQueues] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ContentReviewQueues_UserId] ON [ContentReviewQueues] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_Amount_Created] ON [CreditTransactions] ([Amount], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_Chain_Integrity] ON [CreditTransactions] ([CreatedAt], [PreviousTransactionHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_Completion] ON [CreditTransactions] ([Status], [CompletedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_Escrow_Operations] ON [CreditTransactions] ([ProjectId], [Type], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_FromUser_Created] ON [CreditTransactions] ([FromUserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CreditTransactions_Hash_Unique] ON [CreditTransactions] ([TransactionHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_IP_Created] ON [CreditTransactions] ([InitiatedFromIP], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_IsFlagged] ON [CreditTransactions] ([IsFlagged]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_Project_Type] ON [CreditTransactions] ([ProjectId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_ProjectEscrowId] ON [CreditTransactions] ([ProjectEscrowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_Reporting] ON [CreditTransactions] ([Type], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_Status] ON [CreditTransactions] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_ToUser_Created] ON [CreditTransactions] ([ToUserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_Type] ON [CreditTransactions] ([Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransactions_Users_Created] ON [CreditTransactions] ([FromUserId], [ToUserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransfers_CreatedAt] ON [CreditTransfers] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransfers_CreditTransactionId] ON [CreditTransfers] ([CreditTransactionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransfers_FromUser_CreatedAt] ON [CreditTransfers] ([FromUserId], [CreatedAt]) INCLUDE ([Amount], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransfers_FromUser_Status_CreatedAt] ON [CreditTransfers] ([FromUserId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransfers_IP_CreatedAt] ON [CreditTransfers] ([InitiatedFromIP], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransfers_ReversedByUserId] ON [CreditTransfers] ([ReversedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransfers_Status] ON [CreditTransfers] ([Status]) INCLUDE ([CreatedAt], [Amount]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_CreditTransfers_Status_CompletedAt] ON [CreditTransfers] ([Status], [CompletedAt]) WHERE [CompletedAt] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransfers_Status_CreatedAt_Amount] ON [CreditTransfers] ([Status], [CreatedAt], [Amount]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditTransfers_ToUser_CreatedAt] ON [CreditTransfers] ([ToUserId], [CreatedAt]) INCLUDE ([Amount], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CreditTransfers_TransactionHash_Unique] ON [CreditTransfers] ([TransactionHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditWallets_IsBlocked] ON [CreditWallets] ([IsBlocked]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditWallets_LastTransactionAt] ON [CreditWallets] ([LastTransactionAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CreditWallets_User_Created] ON [CreditWallets] ([UserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CreditWallets_UserId_Unique] ON [CreditWallets] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CustomBlocklistTerms_AddedByUserId] ON [CustomBlocklistTerms] ([AddedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CustomBlocklistTerms_ExpiresAt] ON [CustomBlocklistTerms] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CustomBlocklistTerms_IsActive] ON [CustomBlocklistTerms] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_CustomBlocklistTerms_Term] ON [CustomBlocklistTerms] ([Term]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissionFiles_FileId] ON [DeliverableSubmissionFiles] ([FileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissionFiles_SubmissionId] ON [DeliverableSubmissionFiles] ([SubmissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissions_IsReviewed] ON [DeliverableSubmissions] ([IsReviewed]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissions_Milestone_SubmittedAt] ON [DeliverableSubmissions] ([MilestoneId], [SubmittedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissions_MilestoneId] ON [DeliverableSubmissions] ([MilestoneId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissions_Review_Status] ON [DeliverableSubmissions] ([IsReviewed], [IsApproved], [SubmittedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissions_ReviewedByUserId] ON [DeliverableSubmissions] ([ReviewedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissions_SubmittedAt] ON [DeliverableSubmissions] ([SubmittedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissions_SubmittedBy] ON [DeliverableSubmissions] ([SubmittedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeliverableSubmissions_User_SubmittedAt] ON [DeliverableSubmissions] ([SubmittedByUserId], [SubmittedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeviceFingerprints_CreatedAt] ON [DeviceFingerprints] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeviceFingerprints_FingerprintHash] ON [DeviceFingerprints] ([FingerprintHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DeviceFingerprints_UserId] ON [DeviceFingerprints] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentAccesses_AccessedAt] ON [DocumentAccesses] ([AccessedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentAccesses_Document_User] ON [DocumentAccesses] ([DocumentId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentAccesses_DocumentId] ON [DocumentAccesses] ([DocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentAccesses_User_AccessedAt] ON [DocumentAccesses] ([UserId], [AccessedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentAccesses_UserId] ON [DocumentAccesses] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentFolders_CreatedAt] ON [DocumentFolders] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentFolders_CreatedBy] ON [DocumentFolders] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentFolders_DeletedBy] ON [DocumentFolders] ([DeletedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentFolders_ParentFolderId] ON [DocumentFolders] ([ParentFolderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DocumentFolders_UniqueName] ON [DocumentFolders] ([WorkspaceId], [ParentFolderId], [FolderName]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentFolders_Workspace_NotDeleted] ON [DocumentFolders] ([WorkspaceId], [IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentFolders_WorkspaceId] ON [DocumentFolders] ([WorkspaceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DocumentShares_AccessToken] ON [DocumentShares] ([AccessToken]) WHERE [AccessToken] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_CreatedAt] ON [DocumentShares] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_Document_User] ON [DocumentShares] ([DocumentId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_DocumentId] ON [DocumentShares] ([DocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_ExpiresAt] ON [DocumentShares] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_RevokedBy] ON [DocumentShares] ([RevokedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_SharedBy] ON [DocumentShares] ([SharedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_User_Active] ON [DocumentShares] ([UserId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_DocumentShares_UserId] ON [DocumentShares] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_EmailVerifications_ExpiresAt] ON [EmailVerifications] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmailVerifications_Token] ON [EmailVerifications] ([Token]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_EmailVerifications_UserId] ON [EmailVerifications] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_EscrowMilestones_EscrowId] ON [EscrowMilestones] ([EscrowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_EscrowMilestones_EscrowId_IsReleased] ON [EscrowMilestones] ([EscrowId], [IsReleased]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_EscrowMilestones_EscrowId_SequenceOrder] ON [EscrowMilestones] ([EscrowId], [SequenceOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_EscrowMilestones_ExpectedCompletionDate] ON [EscrowMilestones] ([ExpectedCompletionDate]) WHERE [ExpectedCompletionDate] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_EscrowMilestones_IsReleased] ON [EscrowMilestones] ([IsReleased]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_EscrowMilestones_LinkedDeliverableId] ON [EscrowMilestones] ([LinkedDeliverableId]) WHERE [LinkedDeliverableId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_EscrowMilestones_ReleasedByUserId] ON [EscrowMilestones] ([ReleasedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Experiences_UserId] ON [Experiences] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Experiences_UserId_DisplayOrder] ON [Experiences] ([UserId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Experiences_UserId_IsCurrent] ON [Experiences] ([UserId], [IsCurrent]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Experiences_UserId_IsVisible] ON [Experiences] ([UserId], [IsVisible]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Experiences_UserId_StartDate] ON [Experiences] ([UserId], [StartDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Experiences_UserId_Type] ON [Experiences] ([UserId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ExperienceSkills_ExperienceId] ON [ExperienceSkills] ([ExperienceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExperienceSkills_ExperienceId_SkillId] ON [ExperienceSkills] ([ExperienceId], [SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ExperienceSkills_SkillId] ON [ExperienceSkills] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_GamingRiskAssessments_AnalyzedAt] ON [GamingRiskAssessments] ([AnalyzedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_GamingRiskAssessments_ModelVersion] ON [GamingRiskAssessments] ([ModelVersion]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_GamingRiskAssessments_RiskScore] ON [GamingRiskAssessments] ([RiskScore]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_GamingRiskAssessments_UserId] ON [GamingRiskAssessments] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_IpGeolocations_ExpiresAt] ON [IpGeolocations] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IpGeolocations_IpAddressHash] ON [IpGeolocations] ([IpAddressHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_MessageReactions_CreatedAt] ON [MessageReactions] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_MessageReactions_MessageId] ON [MessageReactions] ([MessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_MessageReactions_MessageId_Emoji] ON [MessageReactions] ([MessageId], [Emoji]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MessageReactions_MessageId_UserId_Emoji_Unique] ON [MessageReactions] ([MessageId], [UserId], [Emoji]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_MessageReactions_UserId] ON [MessageReactions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PasswordResets_CreatedAt] ON [PasswordResets] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PasswordResets_ExpiresAt] ON [PasswordResets] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PasswordResets_TokenHash] ON [PasswordResets] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PasswordResets_User_Created] ON [PasswordResets] ([UserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PasswordResets_UserId] ON [PasswordResets] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Permissions_Category] ON [Permissions] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Permissions_IsActive] ON [Permissions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Name] ON [Permissions] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PhoneFraudLogs_CreatedAt] ON [PhoneFraudLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PhoneFraudLogs_PhoneNumberHash] ON [PhoneFraudLogs] ([PhoneNumberHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PhoneFraudLogs_UserId] ON [PhoneFraudLogs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PhoneVerifications_ExpiresAt] ON [PhoneVerifications] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PhoneVerifications_PhoneNumber] ON [PhoneVerifications] ([PhoneNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PhoneVerifications_PhoneNumber_Code] ON [PhoneVerifications] ([PhoneNumber], [VerificationCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_PhoneVerifications_UserId] ON [PhoneVerifications] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Profiles_Company] ON [Profiles] ([Company]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Profiles_FirstName_LastName] ON [Profiles] ([FirstName], [LastName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Profiles_IsPublic] ON [Profiles] ([IsPublic]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Profiles_UserId] ON [Profiles] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplicationAttachments_ContentType] ON [ProjectApplicationAttachments] ([ContentType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplicationAttachments_IsSafe] ON [ProjectApplicationAttachments] ([IsSafe]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplicationAttachments_IsVirusScanned] ON [ProjectApplicationAttachments] ([IsVirusScanned]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplicationAttachments_ProjectApplicationId] ON [ProjectApplicationAttachments] ([ProjectApplicationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplicationAttachments_UploadedAt] ON [ProjectApplicationAttachments] ([UploadedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplications_CreatedAt] ON [ProjectApplications] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplications_ProjectId] ON [ProjectApplications] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplications_ProjectId_Status_CreatedAt] ON [ProjectApplications] ([ProjectId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplications_ProviderId] ON [ProjectApplications] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplications_ProviderId_Status_CreatedAt] ON [ProjectApplications] ([ProviderId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplications_SkillMatchScore] ON [ProjectApplications] ([SkillMatchScore]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectApplications_Status] ON [ProjectApplications] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ProjectApplications_ProjectId_ProviderId] ON [ProjectApplications] ([ProjectId], [ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectDeliverables_IsCompleted] ON [ProjectDeliverables] ([IsCompleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectDeliverables_ProjectId] ON [ProjectDeliverables] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectDeliverables_ProjectId_OrderIndex] ON [ProjectDeliverables] ([ProjectId], [OrderIndex]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectEscrows_ClientId] ON [ProjectEscrows] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectEscrows_CreatedAt] ON [ProjectEscrows] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectEscrows_DisputeResolvedByUserId] ON [ProjectEscrows] ([DisputeResolvedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectEscrows_ProjectId_Unique] ON [ProjectEscrows] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectEscrows_ProviderId] ON [ProjectEscrows] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectEscrows_Status] ON [ProjectEscrows] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectEscrows_Status_CreatedAt] ON [ProjectEscrows] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestones_AssignedToUserId] ON [ProjectMilestones] ([AssignedToUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestones_CreatedAt] ON [ProjectMilestones] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestones_CreatedByUserId] ON [ProjectMilestones] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestones_DueDate] ON [ProjectMilestones] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProjectMilestones_EscrowMilestoneId] ON [ProjectMilestones] ([EscrowMilestoneId]) WHERE [EscrowMilestoneId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestones_ProjectId] ON [ProjectMilestones] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectMilestones_ProjectId_SequenceOrder] ON [ProjectMilestones] ([ProjectId], [SequenceOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectMilestones_Status] ON [ProjectMilestones] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_CreatedAt] ON [ProjectReviews] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_ModerationStatus] ON [ProjectReviews] ([ModerationStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_ProjectId] ON [ProjectReviews] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_ProjectId_Type] ON [ProjectReviews] ([ProjectId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_PublishedAt] ON [ProjectReviews] ([PublishedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_RevieweeId] ON [ProjectReviews] ([RevieweeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_RevieweeId_Status] ON [ProjectReviews] ([RevieweeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_ReviewerId] ON [ProjectReviews] ([ReviewerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_Status] ON [ProjectReviews] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectReviews_Status_ModerationStatus] ON [ProjectReviews] ([Status], [ModerationStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ProjectReviews_ProjectId_ReviewerId_Type] ON [ProjectReviews] ([ProjectId], [ReviewerId], [Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Projects_ClientId] ON [Projects] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Projects_CreatedAt] ON [Projects] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Projects_CreditBudget] ON [Projects] ([CreditBudget]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Projects_EndDate] ON [Projects] ([EndDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Projects_ModerationStatus] ON [Projects] ([ModerationStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Projects_ProviderId] ON [Projects] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Projects_Status] ON [Projects] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Projects_Status_ModerationStatus] ON [Projects] ([Status], [ModerationStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectSkills_ProficiencyRequired] ON [ProjectSkills] ([ProficiencyRequired]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectSkills_ProjectId] ON [ProjectSkills] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectSkills_SkillId] ON [ProjectSkills] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectSkills_Weight] ON [ProjectSkills] ([Weight]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectWorkspaces_ClientId] ON [ProjectWorkspaces] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectWorkspaces_CreatedAt] ON [ProjectWorkspaces] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectWorkspaces_ProjectId] ON [ProjectWorkspaces] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectWorkspaces_ProviderId] ON [ProjectWorkspaces] ([ProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectWorkspaces_Status] ON [ProjectWorkspaces] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProviderSelections_ProjectId_Unique] ON [ProviderSelections] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProviderSelections_SelectedApplicationId] ON [ProviderSelections] ([SelectedApplicationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProviderSelections_SelectedAt_Status] ON [ProviderSelections] ([SelectedAt], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProviderSelections_SelectedProviderId] ON [ProviderSelections] ([SelectedProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ProviderSelections_Status] ON [ProviderSelections] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireQuestions_DisplayOrder] ON [QuestionnaireQuestions] ([DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireQuestions_IsActive] ON [QuestionnaireQuestions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireQuestions_IsRequired] ON [QuestionnaireQuestions] ([IsRequired]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireQuestions_Questionnaire_Active_Order] ON [QuestionnaireQuestions] ([QuestionnaireId], [IsActive], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireQuestions_Questionnaire_Required_Active] ON [QuestionnaireQuestions] ([QuestionnaireId], [IsRequired], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireQuestions_QuestionnaireId] ON [QuestionnaireQuestions] ([QuestionnaireId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireQuestions_Type] ON [QuestionnaireQuestions] ([Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_IsComplete] ON [QuestionnaireResponses] ([IsComplete]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_IsSubmitted] ON [QuestionnaireResponses] ([IsSubmitted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_Questionnaire_Respondent_Status] ON [QuestionnaireResponses] ([QuestionnaireId], [RespondentUserId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_Questionnaire_Status_Submitted] ON [QuestionnaireResponses] ([QuestionnaireId], [Status], [SubmittedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_QuestionnaireId] ON [QuestionnaireResponses] ([QuestionnaireId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_Respondent_Submitted_Updated] ON [QuestionnaireResponses] ([RespondentUserId], [IsSubmitted], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_RespondentUserId] ON [QuestionnaireResponses] ([RespondentUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_ReviewedAt] ON [QuestionnaireResponses] ([ReviewedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_ReviewedByUserId] ON [QuestionnaireResponses] ([ReviewedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_StartedAt] ON [QuestionnaireResponses] ([StartedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_Status] ON [QuestionnaireResponses] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_Status_Reviewer_Reviewed] ON [QuestionnaireResponses] ([Status], [ReviewedByUserId], [ReviewedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_SubmittedAt] ON [QuestionnaireResponses] ([SubmittedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_QuestionnaireResponses_Unique_Submission] ON [QuestionnaireResponses] ([QuestionnaireId], [RespondentUserId]) WHERE [IsSubmitted] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionnaireResponses_UpdatedAt] ON [QuestionnaireResponses] ([UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_Active_Type_Created] ON [Questionnaires] ([IsActive], [Type], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_CreatedAt] ON [Questionnaires] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_CreatedBy_Active_Updated] ON [Questionnaires] ([CreatedByUserId], [IsActive], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_CreatedByUserId] ON [Questionnaires] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_EndDate] ON [Questionnaires] ([EndDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_IsActive] ON [Questionnaires] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_IsTemplate] ON [Questionnaires] ([IsTemplate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_StartDate] ON [Questionnaires] ([StartDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_Template_Active_Updated] ON [Questionnaires] ([IsTemplate], [IsActive], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_Type] ON [Questionnaires] ([Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Questionnaires_UpdatedAt] ON [Questionnaires] ([UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionOptions_DisplayOrder] ON [QuestionOptions] ([DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionOptions_IsActive] ON [QuestionOptions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionOptions_IsDefault] ON [QuestionOptions] ([IsDefault]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionOptions_Question_Active_Order] ON [QuestionOptions] ([QuestionId], [IsActive], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionOptions_Question_Default_Active] ON [QuestionOptions] ([QuestionId], [IsDefault], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionOptions_QuestionId] ON [QuestionOptions] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionResponses_CreatedAt] ON [QuestionResponses] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionResponses_IsValid] ON [QuestionResponses] ([IsValid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionResponses_Question_Valid_Updated] ON [QuestionResponses] ([QuestionId], [IsValid], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionResponses_QuestionId] ON [QuestionResponses] ([QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionResponses_QuestionnaireResponseId] ON [QuestionResponses] ([QuestionnaireResponseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QuestionResponses_Response_Question] ON [QuestionResponses] ([QuestionnaireResponseId], [QuestionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_QuestionResponses_UpdatedAt] ON [QuestionResponses] ([UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_CreatedByIp] ON [RefreshTokens] ([CreatedByIp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [RefreshTokens] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_User_Status_Expiry] ON [RefreshTokens] ([UserId], [IsRevoked], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ReputationHistories_Date] ON [ReputationHistories] ([Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ReputationHistories_ProjectId] ON [ReputationHistories] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ReputationHistories_ReviewId] ON [ReputationHistories] ([ReviewId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ReputationHistories_UserDate] ON [ReputationHistories] ([UserId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_ReputationHistories_UserId] ON [ReputationHistories] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RevokedTokens_ExpiresAt] ON [RevokedTokens] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RevokedTokens_RevokedAt] ON [RevokedTokens] ([RevokedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RevokedTokens_TokenId] ON [RevokedTokens] ([TokenId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RevokedTokens_TokenId_ExpiresAt] ON [RevokedTokens] ([TokenId], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_GrantedAt] ON [RolePermissions] ([GrantedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_GrantedByUserId] ON [RolePermissions] ([GrantedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_IsActive] ON [RolePermissions] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [RolePermissions] ([RoleId], [PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_SavedSearches_UserId] ON [SavedSearches] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_SkillEndorsements_EndorsedByUserId] ON [SkillEndorsements] ([EndorsedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_SkillEndorsements_SkillId] ON [SkillEndorsements] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_SkillEndorsements_UserSkillId] ON [SkillEndorsements] ([UserSkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SkillEndorsements_UserSkillId_EndorsedByUserId] ON [SkillEndorsements] ([UserSkillId], [EndorsedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_SkillEndorsements_UserSkillId_IsVisible] ON [SkillEndorsements] ([UserSkillId], [IsVisible]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Skills_Category] ON [Skills] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Skills_Category_Name] ON [Skills] ([Category], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_Skills_IsActive] ON [Skills] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Skills_Name] ON [Skills] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_SuspiciousPhones_IsBlocked] ON [SuspiciousPhones] ([IsBlocked]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SuspiciousPhones_PhoneNumberHash] ON [SuspiciousPhones] ([PhoneNumberHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_SuspiciousPhones_ReportedByUserId] ON [SuspiciousPhones] ([ReportedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_TypingIndicators_ConnectionId] ON [TypingIndicators] ([ConnectionId]) WHERE ConnectionId IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_TypingIndicators_LastTypingAt] ON [TypingIndicators] ([LastTypingAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_TypingIndicators_UserId] ON [TypingIndicators] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_TypingIndicators_WorkspaceId] ON [TypingIndicators] ([WorkspaceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_TypingIndicators_WorkspaceId_LastTypingAt] ON [TypingIndicators] ([WorkspaceId], [LastTypingAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TypingIndicators_WorkspaceId_UserId_Unique] ON [TypingIndicators] ([WorkspaceId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UploadedFiles_CreatedAt] ON [UploadedFiles] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UploadedFiles_FileType] ON [UploadedFiles] ([FileType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UploadedFiles_IsApproved] ON [UploadedFiles] ([IsApproved]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UploadedFiles_ProjectReviewId] ON [UploadedFiles] ([ProjectReviewId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UploadedFiles_UserId] ON [UploadedFiles] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBadges_BadgeType] ON [UserBadges] ([BadgeType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBadges_EarnedAt] ON [UserBadges] ([EarnedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBadges_ExpiresAt] ON [UserBadges] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBadges_IsActive_ExpiresAt] ON [UserBadges] ([IsActive], [ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBadges_UserId] ON [UserBadges] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBadges_UserId_BadgeType] ON [UserBadges] ([UserId], [BadgeType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBadges_VerifiedBy] ON [UserBadges] ([VerifiedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBehaviorMetrics_CalculatedAt] ON [UserBehaviorMetrics] ([CalculatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBehaviorMetrics_IsAnomaly] ON [UserBehaviorMetrics] ([IsAnomaly]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBehaviorMetrics_MetricName] ON [UserBehaviorMetrics] ([MetricName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBehaviorMetrics_User_Metric_Date] ON [UserBehaviorMetrics] ([UserId], [MetricName], [CalculatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserBehaviorMetrics_UserId] ON [UserBehaviorMetrics] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserCreditReports_GeneratedAt] ON [UserCreditReports] ([GeneratedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserCreditReports_ReportMonth] ON [UserCreditReports] ([ReportMonth]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserCreditReports_UserId] ON [UserCreditReports] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserCreditReports_UserId_GeneratedAt] ON [UserCreditReports] ([UserId], [GeneratedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserCreditReports_UserId_ReportMonth] ON [UserCreditReports] ([UserId], [ReportMonth]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserNetworkConnections_ConnectionType] ON [UserNetworkConnections] ([ConnectionType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserNetworkConnections_DetectedAt] ON [UserNetworkConnections] ([DetectedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserNetworkConnections_User1Id] ON [UserNetworkConnections] ([User1Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserNetworkConnections_User2Id] ON [UserNetworkConnections] ([User2Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserNetworkConnections_UserPair] ON [UserNetworkConnections] ([User1Id], [User2Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserReputationScores_LastUpdated] ON [UserReputationScores] ([LastUpdated]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserReputationScores_OverallScore] ON [UserReputationScores] ([OverallScore]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserReputationScores_UserId] ON [UserReputationScores] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]) WHERE [Email] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSanctions_ExpiresAt] ON [UserSanctions] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSanctions_IssuedAt] ON [UserSanctions] ([IssuedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSanctions_IssuedBy] ON [UserSanctions] ([IssuedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSanctions_SanctionType] ON [UserSanctions] ([SanctionType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSanctions_Severity] ON [UserSanctions] ([Severity]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSanctions_Status] ON [UserSanctions] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSanctions_UserId] ON [UserSanctions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSkills_SkillId] ON [UserSkills] ([SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSkills_UserId] ON [UserSkills] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSkills_UserId_IsFeatured] ON [UserSkills] ([UserId], [IsFeatured]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_UserSkills_UserId_IsVisible] ON [UserSkills] ([UserId], [IsVisible]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserSkills_UserId_SkillId] ON [UserSkills] ([UserId], [SkillId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_VerificationRequests_BadgeType] ON [VerificationRequests] ([BadgeType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_VerificationRequests_RequestedAt] ON [VerificationRequests] ([RequestedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_VerificationRequests_ReviewedBy] ON [VerificationRequests] ([ReviewedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_VerificationRequests_Status] ON [VerificationRequests] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_VerificationRequests_UserId] ON [VerificationRequests] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_VerificationRequests_UserId_BadgeType_Status] ON [VerificationRequests] ([UserId], [BadgeType], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceDocuments_CreatedAt] ON [WorkspaceDocuments] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceDocuments_DeletedBy] ON [WorkspaceDocuments] ([DeletedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceDocuments_FolderId] ON [WorkspaceDocuments] ([FolderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceDocuments_ParentDocument] ON [WorkspaceDocuments] ([ParentDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceDocuments_UploadedBy] ON [WorkspaceDocuments] ([UploadedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceDocuments_Workspace_NotDeleted] ON [WorkspaceDocuments] ([WorkspaceId], [IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceDocuments_WorkspaceId] ON [WorkspaceDocuments] ([WorkspaceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceMessages_CreatedAt] ON [WorkspaceMessages] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_WorkspaceMessages_MessageText] ON [WorkspaceMessages] ([MessageText]) WHERE MessageText IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceMessages_ReplyToMessageId] ON [WorkspaceMessages] ([ReplyToMessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceMessages_SenderId] ON [WorkspaceMessages] ([SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceMessages_Status_Tracking] ON [WorkspaceMessages] ([WorkspaceId], [Status], [SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceMessages_WorkspaceId] ON [WorkspaceMessages] ([WorkspaceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    CREATE INDEX [IX_WorkspaceMessages_WorkspaceId_CreatedAt] ON [WorkspaceMessages] ([WorkspaceId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250930155000_InitialMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250930155000_InitialMigration', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251001022409_AddFirstNameLastNameToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [FirstName] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251001022409_AddFirstNameLastNameToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [LastName] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251001022409_AddFirstNameLastNameToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251001022409_AddFirstNameLastNameToUser', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007135606_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_UserSkills_SkillId_UserId] ON [UserSkills] ([SkillId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007135606_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_Profiles_ViewCount_UserId] ON [Profiles] ([ViewCount] DESC, [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007135606_AddPerformanceIndexes'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId_Timestamp] ON [AuditLogs] ([UserId], [Timestamp] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007135606_AddPerformanceIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251007135606_AddPerformanceIndexes', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007150926_RemovePhoneVerification'
)
BEGIN
    DROP TABLE [CarrierInfos];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007150926_RemovePhoneVerification'
)
BEGIN
    DROP TABLE [PhoneFraudLogs];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007150926_RemovePhoneVerification'
)
BEGIN
    DROP TABLE [PhoneVerifications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007150926_RemovePhoneVerification'
)
BEGIN
    DROP TABLE [SuspiciousPhones];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007150926_RemovePhoneVerification'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PhoneVerified');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Users] DROP COLUMN [PhoneVerified];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251007150926_RemovePhoneVerification'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251007150926_RemovePhoneVerification', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    DROP TABLE [EmailVerifications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    ALTER TABLE [Projects] DROP CONSTRAINT [CK_Projects_CreditBudget];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'EmailVerified');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Users] DROP COLUMN [EmailVerified];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    ALTER TABLE [WorkspaceMessages] ADD [IdempotencyKey] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    ALTER TABLE [CreditTransfers] ADD [IdempotencyKey] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    CREATE INDEX [IX_Users_Status_CreatedAt] ON [Users] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Users_UserName] ON [Users] ([UserName]) WHERE [UserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [Users] ADD CONSTRAINT [CK_Users_Email_Format] CHECK ([Email] LIKE ''%@%.%'')');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [Users] ADD CONSTRAINT [CK_Users_FailedLoginAttempts_NonNegative] CHECK ([FailedLoginAttempts] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [Projects] ADD CONSTRAINT [CK_Projects_CreditBudget] CHECK ([CreditBudget] >= 50 AND [CreditBudget] <= 50000)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [Projects] ADD CONSTRAINT [CK_Projects_Description_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Description]))) > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [Projects] ADD CONSTRAINT [CK_Projects_ModerationNotes_Logic] CHECK (([ModerationStatus] != 3) OR ([ModerationStatus] = 3 AND [ModerationNotes] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [Projects] ADD CONSTRAINT [CK_Projects_Title_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Title]))) > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [Projects] ADD CONSTRAINT [CK_Projects_UpdatedAt_Logic] CHECK ([UpdatedAt] >= [CreatedAt])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [CreditWallets] ADD CONSTRAINT [CK_CreditWallets_BlockedReason_Logic] CHECK (([IsBlocked] = 0) OR ([IsBlocked] = 1 AND [BlockedReason] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [CreditWallets] ADD CONSTRAINT [CK_CreditWallets_EncryptedBalance_NotEmpty] CHECK (LEN([EncryptedBalance]) > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [CreditWallets] ADD CONSTRAINT [CK_CreditWallets_EncryptedPendingBalance_NotEmpty] CHECK (LEN([EncryptedPendingBalance]) > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    EXEC(N'ALTER TABLE [CreditWallets] ADD CONSTRAINT [CK_CreditWallets_UpdatedAt_Logic] CHECK ([UpdatedAt] >= [CreatedAt])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251025203039_RemoveEmailVerificationTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251025203039_RemoveEmailVerificationTable', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [PaymentMethods] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Provider] nvarchar(50) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [Token] nvarchar(500) NOT NULL,
        [Last4Digits] nvarchar(4) NULL,
        [Brand] nvarchar(100) NULL,
        [ExpiryDate] nvarchar(7) NULL,
        [CardholderName] nvarchar(200) NULL,
        [BillingCountry] nvarchar(2) NULL,
        [BillingPostalCode] nvarchar(20) NULL,
        [IsDefault] bit NOT NULL,
        [IsValid] bit NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [LastUsedAt] datetime2 NULL,
        CONSTRAINT [PK_PaymentMethods] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PaymentMethods_BillingCountry_ValidFormat] CHECK ([BillingCountry] IS NULL OR LEN([BillingCountry]) = 2 AND [BillingCountry] LIKE '[A-Z][A-Z]'),
        CONSTRAINT [CK_PaymentMethods_ExpiryDate_ValidFormat] CHECK ([ExpiryDate] IS NULL OR ([ExpiryDate] LIKE '[0-9][0-9]/[0-9][0-9][0-9][0-9]' AND LEN([ExpiryDate]) = 7)),
        CONSTRAINT [CK_PaymentMethods_Last4Digits_DigitsOnly] CHECK ([Last4Digits] IS NULL OR LEN([Last4Digits]) = 4 AND [Last4Digits] LIKE '[0-9][0-9][0-9][0-9]'),
        CONSTRAINT [CK_PaymentMethods_Provider_NotEmpty] CHECK (LEN([Provider]) > 0),
        CONSTRAINT [CK_PaymentMethods_Token_NotEmpty] CHECK (LEN([Token]) > 0),
        CONSTRAINT [CK_PaymentMethods_Type_NotEmpty] CHECK (LEN([Type]) > 0),
        CONSTRAINT [FK_PaymentMethods_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [SubscriptionTiers] (
        [Id] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Price] decimal(10,2) NOT NULL,
        [AnnualPrice] decimal(10,2) NULL,
        [CreditBonus] int NOT NULL,
        [MaxActiveProjects] int NOT NULL,
        [MaxTeamMembers] int NOT NULL,
        [PrioritySupport] bit NOT NULL,
        [ApiAccess] bit NOT NULL,
        [AdvancedAnalytics] bit NOT NULL,
        [AdvancedFraudDetection] bit NOT NULL,
        [MultiSignature] bit NOT NULL,
        [CustomIntegrations] bit NOT NULL,
        [MaxMonthlyEarnings] int NOT NULL,
        [Features] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_SubscriptionTiers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SubscriptionTiers_AnnualPrice_Positive] CHECK ([AnnualPrice] >= 0),
        CONSTRAINT [CK_SubscriptionTiers_CreditBonus_NonNegative] CHECK ([CreditBonus] >= 0),
        CONSTRAINT [CK_SubscriptionTiers_MaxActiveProjects_Positive] CHECK ([MaxActiveProjects] >= 0),
        CONSTRAINT [CK_SubscriptionTiers_MaxMonthlyEarnings_NonNegative] CHECK ([MaxMonthlyEarnings] >= 0),
        CONSTRAINT [CK_SubscriptionTiers_MaxTeamMembers_NonNegative] CHECK ([MaxTeamMembers] >= 0),
        CONSTRAINT [CK_SubscriptionTiers_Price_Positive] CHECK ([Price] >= 0),
        CONSTRAINT [CK_SubscriptionTiers_SortOrder_NonNegative] CHECK ([SortOrder] >= 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [UserSubscriptions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SubscriptionTierId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [NextBillingDate] datetime2 NULL,
        [TrialEndDate] datetime2 NULL,
        [AutoRenew] bit NOT NULL,
        [PaymentMethodId] uniqueidentifier NULL,
        [ExternalSubscriptionId] nvarchar(200) NULL,
        [ExternalCustomerId] nvarchar(200) NULL,
        [BillingCycleCount] int NOT NULL,
        [IsAnnual] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [CancelledAt] datetime2 NULL,
        [CancellationReason] nvarchar(500) NULL,
        CONSTRAINT [PK_UserSubscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_UserSubscriptions_BillingCycleCount_NonNegative] CHECK ([BillingCycleCount] >= 0),
        CONSTRAINT [CK_UserSubscriptions_NextBillingDate_After_StartDate] CHECK ([NextBillingDate] IS NULL OR [StartDate] <= [NextBillingDate]),
        CONSTRAINT [CK_UserSubscriptions_StartDate_Before_EndDate] CHECK ([EndDate] IS NULL OR [StartDate] <= [EndDate]),
        CONSTRAINT [CK_UserSubscriptions_TrialEndDate_After_StartDate] CHECK ([TrialEndDate] IS NULL OR [StartDate] <= [TrialEndDate]),
        CONSTRAINT [FK_UserSubscriptions_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [PaymentMethods] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_UserSubscriptions_SubscriptionTiers_SubscriptionTierId] FOREIGN KEY ([SubscriptionTierId]) REFERENCES [SubscriptionTiers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE TABLE [SubscriptionTransactions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SubscriptionId] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [Amount] decimal(10,2) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [PaymentMethodId] uniqueidentifier NULL,
        [ExternalTransactionId] nvarchar(200) NULL,
        [ExternalChargeId] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [FailureReason] nvarchar(500) NULL,
        [RetryCount] int NOT NULL,
        [NextRetryAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [ProcessedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [FailedAt] datetime2 NULL,
        [RefundedAt] datetime2 NULL,
        [RefundAmount] decimal(10,2) NULL,
        [CreatedFromIP] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_SubscriptionTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_SubscriptionTransactions_Amount_Positive] CHECK ([Amount] >= 0),
        CONSTRAINT [CK_SubscriptionTransactions_CompletedAt_After_ProcessedAt] CHECK ([CompletedAt] IS NULL OR [ProcessedAt] IS NULL OR [ProcessedAt] <= [CompletedAt]),
        CONSTRAINT [CK_SubscriptionTransactions_Currency_Valid] CHECK (LEN([Currency]) = 3 AND [Currency] LIKE '[A-Z][A-Z][A-Z]'),
        CONSTRAINT [CK_SubscriptionTransactions_FailedAt_After_CreatedAt] CHECK ([FailedAt] IS NULL OR [CreatedAt] <= [FailedAt]),
        CONSTRAINT [CK_SubscriptionTransactions_NextRetryAt_After_FailedAt] CHECK ([NextRetryAt] IS NULL OR [FailedAt] IS NULL OR [FailedAt] <= [NextRetryAt]),
        CONSTRAINT [CK_SubscriptionTransactions_ProcessedAt_After_CreatedAt] CHECK ([ProcessedAt] IS NULL OR [CreatedAt] <= [ProcessedAt]),
        CONSTRAINT [CK_SubscriptionTransactions_RefundAmount_LessEqual_Amount] CHECK ([RefundAmount] IS NULL OR [RefundAmount] <= [Amount]),
        CONSTRAINT [CK_SubscriptionTransactions_RefundAmount_Positive] CHECK ([RefundAmount] IS NULL OR [RefundAmount] >= 0),
        CONSTRAINT [CK_SubscriptionTransactions_RefundedAt_After_CompletedAt] CHECK ([RefundedAt] IS NULL OR [CompletedAt] IS NULL OR [CompletedAt] <= [RefundedAt]),
        CONSTRAINT [CK_SubscriptionTransactions_RetryCount_NonNegative] CHECK ([RetryCount] >= 0),
        CONSTRAINT [FK_SubscriptionTransactions_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [PaymentMethods] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_SubscriptionTransactions_UserSubscriptions_SubscriptionId] FOREIGN KEY ([SubscriptionId]) REFERENCES [UserSubscriptions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_PaymentMethods_ExpiresAt] ON [PaymentMethods] ([ExpiresAt]) WHERE [ExpiresAt] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_PaymentMethods_UserId] ON [PaymentMethods] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PaymentMethods_UserId_DefaultUnique] ON [PaymentMethods] ([UserId], [IsDefault]) WHERE [IsDefault] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_PaymentMethods_UserId_IsValid] ON [PaymentMethods] ([UserId], [IsValid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionTiers_IsActive_SortOrder] ON [SubscriptionTiers] ([IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SubscriptionTiers_Type] ON [SubscriptionTiers] ([Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_SubscriptionTransactions_ExternalChargeId] ON [SubscriptionTransactions] ([ExternalChargeId]) WHERE [ExternalChargeId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_SubscriptionTransactions_ExternalTransactionId] ON [SubscriptionTransactions] ([ExternalTransactionId]) WHERE [ExternalTransactionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_SubscriptionTransactions_NextRetryAt] ON [SubscriptionTransactions] ([NextRetryAt]) WHERE [NextRetryAt] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionTransactions_PaymentMethodId] ON [SubscriptionTransactions] ([PaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionTransactions_Status] ON [SubscriptionTransactions] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionTransactions_SubscriptionId] ON [SubscriptionTransactions] ([SubscriptionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionTransactions_SubscriptionId_Type_CreatedAt] ON [SubscriptionTransactions] ([SubscriptionId], [Type], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionTransactions_UserId] ON [SubscriptionTransactions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_SubscriptionTransactions_UserId_CreatedAt] ON [SubscriptionTransactions] ([UserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_UserSubscriptions_ExternalCustomerId] ON [UserSubscriptions] ([ExternalCustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserSubscriptions_ExternalSubscriptionId] ON [UserSubscriptions] ([ExternalSubscriptionId]) WHERE [ExternalSubscriptionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_UserSubscriptions_NextBillingDate] ON [UserSubscriptions] ([NextBillingDate]) WHERE [NextBillingDate] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_UserSubscriptions_PaymentMethodId] ON [UserSubscriptions] ([PaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_UserSubscriptions_SubscriptionTierId] ON [UserSubscriptions] ([SubscriptionTierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    CREATE INDEX [IX_UserSubscriptions_UserId] ON [UserSubscriptions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserSubscriptions_UserId_ActiveStatus] ON [UserSubscriptions] ([UserId], [Status]) WHERE [Status] IN (1, 2)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026142738_AddSubscriptionSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251026142738_AddSubscriptionSystem', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026143253_AddExternalCustomerIdToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [ExternalCustomerId] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026143253_AddExternalCustomerIdToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251026143253_AddExternalCustomerIdToUser', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026143937_AddSubscriptionRetryFields'
)
BEGIN
    ALTER TABLE [UserSubscriptions] ADD [NextRetryAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026143937_AddSubscriptionRetryFields'
)
BEGIN
    ALTER TABLE [UserSubscriptions] ADD [RetryCount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026143937_AddSubscriptionRetryFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251026143937_AddSubscriptionRetryFields', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251026162205_CreateSubscriptionSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251026162205_CreateSubscriptionSystem', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106195042_UpdateModelSnapshot'
)
BEGIN
    DROP TABLE [RefreshTokens];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106195042_UpdateModelSnapshot'
)
BEGIN
    DROP TABLE [RevokedTokens];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251106195042_UpdateModelSnapshot'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251106195042_UpdateModelSnapshot', N'9.0.10');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118163346_AddUniqueConstraintUserBadges'
)
BEGIN
    DROP INDEX [IX_UserBadges_UserId_BadgeType] ON [UserBadges];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118163346_AddUniqueConstraintUserBadges'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_UserBadges_UserId_BadgeType_IsActive] ON [UserBadges] ([UserId], [BadgeType], [IsActive]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118163346_AddUniqueConstraintUserBadges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118163346_AddUniqueConstraintUserBadges', N'9.0.10');
END;

COMMIT;
GO

