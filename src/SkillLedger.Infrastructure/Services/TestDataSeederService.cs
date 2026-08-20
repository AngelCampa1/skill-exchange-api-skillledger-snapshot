using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs.TestData;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services.TestData;
using System.Diagnostics;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for seeding comprehensive test data for E2E testing
/// </summary>
public class TestDataSeederService : ITestDataSeederService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<TestDataSeederService> _logger;
    private readonly UserTestDataFactory _userFactory;
    private readonly ProjectTestDataFactory _projectFactory;
    private readonly CreditTestDataFactory _creditFactory;
    private readonly WorkspaceTestDataFactory _workspaceFactory;

    private const string TEST_DATA_SEEDER = "TEST_DATA_SEEDER";

    public TestDataSeederService(
        SkillLedgerDbContext context,
        IPasswordHasher<User> passwordHasher,
        IEncryptionService encryptionService,
        ILogger<TestDataSeederService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _encryptionService = encryptionService;
        _logger = logger;

        _userFactory = new UserTestDataFactory(passwordHasher);
        _projectFactory = new ProjectTestDataFactory();
        _creditFactory = new CreditTestDataFactory(encryptionService);
        _workspaceFactory = new WorkspaceTestDataFactory();
    }

    public async Task<SeedResult> SeedAsync(bool fullSeed = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SeedResult
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("🌱 Starting test data seeding...");

            // Clean existing test data first
            await CleanTestDataAsync();

            // Seed all phases
            await SeedPhase1FoundationDataAsync(result);
            await SeedPhase2UserDataAsync(result);
            await SeedPhase3UserRelatedDataAsync(result);
            await SeedPhase4ProjectDataAsync(result);
            await SeedPhase5FinancialDataAsync(result);

            if (fullSeed)
            {
                await SeedPhase6CollaborationDataAsync(result);
                await SeedPhase7ReputationDataAsync(result);
                await SeedPhase8AuditDataAsync(result);
            }

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Success = true;
            result.Message = "Database seeded successfully!";
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("✅ Database seeded successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Success = false;
            result.Message = "Seeding failed";
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogError(ex, "❌ Failed to seed database");

            return result;
        }
    }

    public async Task CleanTestDataAsync()
    {
        _logger.LogInformation("🧹 Cleaning existing test data...");

        // Delete in reverse dependency order
        await _context.ProjectReviews
            .Where(r => _context.Projects.Any(p => p.Id == r.ProjectId && p.CreatedFromIP == TEST_DATA_SEEDER))
            .ExecuteDeleteAsync();

        await _context.WorkspaceDocuments
            .Where(d => _context.ProjectWorkspaces.Any(w => w.Id == d.WorkspaceId &&
                        _context.Projects.Any(p => p.Id == w.ProjectId && p.CreatedFromIP == TEST_DATA_SEEDER)))
            .ExecuteDeleteAsync();

        await _context.WorkspaceMessages
            .Where(m => _context.ProjectWorkspaces.Any(w => w.Id == m.WorkspaceId &&
                        _context.Projects.Any(p => p.Id == w.ProjectId && p.CreatedFromIP == TEST_DATA_SEEDER)))
            .ExecuteDeleteAsync();

        await _context.ProjectWorkspaces
            .Where(w => _context.Projects.Any(p => p.Id == w.ProjectId && p.CreatedFromIP == TEST_DATA_SEEDER))
            .ExecuteDeleteAsync();

        await _context.CreditTransfers
            .Where(t => t.InitiatedFromIP == TEST_DATA_SEEDER)
            .ExecuteDeleteAsync();

        await _context.CreditTransactions
            .Where(t => t.InitiatedFromIP == TEST_DATA_SEEDER)
            .ExecuteDeleteAsync();

        await _context.ProjectEscrows
            .Where(e => e.CreatedFromIP == TEST_DATA_SEEDER)
            .ExecuteDeleteAsync();

        await _context.ProjectApplications
            .Where(a => _context.Projects.Any(p => p.Id == a.ProjectId && p.CreatedFromIP == TEST_DATA_SEEDER))
            .ExecuteDeleteAsync();

        await _context.ProjectDeliverables
            .Where(d => _context.Projects.Any(p => p.Id == d.ProjectId && p.CreatedFromIP == TEST_DATA_SEEDER))
            .ExecuteDeleteAsync();

        await _context.Projects
            .Where(p => p.CreatedFromIP == TEST_DATA_SEEDER)
            .ExecuteDeleteAsync();

        await _context.UserSubscriptions
            .Where(s => _context.Users.Any(u => u.Id == s.UserId && u.CreatedFromIP == TEST_DATA_SEEDER))
            .ExecuteDeleteAsync();

        await _context.Experiences
            .Where(e => _context.Users.Any(u => u.Id == e.UserId && u.CreatedFromIP == TEST_DATA_SEEDER))
            .ExecuteDeleteAsync();

        await _context.UserSkills
            .Where(us => _context.Users.Any(u => u.Id == us.UserId && u.CreatedFromIP == TEST_DATA_SEEDER))
            .ExecuteDeleteAsync();

        await _context.CreditWallets
            .Where(w => _context.Users.Any(u => u.Id == w.UserId && u.CreatedFromIP == TEST_DATA_SEEDER))
            .ExecuteDeleteAsync();

        await _context.Profiles
            .Where(p => _context.Users.Any(u => u.Id == p.UserId && u.CreatedFromIP == TEST_DATA_SEEDER))
            .ExecuteDeleteAsync();

        await _context.Users
            .Where(u => u.CreatedFromIP == TEST_DATA_SEEDER)
            .ExecuteDeleteAsync();

        _logger.LogInformation("✅ Test data cleaned");
    }

    private async Task SeedPhase1FoundationDataAsync(SeedResult result)
    {
        _logger.LogInformation("📦 Phase 1: Seeding foundation data...");

        // Seed subscription tiers
        await SeedSubscriptionTiersAsync();

        // Seed skills
        var skillsCreated = await SeedSkillsAsync();
        result.SkillsCreated = skillsCreated;

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Phase 1 complete: {SkillCount} skills", skillsCreated);
    }

    private async Task SeedPhase2UserDataAsync(SeedResult result)
    {
        _logger.LogInformation("👥 Phase 2: Seeding user data...");

        var users = _userFactory.CreateAllUsers();
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        result.UsersCreated = users.Count;

        _logger.LogInformation("✅ Phase 2 complete: {UserCount} users", users.Count);
    }

    private async Task SeedPhase3UserRelatedDataAsync(SeedResult result)
    {
        _logger.LogInformation("📝 Phase 3: Seeding user-related data...");

        var users = await _context.Users.Where(u => u.CreatedFromIP == TEST_DATA_SEEDER).ToListAsync();

        // Profiles
        var profiles = _userFactory.CreateProfilesForUsers(users);
        await _context.Profiles.AddRangeAsync(profiles);
        result.ProfilesCreated = profiles.Count;

        // Credit Wallets (encrypted)
        var wallets = await _creditFactory.CreateWalletsForUsersAsync(users);
        await _context.CreditWallets.AddRangeAsync(wallets);
        result.WalletsCreated = wallets.Count;

        // User subscriptions
        await SeedUserSubscriptionsAsync(users);

        // User skills
        await SeedUserSkillsAsync(users);

        // Experiences
        await SeedExperiencesAsync(users);

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Phase 3 complete: {ProfileCount} profiles, {WalletCount} wallets",
            profiles.Count, wallets.Count);
    }

    private async Task SeedPhase4ProjectDataAsync(SeedResult result)
    {
        _logger.LogInformation("🚀 Phase 4: Seeding project data...");

        var users = await _context.Users.Where(u => u.CreatedFromIP == TEST_DATA_SEEDER).ToListAsync();

        var projects = _projectFactory.CreateAllProjects(users);
        await _context.Projects.AddRangeAsync(projects);
        await _context.SaveChangesAsync();

        result.ProjectsCreated = projects.Count;

        // Deliverables
        var deliverables = _projectFactory.CreateDeliverablesForProjects(projects);
        await _context.ProjectDeliverables.AddRangeAsync(deliverables);

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Phase 4 complete: {ProjectCount} projects, {DeliverableCount} deliverables",
            projects.Count, deliverables.Count);
    }

    private async Task SeedPhase5FinancialDataAsync(SeedResult result)
    {
        _logger.LogInformation("💰 Phase 5: Seeding financial data...");

        var users = await _context.Users.Where(u => u.CreatedFromIP == TEST_DATA_SEEDER).ToListAsync();
        var projects = await _context.Projects.Where(p => p.CreatedFromIP == TEST_DATA_SEEDER).ToListAsync();

        // Starting credit transactions
        var startingTransactions = _creditFactory.CreateStartingCreditTransactions(users);
        await _context.CreditTransactions.AddRangeAsync(startingTransactions);

        // Purchase transactions
        var purchaseTransactions = _creditFactory.CreatePurchaseTransactions(users);
        await _context.CreditTransactions.AddRangeAsync(purchaseTransactions);

        // Escrow accounts
        var escrows = _creditFactory.CreateEscrowForProjects(projects);
        await _context.ProjectEscrows.AddRangeAsync(escrows);
        await _context.SaveChangesAsync();

        result.EscrowsCreated = escrows.Count;

        // Escrow transactions
        var escrowTransactions = _creditFactory.CreateEscrowTransactions(escrows, projects);
        await _context.CreditTransactions.AddRangeAsync(escrowTransactions);

        // Bonus transactions
        var bonusTransactions = _creditFactory.CreateBonusTransactions(projects);
        await _context.CreditTransactions.AddRangeAsync(bonusTransactions);

        // P2P transfers
        var transfers = _creditFactory.CreateCreditTransfers(users);
        await _context.CreditTransfers.AddRangeAsync(transfers);

        await _context.SaveChangesAsync();

        result.TransactionsCreated = startingTransactions.Count + purchaseTransactions.Count +
                                       escrowTransactions.Count + bonusTransactions.Count;

        _logger.LogInformation("✅ Phase 5 complete: {EscrowCount} escrows, {TransactionCount} transactions",
            escrows.Count, result.TransactionsCreated);
    }

    private async Task SeedPhase6CollaborationDataAsync(SeedResult result)
    {
        _logger.LogInformation("💬 Phase 6: Seeding collaboration data...");

        var users = await _context.Users.Where(u => u.CreatedFromIP == TEST_DATA_SEEDER).ToListAsync();
        var projects = await _context.Projects.Where(p => p.CreatedFromIP == TEST_DATA_SEEDER).ToListAsync();

        // Workspaces
        var workspaces = _workspaceFactory.CreateWorkspacesForProjects(projects, users);
        await _context.ProjectWorkspaces.AddRangeAsync(workspaces);
        await _context.SaveChangesAsync();

        result.WorkspacesCreated = workspaces.Count;

        // Messages
        var messages = _workspaceFactory.CreateMessagesForWorkspaces(workspaces, projects, users);
        await _context.WorkspaceMessages.AddRangeAsync(messages);
        result.MessagesCreated = messages.Count;

        // Documents
        var documents = _workspaceFactory.CreateDocumentsForWorkspaces(workspaces, projects, users);
        await _context.WorkspaceDocuments.AddRangeAsync(documents);

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Phase 6 complete: {WorkspaceCount} workspaces, {MessageCount} messages",
            workspaces.Count, messages.Count);
    }

    private async Task SeedPhase7ReputationDataAsync(SeedResult result)
    {
        _logger.LogInformation("⭐ Phase 7: Seeding reputation data...");

        var users = await _context.Users.Where(u => u.CreatedFromIP == TEST_DATA_SEEDER).ToListAsync();
        var projects = await _context.Projects.Where(p => p.CreatedFromIP == TEST_DATA_SEEDER).ToListAsync();

        // Project reviews
        var reviews = _workspaceFactory.CreateReviewsForProjects(projects, users);
        await _context.ProjectReviews.AddRangeAsync(reviews);
        result.ReviewsCreated = reviews.Count;

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Phase 7 complete: {ReviewCount} reviews", reviews.Count);
    }

    private async Task SeedPhase8AuditDataAsync(SeedResult result)
    {
        _logger.LogInformation("📋 Phase 8: Seeding audit data...");

        // Audit logs would be created automatically by the audit service
        // For test data, we'll just log completion

        _logger.LogInformation("✅ Phase 8 complete");
    }

    private async Task SeedSubscriptionTiersAsync()
    {
        // Only seed if not already present
        if (await _context.SubscriptionTiers.AnyAsync())
            return;

        var tiers = new List<SubscriptionTier>
        {
            new SubscriptionTier
            {
                Id = Guid.NewGuid(),
                Type = SubscriptionTierType.Free,
                Name = "Free",
                Description = "Perfect for getting started",
                Price = 0,
                CreditBonus = 100,
                MaxActiveProjects = 2,
                MaxTeamMembers = 1,
                MaxMonthlyEarnings = 500,
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SubscriptionTier
            {
                Id = Guid.NewGuid(),
                Type = SubscriptionTierType.Professional,
                Name = "Professional",
                Description = "For serious freelancers",
                Price = 29,
                AnnualPrice = 290,
                CreditBonus = 200,
                MaxActiveProjects = 10,
                MaxTeamMembers = 3,
                MaxMonthlyEarnings = 5000,
                PrioritySupport = true,
                AdvancedAnalytics = true,
                IsActive = true,
                SortOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SubscriptionTier
            {
                Id = Guid.NewGuid(),
                Type = SubscriptionTierType.Business,
                Name = "Business",
                Description = "For teams and agencies",
                Price = 99,
                AnnualPrice = 990,
                CreditBonus = 500,
                MaxActiveProjects = 50,
                MaxTeamMembers = 10,
                PrioritySupport = true,
                ApiAccess = true,
                AdvancedAnalytics = true,
                AdvancedFraudDetection = true,
                IsActive = true,
                SortOrder = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SubscriptionTier
            {
                Id = Guid.NewGuid(),
                Type = SubscriptionTierType.Enterprise,
                Name = "Enterprise",
                Description = "For large organizations",
                Price = 499,
                AnnualPrice = 4990,
                CreditBonus = 2000,
                PrioritySupport = true,
                ApiAccess = true,
                AdvancedAnalytics = true,
                AdvancedFraudDetection = true,
                MultiSignature = true,
                CustomIntegrations = true,
                IsActive = true,
                SortOrder = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.SubscriptionTiers.AddRangeAsync(tiers);
    }

    private async Task<int> SeedSkillsAsync()
    {
        if (await _context.Skills.AnyAsync())
            return 0;

        var skillData = new Dictionary<string, string>
        {
            // Programming
            { "JavaScript", "Programming" },
            { "Python", "Programming" },
            { "Java", "Programming" },
            { "C#", "Programming" },
            { "TypeScript", "Programming" },
            { "Go", "Programming" },
            { "Rust", "Programming" },
            { "Swift", "Programming" },
            // Frontend
            { "React", "Frontend" },
            { "Vue.js", "Frontend" },
            { "Angular", "Frontend" },
            // Backend
            { "Node.js", "Backend" },
            { "Django", "Backend" },
            { "ASP.NET", "Backend" },
            // Design
            { "UI/UX Design", "Design" },
            { "Graphic Design", "Design" },
            { "Logo Design", "Design" },
            { "Branding", "Design" },
            { "Figma", "Design Tools" },
            { "Adobe Photoshop", "Design Tools" },
            // Business
            { "Content Writing", "Writing" },
            { "Copywriting", "Writing" },
            { "SEO", "Marketing" },
            { "Social Media Marketing", "Marketing" },
            { "Product Management", "Business" },
            { "Project Management", "Management" },
            // Data
            { "Data Science", "Data" },
            { "Machine Learning", "Data" },
            { "Data Analysis", "Data" },
            // Database
            { "SQL", "Database" },
            { "PostgreSQL", "Database" },
            { "MongoDB", "Database" },
            // Cloud
            { "AWS", "Cloud" },
            { "Azure", "Cloud" },
            { "Docker", "DevOps" },
            { "Kubernetes", "DevOps" },
            // Mobile
            { "iOS Development", "Mobile" },
            { "Android Development", "Mobile" },
            { "Mobile App Development", "Mobile" }
        };

        var skills = skillData.Select(kvp => new Skill
        {
            Id = Guid.NewGuid(),
            Name = kvp.Key,
            Category = kvp.Value,
            IsSystemManaged = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await _context.Skills.AddRangeAsync(skills);
        return skills.Count;
    }

    private async Task SeedUserSubscriptionsAsync(List<User> users)
    {
        var tiers = await _context.SubscriptionTiers.ToListAsync();
        var freeTier = tiers.FirstOrDefault(t => t.Type == SubscriptionTierType.Free);
        var proTier = tiers.FirstOrDefault(t => t.Type == SubscriptionTierType.Professional);
        var businessTier = tiers.FirstOrDefault(t => t.Type == SubscriptionTierType.Business);
        var enterpriseTier = tiers.FirstOrDefault(t => t.Type == SubscriptionTierType.Enterprise);

        foreach (var user in users)
        {
            var tier = GetSubscriptionTierForUser(user, freeTier, proTier, businessTier, enterpriseTier);
            if (tier == null) continue;

            var subscription = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SubscriptionTierId = tier.Id,
                Status = SubscriptionStatus.Active,
                StartDate = user.CreatedAt,
                AutoRenew = true,
                IsAnnual = false,
                CreatedAt = user.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.UserSubscriptions.AddAsync(subscription);
        }
    }

    private SubscriptionTier? GetSubscriptionTierForUser(
        User user,
        SubscriptionTier? free,
        SubscriptionTier? pro,
        SubscriptionTier? business,
        SubscriptionTier? enterprise)
    {
        if (user.Email.Contains("anderson") || user.Email.Contains("patricia")) return enterprise;
        if (user.Email.Contains("robert.chen") || user.Email.Contains("jennifer") || user.Email.Contains("maria")) return business;
        if (user.Email.Contains("kumar") || user.Email.Contains("goldstein") || user.Email.Contains("marcus") ||
            user.Email.Contains("sophia") || user.Email.Contains("alex") || user.Email.Contains("admin") ||
            user.Email.Contains("moderator")) return pro;

        return free;
    }

    private async Task SeedUserSkillsAsync(List<User> users)
    {
        var skills = await _context.Skills.ToListAsync();
        var react = skills.FirstOrDefault(s => s.Name == "React");
        var nodejs = skills.FirstOrDefault(s => s.Name == "Node.js");
        var python = skills.FirstOrDefault(s => s.Name == "Python");
        var design = skills.FirstOrDefault(s => s.Name == "UI/UX Design");
        var typescript = skills.FirstOrDefault(s => s.Name == "TypeScript");
        var csharp = skills.FirstOrDefault(s => s.Name == "C#");
        var aws = skills.FirstOrDefault(s => s.Name == "AWS");
        var sql = skills.FirstOrDefault(s => s.Name == "SQL");
        var projectMgmt = skills.FirstOrDefault(s => s.Name == "Project Management");

        // Add skills to all seeded users for profile completion
        foreach (var user in users)
        {
            // Everyone gets at least one skill based on their email pattern
            var skillsToAdd = new List<(Skill? skill, SkillProficiency proficiency, int years)>();

            if (user.Email.Contains("kumar"))
            {
                skillsToAdd.Add((react, SkillProficiency.Expert, 6));
                skillsToAdd.Add((typescript, SkillProficiency.Expert, 5));
            }
            else if (user.Email.Contains("chen"))
            {
                skillsToAdd.Add((design, SkillProficiency.Intermediate, 3));
                skillsToAdd.Add((react, SkillProficiency.Intermediate, 2));
            }
            else if (user.Email.Contains("goldstein"))
            {
                skillsToAdd.Add((projectMgmt, SkillProficiency.Expert, 8));
                skillsToAdd.Add((sql, SkillProficiency.Intermediate, 4));
            }
            else if (user.Email.Contains("maria"))
            {
                skillsToAdd.Add((design, SkillProficiency.Expert, 7));
            }
            else if (user.Email.Contains("jennifer"))
            {
                skillsToAdd.Add((python, SkillProficiency.Expert, 5));
                skillsToAdd.Add((aws, SkillProficiency.Intermediate, 3));
            }
            else if (user.Email.Contains("marcus"))
            {
                skillsToAdd.Add((nodejs, SkillProficiency.Expert, 6));
            }
            else if (user.Email.Contains("sophia"))
            {
                skillsToAdd.Add((csharp, SkillProficiency.Expert, 5));
            }
            else if (user.Email.Contains("alex"))
            {
                skillsToAdd.Add((react, SkillProficiency.Intermediate, 3));
            }
            else if (user.Email.Contains("admin") || user.Email.Contains("moderator"))
            {
                skillsToAdd.Add((projectMgmt, SkillProficiency.Expert, 10));
            }
            else
            {
                // Default: give all other users at least one skill
                skillsToAdd.Add((python, SkillProficiency.Beginner, 1));
            }

            foreach (var (skill, proficiency, years) in skillsToAdd)
            {
                if (skill != null)
                {
                    await _context.UserSkills.AddAsync(new UserSkill
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        SkillId = skill.Id,
                        Proficiency = proficiency,
                        YearsOfExperience = years,
                        IsFeatured = true,
                        IsVisible = true,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }
    }

    private async Task SeedExperiencesAsync(List<User> users)
    {
        // Add experience for established users
        var experiencedUsers = users.Where(u => u.TaxCompliant).Take(10);

        foreach (var user in experiencedUsers)
        {
            var experience = new Experience
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = ExperienceType.Work,
                Title = "Senior Developer",
                Organization = "Tech Company Inc",
                StartDate = DateTime.UtcNow.AddYears(-5),
                EndDate = DateTime.UtcNow.AddYears(-1),
                IsCurrent = false,
                Description = "Led development of enterprise applications",
                CreatedAt = user.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Experiences.AddAsync(experience);
        }
    }

    public Task<SeedResult> SeedUsersAsync()
    {
        throw new NotImplementedException("Use SeedAsync for full seeding");
    }

    public Task<SeedResult> SeedProjectsAsync()
    {
        throw new NotImplementedException("Use SeedAsync for full seeding");
    }

    public Task<SeedResult> SeedFinancialDataAsync()
    {
        throw new NotImplementedException("Use SeedAsync for full seeding");
    }

    public Task<SeedResult> SeedCollaborationDataAsync()
    {
        throw new NotImplementedException("Use SeedAsync for full seeding");
    }

    public Task<SeedResult> SeedReputationDataAsync()
    {
        throw new NotImplementedException("Use SeedAsync for full seeding");
    }
}
