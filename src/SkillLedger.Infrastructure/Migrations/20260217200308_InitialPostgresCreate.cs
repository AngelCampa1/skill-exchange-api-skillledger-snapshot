using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SkillLedger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BadgeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiredVerification = table.Column<string>(type: "text", nullable: false),
                    ExpirationPeriod = table.Column<TimeSpan>(type: "interval", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DisplayPriority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeDefinitions", x => x.Id);
                    table.UniqueConstraint("AK_BadgeDefinitions_BadgeType", x => x.BadgeType);
                });

            migrationBuilder.CreateTable(
                name: "IpGeolocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddressHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CountryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Isp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsVpn = table.Column<bool>(type: "boolean", nullable: false),
                    IsProxy = table.Column<bool>(type: "boolean", nullable: false),
                    IsDataCenter = table.Column<bool>(type: "boolean", nullable: false),
                    IsRestricted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpGeolocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsSystemManaged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CreditBonus = table.Column<int>(type: "integer", nullable: false),
                    MaxActiveProjects = table.Column<int>(type: "integer", nullable: false),
                    MaxTeamMembers = table.Column<int>(type: "integer", nullable: false),
                    PrioritySupport = table.Column<bool>(type: "boolean", nullable: false),
                    ApiAccess = table.Column<bool>(type: "boolean", nullable: false),
                    AdvancedAnalytics = table.Column<bool>(type: "boolean", nullable: false),
                    AdvancedFraudDetection = table.Column<bool>(type: "boolean", nullable: false),
                    MultiSignature = table.Column<bool>(type: "boolean", nullable: false),
                    CustomIntegrations = table.Column<bool>(type: "boolean", nullable: false),
                    MaxMonthlyEarnings = table.Column<int>(type: "integer", nullable: false),
                    Features = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TaxCompliant = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastLockoutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalCustomerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgeCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CriteriaName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CriteriaValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CriteriaExpression = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadgeCriteria_BadgeDefinitions_BadgeType",
                        column: x => x.BadgeType,
                        principalTable: "BadgeDefinitions",
                        principalColumn: "BadgeType",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AntiGamingAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Open"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AntiGamingAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AntiGamingAlerts_Users_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AntiGamingAlerts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryReputationScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ProjectCount = table.Column<int>(type: "integer", nullable: false),
                    LastProjectAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryReputationScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryReputationScores_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryReputationScores_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentModerationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    WasApproved = table.Column<bool>(type: "boolean", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    RequiredHumanReview = table.Column<bool>(type: "boolean", nullable: false),
                    FlaggedCategories = table.Column<string>(type: "text", nullable: true),
                    ModerationScores = table.Column<string>(type: "text", nullable: true),
                    BlockedTerms = table.Column<string>(type: "text", nullable: true),
                    ReasonForRejection = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AnalysisId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentModerationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentModerationLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentReviewQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    ContentText = table.Column<string>(type: "text", nullable: true),
                    ContentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModerationResult = table.Column<string>(type: "text", nullable: true),
                    ReviewPriority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Decision = table.Column<int>(type: "integer", nullable: true),
                    ReviewComments = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentReviewQueues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentReviewQueues_Users_AssignedReviewerId",
                        column: x => x.AssignedReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContentReviewQueues_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedBalance = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EncryptedPendingBalance = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EncryptedTotalEarned = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EncryptedTotalSpent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    LastTransactionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    KeyIdentifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    BlockedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditWallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditWallets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomBlocklistTerms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Term = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomBlocklistTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomBlocklistTerms_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeviceFingerprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FingerprintHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    UsedForRegistration = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuspicious = table.Column<bool>(type: "boolean", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    RiskFactors = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceFingerprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceFingerprints_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Experiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Organization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Experiences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GamingRiskAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    RiskFactors = table.Column<string>(type: "text", nullable: true),
                    DetectedPatterns = table.Column<string>(type: "text", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ModelVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "1.0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamingRiskAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamingRiskAssessments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Last4Digits = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    CardholderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    BillingPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProfileSlug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Company = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    GitHubUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TwitterUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreditBudget = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModerationStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ModerationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisputeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    LocationLatitude = table.Column<double>(type: "double precision", nullable: true),
                    LocationLongitude = table.Column<double>(type: "double precision", nullable: true),
                    LocationCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsRemoteWork = table.Column<bool>(type: "boolean", nullable: false),
                    SearchText = table.Column<string>(type: "text", nullable: true),
                    ComplexityScore = table.Column<int>(type: "integer", nullable: false),
                    IsUrgent = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Projects_Users_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Questionnaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsTemplate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RequiresReview = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    MaxResponses = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questionnaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questionnaires_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SavedSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SearchCriteriaJson = table.Column<string>(type: "text", nullable: false),
                    SearchCriteria = table.Column<string>(type: "text", nullable: false),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationFrequency = table.Column<int>(type: "integer", nullable: false),
                    LastNotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutionCount = table.Column<int>(type: "integer", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    LastExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedSearches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BadgeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BadgeDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EarnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    VerificationLevel = table.Column<string>(type: "text", nullable: false),
                    VerificationEvidence = table.Column<string>(type: "text", nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IntegrityHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBadges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBadges_Users_VerifiedBy",
                        column: x => x.VerifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserBehaviorMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MetricValue = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    CalculationWindow = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsAnomaly = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviorMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBehaviorMetrics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCreditReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportMonth = table.Column<int>(type: "integer", nullable: false, comment: "Report month in YYYYMM format"),
                    TotalEarned = table.Column<int>(type: "integer", nullable: false, comment: "Total credits earned during the month"),
                    TotalSpent = table.Column<int>(type: "integer", nullable: false, comment: "Total credits spent during the month"),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false, comment: "Number of transactions during the month"),
                    AverageTransactionSize = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, comment: "Average transaction amount (calculated field)"),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()", comment: "When the report was generated"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()", comment: "When the report was last updated"),
                    EarningsByType = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "JSON data of earnings breakdown by transaction type"),
                    SpendingByType = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "JSON data of spending breakdown by transaction type"),
                    ProjectEarnings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "JSON data of project-related earnings"),
                    PeakBalance = table.Column<int>(type: "integer", nullable: false),
                    LowestBalance = table.Column<int>(type: "integer", nullable: false),
                    StartingBalance = table.Column<int>(type: "integer", nullable: false),
                    EndingBalance = table.Column<int>(type: "integer", nullable: false),
                    UniqueProjectsCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedProjectsCount = table.Column<int>(type: "integer", nullable: false),
                    LargestIncomingTransaction = table.Column<int>(type: "integer", nullable: false),
                    LargestOutgoingTransaction = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCreditReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCreditReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNetworkConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    User1Id = table.Column<Guid>(type: "uuid", nullable: false),
                    User2Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConnectionStrength = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    InteractionCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastInteractionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsValidated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNetworkConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNetworkConnections_Users_User1Id",
                        column: x => x.User1Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNetworkConnections_Users_User2Id",
                        column: x => x.User2Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserReputationScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ProjectCompletionRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    AverageResponseTime = table.Column<int>(type: "integer", nullable: false),
                    TotalProjectsCompleted = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReputationScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReputationScores_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSanctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SanctionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    IssuedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    AppealNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AppealSubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSanctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSanctions_Users_IssuedBy",
                        column: x => x.IssuedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSanctions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Proficiency = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSkills_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    SubmittedEvidence = table.Column<string>(type: "text", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationRequests_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VerificationRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperienceSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperienceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperienceSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExperienceSkills_Experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "Experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExperienceSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionTierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalSubscriptionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalCustomerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingCycleCount = table.Column<int>(type: "integer", nullable: false),
                    IsAnnual = table.Column<bool>(type: "boolean", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastPaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AppliedCouponId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AppliedPromoCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DiscountEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionTiers_SubscriptionTierId",
                        column: x => x.SubscriptionTierId,
                        principalTable: "SubscriptionTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoverLetter = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ProposedTimeline = table.Column<int>(type: "integer", nullable: true),
                    SkillMatchScore = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClientFeedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SubmittedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    IsAvailableImmediately = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ProposedBudget = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectApplications_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectApplications_Users_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDeliverables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDeliverables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDeliverables_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectEscrows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<int>(type: "integer", nullable: false, comment: "Total amount of credits in escrow"),
                    ReleasedAmount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Amount released to provider so far"),
                    Status = table.Column<int>(type: "integer", nullable: false, comment: "Current status of escrow account"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()", comment: "When escrow account was created"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()", comment: "When escrow was last updated"),
                    CreatedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true, comment: "IP address where escrow was created"),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Optional notes about the escrow"),
                    DisputeReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Reason for dispute if status is Disputed"),
                    DisputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisputeResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisputeResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisputeResolutionNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Admin notes for dispute resolution"),
                    RequiresMultiSignature = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether escrow requires multi-signature approval")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEscrows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectEscrows_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectEscrows_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectEscrows_Users_DisputeResolvedByUserId",
                        column: x => x.DisputeResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectEscrows_Users_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevieweeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OverallRating = table.Column<int>(type: "integer", nullable: false),
                    QualityRating = table.Column<int>(type: "integer", nullable: true),
                    CommunicationRating = table.Column<int>(type: "integer", nullable: true),
                    TimelinessRating = table.Column<int>(type: "integer", nullable: true),
                    ProfessionalismRating = table.Column<int>(type: "integer", nullable: true),
                    ReviewText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ResponseText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ModerationStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ModerationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    HasPhotoAttachments = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PhotoAttachmentCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectReviews_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectReviews_Users_RevieweeId",
                        column: x => x.RevieweeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectReviews_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSkills",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProficiencyRequired = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    Weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSkills", x => new { x.ProjectId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_ProjectSkills_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimelineData = table.Column<string>(type: "text", nullable: true),
                    MilestoneData = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IntegrationStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectWorkspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectWorkspaces_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectWorkspaces_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectWorkspaces_Users_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Configuration = table.Column<string>(type: "text", nullable: true),
                    DefaultValue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PlaceholderText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ValidationRegex = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValidationMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MinValue = table.Column<int>(type: "integer", nullable: true),
                    MaxValue = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireQuestions_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireId = table.Column<Guid>(type: "uuid", nullable: false),
                    RespondentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SubmittedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireResponses_Questionnaires_QuestionnaireId",
                        column: x => x.QuestionnaireId,
                        principalTable: "Questionnaires",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionnaireResponses_Users_RespondentUserId",
                        column: x => x.RespondentUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionnaireResponses_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BadgeEarningHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    ActionBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ActionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeEarningHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadgeEarningHistory_UserBadges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "UserBadges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BadgeEarningHistory_Users_ActionBy",
                        column: x => x.ActionBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BadgeEarningHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillEndorsements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndorsedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewText = table.Column<string>(type: "text", nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillEndorsements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillEndorsements_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SkillEndorsements_UserSkills_UserSkillId",
                        column: x => x.UserSkillId,
                        principalTable: "UserSkills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillEndorsements_Users_EndorsedByUserId",
                        column: x => x.EndorsedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalTransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalChargeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CreatedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionTransactions_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SubscriptionTransactions_UserSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "UserSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubscriptionTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectApplicationAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StorageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsVirusScanned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsSafe = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectApplicationAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectApplicationAttachments_ProjectApplications_ProjectAp~",
                        column: x => x.ProjectApplicationId,
                        principalTable: "ProjectApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderSelections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContractTerms = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    EscrowAmount = table.Column<int>(type: "integer", nullable: false),
                    SelectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpectedStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedCompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    NegotiationNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SelectedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    IsEscrowFunded = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsContractSigned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderSelections_ProjectApplications_SelectedApplicationId",
                        column: x => x.SelectedApplicationId,
                        principalTable: "ProjectApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderSelections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderSelections_Users_SelectedProviderId",
                        column: x => x.SelectedProviderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TransactionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PreviousTransactionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InitiatedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    FlaggedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FlaggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProjectEscrowId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_ProjectEscrows_ProjectEscrowId",
                        column: x => x.ProjectEscrowId,
                        principalTable: "ProjectEscrows",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CreditTransactions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_Users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_Users_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscrowMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EscrowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Human-readable milestone description"),
                    Amount = table.Column<int>(type: "integer", nullable: false, comment: "Credits to release for this milestone"),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether milestone has been released"),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleaseNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "Notes about milestone release"),
                    ExpectedCompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualCompletionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Display order for milestones"),
                    IsBlocking = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether milestone blocks subsequent releases"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()", comment: "When milestone was created"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()", comment: "When milestone was last updated"),
                    CreatedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true, comment: "IP address where milestone was created"),
                    LinkedDeliverableId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscrowMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscrowMilestones_ProjectDeliverables_LinkedDeliverableId",
                        column: x => x.LinkedDeliverableId,
                        principalTable: "ProjectDeliverables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EscrowMilestones_ProjectEscrows_EscrowId",
                        column: x => x.EscrowId,
                        principalTable: "ProjectEscrows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscrowMilestones_Users_ReleasedByUserId",
                        column: x => x.ReleasedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReputationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReputationHistories_ProjectReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "ProjectReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReputationHistories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReputationHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContainerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileType = table.Column<int>(type: "integer", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false),
                    SecurityScanPassed = table.Column<bool>(type: "boolean", nullable: false),
                    ModerationResult = table.Column<string>(type: "text", nullable: true),
                    SecurityScanResult = table.Column<string>(type: "text", nullable: true),
                    ImageVariants = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProjectReviewId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedFiles_ProjectReviews_ProjectReviewId",
                        column: x => x.ProjectReviewId,
                        principalTable: "ProjectReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UploadedFiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentFolders_DocumentFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "DocumentFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentFolders_ProjectWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "ProjectWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentFolders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentFolders_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TypingIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastTypingAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ConnectionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypingIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TypingIndicators_ProjectWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "ProjectWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TypingIndicators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MessageType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AttachmentSize = table.Column<long>(type: "bigint", nullable: true),
                    AttachmentMimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReplyToMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SenderIpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    SenderUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceMessages_ProjectWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "ProjectWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspaceMessages_WorkspaceMessages_ReplyToMessageId",
                        column: x => x.ReplyToMessageId,
                        principalTable: "WorkspaceMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OptionValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Metadata = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOptions_QuestionnaireQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionnaireQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionnaireResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseValue = table.Column<string>(type: "text", nullable: true),
                    SelectedOptionIds = table.Column<string>(type: "text", nullable: true),
                    FileAttachments = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ValidationError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionResponses_QuestionnaireQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuestionnaireQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionResponses_QuestionnaireResponses_QuestionnaireRespo~",
                        column: x => x.QuestionnaireResponseId,
                        principalTable: "QuestionnaireResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    TransferFee = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TransactionHash = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    CreditTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    InitiatedFromIP = table.Column<string>(type: "character varying(45)", unicode: false, maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReversalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReversedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptSignature = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTransfers_CreditTransactions_CreditTransactionId",
                        column: x => x.CreditTransactionId,
                        principalTable: "CreditTransactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CreditTransfers_FromUser",
                        column: x => x.FromUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditTransfers_ToUser",
                        column: x => x.ToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditTransfers_Users_ReversedByUserId",
                        column: x => x.ReversedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                },
                comment: "Direct credit transfers between users with comprehensive audit trail and fraud prevention");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectReviewId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_EscrowMilestones_EntityId",
                        column: x => x.EntityId,
                        principalTable: "EscrowMilestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogs_ProjectEscrows_EntityId",
                        column: x => x.EntityId,
                        principalTable: "ProjectEscrows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogs_ProjectReviews_ProjectReviewId",
                        column: x => x.ProjectReviewId,
                        principalTable: "ProjectReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EscrowMilestoneId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false),
                    WeightPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    AcceptanceCriteria = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMilestones_EscrowMilestones_EscrowMilestoneId",
                        column: x => x.EscrowMilestoneId,
                        principalTable: "EscrowMilestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectMilestones_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMilestones_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectMilestones_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    FolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    SecurityScanResult = table.Column<string>(type: "text", nullable: true),
                    SecurityScanPassed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ParentDocumentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceDocuments_DocumentFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "DocumentFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkspaceDocuments_ProjectWorkspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "ProjectWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceDocuments_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspaceDocuments_Users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkspaceDocuments_WorkspaceDocuments_ParentDocumentId",
                        column: x => x.ParentDocumentId,
                        principalTable: "WorkspaceDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Emoji = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageReactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageReactions_WorkspaceMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "WorkspaceMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliverableSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Unique identifier for the deliverable submission"),
                    MilestoneId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Foreign key to the associated milestone"),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: false, comment: "User who submitted this deliverable"),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "Type of deliverable submission"),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, comment: "Title or summary of the submission"),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true, comment: "Detailed description of submitted work"),
                    SubmissionUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "URL for link or repository submissions"),
                    TextContent = table.Column<string>(type: "text", nullable: true, comment: "Text content for text-type submissions"),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When the submission was created"),
                    SubmittedFromIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true, comment: "IP address from which submission was made"),
                    SubmissionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Optional notes from the submitter"),
                    IsReviewed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether this submission has been reviewed"),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Whether this submission was approved"),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When this submission was reviewed"),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true, comment: "User who reviewed this submission"),
                    ReviewFeedback = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true, comment: "Feedback from the reviewer")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliverableSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliverableSubmissions_ProjectMilestones",
                        column: x => x.MilestoneId,
                        principalTable: "ProjectMilestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliverableSubmissions_Users_ReviewedBy",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliverableSubmissions_Users_SubmittedBy",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    AccessType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "view"),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAccesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAccesses_WorkspaceDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "WorkspaceDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ShareMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AccessToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentShares_Users_RevokedBy",
                        column: x => x.RevokedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentShares_Users_SharedBy",
                        column: x => x.SharedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentShares_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentShares_WorkspaceDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "WorkspaceDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliverableSubmissionFiles",
                columns: table => new
                {
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliverableSubmissionFiles", x => new { x.SubmissionId, x.FileId });
                    table.ForeignKey(
                        name: "FK_DeliverableSubmissionFiles_DeliverableSubmissions_Submissio~",
                        column: x => x.SubmissionId,
                        principalTable: "DeliverableSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliverableSubmissionFiles_UploadedFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AntiGamingAlerts_AlertType",
                table: "AntiGamingAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_AntiGamingAlerts_CreatedAt",
                table: "AntiGamingAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AntiGamingAlerts_ResolvedBy",
                table: "AntiGamingAlerts",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AntiGamingAlerts_Severity",
                table: "AntiGamingAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AntiGamingAlerts_Status",
                table: "AntiGamingAlerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AntiGamingAlerts_UserId",
                table: "AntiGamingAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityId",
                table: "AuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IPAddress",
                table: "AuditLogs",
                column: "IPAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IPAddress_Timestamp_Success",
                table: "AuditLogs",
                columns: new[] { "IPAddress", "Timestamp", "Success" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ProjectId",
                table: "AuditLogs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ProjectReviewId",
                table: "AuditLogs",
                column: "ProjectReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeCriteria_BadgeType",
                table: "BadgeCriteria",
                column: "BadgeType");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeCriteria_BadgeType_Priority",
                table: "BadgeCriteria",
                columns: new[] { "BadgeType", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeCriteria_IsActive",
                table: "BadgeCriteria",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeDefinitions_BadgeType",
                table: "BadgeDefinitions",
                column: "BadgeType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BadgeDefinitions_Category",
                table: "BadgeDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeDefinitions_DisplayPriority",
                table: "BadgeDefinitions",
                column: "DisplayPriority");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeDefinitions_IsActive",
                table: "BadgeDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeEarningHistory_Action",
                table: "BadgeEarningHistory",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeEarningHistory_ActionAt",
                table: "BadgeEarningHistory",
                column: "ActionAt");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeEarningHistory_ActionBy",
                table: "BadgeEarningHistory",
                column: "ActionBy");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeEarningHistory_BadgeId",
                table: "BadgeEarningHistory",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeEarningHistory_UserId",
                table: "BadgeEarningHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeEarningHistory_UserId_ActionAt",
                table: "BadgeEarningHistory",
                columns: new[] { "UserId", "ActionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryReputationScores_Score",
                table: "CategoryReputationScores",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryReputationScores_SkillId",
                table: "CategoryReputationScores",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryReputationScores_UserId",
                table: "CategoryReputationScores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryReputationScores_UserSkill",
                table: "CategoryReputationScores",
                columns: new[] { "UserId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentModerationLogs_CreatedAt",
                table: "ContentModerationLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentModerationLogs_UserId",
                table: "ContentModerationLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentModerationLogs_WasApproved",
                table: "ContentModerationLogs",
                column: "WasApproved");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReviewQueues_AssignedReviewerId",
                table: "ContentReviewQueues",
                column: "AssignedReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReviewQueues_CreatedAt",
                table: "ContentReviewQueues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReviewQueues_ReviewPriority",
                table: "ContentReviewQueues",
                column: "ReviewPriority");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReviewQueues_Status",
                table: "ContentReviewQueues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContentReviewQueues_UserId",
                table: "ContentReviewQueues",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Amount_Created",
                table: "CreditTransactions",
                columns: new[] { "Amount", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Chain_Integrity",
                table: "CreditTransactions",
                columns: new[] { "CreatedAt", "PreviousTransactionHash" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Completion",
                table: "CreditTransactions",
                columns: new[] { "Status", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Escrow_Operations",
                table: "CreditTransactions",
                columns: new[] { "ProjectId", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_FromUser_Created",
                table: "CreditTransactions",
                columns: new[] { "FromUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Hash_Unique",
                table: "CreditTransactions",
                column: "TransactionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_IP_Created",
                table: "CreditTransactions",
                columns: new[] { "InitiatedFromIP", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_IsFlagged",
                table: "CreditTransactions",
                column: "IsFlagged");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Project_Type",
                table: "CreditTransactions",
                columns: new[] { "ProjectId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_ProjectEscrowId",
                table: "CreditTransactions",
                column: "ProjectEscrowId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Reporting",
                table: "CreditTransactions",
                columns: new[] { "Type", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Status",
                table: "CreditTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_ToUser_Created",
                table: "CreditTransactions",
                columns: new[] { "ToUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Type",
                table: "CreditTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Users_Created",
                table: "CreditTransactions",
                columns: new[] { "FromUserId", "ToUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_CreatedAt",
                table: "CreditTransfers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_CreditTransactionId",
                table: "CreditTransfers",
                column: "CreditTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_FromUser_CreatedAt",
                table: "CreditTransfers",
                columns: new[] { "FromUserId", "CreatedAt" })
                .Annotation("Npgsql:IndexInclude", new[] { "Amount", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_FromUser_Status_CreatedAt",
                table: "CreditTransfers",
                columns: new[] { "FromUserId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_IP_CreatedAt",
                table: "CreditTransfers",
                columns: new[] { "InitiatedFromIP", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_ReversedByUserId",
                table: "CreditTransfers",
                column: "ReversedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_Status",
                table: "CreditTransfers",
                column: "Status")
                .Annotation("Npgsql:IndexInclude", new[] { "CreatedAt", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_Status_CompletedAt",
                table: "CreditTransfers",
                columns: new[] { "Status", "CompletedAt" },
                filter: "\"CompletedAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_Status_CreatedAt_Amount",
                table: "CreditTransfers",
                columns: new[] { "Status", "CreatedAt", "Amount" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_ToUser_CreatedAt",
                table: "CreditTransfers",
                columns: new[] { "ToUserId", "CreatedAt" })
                .Annotation("Npgsql:IndexInclude", new[] { "Amount", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_TransactionHash_Unique",
                table: "CreditTransfers",
                column: "TransactionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditWallets_IsBlocked",
                table: "CreditWallets",
                column: "IsBlocked");

            migrationBuilder.CreateIndex(
                name: "IX_CreditWallets_LastTransactionAt",
                table: "CreditWallets",
                column: "LastTransactionAt");

            migrationBuilder.CreateIndex(
                name: "IX_CreditWallets_User_Created",
                table: "CreditWallets",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditWallets_UserId_Unique",
                table: "CreditWallets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomBlocklistTerms_AddedByUserId",
                table: "CustomBlocklistTerms",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomBlocklistTerms_ExpiresAt",
                table: "CustomBlocklistTerms",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomBlocklistTerms_IsActive",
                table: "CustomBlocklistTerms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CustomBlocklistTerms_Term",
                table: "CustomBlocklistTerms",
                column: "Term");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissionFiles_FileId",
                table: "DeliverableSubmissionFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissionFiles_SubmissionId",
                table: "DeliverableSubmissionFiles",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissions_IsReviewed",
                table: "DeliverableSubmissions",
                column: "IsReviewed");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissions_Milestone_SubmittedAt",
                table: "DeliverableSubmissions",
                columns: new[] { "MilestoneId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissions_MilestoneId",
                table: "DeliverableSubmissions",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissions_Review_Status",
                table: "DeliverableSubmissions",
                columns: new[] { "IsReviewed", "IsApproved", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissions_ReviewedByUserId",
                table: "DeliverableSubmissions",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissions_SubmittedAt",
                table: "DeliverableSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissions_SubmittedBy",
                table: "DeliverableSubmissions",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverableSubmissions_User_SubmittedAt",
                table: "DeliverableSubmissions",
                columns: new[] { "SubmittedByUserId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFingerprints_CreatedAt",
                table: "DeviceFingerprints",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFingerprints_FingerprintHash",
                table: "DeviceFingerprints",
                column: "FingerprintHash");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFingerprints_UserId",
                table: "DeviceFingerprints",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccesses_AccessedAt",
                table: "DocumentAccesses",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccesses_Document_User",
                table: "DocumentAccesses",
                columns: new[] { "DocumentId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccesses_DocumentId",
                table: "DocumentAccesses",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccesses_User_AccessedAt",
                table: "DocumentAccesses",
                columns: new[] { "UserId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccesses_UserId",
                table: "DocumentAccesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_CreatedAt",
                table: "DocumentFolders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_CreatedBy",
                table: "DocumentFolders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_DeletedBy",
                table: "DocumentFolders",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_ParentFolderId",
                table: "DocumentFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_UniqueName",
                table: "DocumentFolders",
                columns: new[] { "WorkspaceId", "ParentFolderId", "FolderName" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_Workspace_NotDeleted",
                table: "DocumentFolders",
                columns: new[] { "WorkspaceId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_WorkspaceId",
                table: "DocumentFolders",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_AccessToken",
                table: "DocumentShares",
                column: "AccessToken",
                unique: true,
                filter: "\"AccessToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_CreatedAt",
                table: "DocumentShares",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_Document_User",
                table: "DocumentShares",
                columns: new[] { "DocumentId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_DocumentId",
                table: "DocumentShares",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_ExpiresAt",
                table: "DocumentShares",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_RevokedBy",
                table: "DocumentShares",
                column: "RevokedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_SharedBy",
                table: "DocumentShares",
                column: "SharedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_User_Active",
                table: "DocumentShares",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentShares_UserId",
                table: "DocumentShares",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowMilestones_EscrowId",
                table: "EscrowMilestones",
                column: "EscrowId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowMilestones_EscrowId_IsReleased",
                table: "EscrowMilestones",
                columns: new[] { "EscrowId", "IsReleased" });

            migrationBuilder.CreateIndex(
                name: "IX_EscrowMilestones_EscrowId_SequenceOrder",
                table: "EscrowMilestones",
                columns: new[] { "EscrowId", "SequenceOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EscrowMilestones_ExpectedCompletionDate",
                table: "EscrowMilestones",
                column: "ExpectedCompletionDate",
                filter: "\"ExpectedCompletionDate\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowMilestones_IsReleased",
                table: "EscrowMilestones",
                column: "IsReleased");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowMilestones_LinkedDeliverableId",
                table: "EscrowMilestones",
                column: "LinkedDeliverableId",
                filter: "\"LinkedDeliverableId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowMilestones_ReleasedByUserId",
                table: "EscrowMilestones",
                column: "ReleasedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_UserId",
                table: "Experiences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_UserId_DisplayOrder",
                table: "Experiences",
                columns: new[] { "UserId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_UserId_IsCurrent",
                table: "Experiences",
                columns: new[] { "UserId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_UserId_IsVisible",
                table: "Experiences",
                columns: new[] { "UserId", "IsVisible" });

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_UserId_StartDate",
                table: "Experiences",
                columns: new[] { "UserId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_UserId_Type",
                table: "Experiences",
                columns: new[] { "UserId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_ExperienceSkills_ExperienceId",
                table: "ExperienceSkills",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperienceSkills_ExperienceId_SkillId",
                table: "ExperienceSkills",
                columns: new[] { "ExperienceId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperienceSkills_SkillId",
                table: "ExperienceSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_GamingRiskAssessments_AnalyzedAt",
                table: "GamingRiskAssessments",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_GamingRiskAssessments_ModelVersion",
                table: "GamingRiskAssessments",
                column: "ModelVersion");

            migrationBuilder.CreateIndex(
                name: "IX_GamingRiskAssessments_RiskScore",
                table: "GamingRiskAssessments",
                column: "RiskScore");

            migrationBuilder.CreateIndex(
                name: "IX_GamingRiskAssessments_UserId",
                table: "GamingRiskAssessments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpGeolocations_ExpiresAt",
                table: "IpGeolocations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IpGeolocations_IpAddressHash",
                table: "IpGeolocations",
                column: "IpAddressHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_CreatedAt",
                table: "MessageReactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId",
                table: "MessageReactions",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId_Emoji",
                table: "MessageReactions",
                columns: new[] { "MessageId", "Emoji" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId_UserId_Emoji_Unique",
                table: "MessageReactions",
                columns: new[] { "MessageId", "UserId", "Emoji" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_UserId",
                table: "MessageReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_CreatedAt",
                table: "PasswordResets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_ExpiresAt",
                table: "PasswordResets",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_TokenHash",
                table: "PasswordResets",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_User_Created",
                table: "PasswordResets",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_UserId",
                table: "PasswordResets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_ExpiresAt",
                table: "PaymentMethods",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_UserId",
                table: "PaymentMethods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_UserId_DefaultUnique",
                table: "PaymentMethods",
                columns: new[] { "UserId", "IsDefault" },
                unique: true,
                filter: "\"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_UserId_IsValid",
                table: "PaymentMethods",
                columns: new[] { "UserId", "IsValid" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Category",
                table: "Permissions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_IsActive",
                table: "Permissions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Company",
                table: "Profiles",
                column: "Company");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_FirstName_LastName",
                table: "Profiles",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_IsPublic",
                table: "Profiles",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId",
                table: "Profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_ViewCount_UserId",
                table: "Profiles",
                columns: new[] { "ViewCount", "UserId" },
                descending: new[] { true, false });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplicationAttachments_ContentType",
                table: "ProjectApplicationAttachments",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplicationAttachments_IsSafe",
                table: "ProjectApplicationAttachments",
                column: "IsSafe");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplicationAttachments_IsVirusScanned",
                table: "ProjectApplicationAttachments",
                column: "IsVirusScanned");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplicationAttachments_ProjectApplicationId",
                table: "ProjectApplicationAttachments",
                column: "ProjectApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplicationAttachments_UploadedAt",
                table: "ProjectApplicationAttachments",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplications_CreatedAt",
                table: "ProjectApplications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplications_ProjectId",
                table: "ProjectApplications",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplications_ProjectId_Status_CreatedAt",
                table: "ProjectApplications",
                columns: new[] { "ProjectId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplications_ProviderId",
                table: "ProjectApplications",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplications_ProviderId_Status_CreatedAt",
                table: "ProjectApplications",
                columns: new[] { "ProviderId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplications_SkillMatchScore",
                table: "ProjectApplications",
                column: "SkillMatchScore");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApplications_Status",
                table: "ProjectApplications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectApplications_ProjectId_ProviderId",
                table: "ProjectApplications",
                columns: new[] { "ProjectId", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeliverables_IsCompleted",
                table: "ProjectDeliverables",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeliverables_ProjectId",
                table: "ProjectDeliverables",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDeliverables_ProjectId_OrderIndex",
                table: "ProjectDeliverables",
                columns: new[] { "ProjectId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEscrows_ClientId",
                table: "ProjectEscrows",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEscrows_CreatedAt",
                table: "ProjectEscrows",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEscrows_DisputeResolvedByUserId",
                table: "ProjectEscrows",
                column: "DisputeResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEscrows_ProjectId_Unique",
                table: "ProjectEscrows",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEscrows_ProviderId",
                table: "ProjectEscrows",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEscrows_Status",
                table: "ProjectEscrows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEscrows_Status_CreatedAt",
                table: "ProjectEscrows",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_AssignedToUserId",
                table: "ProjectMilestones",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_CreatedAt",
                table: "ProjectMilestones",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_CreatedByUserId",
                table: "ProjectMilestones",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_DueDate",
                table: "ProjectMilestones",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_EscrowMilestoneId",
                table: "ProjectMilestones",
                column: "EscrowMilestoneId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_ProjectId",
                table: "ProjectMilestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_ProjectId_SequenceOrder",
                table: "ProjectMilestones",
                columns: new[] { "ProjectId", "SequenceOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_Status",
                table: "ProjectMilestones",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_CreatedAt",
                table: "ProjectReviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_ModerationStatus",
                table: "ProjectReviews",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_ProjectId",
                table: "ProjectReviews",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_ProjectId_Type",
                table: "ProjectReviews",
                columns: new[] { "ProjectId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_PublishedAt",
                table: "ProjectReviews",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_RevieweeId",
                table: "ProjectReviews",
                column: "RevieweeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_RevieweeId_Status",
                table: "ProjectReviews",
                columns: new[] { "RevieweeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_ReviewerId",
                table: "ProjectReviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_Status",
                table: "ProjectReviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReviews_Status_ModerationStatus",
                table: "ProjectReviews",
                columns: new[] { "Status", "ModerationStatus" });

            migrationBuilder.CreateIndex(
                name: "UX_ProjectReviews_ProjectId_ReviewerId_Type",
                table: "ProjectReviews",
                columns: new[] { "ProjectId", "ReviewerId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ClientId",
                table: "Projects",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedAt",
                table: "Projects",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreditBudget",
                table: "Projects",
                column: "CreditBudget");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_EndDate",
                table: "Projects",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ModerationStatus",
                table: "Projects",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProviderId",
                table: "Projects",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status_ModerationStatus",
                table: "Projects",
                columns: new[] { "Status", "ModerationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSkills_ProficiencyRequired",
                table: "ProjectSkills",
                column: "ProficiencyRequired");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSkills_ProjectId",
                table: "ProjectSkills",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSkills_SkillId",
                table: "ProjectSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSkills_Weight",
                table: "ProjectSkills",
                column: "Weight");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_ClientId",
                table: "ProjectWorkspaces",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_CreatedAt",
                table: "ProjectWorkspaces",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_ProjectId",
                table: "ProjectWorkspaces",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_ProviderId",
                table: "ProjectWorkspaces",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWorkspaces_Status",
                table: "ProjectWorkspaces",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSelections_ProjectId_Unique",
                table: "ProviderSelections",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSelections_SelectedApplicationId",
                table: "ProviderSelections",
                column: "SelectedApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSelections_SelectedAt_Status",
                table: "ProviderSelections",
                columns: new[] { "SelectedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSelections_SelectedProviderId",
                table: "ProviderSelections",
                column: "SelectedProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderSelections_Status",
                table: "ProviderSelections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_DisplayOrder",
                table: "QuestionnaireQuestions",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_IsActive",
                table: "QuestionnaireQuestions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_IsRequired",
                table: "QuestionnaireQuestions",
                column: "IsRequired");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_Questionnaire_Active_Order",
                table: "QuestionnaireQuestions",
                columns: new[] { "QuestionnaireId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_Questionnaire_Required_Active",
                table: "QuestionnaireQuestions",
                columns: new[] { "QuestionnaireId", "IsRequired", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_QuestionnaireId",
                table: "QuestionnaireQuestions",
                column: "QuestionnaireId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_Type",
                table: "QuestionnaireQuestions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_IsComplete",
                table: "QuestionnaireResponses",
                column: "IsComplete");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_IsSubmitted",
                table: "QuestionnaireResponses",
                column: "IsSubmitted");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_Questionnaire_Respondent_Status",
                table: "QuestionnaireResponses",
                columns: new[] { "QuestionnaireId", "RespondentUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_Questionnaire_Status_Submitted",
                table: "QuestionnaireResponses",
                columns: new[] { "QuestionnaireId", "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_QuestionnaireId",
                table: "QuestionnaireResponses",
                column: "QuestionnaireId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_Respondent_Submitted_Updated",
                table: "QuestionnaireResponses",
                columns: new[] { "RespondentUserId", "IsSubmitted", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_RespondentUserId",
                table: "QuestionnaireResponses",
                column: "RespondentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_ReviewedAt",
                table: "QuestionnaireResponses",
                column: "ReviewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_ReviewedByUserId",
                table: "QuestionnaireResponses",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_StartedAt",
                table: "QuestionnaireResponses",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_Status",
                table: "QuestionnaireResponses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_Status_Reviewer_Reviewed",
                table: "QuestionnaireResponses",
                columns: new[] { "Status", "ReviewedByUserId", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_SubmittedAt",
                table: "QuestionnaireResponses",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_Unique_Submission",
                table: "QuestionnaireResponses",
                columns: new[] { "QuestionnaireId", "RespondentUserId" },
                unique: true,
                filter: "\"IsSubmitted\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireResponses_UpdatedAt",
                table: "QuestionnaireResponses",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_Active_Type_Created",
                table: "Questionnaires",
                columns: new[] { "IsActive", "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_CreatedAt",
                table: "Questionnaires",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_CreatedBy_Active_Updated",
                table: "Questionnaires",
                columns: new[] { "CreatedByUserId", "IsActive", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_CreatedByUserId",
                table: "Questionnaires",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_EndDate",
                table: "Questionnaires",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_IsActive",
                table: "Questionnaires",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_IsTemplate",
                table: "Questionnaires",
                column: "IsTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_StartDate",
                table: "Questionnaires",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_Template_Active_Updated",
                table: "Questionnaires",
                columns: new[] { "IsTemplate", "IsActive", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_Type",
                table: "Questionnaires",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Questionnaires_UpdatedAt",
                table: "Questionnaires",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_DisplayOrder",
                table: "QuestionOptions",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_IsActive",
                table: "QuestionOptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_IsDefault",
                table: "QuestionOptions",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_Question_Active_Order",
                table: "QuestionOptions",
                columns: new[] { "QuestionId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_Question_Default_Active",
                table: "QuestionOptions",
                columns: new[] { "QuestionId", "IsDefault", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionId",
                table: "QuestionOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_CreatedAt",
                table: "QuestionResponses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_IsValid",
                table: "QuestionResponses",
                column: "IsValid");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_Question_Valid_Updated",
                table: "QuestionResponses",
                columns: new[] { "QuestionId", "IsValid", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_QuestionId",
                table: "QuestionResponses",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_QuestionnaireResponseId",
                table: "QuestionResponses",
                column: "QuestionnaireResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_Response_Question",
                table: "QuestionResponses",
                columns: new[] { "QuestionnaireResponseId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionResponses_UpdatedAt",
                table: "QuestionResponses",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationHistories_Date",
                table: "ReputationHistories",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationHistories_ProjectId",
                table: "ReputationHistories",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationHistories_ReviewId",
                table: "ReputationHistories",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationHistories_UserDate",
                table: "ReputationHistories",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ReputationHistories_UserId",
                table: "ReputationHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_GrantedAt",
                table: "RolePermissions",
                column: "GrantedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_GrantedByUserId",
                table: "RolePermissions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_IsActive",
                table: "RolePermissions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_UserId",
                table: "SavedSearches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillEndorsements_EndorsedByUserId",
                table: "SkillEndorsements",
                column: "EndorsedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillEndorsements_SkillId",
                table: "SkillEndorsements",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillEndorsements_UserSkillId",
                table: "SkillEndorsements",
                column: "UserSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillEndorsements_UserSkillId_EndorsedByUserId",
                table: "SkillEndorsements",
                columns: new[] { "UserSkillId", "EndorsedByUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillEndorsements_UserSkillId_IsVisible",
                table: "SkillEndorsements",
                columns: new[] { "UserSkillId", "IsVisible" });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Category",
                table: "Skills",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Category_Name",
                table: "Skills",
                columns: new[] { "Category", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_IsActive",
                table: "Skills",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTiers_IsActive_SortOrder",
                table: "SubscriptionTiers",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTiers_Type",
                table: "SubscriptionTiers",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransactions_ExternalChargeId",
                table: "SubscriptionTransactions",
                column: "ExternalChargeId",
                filter: "\"ExternalChargeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransactions_ExternalTransactionId",
                table: "SubscriptionTransactions",
                column: "ExternalTransactionId",
                filter: "\"ExternalTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransactions_NextRetryAt",
                table: "SubscriptionTransactions",
                column: "NextRetryAt",
                filter: "\"NextRetryAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransactions_PaymentMethodId",
                table: "SubscriptionTransactions",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransactions_Status",
                table: "SubscriptionTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransactions_SubscriptionId",
                table: "SubscriptionTransactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransactions_SubscriptionId_Type_CreatedAt",
                table: "SubscriptionTransactions",
                columns: new[] { "SubscriptionId", "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransactions_UserId",
                table: "SubscriptionTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTransactions_UserId_CreatedAt",
                table: "SubscriptionTransactions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TypingIndicators_ConnectionId",
                table: "TypingIndicators",
                column: "ConnectionId",
                filter: "\"ConnectionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TypingIndicators_LastTypingAt",
                table: "TypingIndicators",
                column: "LastTypingAt");

            migrationBuilder.CreateIndex(
                name: "IX_TypingIndicators_UserId",
                table: "TypingIndicators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TypingIndicators_WorkspaceId",
                table: "TypingIndicators",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TypingIndicators_WorkspaceId_LastTypingAt",
                table: "TypingIndicators",
                columns: new[] { "WorkspaceId", "LastTypingAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TypingIndicators_WorkspaceId_UserId_Unique",
                table: "TypingIndicators",
                columns: new[] { "WorkspaceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_CreatedAt",
                table: "UploadedFiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_FileType",
                table: "UploadedFiles",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_IsApproved",
                table: "UploadedFiles",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_ProjectReviewId",
                table: "UploadedFiles",
                column: "ProjectReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFiles_UserId",
                table: "UploadedFiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_BadgeType",
                table: "UserBadges",
                column: "BadgeType");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_EarnedAt",
                table: "UserBadges",
                column: "EarnedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_ExpiresAt",
                table: "UserBadges",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_IsActive_ExpiresAt",
                table: "UserBadges",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserId",
                table: "UserBadges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_UserId_BadgeType_IsActive",
                table: "UserBadges",
                columns: new[] { "UserId", "BadgeType", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_UserBadges_VerifiedBy",
                table: "UserBadges",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorMetrics_CalculatedAt",
                table: "UserBehaviorMetrics",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorMetrics_IsAnomaly",
                table: "UserBehaviorMetrics",
                column: "IsAnomaly");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorMetrics_MetricName",
                table: "UserBehaviorMetrics",
                column: "MetricName");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorMetrics_User_Metric_Date",
                table: "UserBehaviorMetrics",
                columns: new[] { "UserId", "MetricName", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorMetrics_UserId",
                table: "UserBehaviorMetrics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditReports_GeneratedAt",
                table: "UserCreditReports",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditReports_ReportMonth",
                table: "UserCreditReports",
                column: "ReportMonth");

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditReports_UserId",
                table: "UserCreditReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditReports_UserId_GeneratedAt",
                table: "UserCreditReports",
                columns: new[] { "UserId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCreditReports_UserId_ReportMonth",
                table: "UserCreditReports",
                columns: new[] { "UserId", "ReportMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkConnections_ConnectionStrength",
                table: "UserNetworkConnections",
                column: "ConnectionStrength");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkConnections_ConnectionType",
                table: "UserNetworkConnections",
                column: "ConnectionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkConnections_DetectedAt",
                table: "UserNetworkConnections",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkConnections_LastInteractionAt",
                table: "UserNetworkConnections",
                column: "LastInteractionAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkConnections_User1Id",
                table: "UserNetworkConnections",
                column: "User1Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkConnections_User2Id",
                table: "UserNetworkConnections",
                column: "User2Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserNetworkConnections_UserPair",
                table: "UserNetworkConnections",
                columns: new[] { "User1Id", "User2Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationScores_LastUpdated",
                table: "UserReputationScores",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationScores_OverallScore",
                table: "UserReputationScores",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationScores_UserId",
                table: "UserReputationScores",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status_CreatedAt",
                table: "Users",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                filter: "\"UserName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctions_ExpiresAt",
                table: "UserSanctions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctions_IssuedAt",
                table: "UserSanctions",
                column: "IssuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctions_IssuedBy",
                table: "UserSanctions",
                column: "IssuedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctions_SanctionType",
                table: "UserSanctions",
                column: "SanctionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctions_Severity",
                table: "UserSanctions",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctions_Status",
                table: "UserSanctions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctions_UserId",
                table: "UserSanctions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSkills_SkillId",
                table: "UserSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSkills_SkillId_UserId",
                table: "UserSkills",
                columns: new[] { "SkillId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSkills_UserId",
                table: "UserSkills",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSkills_UserId_IsFeatured",
                table: "UserSkills",
                columns: new[] { "UserId", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSkills_UserId_IsVisible",
                table: "UserSkills",
                columns: new[] { "UserId", "IsVisible" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSkills_UserId_SkillId",
                table: "UserSkills",
                columns: new[] { "UserId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_AppliedCouponId",
                table: "UserSubscriptions",
                column: "AppliedCouponId",
                filter: "\"AppliedCouponId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_ExternalCustomerId",
                table: "UserSubscriptions",
                column: "ExternalCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_ExternalSubscriptionId",
                table: "UserSubscriptions",
                column: "ExternalSubscriptionId",
                unique: true,
                filter: "\"ExternalSubscriptionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_NextBillingDate",
                table: "UserSubscriptions",
                column: "NextBillingDate",
                filter: "\"NextBillingDate\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PaymentMethodId",
                table: "UserSubscriptions",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionTierId",
                table: "UserSubscriptions",
                column: "SubscriptionTierId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_ActiveStatus",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Status" },
                unique: true,
                filter: "\"Status\" IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_BadgeType",
                table: "VerificationRequests",
                column: "BadgeType");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_RequestedAt",
                table: "VerificationRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_ReviewedBy",
                table: "VerificationRequests",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_Status",
                table: "VerificationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_UserId",
                table: "VerificationRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_UserId_BadgeType_Status",
                table: "VerificationRequests",
                columns: new[] { "UserId", "BadgeType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceDocuments_CreatedAt",
                table: "WorkspaceDocuments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceDocuments_DeletedBy",
                table: "WorkspaceDocuments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceDocuments_FolderId",
                table: "WorkspaceDocuments",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceDocuments_ParentDocument",
                table: "WorkspaceDocuments",
                column: "ParentDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceDocuments_UploadedBy",
                table: "WorkspaceDocuments",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceDocuments_Workspace_NotDeleted",
                table: "WorkspaceDocuments",
                columns: new[] { "WorkspaceId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceDocuments_WorkspaceId",
                table: "WorkspaceDocuments",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMessages_CreatedAt",
                table: "WorkspaceMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMessages_MessageText",
                table: "WorkspaceMessages",
                column: "MessageText",
                filter: "\"MessageText\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMessages_ReplyToMessageId",
                table: "WorkspaceMessages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMessages_SenderId",
                table: "WorkspaceMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMessages_Status_Tracking",
                table: "WorkspaceMessages",
                columns: new[] { "WorkspaceId", "Status", "SenderId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMessages_WorkspaceId",
                table: "WorkspaceMessages",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceMessages_WorkspaceId_CreatedAt",
                table: "WorkspaceMessages",
                columns: new[] { "WorkspaceId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AntiGamingAlerts");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BadgeCriteria");

            migrationBuilder.DropTable(
                name: "BadgeEarningHistory");

            migrationBuilder.DropTable(
                name: "CategoryReputationScores");

            migrationBuilder.DropTable(
                name: "ContentModerationLogs");

            migrationBuilder.DropTable(
                name: "ContentReviewQueues");

            migrationBuilder.DropTable(
                name: "CreditTransfers");

            migrationBuilder.DropTable(
                name: "CreditWallets");

            migrationBuilder.DropTable(
                name: "CustomBlocklistTerms");

            migrationBuilder.DropTable(
                name: "DeliverableSubmissionFiles");

            migrationBuilder.DropTable(
                name: "DeviceFingerprints");

            migrationBuilder.DropTable(
                name: "DocumentAccesses");

            migrationBuilder.DropTable(
                name: "DocumentShares");

            migrationBuilder.DropTable(
                name: "ExperienceSkills");

            migrationBuilder.DropTable(
                name: "GamingRiskAssessments");

            migrationBuilder.DropTable(
                name: "IpGeolocations");

            migrationBuilder.DropTable(
                name: "MessageReactions");

            migrationBuilder.DropTable(
                name: "PasswordResets");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "ProjectApplicationAttachments");

            migrationBuilder.DropTable(
                name: "ProjectSkills");

            migrationBuilder.DropTable(
                name: "ProviderSelections");

            migrationBuilder.DropTable(
                name: "QuestionOptions");

            migrationBuilder.DropTable(
                name: "QuestionResponses");

            migrationBuilder.DropTable(
                name: "ReputationHistories");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SavedSearches");

            migrationBuilder.DropTable(
                name: "SkillEndorsements");

            migrationBuilder.DropTable(
                name: "SubscriptionTransactions");

            migrationBuilder.DropTable(
                name: "TypingIndicators");

            migrationBuilder.DropTable(
                name: "UserBehaviorMetrics");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserCreditReports");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserNetworkConnections");

            migrationBuilder.DropTable(
                name: "UserReputationScores");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSanctions");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "VerificationRequests");

            migrationBuilder.DropTable(
                name: "BadgeDefinitions");

            migrationBuilder.DropTable(
                name: "UserBadges");

            migrationBuilder.DropTable(
                name: "CreditTransactions");

            migrationBuilder.DropTable(
                name: "DeliverableSubmissions");

            migrationBuilder.DropTable(
                name: "UploadedFiles");

            migrationBuilder.DropTable(
                name: "WorkspaceDocuments");

            migrationBuilder.DropTable(
                name: "Experiences");

            migrationBuilder.DropTable(
                name: "WorkspaceMessages");

            migrationBuilder.DropTable(
                name: "ProjectApplications");

            migrationBuilder.DropTable(
                name: "QuestionnaireQuestions");

            migrationBuilder.DropTable(
                name: "QuestionnaireResponses");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "UserSkills");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ProjectMilestones");

            migrationBuilder.DropTable(
                name: "ProjectReviews");

            migrationBuilder.DropTable(
                name: "DocumentFolders");

            migrationBuilder.DropTable(
                name: "Questionnaires");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "SubscriptionTiers");

            migrationBuilder.DropTable(
                name: "EscrowMilestones");

            migrationBuilder.DropTable(
                name: "ProjectWorkspaces");

            migrationBuilder.DropTable(
                name: "ProjectDeliverables");

            migrationBuilder.DropTable(
                name: "ProjectEscrows");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
