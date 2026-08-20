using Bogus;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Services.TestData;

/// <summary>
/// Factory for creating test project scenarios
/// </summary>
public class ProjectTestDataFactory
{
    private readonly Faker _faker;

    public ProjectTestDataFactory()
    {
        _faker = new Faker();
    }

    /// <summary>
    /// Creates all 30 test projects in various states
    /// </summary>
    public List<Project> CreateAllProjects(List<User> users)
    {
        var projects = new List<Project>();

        var clients = users.Where(u => u.Email.Contains("client") || u.Email.Contains("goldstein") || u.Email.Contains("anderson") || u.Email.Contains("lee") || u.Email.Contains("chen") || u.Email.Contains("santos") || u.Email.Contains("rodriguez")).ToList();
        var providers = users.Where(u => u.Email.Contains("kumar") || u.Email.Contains("thompson") || u.Email.Contains("kim") || u.Email.Contains("martinez") || u.Email.Contains("johnson") || u.Email.Contains("park")).ToList();

        if (!clients.Any()) clients = users.Take(10).ToList();
        if (!providers.Any()) providers = users.Skip(10).Take(10).ToList();

        // Draft projects (5)
        projects.AddRange(CreateDraftProjects(clients));

        // Published projects (8)
        projects.AddRange(CreatePublishedProjects(clients));

        // In-Progress projects (8)
        projects.AddRange(CreateInProgressProjects(clients, providers));

        // Completed projects (5)
        projects.AddRange(CreateCompletedProjects(clients, providers));

        // Cancelled/Disputed projects (4)
        projects.AddRange(CreateCancelledDisputedProjects(clients, providers));

        return projects;
    }

