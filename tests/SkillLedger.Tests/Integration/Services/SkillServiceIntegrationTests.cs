using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
/// Integration tests for SkillService - PLATFORM CORE (skills marketplace).
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Uses MockCacheService (simulates caching behavior)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 1 (Cache - since Redis is external)
/// </summary>
[IntegrationTest]
public class SkillServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;
    private readonly MockCacheService _cacheService;
    private readonly ILogger<SkillService> _logger;
    private readonly SkillService _skillService;

    public SkillServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"SkillServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        _auditLogService = new MockAuditLogService(_context);
        _cacheService = new MockCacheService();
        _logger = new LoggerFactory().CreateLogger<SkillService>();

        _skillService = new SkillService(
            _context,
            _auditLogService,
            _cacheService,
            _logger
        );
    }

    #region Helper Methods

    private async Task<User> CreateTestUserAsync(string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active
        };

        // Create associated profile
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Title = "Developer",
            Company = "Test Corp"
        };

        _context.Users.Add(user);
        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Skill> CreateTestSkillAsync(string name, string category, bool isSystemManaged = false)
    {
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"Description for {name}",
            Category = category,
            IsSystemManaged = isSystemManaged,
            IsActive = true
        };
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        return skill;
    }

    private async Task<UserSkill> CreateUserSkillAsync(Guid userId, Guid skillId, SkillProficiency proficiency = SkillProficiency.Intermediate)
    {
        var userSkill = new UserSkill
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SkillId = skillId,
            Proficiency = proficiency,
            YearsOfExperience = 3,
            Notes = "Test notes",
            IsFeatured = false,
            IsVisible = true
        };
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync();
        return userSkill;
    }

    #endregion

    #region CreateSkillAsync Tests

    [Fact]
    public async Task CreateSkillAsync_ValidSkill_CreatesSuccessfully()
    {
        // Arrange
        var dto = new CreateSkillDto
        {
            Name = "React Testing",
            Description = "Testing React applications",
            Category = "Testing"
        };

        // Act
        var result = await _skillService.CreateSkillAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Skill created successfully");
        result.Data.Should().NotBeNull();

        var skillDto = result.Data as SkillDto;
        skillDto.Should().NotBeNull();
        skillDto!.Name.Should().Be("React Testing");
        skillDto.Category.Should().Be("Testing");
        skillDto.IsSystemManaged.Should().BeFalse();
        skillDto.IsActive.Should().BeTrue();

        // Verify in database
        var savedSkill = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "React Testing");
        savedSkill.Should().NotBeNull();
        savedSkill!.Category.Should().Be("Testing");
    }

    [Fact]
    public async Task CreateSkillAsync_DuplicateName_ReturnsError()
    {
        // Arrange
        await CreateTestSkillAsync("JavaScript", "Programming");
        var dto = new CreateSkillDto
        {
            Name = "JavaScript",  // Duplicate
            Description = "Another JS skill",
            Category = "Web Development"
        };

        // Act
        var result = await _skillService.CreateSkillAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("A skill with this name already exists");
    }

    [Fact]
    public async Task CreateSkillAsync_DuplicateCaseInsensitive_ReturnsError()
    {
        // Arrange
        await CreateTestSkillAsync("TypeScript", "Programming");
        var dto = new CreateSkillDto
        {
            Name = "TYPESCRIPT",  // Case-insensitive duplicate
            Description = "Another TS skill",
            Category = "Web Development"
        };

        // Act
        var result = await _skillService.CreateSkillAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("A skill with this name already exists");
    }

    [Fact]
    public async Task CreateSkillAsync_TrimsWhitespace()
    {
        // Arrange
        var dto = new CreateSkillDto
        {
            Name = "  Python  ",
            Description = "  Python programming  ",
            Category = "  Programming  "
        };

        // Act
        var result = await _skillService.CreateSkillAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        var skillDto = result.Data as SkillDto;
        skillDto!.Name.Should().Be("Python");
        skillDto.Category.Should().Be("Programming");
    }

    #endregion

    #region UpdateSkillAsync Tests

    [Fact]
    public async Task UpdateSkillAsync_ValidUpdate_UpdatesSuccessfully()
    {
        // Arrange
        var skill = await CreateTestSkillAsync("Old Name", "Old Category");
        var dto = new UpdateSkillDto
        {
            Name = "New Name",
            Description = "Updated description",
            Category = "New Category",
            IsActive = true
        };

        // Act
        var result = await _skillService.UpdateSkillAsync(skill.Id, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Skill updated successfully");

        var updatedSkill = await _context.Skills.FindAsync(skill.Id);
        updatedSkill!.Name.Should().Be("New Name");
        updatedSkill.Description.Should().Be("Updated description");
        updatedSkill.Category.Should().Be("New Category");
    }

    [Fact]
    public async Task UpdateSkillAsync_NonExistentSkill_ReturnsError()
    {
        // Arrange
        var dto = new UpdateSkillDto
        {
            Name = "New Name",
            Category = "Category"
        };

        // Act
        var result = await _skillService.UpdateSkillAsync(Guid.NewGuid(), dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Skill not found");
    }

    [Fact]
    public async Task UpdateSkillAsync_SystemManagedSkill_ReturnsError()
    {
        // Arrange
        var skill = await CreateTestSkillAsync("C#", "Programming", isSystemManaged: true);
        var dto = new UpdateSkillDto
        {
            Name = "C Sharp"
        };

        // Act
        var result = await _skillService.UpdateSkillAsync(skill.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("System-managed skills cannot be modified");
    }

    [Fact]
    public async Task UpdateSkillAsync_NameConflict_ReturnsError()
    {
        // Arrange
        await CreateTestSkillAsync("Existing Skill", "Category");
        var skillToUpdate = await CreateTestSkillAsync("Original Skill", "Category");
        var dto = new UpdateSkillDto
        {
            Name = "Existing Skill"  // Conflicts with existing
        };

        // Act
        var result = await _skillService.UpdateSkillAsync(skillToUpdate.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("A skill with this name already exists");
    }

    [Fact]
    public async Task UpdateSkillAsync_SameNameDifferentCase_Succeeds()
    {
        // Arrange
        var skill = await CreateTestSkillAsync("MySkill", "Category");
        var dto = new UpdateSkillDto
        {
            Name = "MYSKILL"  // Same skill, different case - should succeed
        };

        // Act
        var result = await _skillService.UpdateSkillAsync(skill.Id, dto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSkillAsync_SetInactive_UpdatesStatus()
    {
        // Arrange
        var skill = await CreateTestSkillAsync("Active Skill", "Category");
        var dto = new UpdateSkillDto
        {
            IsActive = false
        };

        // Act
        var result = await _skillService.UpdateSkillAsync(skill.Id, dto);

        // Assert
        result.Success.Should().BeTrue();
        var updatedSkill = await _context.Skills.FindAsync(skill.Id);
        updatedSkill!.IsActive.Should().BeFalse();
    }

    #endregion

    #region DeleteSkillAsync Tests

    [Fact]
    public async Task DeleteSkillAsync_UnusedSkill_HardDeletes()
    {
        // Arrange
        var skill = await CreateTestSkillAsync("Unused Skill", "Category");

        // Act
        var result = await _skillService.DeleteSkillAsync(skill.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Skill deleted successfully");

        var deletedSkill = await _context.Skills.FindAsync(skill.Id);
        deletedSkill.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSkillAsync_UsedSkill_SoftDeletes()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Used Skill", "Category");
        await CreateUserSkillAsync(user.Id, skill.Id);

        // Act
        var result = await _skillService.DeleteSkillAsync(skill.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("deactivated");
        result.Message.Should().Contain("1 users");

        var deactivatedSkill = await _context.Skills.FindAsync(skill.Id);
        deactivatedSkill.Should().NotBeNull();
        deactivatedSkill!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSkillAsync_NonExistentSkill_ReturnsError()
    {
        // Act
        var result = await _skillService.DeleteSkillAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Skill not found");
    }

    [Fact]
    public async Task DeleteSkillAsync_SystemManagedSkill_ReturnsError()
    {
        // Arrange
        var skill = await CreateTestSkillAsync("System Skill", "Category", isSystemManaged: true);

        // Act
        var result = await _skillService.DeleteSkillAsync(skill.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("System-managed skills cannot be deleted");
    }

    #endregion

    #region GetSkillByIdAsync Tests

    [Fact]
    public async Task GetSkillByIdAsync_ExistingSkill_ReturnsSkill()
    {
        // Arrange
        var skill = await CreateTestSkillAsync("C#", "Programming");

        // Act
        var result = await _skillService.GetSkillByIdAsync(skill.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(skill.Id);
        result.Name.Should().Be("C#");
        result.Category.Should().Be("Programming");
    }

    [Fact]
    public async Task GetSkillByIdAsync_NonExistentSkill_ReturnsNull()
    {
        // Act
        var result = await _skillService.GetSkillByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSkillByIdAsync_SecondCall_UsesCacheWhenAvailable()
    {
        // Arrange
        var skill = await CreateTestSkillAsync("Cached Skill", "Category");

        // Act - First call (populates cache)
        var result1 = await _skillService.GetSkillByIdAsync(skill.Id);

        // Verify cache was populated
        var cacheKey = $"skill:{skill.Id}";
        var cachedValue = await _cacheService.GetAsync<SkillDto>(cacheKey);
        cachedValue.Should().NotBeNull();

        // Act - Second call (should use cache)
        var result2 = await _skillService.GetSkillByIdAsync(skill.Id);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.Id.Should().Be(result2!.Id);
    }

    #endregion

    #region GetSkillByNameAsync Tests

    [Fact]
    public async Task GetSkillByNameAsync_ExistingSkill_ReturnsSkill()
    {
        // Arrange
        await CreateTestSkillAsync("Docker", "DevOps");

        // Act
        var result = await _skillService.GetSkillByNameAsync("Docker");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Docker");
    }

    [Fact]
    public async Task GetSkillByNameAsync_CaseInsensitive_ReturnsSkill()
    {
        // Arrange
        await CreateTestSkillAsync("Kubernetes", "DevOps");

        // Act
        var result = await _skillService.GetSkillByNameAsync("KUBERNETES");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Kubernetes");
    }

    [Fact]
    public async Task GetSkillByNameAsync_NonExistentSkill_ReturnsNull()
    {
        // Act
        var result = await _skillService.GetSkillByNameAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SearchSkillsAsync Tests

    [Fact]
    public async Task SearchSkillsAsync_ByQuery_ReturnsMatchingSkills()
    {
        // Arrange - Use unique names to avoid collision with system skills
        await CreateTestSkillAsync("TestCoffeeScript", "Programming");
        await CreateTestSkillAsync("TestActionScript", "Programming");
        await CreateTestSkillAsync("TestPythonLang", "Programming");

        var searchDto = new SkillSearchDto
        {
            Query = "ActionScript",
            ActiveOnly = true
        };

        // Act
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        totalCount.Should().Be(1);
        skills.Should().HaveCount(1);
        skills.Should().Contain(s => s.Name == "TestActionScript");
    }

    [Fact]
    public async Task SearchSkillsAsync_ByCategory_ReturnsMatchingSkills()
    {
        // Arrange
        await CreateTestSkillAsync("React", "Web Development");
        await CreateTestSkillAsync("Angular", "Web Development");
        await CreateTestSkillAsync("Python", "Programming");

        var searchDto = new SkillSearchDto
        {
            Category = "Web Development",
            ActiveOnly = true
        };

        // Act
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        totalCount.Should().Be(2);
        skills.Should().HaveCount(2);
        skills.All(s => s.Category == "Web Development").Should().BeTrue();
    }

    [Fact]
    public async Task SearchSkillsAsync_SystemManagedOnly_ReturnsOnlySystemSkills()
    {
        // Arrange
        await CreateTestSkillAsync("System Skill", "Category", isSystemManaged: true);
        await CreateTestSkillAsync("User Skill", "Category", isSystemManaged: false);

        var searchDto = new SkillSearchDto
        {
            SystemManagedOnly = true,
            ActiveOnly = true
        };

        // Act
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        totalCount.Should().Be(1);
        skills.Should().HaveCount(1);
        skills.First().Name.Should().Be("System Skill");
        skills.First().IsSystemManaged.Should().BeTrue();
    }

    [Fact]
    public async Task SearchSkillsAsync_IncludesInactive_WhenActiveOnlyFalse()
    {
        // Arrange
        var activeSkill = await CreateTestSkillAsync("Active", "Category");
        var inactiveSkill = await CreateTestSkillAsync("Inactive", "Category");
        inactiveSkill.IsActive = false;
        await _context.SaveChangesAsync();

        var searchDto = new SkillSearchDto
        {
            ActiveOnly = false
        };

        // Act
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        totalCount.Should().Be(2);
        skills.Should().Contain(s => s.Name == "Active");
        skills.Should().Contain(s => s.Name == "Inactive");
    }

    [Fact]
    public async Task SearchSkillsAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await CreateTestSkillAsync($"Skill_{i:D2}", "Category");
        }

        var searchDto = new SkillSearchDto
        {
            Skip = 3,
            Take = 3,
            ActiveOnly = true
        };

        // Act
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        totalCount.Should().Be(10);
        skills.Should().HaveCount(3);
    }

    #endregion

    #region GetSkillCategoriesAsync Tests

    [Fact]
    public async Task GetSkillCategoriesAsync_ReturnsAllCategories()
    {
        // Arrange
        await CreateTestSkillAsync("JavaScript", "Programming");
        await CreateTestSkillAsync("TypeScript", "Programming");
        await CreateTestSkillAsync("React", "Web Development");
        await CreateTestSkillAsync("Figma", "Design");

        // Act
        var categories = await _skillService.GetSkillCategoriesAsync();

        // Assert
        categories.Should().HaveCount(3);
        categories.Should().Contain(c => c.Name == "Programming" && c.SkillCount == 2);
        categories.Should().Contain(c => c.Name == "Web Development" && c.SkillCount == 1);
        categories.Should().Contain(c => c.Name == "Design" && c.SkillCount == 1);
    }

    [Fact]
    public async Task GetSkillCategoriesAsync_CountsUsersCorrectly()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com");
        var user2 = await CreateTestUserAsync("user2@test.com");
        var jsSkill = await CreateTestSkillAsync("JavaScript", "Programming");
        var tsSkill = await CreateTestSkillAsync("TypeScript", "Programming");

        await CreateUserSkillAsync(user1.Id, jsSkill.Id);
        await CreateUserSkillAsync(user2.Id, jsSkill.Id);
        await CreateUserSkillAsync(user1.Id, tsSkill.Id);
        _context.ChangeTracker.Clear();

        // Act
        var categories = await _skillService.GetSkillCategoriesAsync();

        // Assert
        var programmingCategory = categories.FirstOrDefault(c => c.Name == "Programming");
        programmingCategory.Should().NotBeNull();
        programmingCategory!.SkillCount.Should().Be(2);
        programmingCategory.UserCount.Should().Be(2); // 2 distinct users
    }

    [Fact]
    public async Task GetSkillCategoriesAsync_ExcludesInactiveSkills()
    {
        // Arrange
        var activeSkill = await CreateTestSkillAsync("Active", "Category1");
        var inactiveSkill = await CreateTestSkillAsync("Inactive", "Category2");
        inactiveSkill.IsActive = false;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var categories = await _skillService.GetSkillCategoriesAsync();

        // Assert
        categories.Should().HaveCount(1);
        categories.First().Name.Should().Be("Category1");
    }

    #endregion

    #region AddUserSkillAsync Tests

    [Fact]
    public async Task AddUserSkillAsync_ValidSkill_AddsSuccessfully()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Python", "Programming");

        var dto = new AddUserSkillDto
        {
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Advanced,
            YearsOfExperience = 5,
            Notes = "Expert in Python",
            IsFeatured = true,
            IsVisible = true
        };

        // Act
        var result = await _skillService.AddUserSkillAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Skill added to profile successfully");

        var userSkillDto = result.Data as UserSkillDto;
        userSkillDto.Should().NotBeNull();
        userSkillDto!.UserId.Should().Be(user.Id);
        userSkillDto.Proficiency.Should().Be(SkillProficiency.Advanced);
        userSkillDto.YearsOfExperience.Should().Be(5);

        // Verify in database
        var savedUserSkill = await _context.UserSkills.FirstOrDefaultAsync(us => us.UserId == user.Id);
        savedUserSkill.Should().NotBeNull();
        savedUserSkill!.SkillId.Should().Be(skill.Id);
    }

    [Fact]
    public async Task AddUserSkillAsync_NonExistentUser_ReturnsError()
    {
        // Arrange
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var dto = new AddUserSkillDto { SkillId = skill.Id };

        // Act
        var result = await _skillService.AddUserSkillAsync(Guid.NewGuid(), dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User not found");
    }

    [Fact]
    public async Task AddUserSkillAsync_NonExistentSkill_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var dto = new AddUserSkillDto { SkillId = Guid.NewGuid() };

        // Act
        var result = await _skillService.AddUserSkillAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Skill not found or inactive");
    }

    [Fact]
    public async Task AddUserSkillAsync_InactiveSkill_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Inactive Skill", "Category");
        skill.IsActive = false;
        await _context.SaveChangesAsync();

        var dto = new AddUserSkillDto { SkillId = skill.Id };

        // Act
        var result = await _skillService.AddUserSkillAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Skill not found or inactive");
    }

    [Fact]
    public async Task AddUserSkillAsync_DuplicateSkill_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Existing Skill", "Category");
        await CreateUserSkillAsync(user.Id, skill.Id);

        var dto = new AddUserSkillDto { SkillId = skill.Id };

        // Act
        var result = await _skillService.AddUserSkillAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User already has this skill");
    }

    #endregion

    #region UpdateUserSkillAsync Tests

    [Fact]
    public async Task UpdateUserSkillAsync_ValidUpdate_UpdatesSuccessfully()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Python", "Programming");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id, SkillProficiency.Beginner);

        var dto = new UpdateUserSkillDto
        {
            Proficiency = SkillProficiency.Expert,
            YearsOfExperience = 10,
            Notes = "Updated notes",
            IsFeatured = true,
            IsVisible = false
        };

        // Act
        var result = await _skillService.UpdateUserSkillAsync(user.Id, userSkill.Id, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("User skill updated successfully");

        var updatedUserSkill = await _context.UserSkills.FindAsync(userSkill.Id);
        updatedUserSkill!.Proficiency.Should().Be(SkillProficiency.Expert);
        updatedUserSkill.YearsOfExperience.Should().Be(10);
        updatedUserSkill.Notes.Should().Be("Updated notes");
        updatedUserSkill.IsFeatured.Should().BeTrue();
        updatedUserSkill.IsVisible.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserSkillAsync_NonExistentUserSkill_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var dto = new UpdateUserSkillDto { Proficiency = SkillProficiency.Expert };

        // Act
        var result = await _skillService.UpdateUserSkillAsync(user.Id, Guid.NewGuid(), dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User skill not found");
    }

    [Fact]
    public async Task UpdateUserSkillAsync_WrongUser_ReturnsError()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com");
        var user2 = await CreateTestUserAsync("user2@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user1.Id, skill.Id);

        var dto = new UpdateUserSkillDto { Proficiency = SkillProficiency.Expert };

        // Act - User2 tries to update User1's skill
        var result = await _skillService.UpdateUserSkillAsync(user2.Id, userSkill.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User skill not found");
    }

    [Fact]
    public async Task UpdateUserSkillAsync_PartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Python", "Programming");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id, SkillProficiency.Intermediate);
        userSkill.Notes = "Original notes";
        userSkill.YearsOfExperience = 3;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var dto = new UpdateUserSkillDto
        {
            Proficiency = SkillProficiency.Expert
            // Not updating Notes or YearsOfExperience
        };

        // Act
        var result = await _skillService.UpdateUserSkillAsync(user.Id, userSkill.Id, dto);

        // Assert
        result.Success.Should().BeTrue();
        var updatedUserSkill = await _context.UserSkills.FindAsync(userSkill.Id);
        updatedUserSkill!.Proficiency.Should().Be(SkillProficiency.Expert);
        updatedUserSkill.Notes.Should().Be("Original notes"); // Unchanged
        updatedUserSkill.YearsOfExperience.Should().Be(3); // Unchanged
    }

    #endregion

    #region RemoveUserSkillAsync Tests

    [Fact]
    public async Task RemoveUserSkillAsync_ValidRemoval_RemovesSuccessfully()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Skill to Remove", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        // Act
        var result = await _skillService.RemoveUserSkillAsync(user.Id, userSkill.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Skill removed from profile successfully");

        var removedUserSkill = await _context.UserSkills.FindAsync(userSkill.Id);
        removedUserSkill.Should().BeNull();
    }

    [Fact]
    public async Task RemoveUserSkillAsync_NonExistentUserSkill_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");

        // Act
        var result = await _skillService.RemoveUserSkillAsync(user.Id, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User skill not found");
    }

    [Fact]
    public async Task RemoveUserSkillAsync_WrongUser_ReturnsError()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com");
        var user2 = await CreateTestUserAsync("user2@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user1.Id, skill.Id);

        // Act - User2 tries to remove User1's skill
        var result = await _skillService.RemoveUserSkillAsync(user2.Id, userSkill.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User skill not found");
    }

    #endregion

    #region GetUserSkillAsync Tests

    [Fact]
    public async Task GetUserSkillAsync_ExistingSkill_ReturnsUserSkill()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Python", "Programming");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id, SkillProficiency.Expert);

        // Act
        var result = await _skillService.GetUserSkillAsync(user.Id, userSkill.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userSkill.Id);
        result.UserId.Should().Be(user.Id);
        result.Proficiency.Should().Be(SkillProficiency.Expert);
        result.Skill.Name.Should().Be("Python");
    }

    [Fact]
    public async Task GetUserSkillAsync_NonExistentSkill_ReturnsNull()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");

        // Act
        var result = await _skillService.GetUserSkillAsync(user.Id, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSkillAsync_WrongUser_ReturnsNull()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com");
        var user2 = await CreateTestUserAsync("user2@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user1.Id, skill.Id);

        // Act
        var result = await _skillService.GetUserSkillAsync(user2.Id, userSkill.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetUserSkillsAsync Tests

    [Fact]
    public async Task GetUserSkillsAsync_ReturnsAllVisibleSkills()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill1 = await CreateTestSkillAsync("Skill1", "Category");
        var skill2 = await CreateTestSkillAsync("Skill2", "Category");
        var skill3 = await CreateTestSkillAsync("Skill3", "Category");

        await CreateUserSkillAsync(user.Id, skill1.Id);
        await CreateUserSkillAsync(user.Id, skill2.Id);
        var hiddenUserSkill = await CreateUserSkillAsync(user.Id, skill3.Id);
        hiddenUserSkill.IsVisible = false;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _skillService.GetUserSkillsAsync(user.Id, visibleOnly: true);

        // Assert
        result.Should().HaveCount(2);
        result.Should().NotContain(us => us.Skill.Name == "Skill3");
    }

    [Fact]
    public async Task GetUserSkillsAsync_IncludesHidden_WhenVisibleOnlyFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill1 = await CreateTestSkillAsync("Visible", "Category");
        var skill2 = await CreateTestSkillAsync("Hidden", "Category");

        await CreateUserSkillAsync(user.Id, skill1.Id);
        var hiddenUserSkill = await CreateUserSkillAsync(user.Id, skill2.Id);
        hiddenUserSkill.IsVisible = false;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _skillService.GetUserSkillsAsync(user.Id, visibleOnly: false);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserSkillsAsync_OrdersByCategory()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill1 = await CreateTestSkillAsync("Zebra Skill", "Z Category");
        var skill2 = await CreateTestSkillAsync("Alpha Skill", "A Category");

        await CreateUserSkillAsync(user.Id, skill1.Id);
        await CreateUserSkillAsync(user.Id, skill2.Id);
        _context.ChangeTracker.Clear();

        // Act
        var result = await _skillService.GetUserSkillsAsync(user.Id);

        // Assert
        result.Should().HaveCount(2);
        result.First().Skill.Category.Should().Be("A Category");
        result.Last().Skill.Category.Should().Be("Z Category");
    }

    #endregion

    #region SearchUserSkillsAsync Tests

    [Fact]
    public async Task SearchUserSkillsAsync_ByUserId_ReturnsUserSkills()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Python", "Programming");
        await CreateUserSkillAsync(user.Id, skill.Id);

        var searchDto = new UserSkillSearchDto
        {
            UserId = user.Id,
            VisibleOnly = true
        };

        // Act
        var (userSkills, totalCount) = await _skillService.SearchUserSkillsAsync(searchDto);

        // Assert
        totalCount.Should().Be(1);
        userSkills.Should().HaveCount(1);
        userSkills.First().Skill.Name.Should().Be("Python");
    }

    [Fact]
    public async Task SearchUserSkillsAsync_ByProficiency_FiltersCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill1 = await CreateTestSkillAsync("Expert Skill", "Category");
        var skill2 = await CreateTestSkillAsync("Beginner Skill", "Category");

        await CreateUserSkillAsync(user.Id, skill1.Id, SkillProficiency.Expert);
        await CreateUserSkillAsync(user.Id, skill2.Id, SkillProficiency.Beginner);
        _context.ChangeTracker.Clear();

        var searchDto = new UserSkillSearchDto
        {
            UserId = user.Id,
            Proficiency = SkillProficiency.Expert,
            VisibleOnly = true
        };

        // Act
        var (userSkills, totalCount) = await _skillService.SearchUserSkillsAsync(searchDto);

        // Assert
        totalCount.Should().Be(1);
        userSkills.First().Proficiency.Should().Be(SkillProficiency.Expert);
    }

    [Fact]
    public async Task SearchUserSkillsAsync_ByQuery_SearchesNameAndNotes()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Python", "Programming");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);
        userSkill.Notes = "Machine learning expertise";
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var searchDto = new UserSkillSearchDto
        {
            Query = "machine learning",
            VisibleOnly = true
        };

        // Act
        var (userSkills, totalCount) = await _skillService.SearchUserSkillsAsync(searchDto);

        // Assert
        totalCount.Should().Be(1);
        userSkills.First().Notes.Should().Contain("Machine learning");
    }

    [Fact]
    public async Task SearchUserSkillsAsync_FeaturedOnly_FiltersCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill1 = await CreateTestSkillAsync("Featured Skill", "Category");
        var skill2 = await CreateTestSkillAsync("Normal Skill", "Category");

        var featuredUserSkill = await CreateUserSkillAsync(user.Id, skill1.Id);
        featuredUserSkill.IsFeatured = true;
        await CreateUserSkillAsync(user.Id, skill2.Id);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var searchDto = new UserSkillSearchDto
        {
            FeaturedOnly = true,
            VisibleOnly = true
        };

        // Act
        var (userSkills, totalCount) = await _skillService.SearchUserSkillsAsync(searchDto);

        // Assert
        totalCount.Should().Be(1);
        userSkills.First().IsFeatured.Should().BeTrue();
    }

    #endregion

    #region CreateSkillEndorsementAsync Tests

    [Fact]
    public async Task CreateSkillEndorsementAsync_ValidEndorsement_CreatesSuccessfully()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var endorser = await CreateTestUserAsync("endorser@test.com");
        var skill = await CreateTestSkillAsync("Python", "Programming");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var dto = new CreateSkillEndorsementDto
        {
            UserSkillId = userSkill.Id,
            Comment = "Great Python developer!",
            IsVisible = true
        };

        // Act
        var result = await _skillService.CreateSkillEndorsementAsync(endorser.Id, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Skill endorsed successfully");

        var endorsementDto = result.Data as SkillEndorsementDto;
        endorsementDto.Should().NotBeNull();
        endorsementDto!.UserSkillId.Should().Be(userSkill.Id);
        endorsementDto.Comment.Should().Be("Great Python developer!");

        // Verify in database
        var savedEndorsement = await _context.SkillEndorsements
            .FirstOrDefaultAsync(se => se.UserSkillId == userSkill.Id);
        savedEndorsement.Should().NotBeNull();
        savedEndorsement!.EndorsedByUserId.Should().Be(endorser.Id);
    }

    [Fact]
    public async Task CreateSkillEndorsementAsync_NonExistentEndorser_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var dto = new CreateSkillEndorsementDto { UserSkillId = userSkill.Id };

        // Act
        var result = await _skillService.CreateSkillEndorsementAsync(Guid.NewGuid(), dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Endorser not found");
    }

    [Fact]
    public async Task CreateSkillEndorsementAsync_NonExistentUserSkill_ReturnsError()
    {
        // Arrange
        var endorser = await CreateTestUserAsync("endorser@test.com");
        var dto = new CreateSkillEndorsementDto { UserSkillId = Guid.NewGuid() };

        // Act
        var result = await _skillService.CreateSkillEndorsementAsync(endorser.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User skill not found");
    }

    [Fact]
    public async Task CreateSkillEndorsementAsync_SelfEndorsement_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var dto = new CreateSkillEndorsementDto { UserSkillId = userSkill.Id };

        // Act - User tries to endorse their own skill
        var result = await _skillService.CreateSkillEndorsementAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Cannot endorse your own skills");
    }

    [Fact]
    public async Task CreateSkillEndorsementAsync_DuplicateEndorsement_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var endorser = await CreateTestUserAsync("endorser@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var endorsement = new SkillEndorsement
        {
            Id = Guid.NewGuid(),
            UserSkillId = userSkill.Id,
            EndorsedByUserId = endorser.Id,
            Comment = "First endorsement"
        };
        _context.SkillEndorsements.Add(endorsement);
        await _context.SaveChangesAsync();

        var dto = new CreateSkillEndorsementDto { UserSkillId = userSkill.Id };

        // Act - Endorser tries to endorse again
        var result = await _skillService.CreateSkillEndorsementAsync(endorser.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("You have already endorsed this skill");
    }

    #endregion

    #region RemoveSkillEndorsementAsync Tests

    [Fact]
    public async Task RemoveSkillEndorsementAsync_ValidRemoval_RemovesSuccessfully()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var endorser = await CreateTestUserAsync("endorser@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var endorsement = new SkillEndorsement
        {
            Id = Guid.NewGuid(),
            UserSkillId = userSkill.Id,
            EndorsedByUserId = endorser.Id,
            Comment = "Endorsement to remove"
        };
        _context.SkillEndorsements.Add(endorsement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _skillService.RemoveSkillEndorsementAsync(endorser.Id, endorsement.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Endorsement removed successfully");

        var removedEndorsement = await _context.SkillEndorsements.FindAsync(endorsement.Id);
        removedEndorsement.Should().BeNull();
    }

    [Fact]
    public async Task RemoveSkillEndorsementAsync_NonExistentEndorsement_ReturnsError()
    {
        // Arrange
        var endorser = await CreateTestUserAsync("endorser@test.com");

        // Act
        var result = await _skillService.RemoveSkillEndorsementAsync(endorser.Id, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Endorsement not found");
    }

    [Fact]
    public async Task RemoveSkillEndorsementAsync_WrongEndorser_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var endorser1 = await CreateTestUserAsync("endorser1@test.com");
        var endorser2 = await CreateTestUserAsync("endorser2@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var endorsement = new SkillEndorsement
        {
            Id = Guid.NewGuid(),
            UserSkillId = userSkill.Id,
            EndorsedByUserId = endorser1.Id,
            Comment = "Endorsement"
        };
        _context.SkillEndorsements.Add(endorsement);
        await _context.SaveChangesAsync();

        // Act - Endorser2 tries to remove Endorser1's endorsement
        var result = await _skillService.RemoveSkillEndorsementAsync(endorser2.Id, endorsement.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Endorsement not found");
    }

    #endregion

    #region GetSkillEndorsementsAsync Tests

    [Fact]
    public async Task GetSkillEndorsementsAsync_ReturnsVisibleEndorsements()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var endorser1 = await CreateTestUserAsync("endorser1@test.com");
        var endorser2 = await CreateTestUserAsync("endorser2@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var visibleEndorsement = new SkillEndorsement
        {
            Id = Guid.NewGuid(),
            UserSkillId = userSkill.Id,
            EndorsedByUserId = endorser1.Id,
            Comment = "Visible endorsement",
            IsVisible = true
        };
        var hiddenEndorsement = new SkillEndorsement
        {
            Id = Guid.NewGuid(),
            UserSkillId = userSkill.Id,
            EndorsedByUserId = endorser2.Id,
            Comment = "Hidden endorsement",
            IsVisible = false
        };

        _context.SkillEndorsements.Add(visibleEndorsement);
        _context.SkillEndorsements.Add(hiddenEndorsement);
        await _context.SaveChangesAsync();

        // Act
        var endorsements = await _skillService.GetSkillEndorsementsAsync(userSkill.Id);

        // Assert
        endorsements.Should().HaveCount(1);
        endorsements.First().Comment.Should().Be("Visible endorsement");
    }

    [Fact]
    public async Task GetSkillEndorsementsAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var endorser1 = await CreateTestUserAsync("endorser1@test.com");
        var endorser2 = await CreateTestUserAsync("endorser2@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var olderEndorsement = new SkillEndorsement
        {
            Id = Guid.NewGuid(),
            UserSkillId = userSkill.Id,
            EndorsedByUserId = endorser1.Id,
            Comment = "Older",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            IsVisible = true
        };
        var newerEndorsement = new SkillEndorsement
        {
            Id = Guid.NewGuid(),
            UserSkillId = userSkill.Id,
            EndorsedByUserId = endorser2.Id,
            Comment = "Newer",
            CreatedAt = DateTime.UtcNow,
            IsVisible = true
        };

        _context.SkillEndorsements.Add(olderEndorsement);
        _context.SkillEndorsements.Add(newerEndorsement);
        await _context.SaveChangesAsync();

        // Act
        var endorsements = await _skillService.GetSkillEndorsementsAsync(userSkill.Id);

        // Assert
        endorsements.Should().HaveCount(2);
        endorsements.First().Comment.Should().Be("Newer");
        endorsements.Last().Comment.Should().Be("Older");
    }

    #endregion

    #region CanEndorseSkillAsync Tests

    [Fact]
    public async Task CanEndorseSkillAsync_CanEndorse_ReturnsTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var endorser = await CreateTestUserAsync("endorser@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        // Act
        var canEndorse = await _skillService.CanEndorseSkillAsync(endorser.Id, userSkill.Id);

        // Assert
        canEndorse.Should().BeTrue();
    }

    [Fact]
    public async Task CanEndorseSkillAsync_OwnSkill_ReturnsFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        // Act
        var canEndorse = await _skillService.CanEndorseSkillAsync(user.Id, userSkill.Id);

        // Assert
        canEndorse.Should().BeFalse();
    }

    [Fact]
    public async Task CanEndorseSkillAsync_AlreadyEndorsed_ReturnsFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var endorser = await CreateTestUserAsync("endorser@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var existingEndorsement = new SkillEndorsement
        {
            Id = Guid.NewGuid(),
            UserSkillId = userSkill.Id,
            EndorsedByUserId = endorser.Id
        };
        _context.SkillEndorsements.Add(existingEndorsement);
        await _context.SaveChangesAsync();

        // Act
        var canEndorse = await _skillService.CanEndorseSkillAsync(endorser.Id, userSkill.Id);

        // Assert
        canEndorse.Should().BeFalse();
    }

    [Fact]
    public async Task CanEndorseSkillAsync_NonExistentUserSkill_ReturnsFalse()
    {
        // Arrange
        var endorser = await CreateTestUserAsync("endorser@test.com");

        // Act
        var canEndorse = await _skillService.CanEndorseSkillAsync(endorser.Id, Guid.NewGuid());

        // Assert
        canEndorse.Should().BeFalse();
    }

    #endregion

    #region InitializeSystemSkillsAsync Tests

    [Fact]
    public async Task InitializeSystemSkillsAsync_CreatesSystemSkills()
    {
        // Act
        await _skillService.InitializeSystemSkillsAsync();

        // Assert - Verify some key system skills were created
        var csharpSkill = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "C#");
        csharpSkill.Should().NotBeNull();
        csharpSkill!.IsSystemManaged.Should().BeTrue();
        csharpSkill.Category.Should().Be("Programming");

        var reactSkill = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "React");
        reactSkill.Should().NotBeNull();
        reactSkill!.IsSystemManaged.Should().BeTrue();
        reactSkill.Category.Should().Be("Web Development");

        var azureSkill = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "Microsoft Azure");
        azureSkill.Should().NotBeNull();
        azureSkill!.IsSystemManaged.Should().BeTrue();
        azureSkill.Category.Should().Be("Cloud");
    }

    [Fact]
    public async Task InitializeSystemSkillsAsync_Idempotent_NoDoubleCreation()
    {
        // Act
        await _skillService.InitializeSystemSkillsAsync();
        var initialCount = await _context.Skills.CountAsync();

        await _skillService.InitializeSystemSkillsAsync();
        var finalCount = await _context.Skills.CountAsync();

        // Assert - Count should remain the same
        finalCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task InitializeSystemSkillsAsync_MarksExistingSkillsAsSystemManaged()
    {
        // Arrange - Create a skill with the same name as a system skill
        var existingSkill = await CreateTestSkillAsync("C#", "Programming", isSystemManaged: false);

        // Act
        await _skillService.InitializeSystemSkillsAsync();

        // Assert - Should now be system managed
        _context.ChangeTracker.Clear();
        var updatedSkill = await _context.Skills.FindAsync(existingSkill.Id);
        updatedSkill!.IsSystemManaged.Should().BeTrue();
    }

    #endregion

    #region Audit Logging Tests

    [Fact]
    public async Task CreateSkillAsync_CreatesAuditLog()
    {
        // Arrange
        var dto = new CreateSkillDto
        {
            Name = "Audited Skill",
            Description = "Test",
            Category = "Test"
        };

        // Act
        await _skillService.CreateSkillAsync(dto);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "SKILL_CREATED");
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
        auditLog.Details.Should().Contain("Audited Skill");
    }

    [Fact]
    public async Task AddUserSkillAsync_CreatesAuditLog()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var skill = await CreateTestSkillAsync("Audited Skill", "Category");

        var dto = new AddUserSkillDto
        {
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Expert
        };

        // Act
        await _skillService.AddUserSkillAsync(user.Id, dto);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "USER_SKILL_ADDED");
        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(user.Id);
        auditLog.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSkillEndorsementAsync_CreatesAuditLog()
    {
        // Arrange
        var user = await CreateTestUserAsync("user@test.com");
        var endorser = await CreateTestUserAsync("endorser@test.com");
        var skill = await CreateTestSkillAsync("Skill", "Category");
        var userSkill = await CreateUserSkillAsync(user.Id, skill.Id);

        var dto = new CreateSkillEndorsementDto { UserSkillId = userSkill.Id };

        // Act
        await _skillService.CreateSkillEndorsementAsync(endorser.Id, dto);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "SKILL_ENDORSED");
        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(endorser.Id);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
