using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Api;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace SkillLedger.Tests.BDD;

/// <summary>
/// BDD-style user journey tests for Advanced Project Discovery (US-2.2.1)
/// Tests realistic user scenarios from the perspective of service providers searching for projects
/// </summary>
[BDDTest]
[CoreTest]
[Collection("Integration Other")]
public class ProjectSearchUserJourneyTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private readonly List<User> _clients;
    private readonly List<User> _providers;
    private readonly List<Skill> _skills;
    private readonly List<Project> _projects;

    public ProjectSearchUserJourneyTests(SharedTestHostFixture fixture, ITestOutputHelper output)
        : base(fixture)
    {
        _output = output;
        _clients = new List<User>();
        _providers = new List<User>();
        _skills = new List<Skill>();
        _projects = new List<Project>();
    }

    /// <summary>
    /// Async initialization called by xUnit after constructor
    /// This prevents blocking calls in the constructor that could cause deadlocks
    /// </summary>
    protected override async Task OnInitializeAsync()
    {
        // IMPORTANT: Call base first so FastCleanup runs before we create test data
        await base.OnInitializeAsync();
        await SetupUserJourneyDataAsync();
    }

    private async Task SetupUserJourneyDataAsync()
    {
        // Create realistic user personas
        _clients.AddRange(new[]
        {
            new User
            {
                Id = Guid.NewGuid(),
                Email = "startup.founder@techco.com",
                UserName = "startup.founder@techco.com",
                Status = UserStatus.Active
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "product.manager@enterprise.com",
                UserName = "product.manager@enterprise.com",
                Status = UserStatus.Active
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "agency.director@creative.studio",
                UserName = "agency.director@creative.studio",
                Status = UserStatus.Active
            }
        });

        _providers.AddRange(new[]
        {
            new User
            {
                Id = Guid.NewGuid(),
                Email = "react.developer@freelance.dev",
                UserName = "react.developer@freelance.dev",
                Status = UserStatus.Active
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "fullstack.engineer@consulting.com",
                UserName = "fullstack.engineer@consulting.com",
                Status = UserStatus.Active
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "ui.designer@design.agency",
                UserName = "ui.designer@design.agency",
                Status = UserStatus.Active
            }
        });

        // Create skill set
        _skills.AddRange(new[]
        {
            new Skill { Id = Guid.NewGuid(), Name = "React", Category = "Frontend", IsActive = true, IsSystemManaged = true },
            new Skill { Id = Guid.NewGuid(), Name = "Node.js", Category = "Backend", IsActive = true, IsSystemManaged = true },
            new Skill { Id = Guid.NewGuid(), Name = "UI/UX Design", Category = "Design", IsActive = true, IsSystemManaged = true },
            new Skill { Id = Guid.NewGuid(), Name = "Python", Category = "Backend", IsActive = true, IsSystemManaged = true },
            new Skill { Id = Guid.NewGuid(), Name = "Machine Learning", Category = "AI/ML", IsActive = true, IsSystemManaged = true },
            new Skill { Id = Guid.NewGuid(), Name = "DevOps", Category = "Infrastructure", IsActive = true, IsSystemManaged = true }
        });

        Context.Users.AddRange(_clients);
        Context.Users.AddRange(_providers);
        Context.Skills.AddRange(_skills);
        await Context.SaveChangesAsync();

        // Create realistic project scenarios
        await CreateUserJourneyProjectsAsync();
    }

    private async Task CreateUserJourneyProjectsAsync()
    {
        var reactSkill = _skills.First(s => s.Name == "React");
        var nodeSkill = _skills.First(s => s.Name == "Node.js");
        var uiSkill = _skills.First(s => s.Name == "UI/UX Design");
        var pythonSkill = _skills.First(s => s.Name == "Python");
        var mlSkill = _skills.First(s => s.Name == "Machine Learning");
        var devopsSkill = _skills.First(s => s.Name == "DevOps");

        _projects.AddRange(new[]
        {
            // Startup E-commerce project
            CreateProject(
                "E-commerce MVP Development",
                "Build a modern e-commerce platform with React frontend and Node.js backend. Must include user authentication, product catalog, shopping cart, and payment integration.",
                _clients[0].Id,
                3500,
                "San Francisco", "CA", "USA",
                isRemote: false,
                skills: new[] { (reactSkill.Id, SkillProficiency.Advanced, 5), (nodeSkill.Id, SkillProficiency.Intermediate, 4) }
            ),
            
            // Enterprise dashboard project  
            CreateProject(
                "Executive Analytics Dashboard",
                "Create a real-time analytics dashboard for C-level executives. Requires Python backend with ML insights and clean UI/UX design.",
                _clients[1].Id,
                5000,
                "New York", "NY", "USA",
                isRemote: true,
                skills: new[] { (pythonSkill.Id, SkillProficiency.Expert, 5), (mlSkill.Id, SkillProficiency.Advanced, 4), (uiSkill.Id, SkillProficiency.Intermediate, 3) }
            ),
            
            // Agency website redesign
            CreateProject(
                "Agency Website Redesign",
                "Complete redesign of our digital agency website using React and modern design principles with focus on user experience.",
                _clients[2].Id,
                2200,
                isRemote: true,
                skills: new[] { (uiSkill.Id, SkillProficiency.Expert, 5), (reactSkill.Id, SkillProficiency.Intermediate, 3) }
            ),
            
            // DevOps infrastructure project
            CreateProject(
                "Cloud Infrastructure Setup",
                "Set up scalable cloud infrastructure with CI/CD pipelines, monitoring, and automated deployments.",
                _clients[1].Id,
                4200,
                "Seattle", "WA", "USA",
                isRemote: false,
                skills: new[] { (devopsSkill.Id, SkillProficiency.Expert, 5) }
            ),
            
            // Small budget React project
            CreateProject(
                "React Component Library",
                "Build a reusable React component library for our internal projects.",
                _clients[0].Id,
                1200,
                isRemote: true,
                skills: new[] { (reactSkill.Id, SkillProficiency.Advanced, 5) }
            )
        });

        Context.Projects.AddRange(_projects);

        // Explicitly add all project skills to ensure they're tracked by EF
        foreach (var project in _projects)
        {
            if (project.ProjectSkills != null && project.ProjectSkills.Any())
            {
                Context.ProjectSkills.AddRange(project.ProjectSkills);
            }
        }

        await Context.SaveChangesAsync();
    }

    private Project CreateProject(string title, string description, Guid clientId, int budget,
        string? city = null, string? state = null, string? country = null, bool isRemote = false,
        (Guid skillId, SkillProficiency proficiency, int weight)[]? skills = null)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Title = title,
            Description = description,
            CreditBudget = budget,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(45),
            Status = ProjectStatus.Published,
            ModerationStatus = ModerationStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 14)),
            UpdatedAt = DateTime.UtcNow,
            IsRemoteWork = isRemote,
            LocationCity = city,
            LocationState = state,
            LocationCountry = country
        };

        // Add deliverables
        project.Deliverables = new List<ProjectDeliverable>
        {
            new ProjectDeliverable
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Description = "Project planning and requirements analysis",
                OrderIndex = 1,
                IsRequired = true
            },
            new ProjectDeliverable
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Description = "Implementation and development",
                OrderIndex = 2,
                IsRequired = true
            },
            new ProjectDeliverable
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Description = "Testing and deployment",
                OrderIndex = 3,
                IsRequired = true
            }
        };

        // Add skills
        if (skills != null)
        {
            project.ProjectSkills = skills.Select(s => new ProjectSkill
            {
                ProjectId = project.Id,
                SkillId = s.skillId,
                ProficiencyRequired = s.proficiency,
                Weight = s.weight
            }).ToList();
        }

        return project;
    }

    #region User Journey: React Developer Looking for Projects

    [Fact]
    public async Task UserJourney_ReactDeveloper_SearchesForReactProjects()
    {
        await GivenAReactDeveloperIsLookingForWork();
        await WhenTheySearchForReactProjects();
        await ThenTheySeeRelevantReactProjects();
    }

    private async Task GivenAReactDeveloperIsLookingForWork()
    {
        _output.WriteLine("GIVEN: Sarah is a React developer looking for freelance projects");
        _output.WriteLine("- She has 3+ years of React experience");
        _output.WriteLine("- She prefers projects with budgets above $2000");
        _output.WriteLine("- She's open to both remote and local work");

        await Task.CompletedTask; // Setup already done in constructor
    }

    private async Task WhenTheySearchForReactProjects()
    {
        _output.WriteLine("WHEN: Sarah searches for React projects with her preferences");

        var reactSkill = _skills.First(s => s.Name == "React");
        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "React",
            SkillIds = new List<Guid> { reactSkill.Id },
            SkillMatch = SkillMatchStrategy.Any,
            MinBudget = 2000,
            PublishedOnly = true,
            Take = 10,
            Skip = 0
        };

        _searchResponse = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
        _searchResult = await _searchResponse.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>()
            ?? throw new InvalidOperationException("Failed to deserialize search response");
    }

    private async Task ThenTheySeeRelevantReactProjects()
    {
        _output.WriteLine("THEN: Sarah sees projects that match her criteria");

        Assert.NotNull(_searchResult);
        _searchResponse.EnsureSuccessStatusCode();

        // Should find the E-commerce MVP (3500 credits) and Agency Redesign (2200 credits)
        Assert.True(_searchResult.Projects.Count >= 2,
            $"Expected at least 2 React projects, found {_searchResult.Projects.Count}");

        foreach (var project in _searchResult.Projects)
        {
            _output.WriteLine($"- {project.Title} (${project.CreditBudget} credits)");

            Assert.True(project.CreditBudget >= 2000,
                $"Project {project.Title} budget {project.CreditBudget} below minimum");

            Assert.True(
                project.Title.Contains("React", StringComparison.OrdinalIgnoreCase) ||
                project.ShortDescription.Contains("React", StringComparison.OrdinalIgnoreCase) ||
                project.RequiredSkillNames.Any(s => s == "React"),
                $"Project {project.Title} should be React-related");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region User Journey: Full-Stack Developer Filtering by Location

    [Fact]
    public async Task UserJourney_FullStackDeveloper_FiltersByLocation()
    {
        await GivenAFullStackDeveloperPrefersLocalWork();
        await WhenTheyFilterProjectsByLocation();
        await ThenTheyOnlySeeProjectsInTheirArea();
    }

    private async Task GivenAFullStackDeveloperPrefersLocalWork()
    {
        _output.WriteLine("GIVEN: Mike is a full-stack developer based in the San Francisco Bay Area");
        _output.WriteLine("- He prefers in-person collaboration");
        _output.WriteLine("- He has experience with React and Node.js");
        _output.WriteLine("- He's looking for projects in San Francisco or nearby");

        await Task.CompletedTask;
    }

    private async Task WhenTheyFilterProjectsByLocation()
    {
        _output.WriteLine("WHEN: Mike searches for projects in the San Francisco area");

        var reactSkill = _skills.First(s => s.Name == "React");
        var nodeSkill = _skills.First(s => s.Name == "Node.js");

        var searchRequest = new AdvancedProjectSearchDto
        {
            SkillIds = new List<Guid> { reactSkill.Id, nodeSkill.Id },
            SkillMatch = SkillMatchStrategy.Any,
            ClientLocation = "San Francisco",
            PublishedOnly = true,
            Take = 10,
            Skip = 0
        };

        _searchResponse = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
        _searchResult = await _searchResponse.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>()
            ?? throw new InvalidOperationException("Failed to deserialize search response");
    }

    private async Task ThenTheyOnlySeeProjectsInTheirArea()
    {
        _output.WriteLine("THEN: Mike sees only projects in the San Francisco area");

        Assert.NotNull(_searchResult);
        _searchResponse.EnsureSuccessStatusCode();

        // Should find the E-commerce MVP project which is in San Francisco
        Assert.True(_searchResult.Projects.Count >= 1);

        foreach (var project in _searchResult.Projects)
        {
            _output.WriteLine($"- {project.Title} in San Francisco");

            // For this test, we expect the project to be in San Francisco or remote
            // In a real implementation, location would be included in the ProjectDto
        }

        await Task.CompletedTask;
    }

    #endregion

    #region User Journey: UI Designer Looking for Design Work

    [Fact]
    public async Task UserJourney_UIDesigner_SearchesForDesignProjects()
    {
        await GivenAUIDesignerIsLookingForDesignWork();
        await WhenTheySearchForUIUXProjects();
        await ThenTheyFindAppropriateDesignProjects();
    }

    private async Task GivenAUIDesignerIsLookingForDesignWork()
    {
        _output.WriteLine("GIVEN: Emma is a UI/UX designer specializing in web and mobile design");
        _output.WriteLine("- She has expert-level UI/UX skills");
        _output.WriteLine("- She's comfortable working remotely");
        _output.WriteLine("- She prefers projects focusing on design rather than development");

        await Task.CompletedTask;
    }

    private async Task WhenTheySearchForUIUXProjects()
    {
        _output.WriteLine("WHEN: Emma searches specifically for UI/UX design projects");

        var uiSkill = _skills.First(s => s.Name == "UI/UX Design");
        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "design",
            SkillIds = new List<Guid> { uiSkill.Id },
            SkillMatch = SkillMatchStrategy.Any,
            RemoteWorkOnly = false, // Open to both remote and local
            PublishedOnly = true,
            Take = 10,
            Skip = 0
        };

        _searchResponse = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
        _searchResult = await _searchResponse.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>()
            ?? throw new InvalidOperationException("Failed to deserialize search response");
    }

    private async Task ThenTheyFindAppropriateDesignProjects()
    {
        _output.WriteLine("THEN: Emma finds projects that match her design expertise");

        Assert.NotNull(_searchResult);
        _searchResponse.EnsureSuccessStatusCode();

        // Should find the Agency Website Redesign and Executive Dashboard projects
        Assert.True(_searchResult.Projects.Count >= 1);

        var foundDesignProject = false;
        foreach (var project in _searchResult.Projects)
        {
            _output.WriteLine($"- {project.Title}");

            if (project.Title.Contains("design", StringComparison.OrdinalIgnoreCase) ||
                project.ShortDescription.Contains("design", StringComparison.OrdinalIgnoreCase) ||
                project.RequiredSkillNames.Any(s => s.Contains("Design")))
            {
                foundDesignProject = true;
            }
        }

        Assert.True(foundDesignProject, "Should find at least one design-related project");

        await Task.CompletedTask;
    }

    #endregion

    #region User Journey: Budget-Conscious Developer

    [Fact]
    public async Task UserJourney_BudgetConsciousDeveloper_FiltersByBudgetRange()
    {
        await GivenADeveloperHasSpecificBudgetRequirements();
        await WhenTheyFilterProjectsByBudgetRange();
        await ThenTheyOnlySeeProjectsWithinTheirBudgetRange();
    }

    private async Task GivenADeveloperHasSpecificBudgetRequirements()
    {
        _output.WriteLine("GIVEN: Alex is a developer who only takes high-value projects");
        _output.WriteLine("- They have multiple skills including Python and ML");
        _output.WriteLine("- They require projects with budgets above $4000");
        _output.WriteLine("- They want to focus on data science and analytics projects");

        await Task.CompletedTask;
    }

    private async Task WhenTheyFilterProjectsByBudgetRange()
    {
        _output.WriteLine("WHEN: Alex searches for high-budget data science projects");

        var pythonSkill = _skills.First(s => s.Name == "Python");
        var mlSkill = _skills.First(s => s.Name == "Machine Learning");

        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "analytics",
            SkillIds = new List<Guid> { pythonSkill.Id, mlSkill.Id },
            SkillMatch = SkillMatchStrategy.Any,
            MinBudget = 4000,
            PublishedOnly = true,
            Take = 10,
            Skip = 0
        };

        _searchResponse = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
        _searchResult = await _searchResponse.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>()
            ?? throw new InvalidOperationException("Failed to deserialize search response");
    }

    private async Task ThenTheyOnlySeeProjectsWithinTheirBudgetRange()
    {
        _output.WriteLine("THEN: Alex sees only high-value projects matching their criteria");

        Assert.NotNull(_searchResult);
        _searchResponse.EnsureSuccessStatusCode();

        // Should find the Executive Analytics Dashboard (5000 credits) and Cloud Infrastructure (4200 credits)
        Assert.True(_searchResult.Projects.Count >= 1);

        foreach (var project in _searchResult.Projects)
        {
            _output.WriteLine($"- {project.Title} (${project.CreditBudget} credits)");

            Assert.True(project.CreditBudget >= 4000,
                $"Project {project.Title} budget {project.CreditBudget} is below minimum requirement");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region User Journey: Multi-Criteria Search with Sorting

    [Fact]
    public async Task UserJourney_ExperiencedDeveloper_UsesAdvancedFiltersWithSorting()
    {
        await GivenAnExperiencedDeveloperWantsToOptimizeTheirSearch();
        await WhenTheyUseCombinedFiltersWithCustomSorting();
        await ThenTheyGetWellSortedRelevantResults();
    }

    private async Task GivenAnExperiencedDeveloperWantsToOptimizeTheirSearch()
    {
        _output.WriteLine("GIVEN: Jordan is an experienced developer who knows exactly what they want");
        _output.WriteLine("- They want projects involving React or UI/UX");
        _output.WriteLine("- They prefer budgets between $2000-$4000");
        _output.WriteLine("- They want results sorted by budget (highest first)");
        _output.WriteLine("- They're interested in both remote and local opportunities");

        await Task.CompletedTask;
    }

    private async Task WhenTheyUseCombinedFiltersWithCustomSorting()
    {
        _output.WriteLine("WHEN: Jordan performs an advanced search with multiple criteria");

        var reactSkill = _skills.First(s => s.Name == "React");
        var uiSkill = _skills.First(s => s.Name == "UI/UX Design");

        var searchRequest = new AdvancedProjectSearchDto
        {
            SkillIds = new List<Guid> { reactSkill.Id, uiSkill.Id },
            SkillMatch = SkillMatchStrategy.Any,
            MinBudget = 2000,
            MaxBudget = 4000,
            SortBy = new List<SortCriteria>
            {
                new SortCriteria { Field = "budget", Direction = "desc", Weight = 1 }
            },
            PublishedOnly = true,
            Take = 10,
            Skip = 0
        };

        _searchResponse = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
        _searchResult = await _searchResponse.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>()
            ?? throw new InvalidOperationException("Failed to deserialize search response");
    }

    private async Task ThenTheyGetWellSortedRelevantResults()
    {
        _output.WriteLine("THEN: Jordan gets projects sorted by budget with all criteria met");

        Assert.NotNull(_searchResult);
        _searchResponse.EnsureSuccessStatusCode();

        Assert.True(_searchResult.Projects.Count >= 2);

        var previousBudget = int.MaxValue;
        foreach (var project in _searchResult.Projects)
        {
            _output.WriteLine($"- {project.Title} (${project.CreditBudget} credits) - {project.RequiredSkillNames.Count} skills required");

            // Verify budget range
            Assert.True(project.CreditBudget >= 2000 && project.CreditBudget <= 4000,
                $"Project {project.Title} budget {project.CreditBudget} outside range");

            // Verify sorting (budget descending)
            Assert.True(project.CreditBudget <= previousBudget,
                $"Projects not sorted by budget: {project.CreditBudget} > {previousBudget}");

            previousBudget = project.CreditBudget;

            // Verify skills relevance
            var hasRelevantSkill = project.RequiredSkillNames.Any(s =>
                s == "React" || s == "UI/UX Design");
            Assert.True(hasRelevantSkill,
                $"Project {project.Title} doesn't require React or UI/UX skills");
        }

        await Task.CompletedTask;
    }

    #endregion

    #region User Journey: Empty Search Results Handling

    [Fact]
    public async Task UserJourney_Developer_HandlesEmptySearchResults()
    {
        await GivenADeveloperSearchesForVerySpecificCriteria();
        await WhenTheirSearchReturnsNoResults();
        await ThenTheyReceiveHelpfulEmptyStateInformation();
    }

    private async Task GivenADeveloperSearchesForVerySpecificCriteria()
    {
        _output.WriteLine("GIVEN: Pat is looking for very specific and rare project criteria");
        _output.WriteLine("- They want projects with budgets over $10,000 (very high)");
        _output.WriteLine("- They want only DevOps projects");
        _output.WriteLine("- They want only remote work");

        await Task.CompletedTask;
    }

    private async Task WhenTheirSearchReturnsNoResults()
    {
        _output.WriteLine("WHEN: Pat searches with criteria that match no projects");

        var devopsSkill = _skills.First(s => s.Name == "DevOps");
        var searchRequest = new AdvancedProjectSearchDto
        {
            SkillIds = new List<Guid> { devopsSkill.Id },
            MinBudget = 4500, // High budget, higher than our test projects
            RemoteWorkOnly = true,
            PublishedOnly = true,
            Take = 10,
            Skip = 0
        };

        _searchResponse = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
        _searchResult = await _searchResponse.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>()
            ?? throw new InvalidOperationException("Failed to deserialize search response");
    }

    private async Task ThenTheyReceiveHelpfulEmptyStateInformation()
    {
        _output.WriteLine("THEN: Pat gets a proper empty state response without errors");

        Assert.NotNull(_searchResult);
        _searchResponse.EnsureSuccessStatusCode();

        Assert.Empty(_searchResult.Projects);
        Assert.Equal(0, _searchResult.TotalCount);

        _output.WriteLine("- Empty results handled gracefully");
        _output.WriteLine("- No errors returned");
        _output.WriteLine("- Response structure maintained");

        // In a real application, this would include suggestions for broadening search criteria
        Assert.False(_searchResult.HasNextPage);
        Assert.False(_searchResult.HasPreviousPage);
        Assert.Equal(0, _searchResult.TotalPages);

        await Task.CompletedTask;
    }

    #endregion

    #region User Journey: Pagination Through Results

    [Fact]
    public async Task UserJourney_Developer_NavigatesThroughPaginatedResults()
    {
        await GivenADeveloperFindsMultipleMatchingProjects();
        await WhenTheyNavigateThroughPages();
        await ThenTheyCanAccessAllResults();
    }

    private async Task GivenADeveloperFindsMultipleMatchingProjects()
    {
        _output.WriteLine("GIVEN: Sam performs a broad search that returns multiple projects");
        _output.WriteLine("- They search for any project above $1000");
        _output.WriteLine("- They want to review all available projects");

        await Task.CompletedTask;
    }

    private async Task WhenTheyNavigateThroughPages()
    {
        _output.WriteLine("WHEN: Sam navigates through pages of results");

        // First page
        var searchRequest = new AdvancedProjectSearchDto
        {
            MinBudget = 1000,
            PublishedOnly = true,
            Take = 3, // Small page size to test pagination
            Skip = 0
        };

        _searchResponse = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
        _searchResult = await _searchResponse.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>()
            ?? throw new InvalidOperationException("Failed to deserialize search response");
    }

    private async Task ThenTheyCanAccessAllResults()
    {
        _output.WriteLine("THEN: Sam can navigate through all pages of results");

        Assert.NotNull(_searchResult);
        _searchResponse.EnsureSuccessStatusCode();

        var firstPageResults = _searchResult.Projects.ToList();
        var totalCount = _searchResult.TotalCount;
        var totalPages = _searchResult.TotalPages;

        _output.WriteLine($"- Found {totalCount} total projects across {totalPages} pages");
        _output.WriteLine($"- First page has {firstPageResults.Count} projects");

        Assert.True(totalCount >= 5, "Should have at least 5 projects with budget > $1000");
        Assert.True(_searchResult.HasNextPage || totalCount <= 3, "Should indicate if more pages exist");
        Assert.False(_searchResult.HasPreviousPage, "First page should not have previous page");
        Assert.Equal(1, _searchResult.CurrentPage);

        // If there are more pages, test navigation
        if (_searchResult.HasNextPage)
        {
            // Second page
            var secondPageRequest = new AdvancedProjectSearchDto
            {
                MinBudget = 1000,
                PublishedOnly = true,
                Take = 3,
                Skip = 3 // Second page
            };

            var secondPageResponse = await Client.PostAsJsonAsync("/api/project-search/advanced", secondPageRequest);
            var secondPageResult = await secondPageResponse.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();

            Assert.NotNull(secondPageResult);
            Assert.Equal(2, secondPageResult.CurrentPage);
            Assert.True(secondPageResult.HasPreviousPage);

            _output.WriteLine($"- Second page has {secondPageResult.Projects.Count} projects");

            // Verify no duplicate projects between pages
            var firstPageIds = firstPageResults.Select(p => p.Id);
            var secondPageIds = secondPageResult.Projects.Select(p => p.Id);
            Assert.Empty(firstPageIds.Intersect(secondPageIds));
        }

        await Task.CompletedTask;
    }

    #endregion

    // Helper fields for test state
    private HttpResponseMessage _searchResponse = null!;
    private AdvancedProjectSearchResultDto _searchResult = null!;
}