using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Simplified test data seeder focused on performance without complex cleanup
/// UPDATED: Each test gets its own data to prevent concurrency issues
/// </summary>
public class SimpleTestDataSeeder
{
    // Well-known test tier ID so tests can reference it without querying
    public static readonly Guid TestProfessionalTierId = new Guid("10000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Seeds minimal standard test data for each test database
    /// Each test gets its own isolated data to prevent concurrency issues
    /// </summary>
    public static Task SeedStandardDataAsync(SkillLedgerDbContext context)
    {
        // FIXED: Check if data already exists in this context instead of using static state
        if (context.Skills.Any() || context.Users.Any())
        {
            return Task.CompletedTask; // Data already seeded for this context
        }

        // Seed a Professional tier so test users can have active subscriptions
        // (free tier has been removed; all users must have a paid subscription)
        var professionalTier = new SubscriptionTier
        {
            Id = TestProfessionalTierId,
            Name = "Professional",
            Type = SubscriptionTierType.Professional,
            Price = 19.00m,
            AnnualPrice = 190.00m,
            MaxActiveProjects = 10,
            MaxTeamMembers = 5,
            IsActive = true,
            SortOrder = 1,
            Features = "[\"escrow\",\"messaging\",\"analytics\"]"
        };

        // TEST-MED-002 FIX: Generate unique GUIDs for test data to prevent parallel test conflicts
        // Skills use predictable GUIDs since they're system-managed and need consistency
        var standardSkills = new[]
        {
            new Skill { Id = Guid.NewGuid(), Name = "React", Category = "Frontend", IsActive = true, IsSystemManaged = true },
            new Skill { Id = Guid.NewGuid(), Name = "Node.js", Category = "Backend", IsActive = true, IsSystemManaged = true },
            new Skill { Id = Guid.NewGuid(), Name = "Python", Category = "Backend", IsActive = true, IsSystemManaged = true }
        };

        // TEST-MED-002 FIX: Generate unique user IDs and emails per test run to prevent conflicts
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        var standardUsers = new[]
        {
            new User { Id = Guid.NewGuid(), Email = $"standard.client1.{uniquePrefix}@test.com", UserName = $"standard.client1.{uniquePrefix}@test.com", Status = UserStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new User { Id = Guid.NewGuid(), Email = $"standard.client2.{uniquePrefix}@test.com", UserName = $"standard.client2.{uniquePrefix}@test.com", Status = UserStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-25) }
        };

        context.SubscriptionTiers.Add(professionalTier);
        context.Skills.AddRange(standardSkills);
        context.Users.AddRange(standardUsers);
        context.SaveChanges();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates an active subscription for the given user using the seeded Professional tier.
    /// Call this after CreateTestUserAsync to satisfy SubscriptionMiddleware in integration tests.
    /// </summary>
    public static void CreateActiveSubscriptionForUser(SkillLedgerDbContext context, Guid userId)
    {
        // Ensure the tier exists in this context
        if (!context.SubscriptionTiers.Any(t => t.Id == TestProfessionalTierId))
        {
            var tier = new SubscriptionTier
            {
                Id = TestProfessionalTierId,
                Name = "Professional",
                Type = SubscriptionTierType.Professional,
                Price = 19.00m,
                AnnualPrice = 190.00m,
                MaxActiveProjects = 10,
                MaxTeamMembers = 5,
                IsActive = true,
                SortOrder = 1,
                Features = "[\"escrow\",\"messaging\",\"analytics\"]"
            };
            context.SubscriptionTiers.Add(tier);
        }

        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionTierId = TestProfessionalTierId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-10),
            NextBillingDate = DateTime.UtcNow.AddDays(20),
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
        };
        context.UserSubscriptions.Add(subscription);
        context.SaveChanges();
    }

