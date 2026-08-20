using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for ProfileService - User profile management.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database
/// - Uses MockFileStorageService (external service - OK to mock)
/// - Uses MockCacheService (internal - uses in-memory for tests)
/// - Uses mock IHttpContextAccessor (simple infrastructure mock)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 1 (FileStorage)
/// </summary>
[IntegrationTest]
public class ProfileServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;
    private readonly MockFileStorageService _fileStorageService;
    private readonly MockCacheService _cacheService;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly ProfileService _profileService;
    private readonly ILogger<ProfileService> _logger;

    // Test data
    private User _testUser1 = null!;
    private User _testUser2 = null!;
    private User _testUser3 = null!;

    public ProfileServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProfileServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);

        _auditLogService = new MockAuditLogService(_context);
        _fileStorageService = new MockFileStorageService();
        _cacheService = new MockCacheService();

        // Setup mock HTTP context accessor
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");
        httpContext.Request.Headers["User-Agent"] = "TestAgent/1.0";
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        _logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .CreateLogger<ProfileService>();

        _profileService = new ProfileService(
            _context,
            _auditLogService,
            _httpContextAccessor.Object,
            _fileStorageService,
            _cacheService,
            _logger
        );

        SetupTestData();
    }

    private void SetupTestData()
    {
        _testUser1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user1@test.com",
            UserName = "testuser1",
            FirstName = "Test",
            LastName = "User1"
        };

        _testUser2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user2@test.com",
            UserName = "testuser2",
            FirstName = "Test",
            LastName = "User2"
        };

        _testUser3 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user3@test.com",
            UserName = "testuser3",
            FirstName = "Test",
            LastName = "User3"
        };

        _context.Users.AddRange(_testUser1, _testUser2, _testUser3);
        _context.SaveChanges();
    }

    #region CreateProfileAsync Tests

    [Fact]
    public async Task CreateProfileAsync_ValidRequest_CreatesProfileInDatabase()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Software Engineer",
            Summary = "Experienced developer",
            Company = "Tech Corp",
            Location = "New York",
            TimeZone = "America/New_York",
            IsPublic = true,
            Visibility = ProfileVisibility.Public
        };

        // Act
        var result = await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("created successfully");
        result.Profile.Should().NotBeNull();
        result.Profile!.FirstName.Should().Be("John");
        result.Profile.LastName.Should().Be("Doe");
        result.Profile.Title.Should().Be("Software Engineer");
        result.Profile.IsComplete.Should().BeTrue();

        // Verify database state
        var savedProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == _testUser1.Id);
        savedProfile.Should().NotBeNull();
        savedProfile!.FirstName.Should().Be("John");
        savedProfile.Company.Should().Be("Tech Corp");

        // Verify audit log
        _auditLogService.LoggedEvents.Should().Contain(e => e.Action == "PROFILE_CREATED" && e.UserId == _testUser1.Id);
    }

    [Fact]
    public async Task CreateProfileAsync_NonExistentUser_ReturnsFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };

        // Act
        var result = await _profileService.CreateProfileAsync(nonExistentUserId, createDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }

    [Fact]
    public async Task CreateProfileAsync_DuplicateProfile_ReturnsFailure()
    {
        // Arrange - Create first profile
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act - Try to create second profile for same user
        var result = await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateProfileAsync_IncompleteProfile_SetsIsCompleteFalse()
    {
        // Arrange - Missing title
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Profile!.IsComplete.Should().BeFalse();

        // Verify database state
        var savedProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == _testUser1.Id);
        savedProfile!.IsComplete.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProfileAsync_TrimsWhitespace_SavesCleanData()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "  John  ",
            LastName = "  Doe  ",
            Title = "  Engineer  ",
            Company = "  Tech Corp  "
        };

        // Act
        var result = await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Profile!.FirstName.Should().Be("John");
        result.Profile.LastName.Should().Be("Doe");
        result.Profile.Title.Should().Be("Engineer");
        result.Profile.Company.Should().Be("Tech Corp");
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_ValidUpdate_UpdatesProfileInDatabase()
    {
        // Arrange - Create profile first
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Johnny",
            LastName = "Doerman",
            Title = "Senior Engineer",
            Company = "New Corp"
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(_testUser1.Id, updateDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("updated successfully");
        result.Profile!.FirstName.Should().Be("Johnny");
        result.Profile.Title.Should().Be("Senior Engineer");

        // Verify database state
        var savedProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == _testUser1.Id);
        savedProfile!.FirstName.Should().Be("Johnny");
        savedProfile.Company.Should().Be("New Corp");

        // Verify audit log
        _auditLogService.LoggedEvents.Should().Contain(e => e.Action == "PROFILE_UPDATED" && e.UserId == _testUser1.Id);
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExistentProfile_ReturnsFailure()
    {
        // Arrange
        var updateDto = new UpdateProfileDto
        {
            FirstName = "Johnny"
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(_testUser1.Id, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateProfileAsync_InvalidatesCache()
    {
        // Arrange - Create profile and cache it
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Get profile to cache it
        await _profileService.GetMyProfileAsync(_testUser1.Id);

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Johnny"
        };

        // Act
        await _profileService.UpdateProfileAsync(_testUser1.Id, updateDto);

        // Assert - Get profile again should reflect update (cache invalidated)
        var updatedProfile = await _profileService.GetMyProfileAsync(_testUser1.Id);
        updatedProfile!.FirstName.Should().Be("Johnny");
    }

    #endregion

    #region GetProfileByUserIdAsync Tests

    [Fact]
    public async Task GetProfileByUserIdAsync_ExistingProfile_ReturnsProfile()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer",
            IsPublic = true,
            Visibility = ProfileVisibility.Public
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act
        var result = await _profileService.GetProfileByUserIdAsync(_testUser1.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.Visibility.Should().Be(ProfileVisibility.Public);
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_NonExistentProfile_ReturnsNull()
    {
        // Act
        var result = await _profileService.GetProfileByUserIdAsync(_testUser1.Id, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_PrivateProfile_ReturnsNullForOthers()
    {
        // Arrange - Create private profile
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer",
            IsPublic = false,
            Visibility = ProfileVisibility.Private
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act - User2 tries to view User1's private profile
        var result = await _profileService.GetProfileByUserIdAsync(_testUser1.Id, _testUser2.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_PublicProfile_ReturnsForAnyone()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer",
            IsPublic = true,
            Visibility = ProfileVisibility.Public
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act - User2 views User1's public profile
        var result = await _profileService.GetProfileByUserIdAsync(_testUser1.Id, _testUser2.Id);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_VerifiedUsersOnly_ReturnsForAuthenticatedUsers()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer",
            Visibility = ProfileVisibility.VerifiedUsersOnly
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act - Authenticated user2 views the profile
        var result = await _profileService.GetProfileByUserIdAsync(_testUser1.Id, _testUser2.Id);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_UsesCache_OnSecondCall()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer",
            Visibility = ProfileVisibility.Public
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // First call - should hit database and cache
        await _profileService.GetProfileByUserIdAsync(_testUser1.Id, null);

        // Verify cache has the profile
        var cacheKey = $"profile:user:{_testUser1.Id}";
        var cachedProfile = await _cacheService.GetAsync<ProfileDto>(cacheKey);
        cachedProfile.Should().NotBeNull();

        // Act - Second call should use cache
        var result = await _profileService.GetProfileByUserIdAsync(_testUser1.Id, null);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
    }

    #endregion

    #region GetMyProfileAsync Tests

    [Fact]
    public async Task GetMyProfileAsync_ExistingProfile_ReturnsFullProfile()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer",
            Visibility = ProfileVisibility.Private // Even private profiles should be visible to owner
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act
        var result = await _profileService.GetMyProfileAsync(_testUser1.Id);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.Visibility.Should().Be(ProfileVisibility.Private);
    }

    [Fact]
    public async Task GetMyProfileAsync_IncludesUserSkills()
    {
        // Arrange - Create profile and add skills
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Add a skill
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "C#",
            Description = "Programming language",
            Category = "Programming"
        };
        _context.Skills.Add(skill);

        var userSkill = new UserSkill
        {
            UserId = _testUser1.Id,
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Expert
        };
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync();

        // Clear cache to force database read
        await _cacheService.RemoveAsync($"profile:user:{_testUser1.Id}");

        // Act
        var result = await _profileService.GetMyProfileAsync(_testUser1.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserSkills.Should().NotBeEmpty();
        result.UserSkills.Should().Contain(us => us.Skill.Name == "C#");
    }

    [Fact]
    public async Task GetMyProfileAsync_NonExistentProfile_ReturnsNull()
    {
        // Act
        var result = await _profileService.GetMyProfileAsync(_testUser1.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteProfileAsync Tests

    [Fact]
    public async Task DeleteProfileAsync_ExistingProfile_DeletesFromDatabase()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act
        var result = await _profileService.DeleteProfileAsync(_testUser1.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("deleted successfully");

        // Verify database state
        var deletedProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == _testUser1.Id);
        deletedProfile.Should().BeNull();

        // Verify audit log
        _auditLogService.LoggedEvents.Should().Contain(e => e.Action == "PROFILE_DELETED" && e.UserId == _testUser1.Id);
    }

    [Fact]
    public async Task DeleteProfileAsync_NonExistentProfile_ReturnsFailure()
    {
        // Act
        var result = await _profileService.DeleteProfileAsync(_testUser1.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteProfileAsync_InvalidatesCache()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Cache the profile
        await _profileService.GetMyProfileAsync(_testUser1.Id);

        // Act
        await _profileService.DeleteProfileAsync(_testUser1.Id);

        // Assert - Cache should be invalidated
        var cacheKey = $"profile:user:{_testUser1.Id}";
        var cachedProfile = await _cacheService.GetAsync<ProfileDto>(cacheKey);
        cachedProfile.Should().BeNull();
    }

    #endregion

    #region HasCompleteProfileAsync Tests

    [Fact]
    public async Task HasCompleteProfileAsync_CompleteProfile_ReturnsTrue()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act
        var result = await _profileService.HasCompleteProfileAsync(_testUser1.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasCompleteProfileAsync_IncompleteProfile_ReturnsFalse()
    {
        // Arrange - Missing title
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act
        var result = await _profileService.HasCompleteProfileAsync(_testUser1.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasCompleteProfileAsync_NoProfile_ReturnsFalse()
    {
        // Act
        var result = await _profileService.HasCompleteProfileAsync(_testUser1.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region UpdateAvatarAsync Tests

    [Fact]
    public async Task UpdateAvatarAsync_ValidUrl_UpdatesAvatar()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act
        var result = await _profileService.UpdateAvatarAsync(_testUser1.Id, "https://example.com/avatar.jpg");

        // Assert
        result.Success.Should().BeTrue();

        // Verify database state
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == _testUser1.Id);
        profile!.AvatarUrl.Should().Be("https://example.com/avatar.jpg");

        // Verify audit log
        _auditLogService.LoggedEvents.Should().Contain(e => e.Action == "AVATAR_UPDATED" && e.UserId == _testUser1.Id);
    }

    [Fact]
    public async Task UpdateAvatarAsync_NonExistentProfile_ReturnsFailure()
    {
        // Act
        var result = await _profileService.UpdateAvatarAsync(_testUser1.Id, "https://example.com/avatar.jpg");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateAvatarAsync_TrimsWhitespace()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act
        var result = await _profileService.UpdateAvatarAsync(_testUser1.Id, "  https://example.com/avatar.jpg  ");

        // Assert
        result.Success.Should().BeTrue();

        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == _testUser1.Id);
        profile!.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
    }

    #endregion

    #region DeleteAvatarAsync Tests

    [Fact]
    public async Task DeleteAvatarAsync_ExistingAvatar_DeletesAvatar()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);
        await _profileService.UpdateAvatarAsync(_testUser1.Id, "https://example.com/avatar.jpg");

        // Act
        var result = await _profileService.DeleteAvatarAsync(_testUser1.Id);

        // Assert
        result.Success.Should().BeTrue();

        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == _testUser1.Id);
        profile!.AvatarUrl.Should().BeNull();

        // Verify audit log
        _auditLogService.LoggedEvents.Should().Contain(e => e.Action == "AVATAR_DELETED" && e.UserId == _testUser1.Id);
    }

    [Fact]
    public async Task DeleteAvatarAsync_NoAvatar_StillSucceeds()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer"
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act
        var result = await _profileService.DeleteAvatarAsync(_testUser1.Id);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAvatarAsync_NonExistentProfile_ReturnsFailure()
    {
        // Act
        var result = await _profileService.DeleteAvatarAsync(_testUser1.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region GetPublicProfilesAsync Tests

    [Fact]
    public async Task GetPublicProfilesAsync_ReturnsOnlyPublicProfiles()
    {
        // Arrange - Create public and private profiles
        var publicProfileDto = new CreateProfileDto
        {
            FirstName = "Public",
            LastName = "User",
            Title = "Engineer",
            Visibility = ProfileVisibility.Public,
            IsPublic = true
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, publicProfileDto);

        var privateProfileDto = new CreateProfileDto
        {
            FirstName = "Private",
            LastName = "User",
            Title = "Engineer",
            Visibility = ProfileVisibility.Private,
            IsPublic = false
        };
        await _profileService.CreateProfileAsync(_testUser2.Id, privateProfileDto);

        // Act
        var result = await _profileService.GetPublicProfilesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].FirstName.Should().Be("Public");
    }

    [Fact]
    public async Task GetPublicProfilesAsync_WithSearchTerm_FiltersResults()
    {
        // Arrange
        var createDto1 = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Developer",
            Title = "Software Engineer",
            Visibility = ProfileVisibility.Public,
            IsPublic = true
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto1);

        var createDto2 = new CreateProfileDto
        {
            FirstName = "Jane",
            LastName = "Designer",
            Title = "UX Designer",
            Visibility = ProfileVisibility.Public,
            IsPublic = true
        };
        await _profileService.CreateProfileAsync(_testUser2.Id, createDto2);

        // Act
        var result = await _profileService.GetPublicProfilesAsync("Developer");

        // Assert
        result.Should().HaveCount(1);
        result[0].FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetPublicProfilesAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Create multiple public profiles
        for (int i = 1; i <= 5; i++)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"user{i}@test.com",
                UserName = $"user{i}",
                FirstName = $"User{i}",
                LastName = "Test"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var createDto = new CreateProfileDto
            {
                FirstName = $"User{i}",
                LastName = "Test",
                Title = "Engineer",
                Visibility = ProfileVisibility.Public,
                IsPublic = true
            };
            await _profileService.CreateProfileAsync(user.Id, createDto);
        }

        // Act
        var result = await _profileService.GetPublicProfilesAsync(null, skip: 2, take: 2);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPublicProfilesAsync_SearchByCompany_ReturnsMatches()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer",
            Company = "Acme Corporation",
            Visibility = ProfileVisibility.Public,
            IsPublic = true
        };
        await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Act
        var result = await _profileService.GetPublicProfilesAsync("Acme");

        // Assert
        result.Should().HaveCount(1);
        result[0].Company.Should().Be("Acme Corporation");
    }

    #endregion

    #region Visibility Sync Tests

    [Fact]
    public async Task CreateProfileAsync_IsPublicTrue_SetsVisibilityToPublic()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer",
            IsPublic = true,
            Visibility = ProfileVisibility.Private // Intentional mismatch
        };

        // Act
        var result = await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Profile!.Visibility.Should().Be(ProfileVisibility.Public);
        result.Profile.IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProfileAsync_IsPublicFalse_SetsVisibilityToPrivate()
    {
        // Arrange
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Engineer",
            IsPublic = false,
            Visibility = ProfileVisibility.Public // Intentional mismatch
        };

        // Act
        var result = await _profileService.CreateProfileAsync(_testUser1.Id, createDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Profile!.Visibility.Should().Be(ProfileVisibility.Private);
        result.Profile.IsPublic.Should().BeFalse();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
