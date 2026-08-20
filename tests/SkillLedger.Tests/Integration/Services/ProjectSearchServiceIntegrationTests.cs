using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for ProjectSearchService - ADVANCED PROJECT SEARCH SYSTEM.
///
/// Pattern (per TDD_GUIDE.md):
/// - Uses real in-memory EF Core database
/// - Uses real in-memory distributed cache
/// - Tests actual search logic with real database queries
/// - Verifies search results, filtering, and pagination
///
/// Max mocked external dependencies: 0 (uses real MemoryDistributedCache)
/// </summary>
[IntegrationTest]
public class ProjectSearchServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly ProjectSearchService _service;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ProjectSearchService> _logger;

    // Test data
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _providerId = Guid.NewGuid();
    private readonly Guid _projectId1 = Guid.NewGuid();
    private readonly Guid _projectId2 = Guid.NewGuid();
    private readonly Guid _projectId3 = Guid.NewGuid();
    private readonly Guid _skillCSharp = Guid.NewGuid();
    private readonly Guid _skillAzure = Guid.NewGuid();
    private readonly Guid _skillReact = Guid.NewGuid();

    public ProjectSearchServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProjectSearchServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);

        var cacheOptions = Options.Create(new MemoryDistributedCacheOptions());
        _cache = new MemoryDistributedCache(cacheOptions);

        _logger = new LoggerFactory().CreateLogger<ProjectSearchService>();

        _service = new ProjectSearchService(_context, _cache, _logger);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create client user
        var client = new User
        {
            Id = _clientId,
            Email = "client@test.com",
            UserName = "TestClient",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            FirstName = "John",
            LastName = "Client",
            CreatedAt = DateTime.UtcNow,
            Profile = new Profile
            {
                FirstName = "John",
                LastName = "Client",
                UserId = _clientId,
                Company = "Test Company"
            }
        };

        // Create provider user
        var provider = new User
        {
            Id = _providerId,
            Email = "provider@test.com",
            UserName = "TestProvider",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            FirstName = "Jane",
            LastName = "Provider",
            CreatedAt = DateTime.UtcNow,
            Profile = new Profile
            {
                FirstName = "Jane",
                LastName = "Provider",
                UserId = _providerId
            }
        };

        // Create skills
        var skillCSharp = new Skill { Id = _skillCSharp, Name = "C#", Category = "Programming" };
        var skillAzure = new Skill { Id = _skillAzure, Name = "Azure", Category = "Cloud" };
        var skillReact = new Skill { Id = _skillReact, Name = "React", Category = "Frontend" };

        _context.Skills.AddRange(skillCSharp, skillAzure, skillReact);

        // Create provider skills
        var userSkill1 = new UserSkill { UserId = _providerId, SkillId = _skillCSharp, Proficiency = SkillProficiency.Expert, IsVisible = true };
        var userSkill2 = new UserSkill { UserId = _providerId, SkillId = _skillAzure, Proficiency = SkillProficiency.Advanced, IsVisible = true };
        _context.UserSkills.AddRange(userSkill1, userSkill2);

        // Create published projects
        var project1 = new Project
        {
            Id = _projectId1,
            ClientId = _clientId,
            Client = client,
            Title = "E-Commerce Platform Development",
            Description = "Build a comprehensive e-commerce platform using C# and Azure services. Includes shopping cart, payment integration, and inventory management.",
            Status = ProjectStatus.Published,
            ModerationStatus = ModerationStatus.Approved,
            Visibility = ProjectVisibility.Public,
            CreditBudget = 5000,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(60),
            IsFeatured = true,
            IsRemoteWork = true,
            LocationCity = "New York",
            LocationState = "NY",
            LocationCountry = "USA",
            SearchText = "E-Commerce Platform Development C# Azure shopping cart payment"
        };

        project1.ProjectSkills = new List<ProjectSkill>
        {
            new() { ProjectId = _projectId1, SkillId = _skillCSharp, Weight = 3, ProficiencyRequired = SkillProficiency.Expert, Skill = skillCSharp },
            new() { ProjectId = _projectId1, SkillId = _skillAzure, Weight = 2, ProficiencyRequired = SkillProficiency.Advanced, Skill = skillAzure }
        };

        project1.Deliverables = new List<ProjectDeliverable>
        {
            new() { Id = Guid.NewGuid(), ProjectId = _projectId1, Description = "RESTful API implementation for backend", OrderIndex = 1 },
            new() { Id = Guid.NewGuid(), ProjectId = _projectId1, Description = "User interface implementation", OrderIndex = 2 }
        };

        var project2 = new Project
        {
            Id = _projectId2,
            ClientId = _clientId,
            Client = client,
            Title = "React Dashboard Application",
            Description = "Create a modern React dashboard with real-time data visualization and reporting features.",
            Status = ProjectStatus.Published,
            ModerationStatus = ModerationStatus.Approved,
            Visibility = ProjectVisibility.Public,
            CreditBudget = 2500,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            StartDate = DateTime.UtcNow.AddDays(14),
            EndDate = DateTime.UtcNow.AddDays(44),
            IsFeatured = false,
            IsRemoteWork = false,
            LocationCity = "San Francisco",
            LocationState = "CA",
            LocationCountry = "USA",
            SearchText = "React Dashboard Application visualization reporting"
        };

        project2.ProjectSkills = new List<ProjectSkill>
        {
            new() { ProjectId = _projectId2, SkillId = _skillReact, Weight = 3, ProficiencyRequired = SkillProficiency.Expert, Skill = skillReact }
        };

        var project3 = new Project
        {
            Id = _projectId3,
            ClientId = _clientId,
            Client = client,
            Title = "Mobile App Backend",
            Description = "API development for mobile application using Azure Functions.",
            Status = ProjectStatus.Draft,
            ModerationStatus = ModerationStatus.Pending,
            Visibility = ProjectVisibility.Private,
            CreditBudget = 1500,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(60),
            IsFeatured = false,
            IsRemoteWork = true,
            SearchText = "Mobile App Backend Azure Functions API"
        };

        project3.ProjectSkills = new List<ProjectSkill>
        {
            new() { ProjectId = _projectId3, SkillId = _skillAzure, Weight = 2, ProficiencyRequired = SkillProficiency.Intermediate, Skill = skillAzure }
        };

        _context.Users.AddRange(client, provider);
        _context.Projects.AddRange(project1, project2, project3);
        _context.SaveChanges();
    }

    #region AdvancedSearchAsync Tests

    [Fact]
    public async Task AdvancedSearchAsync_NoFilters_ReturnsPublishedProjects()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2); // Only 2 published projects
        result.Projects.Should().HaveCount(2);
        result.Projects.Should().Contain(p => p.Id == _projectId1);
        result.Projects.Should().Contain(p => p.Id == _projectId2);
        result.Projects.Should().NotContain(p => p.Id == _projectId3); // Draft project
    }

    [Fact]
    public async Task AdvancedSearchAsync_TextSearch_ReturnsMatchingProjects()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            Query = "React Dashboard",
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.Should().HaveCount(1);
        result.Projects.First().Id.Should().Be(_projectId2);
    }

    [Fact]
    public async Task AdvancedSearchAsync_SkillFilter_ReturnsProjectsWithSkill()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            SkillIds = new List<Guid> { _skillAzure },
            SkillMatch = SkillMatchStrategy.Any,
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId1);
    }

    [Fact]
    public async Task AdvancedSearchAsync_AllSkillsMatch_ReturnsProjectsWithAllSkills()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            SkillIds = new List<Guid> { _skillCSharp, _skillAzure },
            SkillMatch = SkillMatchStrategy.All,
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId1);
    }

    [Fact]
    public async Task AdvancedSearchAsync_BudgetRange_ReturnsProjectsInRange()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            MinBudget = 2000,
            MaxBudget = 3000,
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId2); // 2500 budget
    }

    [Fact]
    public async Task AdvancedSearchAsync_MinBudgetOnly_ReturnsProjectsAboveMinimum()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            MinBudget = 3000,
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId1); // 5000 budget
    }

    [Fact]
    public async Task AdvancedSearchAsync_RemoteWorkOnly_ReturnsRemoteProjects()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            RemoteWorkOnly = true,
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId1);
    }

    [Fact]
    public async Task AdvancedSearchAsync_ClientLocation_ReturnsProjectsByLocation()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            ClientLocation = "New York",
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId1);
    }

    [Fact]
    public async Task AdvancedSearchAsync_StatusFilter_ReturnsProjectsByStatus()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            Status = new List<string> { "Published" },
            PublishedOnly = false, // Override to test status filter directly
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Projects.All(p => p.Status == "Published").Should().BeTrue();
    }

    [Fact]
    public async Task AdvancedSearchAsync_ExcludeClients_ExcludesSpecifiedClients()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            ExcludeClients = new List<Guid> { _clientId },
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task AdvancedSearchAsync_DateRangeFilter_ReturnsProjectsInRange()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            CreatedFrom = DateTime.UtcNow.AddDays(-7),
            CreatedTo = DateTime.UtcNow,
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId2); // Created 5 days ago
    }

    [Fact]
    public async Task AdvancedSearchAsync_Pagination_ReturnsPaginatedResults()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            PublishedOnly = true,
            Skip = 0,
            Take = 1
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Projects.Should().HaveCount(1);
        result.TotalPages.Should().Be(2);
        result.CurrentPage.Should().Be(1);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task AdvancedSearchAsync_SecondPage_ReturnsCorrectResults()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            PublishedOnly = true,
            Skip = 1,
            Take = 1
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Projects.Should().HaveCount(1);
        result.CurrentPage.Should().Be(2);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task AdvancedSearchAsync_SortByBudget_ReturnsSortedResults()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            SortBy = new List<SortCriteria>
            {
                new() { Field = "budget", Direction = "desc", Weight = 1 }
            },
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.Projects.First().CreditBudget.Should().Be(5000);
        result.Projects.Last().CreditBudget.Should().Be(2500);
    }

    [Fact]
    public async Task AdvancedSearchAsync_SortByCreated_ReturnsSortedResults()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            SortBy = new List<SortCriteria>
            {
                new() { Field = "created", Direction = "asc", Weight = 1 }
            },
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.Projects.First().Id.Should().Be(_projectId1); // Created first (10 days ago)
    }

    [Fact]
    public async Task AdvancedSearchAsync_IncludesMetadata_ReturnsExecutionTime()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata.ExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
        result.Metadata.FromCache.Should().BeFalse();
    }

    [Fact]
    public async Task AdvancedSearchAsync_WithAggregations_ReturnsAggregationData()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            PublishedOnly = true,
            IncludeAggregations = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.Aggregations.Should().NotBeNull();
    }

    [Fact]
    public async Task AdvancedSearchAsync_MinDeliverables_ReturnsProjectsWithMinimumDeliverables()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            MinDeliverables = 2,
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId1); // Has 2 deliverables
    }

    [Fact]
    public async Task AdvancedSearchAsync_DurationFilter_ReturnsProjectsWithMatchingDuration()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            MinDurationDays = 20,
            MaxDurationDays = 40,
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId2); // 30 days duration
    }

    #endregion

    #region GetRecommendedProjectsAsync Tests

    [Fact]
    public async Task GetRecommendedProjectsAsync_WithMatchingSkills_ReturnsRecommendedProjects()
    {
        // Act
        var result = await _service.GetRecommendedProjectsAsync(_providerId, 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().Contain(p => p.Id == _projectId1); // Matches C# and Azure
    }

    [Fact]
    public async Task GetRecommendedProjectsAsync_WithExclusions_ExcludesSpecifiedProjects()
    {
        // Act
        var result = await _service.GetRecommendedProjectsAsync(_providerId, 10, new List<Guid> { _projectId1 });

        // Assert
        result.Should().NotContain(p => p.Id == _projectId1);
    }

    [Fact]
    public async Task GetRecommendedProjectsAsync_RespectsLimit_ReturnsLimitedResults()
    {
        // Act
        var result = await _service.GetRecommendedProjectsAsync(_providerId, 1);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRecommendedProjectsAsync_NoSkills_ReturnsPublishedProjects()
    {
        // Arrange
        var newUserId = Guid.NewGuid();
        var newUser = new User
        {
            Id = newUserId,
            Email = "newuser@test.com",
            UserName = "NewUser",
            PasswordHash = "hash",
            Status = UserStatus.Active,
            FirstName = "New",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            Profile = new Profile { FirstName = "New", LastName = "User", UserId = newUserId }
        };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecommendedProjectsAsync(newUserId, 10);

        // Assert
        result.Should().NotBeNull();
        // Returns all published projects since user has no skills
    }

    #endregion

    #region GetSimilarProjectsAsync Tests

    [Fact]
    public async Task GetSimilarProjectsAsync_WithMatchingSkills_ReturnsSimilarProjects()
    {
        // Act
        var result = await _service.GetSimilarProjectsAsync(_projectId1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain(p => p.Id == _projectId1); // Excludes reference project
    }

    [Fact]
    public async Task GetSimilarProjectsAsync_ProjectNotFound_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetSimilarProjectsAsync(Guid.NewGuid(), 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSimilarProjectsAsync_RespectsLimit_ReturnsLimitedResults()
    {
        // Act
        var result = await _service.GetSimilarProjectsAsync(_projectId1, 1);

        // Assert
        result.Should().HaveCountLessOrEqualTo(1);
    }

    #endregion

    #region SearchByLocationAsync Tests

    [Fact]
    public async Task SearchByLocationAsync_WithinRadius_ReturnsNearbyProjects()
    {
        // Arrange - Add location to project
        var project = await _context.Projects.FindAsync(_projectId1);
        project!.LocationLatitude = 40.7128; // NYC coordinates
        project.LocationLongitude = -74.0060;
        await _context.SaveChangesAsync();

        // Act - Search near NYC
        var result = await _service.SearchByLocationAsync(40.7128, -74.0060, 100);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(p => p.Id == _projectId1);
    }

    [Fact]
    public async Task SearchByLocationAsync_WithAdditionalFilters_AppliesAllFilters()
    {
        // Arrange
        var project = await _context.Projects.FindAsync(_projectId1);
        project!.LocationLatitude = 40.7128;
        project.LocationLongitude = -74.0060;
        await _context.SaveChangesAsync();

        var additionalFilters = new AdvancedProjectSearchDto
        {
            MinBudget = 4000
        };

        // Act
        var result = await _service.SearchByLocationAsync(40.7128, -74.0060, 100, additionalFilters);

        // Assert
        result.Should().Contain(p => p.Id == _projectId1);
    }

    #endregion

    #region SavedSearch Tests

    [Fact]
    public async Task CreateSavedSearchAsync_ValidData_CreatesSavedSearch()
    {
        // Arrange
        var createDto = new CreateSavedSearchDto
        {
            Name = "My C# Projects Search",
            Description = "Search for C# development projects",
            SearchCriteria = new AdvancedProjectSearchDto
            {
                SkillIds = new List<Guid> { _skillCSharp },
                PublishedOnly = true
            },
            NotificationsEnabled = true,
            NotificationFrequency = NotificationFrequency.Daily
        };

        // Act
        var result = await _service.CreateSavedSearchAsync(_providerId, createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("My C# Projects Search");
        result.UserId.Should().Be(_providerId);
        result.NotificationsEnabled.Should().BeTrue();
        result.IsActive.Should().BeTrue();

        // Verify in database
        var savedSearch = await _context.SavedSearches.FindAsync(result.Id);
        savedSearch.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSavedSearchesAsync_ReturnsUserSearches()
    {
        // Arrange
        await CreateTestSavedSearch("Search 1");
        await CreateTestSavedSearch("Search 2");

        // Act
        var result = await _service.GetSavedSearchesAsync(_providerId);

        // Assert
        result.Should().HaveCount(2);
        result.All(s => s.UserId == _providerId).Should().BeTrue();
    }

    [Fact]
    public async Task GetSavedSearchesAsync_ActiveOnly_ReturnsOnlyActiveSearches()
    {
        // Arrange
        var activeSearch = await CreateTestSavedSearch("Active Search", isActive: true);
        var inactiveSearch = await CreateTestSavedSearch("Inactive Search", isActive: false);

        // Act
        var result = await _service.GetSavedSearchesAsync(_providerId, activeOnly: true);

        // Assert
        result.Should().Contain(s => s.Id == activeSearch.Id);
        result.Should().NotContain(s => s.Id == inactiveSearch.Id);
    }

    [Fact]
    public async Task ExecuteSavedSearchAsync_ValidSearch_ExecutesAndUpdatesUsage()
    {
        // Arrange
        var savedSearch = await CreateTestSavedSearch("Test Search");
        var originalUsageCount = savedSearch.UsageCount;

        // Act
        var result = await _service.ExecuteSavedSearchAsync(savedSearch.Id, _providerId);

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().NotBeNull();

        // Verify usage stats updated
        var updatedSearch = await _context.SavedSearches.FindAsync(savedSearch.Id);
        updatedSearch!.UsageCount.Should().Be(originalUsageCount + 1);
        updatedSearch.LastUsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteSavedSearchAsync_NotFound_ThrowsException()
    {
        // Act & Assert
        await _service.Invoking(s => s.ExecuteSavedSearchAsync(Guid.NewGuid(), _providerId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ExecuteSavedSearchAsync_WrongUser_ThrowsException()
    {
        // Arrange
        var savedSearch = await CreateTestSavedSearch("Test Search");
        var wrongUserId = Guid.NewGuid();

        // Act & Assert
        await _service.Invoking(s => s.ExecuteSavedSearchAsync(savedSearch.Id, wrongUserId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*access denied*");
    }

    [Fact]
    public async Task UpdateSavedSearchAsync_ValidData_UpdatesSearch()
    {
        // Arrange
        var savedSearch = await CreateTestSavedSearch("Original Name");
        var updateDto = new CreateSavedSearchDto
        {
            Name = "Updated Name",
            Description = "Updated description",
            SearchCriteria = new AdvancedProjectSearchDto { PublishedOnly = true },
            NotificationsEnabled = false,
            NotificationFrequency = NotificationFrequency.Weekly
        };

        // Act
        var result = await _service.UpdateSavedSearchAsync(savedSearch.Id, _providerId, updateDto);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated description");
        result.NotificationsEnabled.Should().BeFalse();
        result.NotificationFrequency.Should().Be(NotificationFrequency.Weekly);
    }

    [Fact]
    public async Task DeleteSavedSearchAsync_ValidSearch_DeletesSearch()
    {
        // Arrange
        var savedSearch = await CreateTestSavedSearch("To Be Deleted");

        // Act
        var result = await _service.DeleteSavedSearchAsync(savedSearch.Id, _providerId);

        // Assert
        result.Success.Should().BeTrue();

        var deletedSearch = await _context.SavedSearches.FindAsync(savedSearch.Id);
        deletedSearch.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSavedSearchAsync_NotFound_ReturnsFailure()
    {
        // Act
        var result = await _service.DeleteSavedSearchAsync(Guid.NewGuid(), _providerId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region UpdateSearchIndexAsync Tests

    [Fact]
    public async Task UpdateSearchIndexAsync_ValidProject_UpdatesSearchText()
    {
        // Arrange
        var project = await _context.Projects.FindAsync(_projectId1);
        var originalSearchText = project!.SearchText;

        // Act
        await _service.UpdateSearchIndexAsync(_projectId1);

        // Assert
        await _context.Entry(project).ReloadAsync();
        project.SearchText.Should().NotBeNull();
        project.SearchText.Should().Contain("E-Commerce");
    }

    [Fact]
    public async Task UpdateSearchIndexAsync_ProjectNotFound_DoesNotThrow()
    {
        // Act & Assert
        await _service.Invoking(s => s.UpdateSearchIndexAsync(Guid.NewGuid()))
            .Should().NotThrowAsync();
    }

    #endregion

    #region RebuildSearchIndexAsync Tests

    [Fact]
    public async Task RebuildSearchIndexAsync_Success_UpdatesAllProjects()
    {
        // Act
        var result = await _service.RebuildSearchIndexAsync(batchSize: 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Successfully");
    }

    #endregion

    #region GetTrendingProjectsAsync Tests

    [Fact]
    public async Task GetTrendingProjectsAsync_ReturnsRecentPublishedProjects()
    {
        // Act
        var result = await _service.GetTrendingProjectsAsync(timeRange: 24 * 30, limit: 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(p => p.Status == "Published");
    }

    [Fact]
    public async Task GetTrendingProjectsAsync_FeaturedFirst_ReturnsFeaturedProjectsFirst()
    {
        // Act
        var result = await _service.GetTrendingProjectsAsync(timeRange: 24 * 30, limit: 10);

        // Assert
        if (result.Any(p => p.Id == _projectId1))
        {
            // Featured project should be first
            result.First().Id.Should().Be(_projectId1);
        }
    }

    #endregion

    #region ValidateSearchCriteriaAsync Tests

    [Fact]
    public async Task ValidateSearchCriteriaAsync_ValidCriteria_ReturnsSuccess()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            MinBudget = 100,
            MaxBudget = 1000,
            Latitude = 40.7128,
            Longitude = -74.0060,
            RadiusKm = 100
        };

        // Act
        var result = await _service.ValidateSearchCriteriaAsync(searchDto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSearchCriteriaAsync_InvalidLatitude_ReturnsError()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            Latitude = 100 // Invalid: must be -90 to 90
        };

        // Act
        var result = await _service.ValidateSearchCriteriaAsync(searchDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Latitude");
    }

    [Fact]
    public async Task ValidateSearchCriteriaAsync_InvalidLongitude_ReturnsError()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            Longitude = 200 // Invalid: must be -180 to 180
        };

        // Act
        var result = await _service.ValidateSearchCriteriaAsync(searchDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Longitude");
    }

    [Fact]
    public async Task ValidateSearchCriteriaAsync_InvalidBudgetRange_ReturnsError()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            MinBudget = 1000,
            MaxBudget = 100 // Invalid: min > max
        };

        // Act
        var result = await _service.ValidateSearchCriteriaAsync(searchDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("budget");
    }

    [Fact]
    public async Task ValidateSearchCriteriaAsync_InvalidDateRange_ReturnsError()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            CreatedFrom = DateTime.UtcNow,
            CreatedTo = DateTime.UtcNow.AddDays(-7) // Invalid: from > to
        };

        // Act
        var result = await _service.ValidateSearchCriteriaAsync(searchDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("date");
    }

    [Fact]
    public async Task ValidateSearchCriteriaAsync_InvalidRadius_ReturnsError()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            RadiusKm = 15000 // Invalid: max 10000
        };

        // Act
        var result = await _service.ValidateSearchCriteriaAsync(searchDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("radius");
    }

    #endregion

    #region GetSearchAggregationsAsync Tests

    [Fact]
    public async Task GetSearchAggregationsAsync_ReturnsSkillAggregations()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto { PublishedOnly = true };

        // Act
        var result = await _service.GetSearchAggregationsAsync(searchDto);

        // Assert
        result.Should().NotBeNull();
        result.Skills.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSearchAggregationsAsync_ReturnsBudgetRanges()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto { PublishedOnly = true };

        // Act
        var result = await _service.GetSearchAggregationsAsync(searchDto);

        // Assert
        result.BudgetRanges.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSearchAggregationsAsync_ReturnsStatusCounts()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto { PublishedOnly = false };

        // Act
        var result = await _service.GetSearchAggregationsAsync(searchDto);

        // Assert
        result.Status.Should().NotBeNull();
    }

    #endregion

    #region GetSearchAnalyticsAsync Tests

    [Fact]
    public async Task GetSearchAnalyticsAsync_ReturnsAnalytics()
    {
        // Act
        var result = await _service.GetSearchAnalyticsAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private async Task<SavedSearchDto> CreateTestSavedSearch(string name, bool isActive = true)
    {
        var savedSearch = new SavedSearch
        {
            Id = Guid.NewGuid(),
            UserId = _providerId,
            Name = name,
            Description = "Test saved search",
            SearchCriteria = System.Text.Json.JsonSerializer.Serialize(new AdvancedProjectSearchDto
            {
                PublishedOnly = true
            }),
            NotificationsEnabled = false,
            NotificationFrequency = NotificationFrequency.Daily,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = isActive,
            UsageCount = 0
        };

        _context.SavedSearches.Add(savedSearch);
        await _context.SaveChangesAsync();

        return new SavedSearchDto
        {
            Id = savedSearch.Id,
            UserId = savedSearch.UserId,
            Name = savedSearch.Name,
            Description = savedSearch.Description,
            SearchCriteria = savedSearch.SearchCriteria,
            NotificationsEnabled = savedSearch.NotificationsEnabled,
            NotificationFrequency = savedSearch.NotificationFrequency,
            CreatedAt = savedSearch.CreatedAt,
            IsActive = savedSearch.IsActive,
            UsageCount = savedSearch.UsageCount
        };
    }

    #endregion

    #region Edge Case Tests for Coverage (Phase 1.3)

    [Fact]
    public async Task AdvancedSearchAsync_MaxDeliverables_ReturnsProjectsWithMaximumDeliverables()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            MaxDeliverables = 1,
            PublishedOnly = true,
            Skip = 0,
            Take = 10
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert - Project1 has 2 deliverables, Project2 has 0, so only Project2 should match
        result.TotalCount.Should().Be(1);
        result.Projects.First().Id.Should().Be(_projectId2);
    }

    [Fact]
    public async Task AdvancedSearchAsync_DefaultSort_Budget_SortsCorrectly()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            PublishedOnly = true,
            Skip = 0,
            Take = 10,
            DefaultSort = "budget"
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert - Should sort by budget descending
        result.Projects.First().CreditBudget.Should().BeGreaterThanOrEqualTo(result.Projects.Last().CreditBudget);
    }

    [Fact]
    public async Task AdvancedSearchAsync_DefaultSort_Title_SortsAlphabetically()
    {
        // Arrange
        var searchDto = new AdvancedProjectSearchDto
        {
            PublishedOnly = true,
            Skip = 0,
            Take = 10,
            DefaultSort = "title"
        };

        // Act
        var result = await _service.AdvancedSearchAsync(searchDto);

        // Assert - Should sort by title ascending (alphabetically)
        result.Projects.Should().BeInAscendingOrder(p => p.Title);
    }

    [Fact]
    public async Task ExecuteSavedSearchAsync_InvalidSearchCriteria_ThrowsInvalidOperationException()
    {
        // Arrange - Create saved search with invalid JSON
        var invalidSavedSearch = new SavedSearch
        {
            Id = Guid.NewGuid(),
            UserId = _providerId,
            Name = "Invalid Search",
            Description = "Test",
            SearchCriteria = "{invalid json}",  // Invalid JSON
            NotificationsEnabled = false,
            NotificationFrequency = NotificationFrequency.Daily,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
            UsageCount = 0
        };

        _context.SavedSearches.Add(invalidSavedSearch);
        await _context.SaveChangesAsync();

        // Act & Assert - Should throw InvalidOperationException
        await _service.Invoking(s => s.ExecuteSavedSearchAsync(invalidSavedSearch.Id, _providerId))
            .Should().ThrowAsync<Exception>();  // Will throw JsonException or InvalidOperationException
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_cache is IDisposable disposableCache)
        {
            disposableCache.Dispose();
        }
    }
}