    /// <summary>
    /// Gets standard test users for a specific context
    /// </summary>
    public static IReadOnlyList<User> GetStandardUsers(SkillLedgerDbContext context)
    {
        return context.Users
            .Where(u => u.Email.StartsWith("standard."))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets standard test skills for a specific context
    /// </summary>
    public static IReadOnlyList<Skill> GetStandardSkills(SkillLedgerDbContext context)
    {
        return context.Skills
            .Where(s => s.IsSystemManaged)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Creates a test user with a unique ID for test-specific scenarios
    /// </summary>
    public static User CreateTestUser(string emailPrefix, UserStatus status = UserStatus.Active)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = $"{emailPrefix}.{Guid.NewGuid().ToString("N")[..8]}@test.com",
            UserName = $"{emailPrefix}.{Guid.NewGuid().ToString("N")[..8]}@test.com",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test project with a unique ID for test-specific scenarios
    /// </summary>
    public static Project CreateTestProject(string titlePrefix, Guid clientId, int budget = 2000)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Title = $"{titlePrefix} {Guid.NewGuid().ToString("N")[..8]}",
            Description = $"Test project description for {titlePrefix}",
            CreditBudget = budget,
            Status = ProjectStatus.Published,
            IsRemoteWork = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Smart cleanup - removes test-specific data while preserving standard test data
    /// This ensures that standard data (users, skills) needed by tests remains available
    /// </summary>
    public static void FastCleanup(SkillLedgerDbContext context)
    {
        try
        {
            // FIXED: Preserve standard test data while cleaning test-specific data
            // Standard data is identified by specific patterns (system managed, standard.* emails, etc.)

            // Get standard user and skill IDs that should be preserved
            var standardUserIds = context.Users
                .Where(u => u.Email.StartsWith("standard."))
                .Select(u => u.Id)
                .ToHashSet();

            var standardSkillIds = context.Skills
                .Where(s => s.IsSystemManaged == true)
                .Select(s => s.Id)
                .ToHashSet();

            // Remove test-specific data in proper order to avoid foreign key constraints
            var testSpecificEntities = new List<object>();

            // Collect test-specific entities (exclude standard data)
            testSpecificEntities.AddRange(context.ReputationHistories
                .Where(rh => !standardUserIds.Contains(rh.UserId)).ToList());

            testSpecificEntities.AddRange(context.CategoryReputationScores.ToList()); // These are test-specific
            testSpecificEntities.AddRange(context.UserReputationScores
                .Where(urs => !standardUserIds.Contains(urs.UserId)).ToList());

            testSpecificEntities.AddRange(context.ProjectReviews.ToList()); // Projects are test-specific
            testSpecificEntities.AddRange(context.SkillEndorsements.ToList()); // Endorsements are test-specific
            testSpecificEntities.AddRange(context.UserSkills
                .Where(us => !standardUserIds.Contains(us.UserId) || !standardSkillIds.Contains(us.SkillId)).ToList());

            testSpecificEntities.AddRange(context.ExperienceSkills
                .Where(es => !standardSkillIds.Contains(es.SkillId)).ToList());

            testSpecificEntities.AddRange(context.Experiences
                .Where(e => !standardUserIds.Contains(e.UserId)).ToList());

            testSpecificEntities.AddRange(context.ProjectWorkspaces.ToList()); // Projects are test-specific
            testSpecificEntities.AddRange(context.ProjectSkills.ToList()); // Projects are test-specific
            testSpecificEntities.AddRange(context.ProjectDeliverables.ToList()); // Projects are test-specific
            testSpecificEntities.AddRange(context.Projects.ToList()); // Projects are test-specific

            // Remove test-specific skills (keep system-managed ones)
            testSpecificEntities.AddRange(context.Skills
                .Where(s => !s.IsSystemManaged).ToList());

            testSpecificEntities.AddRange(context.Profiles
                .Where(p => !standardUserIds.Contains(p.UserId)).ToList());

            testSpecificEntities.AddRange(context.CreditTransfers.ToList());
            testSpecificEntities.AddRange(context.ProjectEscrows.ToList());
            testSpecificEntities.AddRange(context.CreditTransactions.ToList());
            testSpecificEntities.AddRange(context.CreditWallets
                .Where(cw => !standardUserIds.Contains(cw.UserId)).ToList());

            // Remove test user subscriptions (keep none — these are test-specific)
            testSpecificEntities.AddRange(context.UserSubscriptions
                .Where(us => !standardUserIds.Contains(us.UserId)).ToList());

            testSpecificEntities.AddRange(context.PasswordResets.ToList());
            // RefreshTokens removed - cookie-based authentication
            testSpecificEntities.AddRange(context.UserRoles
                .Where(ur => !standardUserIds.Contains(ur.UserId)).ToList());

            testSpecificEntities.AddRange(context.RolePermissions.ToList());
            testSpecificEntities.AddRange(context.Permissions.ToList());
            testSpecificEntities.AddRange(context.Roles.ToList());
            testSpecificEntities.AddRange(context.AuditLogs
                .Where(al => !al.UserId.HasValue || !standardUserIds.Contains(al.UserId.Value)).ToList());

            // Remove test-specific users (keep standard users)
            testSpecificEntities.AddRange(context.Users
                .Where(u => !standardUserIds.Contains(u.Id)).ToList());

            // Remove all test-specific entities
            if (testSpecificEntities.Any())
            {
                context.RemoveRange(testSpecificEntities);
                context.SaveChanges();
            }
        }
        catch (Exception)
        {
            // If cleanup fails, that's okay - the in-memory database will be recreated
            // for the next test anyway
        }
    }
}