    private List<Project> CreateDraftProjects(List<User> clients)
    {
        var projects = new List<Project>();

        // Project 1: Incomplete Draft
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000001"),
            clients[0].Id,
            "E-commerce Website Design",
            "Need modern design for online store",
            ProjectStatus.Draft,
            500,
            null,
            null,
            ModerationStatus.Pending
        ));

        // Project 2: Complete Draft Ready to Publish
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000002"),
            clients[1].Id,
            "Mobile App UI/UX Redesign",
            "Complete redesign of our mobile application user interface. Must follow iOS and Android design guidelines. Looking for modern, clean aesthetic with emphasis on usability.",
            ProjectStatus.Draft,
            1500,
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow.AddDays(74),
            ModerationStatus.Pending
        ));

        // Project 3: Draft with Errors (minimal budget at lower bound)
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000003"),
            clients[2].Id,
            "Web Development Project",
            "Short desc", // Too short
            ProjectStatus.Draft,
            50, // At minimum bound
            null,
            null,
            ModerationStatus.Pending
        ));

        // Project 4: Draft Saved for Later (old draft)
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000004"),
            clients[3].Id,
            "Data Analysis Dashboard",
            "Building comprehensive analytics dashboard with real-time data visualization, custom reporting capabilities, and export functionality.",
            ProjectStatus.Draft,
            800,
            null,
            null,
            ModerationStatus.Pending,
            createdDaysAgo: 30
        ));

        // Project 5: Draft with Max Budget
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000005"),
            clients[4].Id,
            "Enterprise Platform Migration",
            "Migrate legacy enterprise system to modern cloud architecture. Requires extensive planning, data migration strategy, and minimal downtime during transition.",
            ProjectStatus.Draft,
            5000, // Maximum allowed
            null,
            null,
            ModerationStatus.Pending
        ));

        return projects;
    }

    private List<Project> CreatePublishedProjects(List<User> clients)
    {
        var projects = new List<Project>();

        // Project 6: New Published Project - Zero Applications
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000006"),
            clients[0].Id,
            "React Dashboard Development",
            "Build responsive admin dashboard using React, TypeScript, and Material-UI. Needs data visualization, user management, and reporting features.",
            ProjectStatus.Published,
            1200,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(52),
            ModerationStatus.Approved
        ));

        // Project 7: Published with Multiple Applications
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000007"),
            clients[1].Id,
            "Logo Design and Branding Package",
            "Complete branding package including logo, color palette, typography guide, and brand guidelines document.",
            ProjectStatus.Published,
            800,
            DateTime.UtcNow.AddDays(14),
            DateTime.UtcNow.AddDays(44),
            ModerationStatus.Approved
        ));

        // Project 8: Published - Urgent Project
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000008"),
            clients[2].Id,
            "Bug Fix - Production Issue",
            "Critical bug in production system needs immediate fix. Payment processing failing for certain edge cases.",
            ProjectStatus.Published,
            300,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(3),
            ModerationStatus.Approved,
            isUrgent: true
        ));

        // Project 9: Published - Featured Project
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000009"),
            clients[3].Id,
            "Enterprise Cloud Architecture Consulting",
            "Strategic consulting for cloud migration and microservices architecture. Need experienced architect for 3-month engagement.",
            ProjectStatus.Published,
            4500,
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(120),
            ModerationStatus.Approved,
            isFeatured: true
        ));

        // Project 10: Published - Remote Work
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000010"),
            clients[4].Id,
            "Content Writing - Blog Series",
            "Write 10 SEO-optimized blog posts for tech startup. Topics provided, research required.",
            ProjectStatus.Published,
            600,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(37),
            ModerationStatus.Approved,
            isRemoteWork: true
        ));

        // Project 11: Published - Private Project
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000011"),
            clients[0].Id,
            "Confidential Corporate Project",
            "NDA required for details. Will share full requirements with selected candidates only.",
            ProjectStatus.Published,
            3000,
            DateTime.UtcNow.AddDays(21),
            DateTime.UtcNow.AddDays(111),
            ModerationStatus.Approved,
            visibility: ProjectVisibility.Private
        ));

        // Project 12: Published - Under Moderation
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000012"),
            clients[1].Id,
            "Suspicious Project Title",
            "Contact via email for payment details...",
            ProjectStatus.Published,
            100,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(37),
            ModerationStatus.Pending // Flagged for review
        ));

        // Project 13: Published - Expiring Soon
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000013"),
            clients[2].Id,
            "Quick Graphic Design Task",
            "Simple graphic design work needed for social media campaign.",
            ProjectStatus.Published,
            150,
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(9),
            ModerationStatus.Approved,
            createdDaysAgo: 25 // Close to 30-day expiration
        ));

        return projects;
    }

    private List<Project> CreateInProgressProjects(List<User> clients, List<User> providers)
    {
        var projects = new List<Project>();

        // Project 14: Just Started - Fresh Escrow
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000014"),
            clients[0].Id,
            "API Integration Development",
            "Integrate third-party payment API with existing e-commerce platform.",
            ProjectStatus.InProgress,
            1000,
            DateTime.UtcNow.AddDays(-3),
            DateTime.UtcNow.AddDays(27),
            ModerationStatus.Approved,
            providerId: providers[0].Id,
            createdDaysAgo: 5
        ));

        // Project 15: Mid-Progress - One Milestone Released
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000015"),
            clients[1].Id,
            "Website Redesign Phase 1",
            "Modern website redesign with focus on mobile responsiveness.",
            ProjectStatus.InProgress,
            2000,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(30),
            ModerationStatus.Approved,
            providerId: providers[0].Id,
            createdDaysAgo: 35
        ));

        // Project 16: Near Completion
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000016"),
            clients[2].Id,
            "Backend API Development",
            "RESTful API for mobile app with authentication and data management.",
            ProjectStatus.InProgress,
            1500,
            DateTime.UtcNow.AddDays(-50),
            DateTime.UtcNow.AddDays(7),
            ModerationStatus.Approved,
            providerId: providers[1].Id,
            createdDaysAgo: 55
        ));

        // Project 17: Overdue Project
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000017"),
            clients[3].Id,
            "Data Visualization Dashboard",
            "Interactive data dashboard with custom charts and filters.",
            ProjectStatus.InProgress,
            700,
            DateTime.UtcNow.AddDays(-60),
            DateTime.UtcNow.AddDays(-7), // Overdue!
            ModerationStatus.Approved,
            providerId: providers[2].Id,
            createdDaysAgo: 65
        ));

        // Project 18: High-Value Enterprise Project
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000018"),
            clients[4].Id,
            "Enterprise System Integration",
            "Integrate multiple enterprise systems with custom middleware layer.",
            ProjectStatus.InProgress,
            5000,
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(110),
            ModerationStatus.Approved,
            providerId: providers[3].Id,
            createdDaysAgo: 15
        ));

        // Project 19: Project with Active Communication
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000019"),
            clients[0].Id,
            "Mobile App Feature Development",
            "Add new features to existing mobile app including push notifications and offline mode.",
            ProjectStatus.InProgress,
            1300,
            DateTime.UtcNow.AddDays(-20),
            DateTime.UtcNow.AddDays(40),
            ModerationStatus.Approved,
            providerId: providers[0].Id,
            createdDaysAgo: 25
        ));

        // Project 20: Project Pending Review
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000020"),
            clients[1].Id,
            "Market Research Report",
            "Comprehensive market analysis for new product launch.",
            ProjectStatus.InProgress,
            900,
            DateTime.UtcNow.AddDays(-35),
            DateTime.UtcNow.AddDays(5),
            ModerationStatus.Approved,
            providerId: providers[4].Id,
            createdDaysAgo: 40
        ));

        // Project 21: Remote International Project
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000021"),
            clients[2].Id,
            "International Design Collaboration",
            "Cross-timezone design work for global marketing campaign.",
            ProjectStatus.InProgress,
            2200,
            DateTime.UtcNow.AddDays(-15),
            DateTime.UtcNow.AddDays(45),
            ModerationStatus.Approved,
            providerId: providers[1].Id,
            createdDaysAgo: 20,
            isRemoteWork: true
        ));

        return projects;
    }

    private List<Project> CreateCompletedProjects(List<User> clients, List<User> providers)
    {
        var projects = new List<Project>();

        // Project 22: Recently Completed - Pending Reviews
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000022"),
            clients[0].Id,
            "Website Landing Page",
            "Modern landing page design with conversion optimization focus.",
            ProjectStatus.Completed,
            800,
            DateTime.UtcNow.AddDays(-32),
            DateTime.UtcNow.AddDays(-2),
            ModerationStatus.Approved,
            providerId: providers[0].Id,
            createdDaysAgo: 35,
            completedDaysAgo: 2
        ));

        // Project 23: Completed with Mutual Reviews
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000023"),
            clients[1].Id,
            "Brand Identity Design",
            "Complete brand identity package with logo and guidelines.",
            ProjectStatus.Completed,
            1500,
            DateTime.UtcNow.AddDays(-45),
            DateTime.UtcNow.AddDays(-15),
            ModerationStatus.Approved,
            providerId: providers[0].Id,
            createdDaysAgo: 50,
            completedDaysAgo: 15
        ));

        // Project 24: Completed with Bonus Payment
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000024"),
            clients[2].Id,
            "Critical Infrastructure Upgrade",
            "Upgrade core infrastructure with zero downtime requirement.",
            ProjectStatus.Completed,
            3500,
            DateTime.UtcNow.AddDays(-60),
            DateTime.UtcNow.AddDays(-20),
            ModerationStatus.Approved,
            providerId: providers[1].Id,
            createdDaysAgo: 65,
            completedDaysAgo: 20
        ));

        // Project 25: Completed Long Ago
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000025"),
            clients[3].Id,
            "Mobile App MVP Development",
            "Build minimum viable product for iOS and Android.",
            ProjectStatus.Completed,
            2500,
            DateTime.UtcNow.AddDays(-210),
            DateTime.UtcNow.AddDays(-180),
            ModerationStatus.Approved,
            providerId: providers[2].Id,
            createdDaysAgo: 215,
            completedDaysAgo: 180
        ));

        // Project 26: Completed with Mixed Review
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000026"),
            clients[4].Id,
            "Data Analysis Project",
            "Analyze customer data and provide actionable insights.",
            ProjectStatus.Completed,
            600,
            DateTime.UtcNow.AddDays(-50),
            DateTime.UtcNow.AddDays(-30),
            ModerationStatus.Approved,
            providerId: providers[3].Id,
            createdDaysAgo: 55,
            completedDaysAgo: 30
        ));

        return projects;
    }

    private List<Project> CreateCancelledDisputedProjects(List<User> clients, List<User> providers)
    {
        var projects = new List<Project>();

        // Project 27: Cancelled Before Assignment
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000027"),
            clients[0].Id,
            "Simple Logo Design",
            "Need basic logo for startup.",
            ProjectStatus.Cancelled,
            200,
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(20),
            ModerationStatus.Approved,
            createdDaysAgo: 15,
            cancelledDaysAgo: 5
        ));

        // Project 28: Cancelled During Progress
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000028"),
            clients[1].Id,
            "Backend Development Project",
            "API development for mobile application.",
            ProjectStatus.Cancelled,
            1100,
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(30),
            ModerationStatus.Approved,
            providerId: providers[0].Id,
            createdDaysAgo: 35,
            cancelledDaysAgo: 10
        ));

        // Project 29: Disputed Project
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000029"),
            clients[2].Id,
            "Website Development",
            "Full website build with custom features.",
            ProjectStatus.Disputed,
            1800,
            DateTime.UtcNow.AddDays(-50),
            DateTime.UtcNow.AddDays(10),
            ModerationStatus.Approved,
            providerId: providers[1].Id,
            createdDaysAgo: 55
        ));

        // Project 30: Suspended for Moderation
        projects.Add(CreateProject(
            new Guid("20000000-0000-0000-0000-000000000030"),
            clients[3].Id,
            "Suspicious Project",
            "Contact via external site for more info...",
            ProjectStatus.Suspended,
            100,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(37),
            ModerationStatus.Pending,
            createdDaysAgo: 3
        ));

        return projects;
    }

    private Project CreateProject(
        Guid id,
        Guid clientId,
        string title,
        string description,
        ProjectStatus status,
        int creditBudget,
        DateTime? startDate,
        DateTime? endDate,
        ModerationStatus moderationStatus,
        Guid? providerId = null,
        int createdDaysAgo = 0,
        int? completedDaysAgo = null,
        int? cancelledDaysAgo = null,
        bool isUrgent = false,
        bool isFeatured = false,
        bool isRemoteWork = false,
        ProjectVisibility visibility = ProjectVisibility.Public)
    {
        var createdAt = DateTime.UtcNow.AddDays(-createdDaysAgo);

        var project = new Project
        {
            Id = id,
            ClientId = clientId,
            ProviderId = providerId,
            Title = title,
            Description = description,
            Status = status,
            CreditBudget = creditBudget,
            StartDate = startDate,
            EndDate = endDate,
            ModerationStatus = moderationStatus,
            IsUrgent = isUrgent,
            IsFeatured = isFeatured,
            IsRemoteWork = isRemoteWork,
            Visibility = visibility,
            ComplexityScore = _faker.Random.Int(1, 10),
            CreatedAt = createdAt,
            UpdatedAt = DateTime.UtcNow,
            CreatedFromIP = "TEST_DATA_SEEDER"
        };

        if (completedDaysAgo.HasValue)
        {
            project.CompletedAt = DateTime.UtcNow.AddDays(-completedDaysAgo.Value);
        }

        if (cancelledDaysAgo.HasValue)
        {
            project.CancelledAt = DateTime.UtcNow.AddDays(-cancelledDaysAgo.Value);
            project.CancellationReason = "Requirements changed";
        }

        if (status == ProjectStatus.Disputed)
        {
            project.DisputeReason = "Work quality does not meet requirements";
        }

        return project;
    }

    /// <summary>
    /// Creates deliverables for projects
    /// </summary>
    public List<ProjectDeliverable> CreateDeliverablesForProjects(List<Project> projects)
    {
        var deliverables = new List<ProjectDeliverable>();

        foreach (var project in projects)
        {
            // Skip draft projects with errors and very simple projects
            if (project.CreditBudget < 100)
                continue;

            int deliverableCount = project.CreditBudget switch
            {
                < 500 => 1,
                < 1000 => 2,
                < 2000 => 3,
                < 3000 => 5,
                _ => 8
            };

            for (int i = 0; i < deliverableCount; i++)
            {
                deliverables.Add(new ProjectDeliverable
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Description = $"Deliverable {i + 1}: {_faker.Lorem.Sentence()}",
                    OrderIndex = i,
                    IsRequired = i < 2, // First 2 are required
                    IsCompleted = project.Status == ProjectStatus.Completed,
                    CreatedAt = project.CreatedAt,
                    CompletedAt = project.Status == ProjectStatus.Completed ? project.CompletedAt : null
                });
            }
        }

        return deliverables;
    }
}
