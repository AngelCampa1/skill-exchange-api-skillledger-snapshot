using SkillLedger.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Core.Services;

[UnitTest]
[CoreTest]
public class ExperienceServiceTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly IExperienceService _experienceService;
    private readonly IAuditLogService _auditLogService;

    public ExperienceServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<SkillLedgerDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Add logging services
        services.AddLogging();

        // Add memory cache
        services.AddMemoryCache();

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IExperienceService, ExperienceService>();

        var serviceProvider = services.BuildServiceProvider();
        _context = serviceProvider.GetRequiredService<SkillLedgerDbContext>();
        _auditLogService = serviceProvider.GetRequiredService<IAuditLogService>();
        _experienceService = serviceProvider.GetRequiredService<IExperienceService>();
    }

    [Fact]
    public async Task CreateExperienceAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var createDto = new CreateExperienceDto
        {
            Type = ExperienceType.Work,
            Title = "Software Engineer",
            Organization = "Tech Company",
            Location = "San Francisco, CA",
            Description = "Developed web applications",
            StartDate = DateTime.UtcNow.AddYears(-2),
            EndDate = DateTime.UtcNow.AddMonths(-6),
            IsCurrent = false,
            IsVisible = true
        };

        // Act
        var result = await _experienceService.CreateExperienceAsync(user.Id, createDto);

        // Assert
        Assert.True(result.Success);

        var experienceDto = result.Data as ExperienceDto;
        Assert.NotNull(experienceDto);
        Assert.Equal("Software Engineer", experienceDto.Title);
        Assert.Equal("Tech Company", experienceDto.Organization);
        Assert.Equal(ExperienceType.Work, experienceDto.Type);
    }

    [Fact]
    public async Task CreateExperienceAsync_WithEndDateBeforeStartDate_ReturnsFailure()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var createDto = new CreateExperienceDto
        {
            Title = "Test Position",
            Organization = "Test Company",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-1) // End before start
        };

        // Act
        var result = await _experienceService.CreateExperienceAsync(user.Id, createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("End date cannot be before start date", result.Message);
    }

    [Fact]
    public async Task CreateExperienceAsync_WithCurrentExperience_SetsEndDateToNull()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var createDto = new CreateExperienceDto
        {
            Title = "Current Job",
            Organization = "Current Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow, // This should be nulled out
            IsCurrent = true
        };

        // Act
        var result = await _experienceService.CreateExperienceAsync(user.Id, createDto);

        // Assert
        Assert.True(result.Success);

        var experienceDto = result.Data as ExperienceDto;
        Assert.NotNull(experienceDto);
        Assert.Null(experienceDto.EndDate);
        Assert.True(experienceDto.IsCurrent);
    }

    [Fact]
    public async Task CreateExperienceAsync_WithSkills_AssociatesSkills()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var skill1 = new Skill { Name = "C#", Category = "Programming" };
        var skill2 = new Skill { Name = "Azure", Category = "Cloud" };
        _context.Skills.AddRange(skill1, skill2);
        await _context.SaveChangesAsync();

        var createDto = new CreateExperienceDto
        {
            Title = "Developer",
            Organization = "Tech Corp",
            StartDate = DateTime.UtcNow.AddYears(-1),
            SkillIds = new List<Guid> { skill1.Id, skill2.Id }
        };

        // Act
        var result = await _experienceService.CreateExperienceAsync(user.Id, createDto);

        // Assert
        Assert.True(result.Success);

        var experienceDto = result.Data as ExperienceDto;
        Assert.NotNull(experienceDto);
        Assert.Equal(2, experienceDto.Skills.Count);
        Assert.Contains(experienceDto.Skills, s => s.Name == "C#");
        Assert.Contains(experienceDto.Skills, s => s.Name == "Azure");
    }

    [Fact]
    public async Task UpdateExperienceAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var experience = new Experience
        {
            UserId = user.Id,
            Title = "Original Title",
            Organization = "Original Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            Type = ExperienceType.Work
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateExperienceDto
        {
            Title = "Updated Title",
            Organization = "Updated Company",
            Description = "New description"
        };

        // Act
        var result = await _experienceService.UpdateExperienceAsync(user.Id, experience.Id, updateDto);

        // Assert
        Assert.True(result.Success);

        var updatedExperience = await _context.Experiences.FirstOrDefaultAsync(e => e.Id == experience.Id);
        Assert.NotNull(updatedExperience);
        Assert.Equal("Updated Title", updatedExperience.Title);
        Assert.Equal("Updated Company", updatedExperience.Organization);
        Assert.Equal("New description", updatedExperience.Description);
    }

    [Fact]
    public async Task DeleteExperienceAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var experience = new Experience
        {
            UserId = user.Id,
            Title = "To Delete",
            Organization = "Delete Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            Type = ExperienceType.Work
        };
        _context.Experiences.Add(experience);
        await _context.SaveChangesAsync();

        // Act
        var result = await _experienceService.DeleteExperienceAsync(user.Id, experience.Id);

        // Assert
        Assert.True(result.Success);

        var deletedExperience = await _context.Experiences.FirstOrDefaultAsync(e => e.Id == experience.Id);
        Assert.Null(deletedExperience);
    }

    [Fact]
    public async Task GetExperienceTimelineAsync_ReturnsChronologicalOrder()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var experience1 = new Experience
        {
            UserId = user.Id,
            Title = "First Job",
            Organization = "First Company",
            StartDate = DateTime.UtcNow.AddYears(-3),
            EndDate = DateTime.UtcNow.AddYears(-2),
            Type = ExperienceType.Work
        };

        var experience2 = new Experience
        {
            UserId = user.Id,
            Title = "Current Job",
            Organization = "Current Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = true,
            Type = ExperienceType.Work
        };

        var experience3 = new Experience
        {
            UserId = user.Id,
            Title = "Education",
            Organization = "University",
            StartDate = DateTime.UtcNow.AddYears(-5),
            EndDate = DateTime.UtcNow.AddYears(-4),
            Type = ExperienceType.Education
        };

        _context.Experiences.AddRange(experience1, experience2, experience3);
        await _context.SaveChangesAsync();

        // Act
        var timeline = await _experienceService.GetExperienceTimelineAsync(user.Id);

        // Assert
        Assert.Equal(3, timeline.Count);

        // Should be ordered by start date descending (most recent first)
        Assert.Equal("Current Job", timeline[0].Title);
        Assert.Equal("First Job", timeline[1].Title);
        Assert.Equal("Education", timeline[2].Title);
    }

    [Fact]
    public async Task GetCurrentExperiencesAsync_ReturnsOnlyCurrentExperiences()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var currentExp1 = new Experience
        {
            UserId = user.Id,
            Title = "Current Job 1",
            Organization = "Company A",
            StartDate = DateTime.UtcNow.AddMonths(-6),
            IsCurrent = true,
            Type = ExperienceType.Work
        };

        var currentExp2 = new Experience
        {
            UserId = user.Id,
            Title = "Current Job 2",
            Organization = "Company B",
            StartDate = DateTime.UtcNow.AddMonths(-3),
            IsCurrent = true,
            Type = ExperienceType.Work
        };

        var pastExp = new Experience
        {
            UserId = user.Id,
            Title = "Past Job",
            Organization = "Old Company",
            StartDate = DateTime.UtcNow.AddYears(-2),
            EndDate = DateTime.UtcNow.AddYears(-1),
            IsCurrent = false,
            Type = ExperienceType.Work
        };

        _context.Experiences.AddRange(currentExp1, currentExp2, pastExp);
        await _context.SaveChangesAsync();

        // Act
        var currentExperiences = await _experienceService.GetCurrentExperiencesAsync(user.Id);

        // Assert
        Assert.Equal(2, currentExperiences.Count);
        Assert.All(currentExperiences, exp => Assert.True(exp.IsCurrent));
        Assert.Contains(currentExperiences, exp => exp.Title == "Current Job 1");
        Assert.Contains(currentExperiences, exp => exp.Title == "Current Job 2");
    }

    [Fact]
    public async Task GetFeaturedExperiencesAsync_ReturnsOnlyFeatured()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var featuredExp = new Experience
        {
            UserId = user.Id,
            Title = "Featured Experience",
            Organization = "Important Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            IsFeatured = true,
            Type = ExperienceType.Work
        };

        var regularExp = new Experience
        {
            UserId = user.Id,
            Title = "Regular Experience",
            Organization = "Regular Company",
            StartDate = DateTime.UtcNow.AddYears(-2),
            IsFeatured = false,
            Type = ExperienceType.Work
        };

        _context.Experiences.AddRange(featuredExp, regularExp);
        await _context.SaveChangesAsync();

        // Act
        var featuredExperiences = await _experienceService.GetFeaturedExperiencesAsync(user.Id);

        // Assert
        Assert.Single(featuredExperiences);
        Assert.Equal("Featured Experience", featuredExperiences[0].Title);
        Assert.True(featuredExperiences[0].IsFeatured);
    }

    [Fact]
    public async Task UpdateExperienceOrderAsync_UpdatesDisplayOrder()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var exp1 = new Experience
        {
            UserId = user.Id,
            Title = "Experience 1",
            Organization = "Company 1",
            StartDate = DateTime.UtcNow.AddYears(-3),
            DisplayOrder = 1,
            Type = ExperienceType.Work
        };

        var exp2 = new Experience
        {
            UserId = user.Id,
            Title = "Experience 2",
            Organization = "Company 2",
            StartDate = DateTime.UtcNow.AddYears(-2),
            DisplayOrder = 2,
            Type = ExperienceType.Work
        };

        var exp3 = new Experience
        {
            UserId = user.Id,
            Title = "Experience 3",
            Organization = "Company 3",
            StartDate = DateTime.UtcNow.AddYears(-1),
            DisplayOrder = 3,
            Type = ExperienceType.Work
        };

        _context.Experiences.AddRange(exp1, exp2, exp3);
        await _context.SaveChangesAsync();

        // Reorder: exp3, exp1, exp2
        var newOrder = new List<Guid> { exp3.Id, exp1.Id, exp2.Id };

        // Act
        var result = await _experienceService.UpdateExperienceOrderAsync(user.Id, newOrder);

        // Assert
        Assert.True(result.Success);

        var reorderedExperiences = await _context.Experiences
            .Where(e => e.UserId == user.Id)
            .OrderBy(e => e.DisplayOrder)
            .ToListAsync();

        Assert.Equal(exp3.Id, reorderedExperiences[0].Id);
        Assert.Equal(1, reorderedExperiences[0].DisplayOrder);

        Assert.Equal(exp1.Id, reorderedExperiences[1].Id);
        Assert.Equal(2, reorderedExperiences[1].DisplayOrder);

        Assert.Equal(exp2.Id, reorderedExperiences[2].Id);
        Assert.Equal(3, reorderedExperiences[2].DisplayOrder);
    }

    [Fact]
    public async Task AddSkillsToExperienceAsync_AddsSkillsSuccessfully()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var experience = new Experience
        {
            UserId = user.Id,
            Title = "Developer",
            Organization = "Tech Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            Type = ExperienceType.Work
        };
        _context.Experiences.Add(experience);

        var skill1 = new Skill { Name = "React", Category = "Programming" };
        var skill2 = new Skill { Name = "Node.js", Category = "Programming" };
        _context.Skills.AddRange(skill1, skill2);
        await _context.SaveChangesAsync();

        var skillIds = new List<Guid> { skill1.Id, skill2.Id };

        // Act
        var result = await _experienceService.AddSkillsToExperienceAsync(user.Id, experience.Id, skillIds);

        // Assert
        Assert.True(result.Success);

        var experienceSkills = await _context.ExperienceSkills
            .Where(es => es.ExperienceId == experience.Id)
            .ToListAsync();

        Assert.Equal(2, experienceSkills.Count);
        Assert.Contains(experienceSkills, es => es.SkillId == skill1.Id);
        Assert.Contains(experienceSkills, es => es.SkillId == skill2.Id);
    }

    [Fact]
    public async Task RemoveSkillsFromExperienceAsync_RemovesSkillsSuccessfully()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var experience = new Experience
        {
            UserId = user.Id,
            Title = "Developer",
            Organization = "Tech Company",
            StartDate = DateTime.UtcNow.AddYears(-1),
            Type = ExperienceType.Work
        };
        _context.Experiences.Add(experience);

        var skill1 = new Skill { Name = "Python", Category = "Programming" };
        var skill2 = new Skill { Name = "Django", Category = "Programming" };
        _context.Skills.AddRange(skill1, skill2);

        var expSkill1 = new ExperienceSkill
        {
            ExperienceId = experience.Id,
            SkillId = skill1.Id
        };
        var expSkill2 = new ExperienceSkill
        {
            ExperienceId = experience.Id,
            SkillId = skill2.Id
        };
        _context.ExperienceSkills.AddRange(expSkill1, expSkill2);
        await _context.SaveChangesAsync();

        // Act - Remove skill1 only
        var result = await _experienceService.RemoveSkillsFromExperienceAsync(
            user.Id, experience.Id, new List<Guid> { skill1.Id });

        // Assert
        Assert.True(result.Success);

        var remainingSkills = await _context.ExperienceSkills
            .Where(es => es.ExperienceId == experience.Id)
            .ToListAsync();

        Assert.Single(remainingSkills);
        Assert.Equal(skill2.Id, remainingSkills[0].SkillId);
    }

    [Fact]
    public async Task SearchExperiencesAsync_WithTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var workExp = new Experience
        {
            UserId = user.Id,
            Title = "Software Engineer",
            Organization = "Tech Company",
            StartDate = DateTime.UtcNow.AddYears(-2),
            Type = ExperienceType.Work
        };

        var educationExp = new Experience
        {
            UserId = user.Id,
            Title = "Computer Science Degree",
            Organization = "University",
            StartDate = DateTime.UtcNow.AddYears(-4),
            Type = ExperienceType.Education
        };

        _context.Experiences.AddRange(workExp, educationExp);
        await _context.SaveChangesAsync();

        var searchDto = new ExperienceSearchDto
        {
            UserId = user.Id,
            Type = ExperienceType.Work,
            Take = 10
        };

        // Act
        var (experiences, totalCount) = await _experienceService.SearchExperiencesAsync(searchDto);

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(experiences);
        Assert.Equal("Software Engineer", experiences[0].Title);
        Assert.Equal(ExperienceType.Work, experiences[0].Type);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}