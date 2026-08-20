using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for seeding sample project data for testing and demonstration
/// </summary>
public class ProjectDataSeeder
{
    private readonly SkillLedgerDbContext _context;
    private readonly ILogger<ProjectDataSeeder> _logger;

    public ProjectDataSeeder(
        SkillLedgerDbContext context,
        ILogger<ProjectDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds sample projects if none exist
    /// </summary>
    public async Task SeedSampleProjectsAsync()
    {
        try
        {
            var existingProjects = await _context.Projects.AnyAsync();

            if (existingProjects)
            {
                _logger.LogInformation("Projects already exist, skipping seeding");
                return;
            }

            _logger.LogInformation("Starting sample projects seeding...");

            // Get a user to be the client (or create a system user)
            var systemUser = await GetOrCreateSystemUserAsync();

            // Get some skills to associate with projects
            var skills = await _context.Skills.Take(20).ToListAsync();

            if (!skills.Any())
            {
                _logger.LogWarning("No skills found, cannot seed projects");
                return;
            }

            var projects = CreateSampleProjects(systemUser.Id, skills);

            await _context.Projects.AddRangeAsync(projects);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} sample projects", projects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding sample projects");
            throw;
        }
    }

    private async Task<User> GetOrCreateSystemUserAsync()
    {
        // Try to find an existing user first
        var existingUser = await _context.Users.FirstOrDefaultAsync();

        if (existingUser != null)
        {
            return existingUser;
        }

        // Create a system user for seeding
        var systemUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            UserName = "system@skillledger.app",
            NormalizedUserName = "SYSTEM@SKILLLEDGER.APP",
            Email = "system@skillledger.app",
            NormalizedEmail = "SYSTEM@SKILLLEDGER.APP",
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Admin",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        await _context.Users.AddAsync(systemUser);
        await _context.SaveChangesAsync();

        return systemUser;
    }

    private List<Project> CreateSampleProjects(Guid clientId, List<Skill> skills)
    {
        var now = DateTime.UtcNow;
        var skillsByCategory = skills.GroupBy(s => s.Category).ToDictionary(g => g.Key ?? "Other", g => g.ToList());

        var projects = new List<Project>
        {
            // Web Development Projects
            CreateProject(
                clientId,
                "E-Commerce Platform Redesign",
                "We're looking for an experienced full-stack developer to redesign our e-commerce platform. The project involves modernizing the UI/UX, implementing a new checkout flow, and integrating with our existing inventory management system. We need someone with strong React and Node.js skills.",
                500,
                now.AddDays(7),
                now.AddDays(60),
                true,
                "San Francisco", "California", "United States",
                37.7749, -122.4194,
                GetSkillsForProject(skillsByCategory, "Web Development", "Programming"),
                new[] { "Responsive design mockups", "Frontend implementation", "Backend API integration", "Testing and deployment" }
            ),

            // E2E-012 FIX: Uses "Mobile Development" category (now exists in system skills)
            CreateProject(
                clientId,
                "Mobile App Development - iOS & Android",
                "Seeking a skilled mobile developer to build a cross-platform fitness tracking app. The app should sync with wearable devices, track workouts, and provide personalized recommendations. Experience with React Native or Flutter is preferred.",
                800,
                now.AddDays(14),
                now.AddDays(90),
                true,
                "Austin", "Texas", "United States",
                30.2672, -97.7431,
                GetSkillsForProject(skillsByCategory, "Mobile Development", "Programming"),
                new[] { "UI/UX design", "Core app functionality", "Wearable integration", "Backend API", "App store submission" }
            ),

            // E2E-012 FIX: Uses "Data Science" category (now exists in system skills)
            CreateProject(
                clientId,
                "Data Analytics Dashboard",
                "Looking for a data engineer to build a real-time analytics dashboard for our marketing team. The dashboard should pull data from multiple sources, perform ETL operations, and display key metrics with interactive visualizations.",
                350,
                now.AddDays(5),
                now.AddDays(45),
                true,
                "New York", "New York", "United States",
                40.7128, -74.0060,
                GetSkillsForProject(skillsByCategory, "Data Science", "Cloud"),
                new[] { "Data pipeline setup", "Dashboard development", "Visualization implementation", "Documentation" }
            ),

            CreateProject(
                clientId,
                "Brand Identity Design Package",
                "We need a creative designer to develop a complete brand identity for our startup. This includes logo design, color palette, typography, and brand guidelines. Experience with tech startups is a plus.",
                250,
                now.AddDays(3),
                now.AddDays(30),
                true,
                "Los Angeles", "California", "United States",
                34.0522, -118.2437,
                GetSkillsForProject(skillsByCategory, "Design"),
                new[] { "Logo concepts", "Color palette & typography", "Brand guidelines document", "Stationery design" }
            ),

            CreateProject(
                clientId,
                "DevOps Infrastructure Setup",
                "Looking for a DevOps engineer to set up our cloud infrastructure on AWS. This includes CI/CD pipelines, container orchestration with Kubernetes, monitoring, and automated deployments.",
                600,
                now.AddDays(10),
                now.AddDays(50),
                true,
                "Seattle", "Washington", "United States",
                47.6062, -122.3321,
                GetSkillsForProject(skillsByCategory, "Cloud", "DevOps"),
                new[] { "Infrastructure architecture", "CI/CD pipeline setup", "Kubernetes deployment", "Monitoring & alerting", "Documentation" }
            ),

            // E2E-012 FIX: Uses "Data Science" category (now exists in system skills with ML/NLP)
            CreateProject(
                clientId,
                "AI Chatbot Development",
                "We need an AI/ML engineer to develop an intelligent customer support chatbot. The bot should handle common queries, escalate complex issues, and learn from interactions over time.",
                700,
                now.AddDays(7),
                now.AddDays(75),
                true,
                "Boston", "Massachusetts", "United States",
                42.3601, -71.0589,
                GetSkillsForProject(skillsByCategory, "Data Science", "Programming"),
                new[] { "NLP model training", "Chatbot framework setup", "Integration with support system", "Testing & optimization" }
            ),

            CreateProject(
                clientId,
                "WordPress Website Customization",
                "Need a WordPress developer to customize our existing website theme, add new features, and optimize for performance. Experience with WooCommerce integration is required.",
                150,
                now.AddDays(2),
                now.AddDays(21),
                true,
                "Denver", "Colorado", "United States",
                39.7392, -104.9903,
                GetSkillsForProject(skillsByCategory, "Web Development"),
                new[] { "Theme customization", "Plugin development", "Performance optimization", "Security hardening" }
            ),

            CreateProject(
                clientId,
                "API Integration Project",
                "Looking for a backend developer to integrate multiple third-party APIs into our platform. APIs include payment gateways, shipping providers, and CRM systems. Strong documentation skills required.",
                400,
                now.AddDays(5),
                now.AddDays(40),
                true,
                "Chicago", "Illinois", "United States",
                41.8781, -87.6298,
                GetSkillsForProject(skillsByCategory, "Web Development", "Programming"),
                new[] { "API analysis & planning", "Integration development", "Error handling", "Testing & documentation" }
            ),

            // E2E-012 FIX: Uses "Security" category (now exists in system skills)
            CreateProject(
                clientId,
                "Security Audit & Penetration Testing",
                "Seeking a security expert to conduct a comprehensive security audit of our web application. This includes vulnerability assessment, penetration testing, and security recommendations.",
                550,
                now.AddDays(14),
                now.AddDays(35),
                false,
                "Washington", "D.C.", "United States",
                38.9072, -77.0369,
                GetSkillsForProject(skillsByCategory, "Security", "DevOps"),
                new[] { "Vulnerability assessment", "Penetration testing", "Security report", "Remediation recommendations" }
            ),

            // E2E-012 FIX: Uses "Writing" category (now exists in system skills)
            CreateProject(
                clientId,
                "Technical Content Writing",
                "We need a technical writer to create documentation for our developer API. This includes getting started guides, API reference documentation, and code samples in multiple languages.",
                200,
                now.AddDays(3),
                now.AddDays(28),
                true,
                "Portland", "Oregon", "United States",
                45.5152, -122.6784,
                GetSkillsForProject(skillsByCategory, "Writing", "Programming"),
                new[] { "API documentation", "Getting started guide", "Code samples", "Tutorial articles" }
            )
        };

        return projects;
    }

    private Project CreateProject(
        Guid clientId,
        string title,
        string description,
        int creditBudget,
        DateTime startDate,
        DateTime endDate,
        bool isRemote,
        string? city,
        string? state,
        string? country,
        double? lat,
        double? lng,
        List<(Guid skillId, SkillProficiency proficiency)> skillRequirements,
        string[] deliverables)
    {
        var projectId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            ClientId = clientId,
            Title = title,
            Description = description,
            Status = ProjectStatus.Published,
            CreditBudget = creditBudget,
            StartDate = startDate,
            EndDate = endDate,
            ModerationStatus = ModerationStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 10)),
            UpdatedAt = DateTime.UtcNow,
            IsRemoteWork = isRemote,
            LocationCity = city,
            LocationState = state,
            LocationCountry = country,
            LocationLatitude = lat,
            LocationLongitude = lng,
            Visibility = ProjectVisibility.Public,
            IsFeatured = creditBudget > 500,
            IsUrgent = startDate <= DateTime.UtcNow.AddDays(7),
            ComplexityScore = CalculateComplexity(creditBudget, (endDate - startDate).Days, skillRequirements.Count),
            SearchText = $"{title} {description}"
        };

        // Add deliverables
        for (int i = 0; i < deliverables.Length; i++)
        {
            project.Deliverables.Add(new ProjectDeliverable
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Description = deliverables[i],
                OrderIndex = i,
                IsRequired = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Add skill requirements
        foreach (var (skillId, proficiency) in skillRequirements)
        {
            project.ProjectSkills.Add(new ProjectSkill
            {
                ProjectId = projectId,
                SkillId = skillId,
                ProficiencyRequired = proficiency,
                Weight = proficiency == SkillProficiency.Expert ? 5 : (proficiency == SkillProficiency.Advanced ? 4 : 3),
                CreatedAt = DateTime.UtcNow
            });
        }

        return project;
    }

    private List<(Guid skillId, SkillProficiency proficiency)> GetSkillsForProject(
        Dictionary<string, List<Skill>> skillsByCategory,
        params string[] categories)
    {
        var result = new List<(Guid, SkillProficiency)>();
        var proficiencies = new[] { SkillProficiency.Intermediate, SkillProficiency.Advanced, SkillProficiency.Expert };

        foreach (var category in categories)
        {
            if (skillsByCategory.TryGetValue(category, out var categorySkills))
            {
                var skillsToAdd = categorySkills.Take(2).ToList();
                foreach (var skill in skillsToAdd)
                {
                    var proficiency = proficiencies[Random.Shared.Next(proficiencies.Length)];
                    result.Add((skill.Id, proficiency));
                }
            }
        }

        // Ensure at least one skill
        if (!result.Any() && skillsByCategory.Any())
        {
            var firstSkill = skillsByCategory.First().Value.First();
            result.Add((firstSkill.Id, SkillProficiency.Intermediate));
        }

        return result;
    }

    private int CalculateComplexity(int budget, int durationDays, int skillCount)
    {
        var budgetScore = budget switch
        {
            < 200 => 2,
            < 400 => 4,
            < 600 => 6,
            < 800 => 8,
            _ => 10
        };

        var durationScore = durationDays switch
        {
            < 14 => 2,
            < 30 => 4,
            < 60 => 6,
            < 90 => 8,
            _ => 10
        };

        var skillScore = skillCount switch
        {
            <= 1 => 2,
            <= 2 => 4,
            <= 4 => 6,
            <= 6 => 8,
            _ => 10
        };

        return (budgetScore + durationScore + skillScore) / 3;
    }
}
