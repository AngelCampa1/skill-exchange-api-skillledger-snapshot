using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Test-only controller for E2E testing support
/// SECURITY: Only enabled in Development/Testing environments
/// </summary>
#if DEBUG
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class TestController : ControllerBase
{
    private readonly SkillLedgerDbContext _dbContext;
    private readonly ILogger<TestController> _logger;

    public TestController(
        SkillLedgerDbContext dbContext,
        ILogger<TestController> logger,
        IWebHostEnvironment environment)
    {
        // BUG-HIGH-001 FIX: Runtime check prevents accidental exposure in production
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "TestController is disabled in production environments. " +
                "This controller contains dangerous endpoints and must only run in Development mode.");
        }

        _dbContext = dbContext;
        _logger = logger;

        _logger.LogWarning("TestController initialized - This should ONLY appear in Development!");
    }

    /// <summary>
    /// Activate a user account for E2E testing (email verification no longer required)
    /// </summary>
    /// <param name="email">Email address to activate</param>
    /// <returns>Success status</returns>
    [HttpPost("verify-email-auto")]
    public async Task<IActionResult> AutoVerifyEmail([FromBody] AutoVerifyEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        try
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Activate user account (email verification no longer required, but keeping this endpoint for backwards compatibility)
            user.Status = UserStatus.Active;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("E2E Test: Activated user account for {Email}", request.Email);

            return Ok(new
            {
                success = true,
                message = $"User account activated for {request.Email}",
                userId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E2E Test: Failed to activate user account for {Email}", request.Email);
            return StatusCode(500, new { message = "Failed to activate user account", error = ex.Message });
        }
    }

    /// <summary>
    /// Reset test data (clean up test users)
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("reset-test-data")]
    public async Task<IActionResult> ResetTestData()
    {
        try
        {
            // Delete test users (emails ending with .test)
            var testUsers = await _dbContext.Users
                .Where(u => u.Email != null && u.Email.EndsWith(".test"))
                .ToListAsync();

            if (testUsers.Any())
            {
                _dbContext.Users.RemoveRange(testUsers);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("E2E Test: Removed {Count} test users", testUsers.Count);

                return Ok(new
                {
                    success = true,
                    message = $"Removed {testUsers.Count} test users",
                    count = testUsers.Count
                });
            }

            return Ok(new
            {
                success = true,
                message = "No test users to remove",
                count = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E2E Test: Failed to reset test data");
            return StatusCode(500, new { message = "Failed to reset test data", error = ex.Message });
        }
    }

    /// <summary>
    /// Health check for tests
    /// </summary>
    /// <returns>Success status</returns>
    [HttpGet("health")]
    public IActionResult TestHealth()
    {
        return Ok(new
        {
            success = true,
            message = "Test controller is available",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Seed test skills for E2E testing
    /// </summary>
    [HttpPost("seed-skills")]
    public async Task<IActionResult> SeedTestSkills()
    {
        try
        {
            // Check if skills already exist
            var existingSkills = await _dbContext.Skills.CountAsync();
            if (existingSkills > 0)
            {
                return Ok(new
                {
                    success = true,
                    message = $"Skills already seeded ({existingSkills} skills exist)",
                    count = existingSkills
                });
            }

            // Seed common skills for testing
            var skills = new[]
            {
                new Core.Entities.Skill { Id = Guid.NewGuid(), Name = "React", Description = "React.js frontend development", Category = "Frontend", CreatedAt = DateTime.UtcNow },
                new Core.Entities.Skill { Id = Guid.NewGuid(), Name = "Node.js", Description = "Node.js backend development", Category = "Backend", CreatedAt = DateTime.UtcNow },
                new Core.Entities.Skill { Id = Guid.NewGuid(), Name = "TypeScript", Description = "TypeScript programming", Category = "Programming", CreatedAt = DateTime.UtcNow },
                new Core.Entities.Skill { Id = Guid.NewGuid(), Name = "UI/UX Design", Description = "User interface and experience design", Category = "Design", CreatedAt = DateTime.UtcNow },
                new Core.Entities.Skill { Id = Guid.NewGuid(), Name = "Project Management", Description = "Agile project management", Category = "Management", CreatedAt = DateTime.UtcNow },
            };

            await _dbContext.Skills.AddRangeAsync(skills);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("E2E Test: Seeded {Count} test skills", skills.Length);

            return Ok(new
            {
                success = true,
                message = $"Seeded {skills.Length} test skills",
                count = skills.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E2E Test: Failed to seed test skills");
            return StatusCode(500, new { message = "Failed to seed skills", error = ex.Message });
        }
    }

    /// <summary>
    /// Seed test projects for E2E testing
    /// </summary>
    [HttpPost("seed-projects")]
    public async Task<IActionResult> SeedTestProjects()
    {
        try
        {
            // E2E-013 FIX: Check if projects already exist to prevent duplicates
            var existingProjectCount = await _dbContext.Projects.CountAsync();
            if (existingProjectCount > 0)
            {
                _logger.LogInformation("E2E Test: Projects already exist ({Count}), skipping seeding", existingProjectCount);
                return Ok(new
                {
                    success = true,
                    message = $"Projects already exist ({existingProjectCount}), skipping seeding",
                    count = existingProjectCount,
                    skipped = true
                });
            }

            // Get a test user (any user will do for seeding)
            var testUser = await _dbContext.Users.FirstOrDefaultAsync();
            if (testUser == null)
            {
                return BadRequest(new { message = "No users found. Register a user first." });
            }

            // Get skills for project association
            var skills = await _dbContext.Skills.Take(3).ToListAsync();
            if (skills.Count == 0)
            {
                return BadRequest(new { message = "No skills found. Seed skills first using /api/Test/seed-skills" });
            }

            // Create sample projects
            var projects = new[]
            {
                new Core.Entities.Project
                {
                    Id = Guid.NewGuid(),
                    ClientId = testUser.Id,
                    Title = "E-commerce Website Development",
                    Description = "Build a modern e-commerce platform with React frontend and Node.js backend. Features include product catalog, shopping cart, checkout, and admin dashboard.",
                    Status = Core.Enums.ProjectStatus.Published,
                    ModerationStatus = Core.Enums.ModerationStatus.Approved,
                    CreditBudget = 500,
                    StartDate = DateTime.UtcNow.AddDays(7),
                    EndDate = DateTime.UtcNow.AddMonths(2),
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow
                },
                new Core.Entities.Project
                {
                    Id = Guid.NewGuid(),
                    ClientId = testUser.Id,
                    Title = "Mobile App UI/UX Design",
                    Description = "Design a beautiful and intuitive mobile app interface for a fitness tracking application. Deliverables include wireframes, mockups, and design system.",
                    Status = Core.Enums.ProjectStatus.Published,
                    ModerationStatus = Core.Enums.ModerationStatus.Approved,
                    CreditBudget = 300,
                    StartDate = DateTime.UtcNow.AddDays(3),
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow
                },
                new Core.Entities.Project
                {
                    Id = Guid.NewGuid(),
                    ClientId = testUser.Id,
                    Title = "API Integration Project",
                    Description = "Integrate third-party payment APIs including Stripe and PayPal. Implement secure payment processing with proper error handling and logging.",
                    Status = Core.Enums.ProjectStatus.Published,
                    ModerationStatus = Core.Enums.ModerationStatus.Approved,
                    CreditBudget = 200,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(21),
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow
                },
                new Core.Entities.Project
                {
                    Id = Guid.NewGuid(),
                    ClientId = testUser.Id,
                    Title = "Database Optimization",
                    Description = "Optimize SQL Server database performance. Tasks include query analysis, index optimization, and implementing caching strategies.",
                    Status = Core.Enums.ProjectStatus.Published,
                    ModerationStatus = Core.Enums.ModerationStatus.Approved,
                    CreditBudget = 150,
                    StartDate = DateTime.UtcNow.AddDays(5),
                    EndDate = DateTime.UtcNow.AddDays(14),
                    CreatedAt = DateTime.UtcNow.AddHours(-12),
                    UpdatedAt = DateTime.UtcNow
                },
                new Core.Entities.Project
                {
                    Id = Guid.NewGuid(),
                    ClientId = testUser.Id,
                    Title = "Technical Documentation",
                    Description = "Write comprehensive technical documentation for an existing SaaS platform. Include API documentation, user guides, and developer onboarding materials.",
                    Status = Core.Enums.ProjectStatus.Published,
                    ModerationStatus = Core.Enums.ModerationStatus.Approved,
                    CreditBudget = 100,
                    StartDate = DateTime.UtcNow.AddDays(2),
                    EndDate = DateTime.UtcNow.AddDays(28),
                    CreatedAt = DateTime.UtcNow.AddHours(-6),
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _dbContext.Projects.AddRangeAsync(projects);
            await _dbContext.SaveChangesAsync();

            // Add skills to projects
            var projectSkills = new List<Core.Entities.ProjectSkill>();
            foreach (var project in projects)
            {
                // Add 1-2 random skills to each project
                var skillsToAdd = skills.Take(Math.Min(2, skills.Count)).ToList();
                foreach (var skill in skillsToAdd)
                {
                    projectSkills.Add(new Core.Entities.ProjectSkill
                    {
                        ProjectId = project.Id,
                        SkillId = skill.Id,
                        ProficiencyRequired = Core.Enums.SkillProficiency.Intermediate,
                        Weight = 3
                    });
                }
            }

            await _dbContext.Set<Core.Entities.ProjectSkill>().AddRangeAsync(projectSkills);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("E2E Test: Seeded {Count} test projects", projects.Length);

            return Ok(new
            {
                success = true,
                message = $"Seeded {projects.Length} test projects with skills",
                count = projects.Length,
                projectIds = projects.Select(p => p.Id).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E2E Test: Failed to seed test projects");
            return StatusCode(500, new { message = "Failed to seed projects", error = ex.Message });
        }
    }

    /// <summary>
    /// Seed test wallet with credits for E2E testing
    /// </summary>
    [HttpPost("seed-wallet")]
    public async Task<IActionResult> SeedTestWallet([FromBody] SeedWalletRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Check if wallet already exists
            var existingWallet = await _dbContext.Set<Core.Entities.CreditWallet>()
                .FirstOrDefaultAsync(w => w.UserId == user.Id);

            if (existingWallet != null)
            {
                return Ok(new
                {
                    success = true,
                    message = "Wallet already exists for this user",
                    walletId = existingWallet.Id
                });
            }

            // Create wallet with initial credits (simple encryption for dev - not secure for production!)
            var initialBalance = request.InitialCredits ?? 1000;
            var wallet = new Core.Entities.CreditWallet
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EncryptedBalance = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(initialBalance.ToString())),
                EncryptedPendingBalance = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("0")),
                EncryptedTotalEarned = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(initialBalance.ToString())),
                EncryptedTotalSpent = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("0")),
                KeyIdentifier = "dev-test-key",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.Set<Core.Entities.CreditWallet>().AddAsync(wallet);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("E2E Test: Created wallet with {Credits} credits for {Email}", initialBalance, request.Email);

            return Ok(new
            {
                success = true,
                message = $"Created wallet with {initialBalance} credits",
                walletId = wallet.Id,
                initialCredits = initialBalance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E2E Test: Failed to seed wallet for {Email}", request.Email);
            return StatusCode(500, new { message = "Failed to seed wallet", error = ex.Message });
        }
    }

    /// <summary>
    /// Get test data summary
    /// </summary>
    [HttpGet("data-summary")]
    public async Task<IActionResult> GetDataSummary()
    {
        try
        {
            var userCount = await _dbContext.Users.CountAsync();
            var profileCount = await _dbContext.Set<Core.Entities.Profile>().CountAsync();
            var projectCount = await _dbContext.Projects.CountAsync();
            var publishedProjectCount = await _dbContext.Projects
                .Where(p => p.Status == Core.Enums.ProjectStatus.Published)
                .CountAsync();
            var skillCount = await _dbContext.Skills.CountAsync();
            var walletCount = await _dbContext.Set<Core.Entities.CreditWallet>().CountAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    users = userCount,
                    profiles = profileCount,
                    projects = projectCount,
                    publishedProjects = publishedProjectCount,
                    skills = skillCount,
                    wallets = walletCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E2E Test: Failed to get data summary");
            return StatusCode(500, new { message = "Failed to get data summary", error = ex.Message });
        }
    }
}

public record SeedWalletRequest(string Email, int? InitialCredits = 1000);

public record AutoVerifyEmailRequest(string Email);
#endif

