using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
/// Integration tests for ExperienceService - PROFESSIONAL EXPERIENCE MANAGEMENT.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 0
/// </summary>
[IntegrationTest]
public class ExperienceServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;
    private readonly ExperienceService _service;

    public ExperienceServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ExperienceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        _auditLogService = new MockAuditLogService(_context);

        _service = new ExperienceService(
            _context,
            _auditLogService
        );
    }

    private async Task<User> CreateTestUserAsync(string email = "test@skillledger.app")
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
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Skill> CreateTestSkillAsync(string name)
    {
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = "Testing",
            IsActive = true,
            IsSystemManaged = false
        };
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        return skill;
    }

    #region CreateExperienceAsync Tests

    [Fact]
    public async Task CreateExperienceAsync_ValidData_CreatesExperience()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var dto = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "Software Engineer",
            Organization = "Tech Corp",
            Location = "San Francisco, CA",
            Description = "Developing software solutions",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2023, 1, 1),
            IsCurrent = false,
            IsVisible = true,
            IsFeatured = false
        };

        // Act
        var result = await _service.CreateExperienceAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");

        var experience = await _context.Experiences.FirstOrDefaultAsync(e => e.UserId == user.Id);
        experience.Should().NotBeNull();
        experience!.Title.Should().Be("Software Engineer");
        experience.Organization.Should().Be("Tech Corp");
        experience.Location.Should().Be("San Francisco, CA");
        experience.Type.Should().Be(ExperienceType.Work);
    }

    [Fact]
    public async Task CreateExperienceAsync_CurrentExperience_SetsEndDateToNull()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var dto = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "Current Position",
            Organization = "Current Company",
            StartDate = new DateTime(2023, 1, 1),
            EndDate = new DateTime(2025, 1, 1),  // Should be nullified
            IsCurrent = true,
            IsVisible = true
        };

        // Act
        var result = await _service.CreateExperienceAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeTrue();

        var experience = await _context.Experiences.FirstOrDefaultAsync(e => e.UserId == user.Id);
        experience.Should().NotBeNull();
        experience!.IsCurrent.Should().BeTrue();
        experience.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateExperienceAsync_NonExistentUser_ReturnsError()
    {
        // Arrange
        var dto = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "Software Engineer",
            Organization = "Tech Corp",
            StartDate = DateTime.UtcNow.AddYears(-2)
        };

        // Act
        var result = await _service.CreateExperienceAsync(Guid.NewGuid(), dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateExperienceAsync_EndDateBeforeStartDate_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var dto = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "Invalid Dates",
            Organization = "Some Company",
            StartDate = new DateTime(2023, 1, 1),
            EndDate = new DateTime(2022, 1, 1)  // Before start
        };

        // Act
        var result = await _service.CreateExperienceAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("End date cannot be before start date");
    }

    [Fact]
    public async Task CreateExperienceAsync_WithSkills_AddsSkillsToExperience()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var skill1 = await CreateTestSkillAsync("TestCSharp");
        var skill2 = await CreateTestSkillAsync("TestAzure");

        var dto = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "Developer",
            Organization = "Tech Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            SkillIds = new List<Guid> { skill1.Id, skill2.Id }
        };

        // Act
        var result = await _service.CreateExperienceAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var experience = await _context.Experiences
            .Include(e => e.ExperienceSkills)
            .FirstOrDefaultAsync(e => e.UserId == user.Id);

        experience.Should().NotBeNull();
        experience!.ExperienceSkills.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateExperienceAsync_MultipleExperiences_IncrementsDisplayOrder()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var dto1 = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "First Job",
            Organization = "Company A",
            StartDate = DateTime.UtcNow.AddYears(-3)
        };

        var dto2 = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "Second Job",
            Organization = "Company B",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };

        // Act
        await _service.CreateExperienceAsync(user.Id, dto1);
        await _service.CreateExperienceAsync(user.Id, dto2);

        // Assert
        var experiences = await _context.Experiences
            .Where(e => e.UserId == user.Id)
            .OrderBy(e => e.DisplayOrder)
            .ToListAsync();

        experiences.Should().HaveCount(2);
        experiences[0].DisplayOrder.Should().Be(1);
        experiences[1].DisplayOrder.Should().Be(2);
    }

    [Fact]
    public async Task CreateExperienceAsync_LogsAuditEvent()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var dto = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "Test Position",
            Organization = "Test Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };

        // Act
        await _service.CreateExperienceAsync(user.Id, dto);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "EXPERIENCE_CREATED");
        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(user.Id);
    }

    #endregion

    #region UpdateExperienceAsync Tests

    [Fact]
    public async Task UpdateExperienceAsync_ValidUpdate_UpdatesExperience()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = ExperienceType.Work,
            Title = "Original Title",
            Organization = "Original Company",
            StartDate = DateTime.UtcNow.AddYears(-2)
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        var dto = new UpdateExperienceDto
        {
            Title = "Updated Title",
            Organization = "Updated Company"
        };

        // Act
        var result = await _service.UpdateExperienceAsync(user.Id, experience.Id, dto);

        // Assert
        result.Success.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var updated = await _context.Experiences.FindAsync(experience.Id);
        updated!.Title.Should().Be("Updated Title");
        updated.Organization.Should().Be("Updated Company");
    }

    [Fact]
    public async Task UpdateExperienceAsync_NonExistentExperience_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var dto = new UpdateExperienceDto { Title = "Updated" };

        // Act
        var result = await _service.UpdateExperienceAsync(user.Id, Guid.NewGuid(), dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateExperienceAsync_WrongUser_ReturnsError()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com");
        var user2 = await CreateTestUserAsync("user2@test.com");

        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            Title = "User1 Experience",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        var dto = new UpdateExperienceDto { Title = "Hijacked!" };

        // Act - User2 trying to update User1's experience
        var result = await _service.UpdateExperienceAsync(user2.Id, experience.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateExperienceAsync_SetIsCurrent_NullifiesEndDate()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Past Position",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-2),
            EndDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = false
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        var dto = new UpdateExperienceDto { IsCurrent = true };

        // Act
        var result = await _service.UpdateExperienceAsync(user.Id, experience.Id, dto);

        // Assert
        result.Success.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var updated = await _context.Experiences.FindAsync(experience.Id);
        updated!.IsCurrent.Should().BeTrue();
        updated.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateExperienceAsync_InvalidDateRange_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Experience",
            Organization = "Company",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2023, 1, 1)
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        var dto = new UpdateExperienceDto
        {
            StartDate = new DateTime(2024, 1, 1)  // After existing end date
        };

        // Act
        var result = await _service.UpdateExperienceAsync(user.Id, experience.Id, dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("End date cannot be before start date");
    }

    [Fact]
    public async Task UpdateExperienceAsync_WithSkills_ReplacesExistingSkills()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var skill1 = await CreateTestSkillAsync("OldSkill");
        var skill2 = await CreateTestSkillAsync("NewSkill1");
        var skill3 = await CreateTestSkillAsync("NewSkill2");

        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Experience",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        // Add old skill
        _context.ExperienceSkills.Add(new ExperienceSkill
        {
            ExperienceId = experience.Id,
            SkillId = skill1.Id
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateExperienceDto
        {
            SkillIds = new List<Guid> { skill2.Id, skill3.Id }
        };

        // Act
        var result = await _service.UpdateExperienceAsync(user.Id, experience.Id, dto);

        // Assert
        result.Success.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var experienceSkills = await _context.ExperienceSkills
            .Where(es => es.ExperienceId == experience.Id)
            .ToListAsync();

        experienceSkills.Should().HaveCount(2);
        experienceSkills.Select(es => es.SkillId).Should().BeEquivalentTo(new[] { skill2.Id, skill3.Id });
    }

    #endregion

    #region DeleteExperienceAsync Tests

    [Fact]
    public async Task DeleteExperienceAsync_ValidExperience_DeletesFromDatabase()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "To Be Deleted",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteExperienceAsync(user.Id, experience.Id);

        // Assert
        result.Success.Should().BeTrue();

        var deleted = await _context.Experiences.FindAsync(experience.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteExperienceAsync_NonExistentExperience_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var result = await _service.DeleteExperienceAsync(user.Id, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteExperienceAsync_WrongUser_ReturnsError()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com");
        var user2 = await CreateTestUserAsync("user2@test.com");

        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            Title = "User1 Experience",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        // Act - User2 trying to delete User1's experience
        var result = await _service.DeleteExperienceAsync(user2.Id, experience.Id);

        // Assert
        result.Success.Should().BeFalse();

        var stillExists = await _context.Experiences.FindAsync(experience.Id);
        stillExists.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteExperienceAsync_LogsAuditEvent()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Experience to Delete",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteExperienceAsync(user.Id, experience.Id);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "EXPERIENCE_DELETED");
        auditLog.Should().NotBeNull();
    }

    #endregion

    #region GetExperienceByIdAsync Tests

    [Fact]
    public async Task GetExperienceByIdAsync_ExistingExperience_ReturnsExperience()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = ExperienceType.Education,
            Title = "Computer Science",
            Organization = "University of Test",
            StartDate = new DateTime(2016, 9, 1),
            EndDate = new DateTime(2020, 5, 15)
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetExperienceByIdAsync(user.Id, experience.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Computer Science");
        result.Organization.Should().Be("University of Test");
        result.Type.Should().Be(ExperienceType.Education);
    }

    [Fact]
    public async Task GetExperienceByIdAsync_NonExistentExperience_ReturnsNull()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var result = await _service.GetExperienceByIdAsync(user.Id, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExperienceByIdAsync_WithSkills_IncludesSkills()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var skill = await CreateTestSkillAsync("TestSkill");

        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Developer",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Experiences.Add(experience);

        _context.ExperienceSkills.Add(new ExperienceSkill
        {
            ExperienceId = experience.Id,
            SkillId = skill.Id
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetExperienceByIdAsync(user.Id, experience.Id, includeSkills: true);

        // Assert
        result.Should().NotBeNull();
        result!.Skills.Should().HaveCount(1);
        result.Skills.First().Name.Should().Be("TestSkill");
    }

    #endregion

    #region GetUserExperiencesAsync Tests

    [Fact]
    public async Task GetUserExperiencesAsync_MultipleExperiences_ReturnsAllInOrder()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var exp1 = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "First Job",
            Organization = "Company A",
            StartDate = new DateTime(2018, 1, 1),
            IsVisible = true
        };
        var exp2 = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Second Job",
            Organization = "Company B",
            StartDate = new DateTime(2020, 1, 1),
            IsVisible = true
        };
        var exp3 = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Current Job",
            Organization = "Company C",
            StartDate = new DateTime(2023, 1, 1),
            IsVisible = true,
            IsCurrent = true
        };

        _context.Experiences.AddRange(exp1, exp2, exp3);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetUserExperiencesAsync(user.Id);

        // Assert
        results.Should().HaveCount(3);
        results[0].Title.Should().Be("Current Job");  // Most recent first
        results[1].Title.Should().Be("Second Job");
        results[2].Title.Should().Be("First Job");
    }

    [Fact]
    public async Task GetUserExperiencesAsync_VisibleOnlyTrue_ExcludesHidden()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var visible = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Visible Experience",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsVisible = true
        };
        var hidden = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Hidden Experience",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-2),
            IsVisible = false
        };

        _context.Experiences.AddRange(visible, hidden);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetUserExperiencesAsync(user.Id, visibleOnly: true);

        // Assert
        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Visible Experience");
    }

    [Fact]
    public async Task GetUserExperiencesAsync_VisibleOnlyFalse_IncludesAll()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var visible = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Visible Experience",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsVisible = true
        };
        var hidden = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Hidden Experience",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-2),
            IsVisible = false
        };

        _context.Experiences.AddRange(visible, hidden);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetUserExperiencesAsync(user.Id, visibleOnly: false);

        // Assert
        results.Should().HaveCount(2);
    }

    #endregion

    #region SearchExperiencesAsync Tests

    [Fact]
    public async Task SearchExperiencesAsync_ByQuery_FindsMatchingExperiences()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var exp1 = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Software Engineer",
            Organization = "Tech Corp",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        var exp2 = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Marketing Manager",
            Organization = "Marketing Inc",
            StartDate = DateTime.UtcNow.AddYears(-2)
        };

        _context.Experiences.AddRange(exp1, exp2);
        await _context.SaveChangesAsync();

        var searchDto = new ExperienceSearchDto
        {
            UserId = user.Id,
            Query = "Software"
        };

        // Act
        var (results, totalCount) = await _service.SearchExperiencesAsync(searchDto);

        // Assert
        results.Should().HaveCount(1);
        totalCount.Should().Be(1);
        results[0].Title.Should().Be("Software Engineer");
    }

    [Fact]
    public async Task SearchExperiencesAsync_ByType_FiltersCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var work = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = ExperienceType.Work,
            Title = "Job",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        var education = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = ExperienceType.Education,
            Title = "Degree",
            Organization = "University",
            StartDate = DateTime.UtcNow.AddYears(-4)
        };

        _context.Experiences.AddRange(work, education);
        await _context.SaveChangesAsync();

        var searchDto = new ExperienceSearchDto
        {
            UserId = user.Id,
            Type = ExperienceType.Education
        };

        // Act
        var (results, _) = await _service.SearchExperiencesAsync(searchDto);

        // Assert
        results.Should().HaveCount(1);
        results[0].Type.Should().Be(ExperienceType.Education);
    }

    [Fact]
    public async Task SearchExperiencesAsync_CurrentOnly_FiltersCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var current = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Current Position",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = true
        };
        var past = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Past Position",
            Organization = "Old Company",
            StartDate = DateTime.UtcNow.AddYears(-3),
            EndDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = false
        };

        _context.Experiences.AddRange(current, past);
        await _context.SaveChangesAsync();

        var searchDto = new ExperienceSearchDto
        {
            UserId = user.Id,
            CurrentOnly = true
        };

        // Act
        var (results, _) = await _service.SearchExperiencesAsync(searchDto);

        // Assert
        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Current Position");
    }

    [Fact]
    public async Task SearchExperiencesAsync_FeaturedOnly_FiltersCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var featured = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Featured Experience",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsFeatured = true
        };
        var notFeatured = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Regular Experience",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-2),
            IsFeatured = false
        };

        _context.Experiences.AddRange(featured, notFeatured);
        await _context.SaveChangesAsync();

        var searchDto = new ExperienceSearchDto
        {
            UserId = user.Id,
            FeaturedOnly = true
        };

        // Act
        var (results, _) = await _service.SearchExperiencesAsync(searchDto);

        // Assert
        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Featured Experience");
    }

    [Fact]
    public async Task SearchExperiencesAsync_Pagination_WorksCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        for (int i = 0; i < 10; i++)
        {
            _context.Experiences.Add(new Experience
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Title = $"Experience {i}",
                Organization = "Company",
                StartDate = DateTime.UtcNow.AddYears(-i),
                DisplayOrder = i
            });
        }
        await _context.SaveChangesAsync();

        var searchDto = new ExperienceSearchDto
        {
            UserId = user.Id,
            Skip = 3,
            Take = 5
        };

        // Act
        var (results, totalCount) = await _service.SearchExperiencesAsync(searchDto);

        // Assert
        results.Should().HaveCount(5);
        totalCount.Should().Be(10);
    }

    #endregion

    #region UpdateExperienceOrderAsync Tests

    [Fact]
    public async Task UpdateExperienceOrderAsync_ValidOrder_UpdatesDisplayOrder()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var exp1 = new Experience { Id = Guid.NewGuid(), UserId = user.Id, Title = "Exp 1", Organization = "Co", StartDate = DateTime.UtcNow, DisplayOrder = 1 };
        var exp2 = new Experience { Id = Guid.NewGuid(), UserId = user.Id, Title = "Exp 2", Organization = "Co", StartDate = DateTime.UtcNow, DisplayOrder = 2 };
        var exp3 = new Experience { Id = Guid.NewGuid(), UserId = user.Id, Title = "Exp 3", Organization = "Co", StartDate = DateTime.UtcNow, DisplayOrder = 3 };

        _context.Experiences.AddRange(exp1, exp2, exp3);
        await _context.SaveChangesAsync();

        // New order: exp3, exp1, exp2
        var newOrder = new List<Guid> { exp3.Id, exp1.Id, exp2.Id };

        // Act
        var result = await _service.UpdateExperienceOrderAsync(user.Id, newOrder);

        // Assert
        result.Success.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var updated = await _context.Experiences
            .Where(e => e.UserId == user.Id)
            .OrderBy(e => e.DisplayOrder)
            .ToListAsync();

        updated[0].Id.Should().Be(exp3.Id);
        updated[1].Id.Should().Be(exp1.Id);
        updated[2].Id.Should().Be(exp2.Id);
    }

    [Fact]
    public async Task UpdateExperienceOrderAsync_MissingExperience_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var exp = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Exp",
            Organization = "Co",
            StartDate = DateTime.UtcNow
        };
        _context.Experiences.Add(exp);
        await _context.SaveChangesAsync();

        var orderWithNonexistent = new List<Guid> { exp.Id, Guid.NewGuid() };

        // Act
        var result = await _service.UpdateExperienceOrderAsync(user.Id, orderWithNonexistent);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region AddSkillsToExperienceAsync Tests

    [Fact]
    public async Task AddSkillsToExperienceAsync_ValidSkills_AddsToExperience()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var skill1 = await CreateTestSkillAsync("Skill1");
        var skill2 = await CreateTestSkillAsync("Skill2");

        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Developer",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AddSkillsToExperienceAsync(
            user.Id,
            experience.Id,
            new List<Guid> { skill1.Id, skill2.Id }
        );

        // Assert
        result.Success.Should().BeTrue();

        var skills = await _context.ExperienceSkills
            .Where(es => es.ExperienceId == experience.Id)
            .ToListAsync();
        skills.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddSkillsToExperienceAsync_DuplicateSkill_DoesNotDuplicate()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var skill = await CreateTestSkillAsync("ExistingSkill");

        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Developer",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Experiences.Add(experience);

        // Add skill already
        _context.ExperienceSkills.Add(new ExperienceSkill
        {
            ExperienceId = experience.Id,
            SkillId = skill.Id
        });
        await _context.SaveChangesAsync();

        // Act - Try to add same skill again
        var result = await _service.AddSkillsToExperienceAsync(
            user.Id,
            experience.Id,
            new List<Guid> { skill.Id }
        );

        // Assert
        result.Success.Should().BeTrue();

        var skills = await _context.ExperienceSkills
            .Where(es => es.ExperienceId == experience.Id)
            .ToListAsync();
        skills.Should().HaveCount(1);  // Still only 1
    }

    [Fact]
    public async Task AddSkillsToExperienceAsync_NonExistentExperience_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var skill = await CreateTestSkillAsync("Skill");

        // Act
        var result = await _service.AddSkillsToExperienceAsync(
            user.Id,
            Guid.NewGuid(),
            new List<Guid> { skill.Id }
        );

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region RemoveSkillsFromExperienceAsync Tests

    [Fact]
    public async Task RemoveSkillsFromExperienceAsync_ValidSkills_RemovesFromExperience()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var skill1 = await CreateTestSkillAsync("Skill1");
        var skill2 = await CreateTestSkillAsync("Skill2");

        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Developer",
            Organization = "Company",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };
        _context.Experiences.Add(experience);

        _context.ExperienceSkills.Add(new ExperienceSkill { ExperienceId = experience.Id, SkillId = skill1.Id });
        _context.ExperienceSkills.Add(new ExperienceSkill { ExperienceId = experience.Id, SkillId = skill2.Id });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RemoveSkillsFromExperienceAsync(
            user.Id,
            experience.Id,
            new List<Guid> { skill1.Id }
        );

        // Assert
        result.Success.Should().BeTrue();

        var remaining = await _context.ExperienceSkills
            .Where(es => es.ExperienceId == experience.Id)
            .ToListAsync();
        remaining.Should().HaveCount(1);
        remaining[0].SkillId.Should().Be(skill2.Id);
    }

    [Fact]
    public async Task RemoveSkillsFromExperienceAsync_NonExistentExperience_ReturnsError()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var skill = await CreateTestSkillAsync("Skill");

        // Act
        var result = await _service.RemoveSkillsFromExperienceAsync(
            user.Id,
            Guid.NewGuid(),
            new List<Guid> { skill.Id }
        );

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region GetExperienceTimelineAsync Tests

    [Fact]
    public async Task GetExperienceTimelineAsync_ReturnsChronologicalOrder()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var oldest = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Oldest",
            Organization = "Co",
            StartDate = new DateTime(2015, 1, 1),
            EndDate = new DateTime(2018, 1, 1)
        };
        var middle = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Middle",
            Organization = "Co",
            StartDate = new DateTime(2018, 1, 1),
            EndDate = new DateTime(2021, 1, 1)
        };
        var newest = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Newest",
            Organization = "Co",
            StartDate = new DateTime(2021, 1, 1),
            IsCurrent = true
        };

        _context.Experiences.AddRange(oldest, middle, newest);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetExperienceTimelineAsync(user.Id);

        // Assert
        results.Should().HaveCount(3);
        results[0].Title.Should().Be("Newest");   // Most recent first
        results[1].Title.Should().Be("Middle");
        results[2].Title.Should().Be("Oldest");
    }

    #endregion

    #region GetFeaturedExperiencesAsync Tests

    [Fact]
    public async Task GetFeaturedExperiencesAsync_ReturnsFeaturedOnly()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var featured1 = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Featured 1",
            Organization = "Co",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsFeatured = true
        };
        var featured2 = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Featured 2",
            Organization = "Co",
            StartDate = DateTime.UtcNow.AddYears(-2),
            IsFeatured = true
        };
        var notFeatured = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Not Featured",
            Organization = "Co",
            StartDate = DateTime.UtcNow.AddYears(-3),
            IsFeatured = false
        };

        _context.Experiences.AddRange(featured1, featured2, notFeatured);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetFeaturedExperiencesAsync(user.Id);

        // Assert
        results.Should().HaveCount(2);
        results.All(e => e.IsFeatured).Should().BeTrue();
    }

    #endregion

    #region GetCurrentExperiencesAsync Tests

    [Fact]
    public async Task GetCurrentExperiencesAsync_ReturnsCurrentOnly()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        var current1 = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Current Job 1",
            Organization = "Company A",
            StartDate = DateTime.UtcNow.AddYears(-2),
            IsCurrent = true
        };
        var current2 = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Current Study",
            Organization = "University",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = true
        };
        var past = new Experience
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Past Job",
            Organization = "Old Company",
            StartDate = DateTime.UtcNow.AddYears(-5),
            EndDate = DateTime.UtcNow.AddYears(-2),
            IsCurrent = false
        };

        _context.Experiences.AddRange(current1, current2, past);
        await _context.SaveChangesAsync();

        // Act
        var results = await _service.GetCurrentExperiencesAsync(user.Id);

        // Assert
        results.Should().HaveCount(2);
        results.All(e => e.IsCurrent).Should().BeTrue();
    }

    #endregion

    #region Edge Cases and Security Tests

    [Theory]
    [InlineData(ExperienceType.Work)]
    [InlineData(ExperienceType.Education)]
    [InlineData(ExperienceType.Project)]
    [InlineData(ExperienceType.Volunteer)]
    [InlineData(ExperienceType.Certification)]
    [InlineData(ExperienceType.Training)]
    [InlineData(ExperienceType.Award)]
    public async Task CreateExperienceAsync_AllExperienceTypes_CreatesSuccessfully(ExperienceType type)
    {
        // Arrange
        var user = await CreateTestUserAsync($"{type.ToString().ToLower()}@test.com");
        var dto = new CreateExperienceDto
        {
            Type = type,
            Title = $"{type} Title",
            Organization = "Test Organization",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };

        // Act
        var result = await _service.CreateExperienceAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeTrue();

        var experience = await _context.Experiences.FirstOrDefaultAsync(e => e.UserId == user.Id);
        experience!.Type.Should().Be(type);
    }

    [Fact]
    public async Task CreateExperienceAsync_TrimsWhitespace()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var dto = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "  Software Engineer  ",
            Organization = "  Tech Corp  ",
            Location = "  San Francisco  ",
            Description = "  Some description  ",
            StartDate = DateTime.UtcNow.AddYears(-1)
        };

        // Act
        var result = await _service.CreateExperienceAsync(user.Id, dto);

        // Assert
        result.Success.Should().BeTrue();

        var experience = await _context.Experiences.FirstOrDefaultAsync(e => e.UserId == user.Id);
        experience!.Title.Should().Be("Software Engineer");
        experience.Organization.Should().Be("Tech Corp");
        experience.Location.Should().Be("San Francisco");
        experience.Description.Should().Be("Some description");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
