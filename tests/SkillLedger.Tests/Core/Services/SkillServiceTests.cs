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
public class SkillServiceTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly ISkillService _skillService;
    private readonly IAuditLogService _auditLogService;

    public SkillServiceTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<SkillLedgerDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Add logging services
        services.AddLogging();

        // Add caching services
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<ISkillService, SkillService>();

        var serviceProvider = services.BuildServiceProvider();
        _context = serviceProvider.GetRequiredService<SkillLedgerDbContext>();
        _auditLogService = serviceProvider.GetRequiredService<IAuditLogService>();
        _skillService = serviceProvider.GetRequiredService<ISkillService>();
    }

    [Fact]
    public async Task CreateSkillAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var createSkillDto = new CreateSkillDto
        {
            Name = "Test Skill",
            Description = "A test skill description",
            Category = "Testing"
        };

        // Act
        var result = await _skillService.CreateSkillAsync(createSkillDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        var skillDto = result.Data as SkillDto;
        Assert.NotNull(skillDto);
        Assert.Equal("Test Skill", skillDto.Name);
        Assert.Equal("Testing", skillDto.Category);
        Assert.False(skillDto.IsSystemManaged);
        Assert.True(skillDto.IsActive);
    }

    [Fact]
    public async Task CreateSkillAsync_WithDuplicateName_ReturnsFailure()
    {
        // Arrange
        await _skillService.CreateSkillAsync(new CreateSkillDto
        {
            Name = "Duplicate Skill",
            Category = "Testing"
        });

        var duplicateDto = new CreateSkillDto
        {
            Name = "Duplicate Skill",
            Category = "Testing"
        };

        // Act
        var result = await _skillService.CreateSkillAsync(duplicateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
    }

    [Fact]
    public async Task SearchSkillsAsync_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange
        await _skillService.CreateSkillAsync(new CreateSkillDto { Name = "C#", Category = "Programming" });
        await _skillService.CreateSkillAsync(new CreateSkillDto { Name = "Design Thinking", Category = "Design" });
        await _skillService.CreateSkillAsync(new CreateSkillDto { Name = "JavaScript", Category = "Programming" });

        var searchDto = new SkillSearchDto
        {
            Category = "Programming",
            Take = 10
        };

        // Act
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.All(skills, skill => Assert.Equal("Programming", skill.Category));
    }

    [Fact]
    public async Task AddUserSkillAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        var addUserSkillDto = new AddUserSkillDto
        {
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Intermediate,
            YearsOfExperience = 3,
            Notes = "Test notes"
        };

        // Act
        var result = await _skillService.AddUserSkillAsync(user.Id, addUserSkillDto);

        // Assert
        Assert.True(result.Success);

        var userSkillDto = result.Data as UserSkillDto;
        Assert.NotNull(userSkillDto);
        Assert.Equal(user.Id, userSkillDto.UserId);
        Assert.Equal(SkillProficiency.Intermediate, userSkillDto.Proficiency);
        Assert.Equal(3, userSkillDto.YearsOfExperience);
    }

    [Fact]
    public async Task AddUserSkillAsync_WithDuplicateSkill_ReturnsFailure()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        var addUserSkillDto = new AddUserSkillDto
        {
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Beginner
        };

        await _skillService.AddUserSkillAsync(user.Id, addUserSkillDto);

        // Act
        var result = await _skillService.AddUserSkillAsync(user.Id, addUserSkillDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already has this skill", result.Message);
    }

    [Fact]
    public async Task UpdateUserSkillAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        _context.Skills.Add(skill);

        var userSkill = new UserSkill
        {
            UserId = user.Id,
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Beginner
        };
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateUserSkillDto
        {
            Proficiency = SkillProficiency.Expert,
            YearsOfExperience = 10
        };

        // Act
        var result = await _skillService.UpdateUserSkillAsync(user.Id, userSkill.Id, updateDto);

        // Assert
        Assert.True(result.Success);

        var updatedSkill = await _context.UserSkills.FirstOrDefaultAsync(us => us.Id == userSkill.Id);
        Assert.NotNull(updatedSkill);
        Assert.Equal(SkillProficiency.Expert, updatedSkill.Proficiency);
        Assert.Equal(10, updatedSkill.YearsOfExperience);
    }

    [Fact]
    public async Task CreateSkillEndorsementAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var skillOwner = new User { Email = "owner@example.com" };
        var endorser = new User { Email = "endorser@example.com" };
        _context.Users.AddRange(skillOwner, endorser);

        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        _context.Skills.Add(skill);

        var userSkill = new UserSkill
        {
            UserId = skillOwner.Id,
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Advanced
        };
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync();

        var endorsementDto = new CreateSkillEndorsementDto
        {
            UserSkillId = userSkill.Id,
            ReviewText = "Great skills!"
        };

        // Act
        var result = await _skillService.CreateSkillEndorsementAsync(endorser.Id, endorsementDto);

        // Assert
        Assert.True(result.Success);

        var endorsement = await _context.SkillEndorsements
            .FirstOrDefaultAsync(se => se.UserSkillId == userSkill.Id);
        Assert.NotNull(endorsement);
        Assert.Equal(endorser.Id, endorsement.EndorsedByUserId);
        Assert.Equal("Great skills!", endorsement.ReviewText);
    }

    [Fact]
    public async Task CreateSkillEndorsementAsync_SelfEndorsement_ReturnsFailure()
    {
        // Arrange
        var user = new User { Email = "user@example.com" };
        _context.Users.Add(user);

        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        _context.Skills.Add(skill);

        var userSkill = new UserSkill
        {
            UserId = user.Id,
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Advanced
        };
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync();

        var endorsementDto = new CreateSkillEndorsementDto
        {
            UserSkillId = userSkill.Id,
            ReviewText = "I'm awesome!"
        };

        // Act
        var result = await _skillService.CreateSkillEndorsementAsync(user.Id, endorsementDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot endorse your own skills", result.Message);
    }

    [Fact]
    public async Task GetSkillCategoriesAsync_ReturnsCategories()
    {
        // Arrange
        await _skillService.CreateSkillAsync(new CreateSkillDto { Name = "C#", Category = "Programming" });
        await _skillService.CreateSkillAsync(new CreateSkillDto { Name = "Java", Category = "Programming" });
        await _skillService.CreateSkillAsync(new CreateSkillDto { Name = "Photoshop", Category = "Design" });

        // Act
        var categories = await _skillService.GetSkillCategoriesAsync();

        // Assert
        Assert.Contains(categories, c => c.Name == "Programming" && c.SkillCount == 2);
        Assert.Contains(categories, c => c.Name == "Design" && c.SkillCount == 1);
    }

    [Fact]
    public async Task CanEndorseSkillAsync_WithValidScenario_ReturnsTrue()
    {
        // Arrange
        var skillOwner = new User { Email = "owner@example.com" };
        var endorser = new User { Email = "endorser@example.com" };
        _context.Users.AddRange(skillOwner, endorser);

        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        _context.Skills.Add(skill);

        var userSkill = new UserSkill
        {
            UserId = skillOwner.Id,
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Advanced
        };
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync();

        // Act
        var canEndorse = await _skillService.CanEndorseSkillAsync(endorser.Id, userSkill.Id);

        // Assert
        Assert.True(canEndorse);
    }

    [Fact]
    public async Task GetUserSkillsAsync_WithVisibilityFilter_ReturnsOnlyVisibleSkills()
    {
        // Arrange
        var user = new User { Email = "test@example.com" };
        _context.Users.Add(user);

        var publicSkill = new Skill { Name = "Public Skill", Category = "Testing" };
        var privateSkill = new Skill { Name = "Private Skill", Category = "Testing" };
        _context.Skills.AddRange(publicSkill, privateSkill);

        var userSkill1 = new UserSkill
        {
            UserId = user.Id,
            SkillId = publicSkill.Id,
            IsVisible = true,
            Proficiency = SkillProficiency.Advanced
        };

        var userSkill2 = new UserSkill
        {
            UserId = user.Id,
            SkillId = privateSkill.Id,
            IsVisible = false,
            Proficiency = SkillProficiency.Beginner
        };

        _context.UserSkills.AddRange(userSkill1, userSkill2);
        await _context.SaveChangesAsync();

        // Act
        var visibleSkills = await _skillService.GetUserSkillsAsync(user.Id, visibleOnly: true);

        // Assert
        Assert.Single(visibleSkills);
        Assert.Equal("Public Skill", visibleSkills[0].Skill.Name);
    }

    [Fact]
    public async Task InitializeSystemSkillsAsync_CreatesSystemSkills()
    {
        // Act
        await _skillService.InitializeSystemSkillsAsync();

        // Assert
        var systemSkills = await _context.Skills
            .Where(s => s.IsSystemManaged)
            .ToListAsync();

        Assert.True(systemSkills.Count > 0);
        Assert.Contains(systemSkills, s => s.Name == "C#");
        Assert.Contains(systemSkills, s => s.Category == "Programming");
    }

    [Fact]
    public async Task InitializeSystemSkillsAsync_RunTwice_DoesNotCreateDuplicates()
    {
        // Act
        await _skillService.InitializeSystemSkillsAsync();
        await _skillService.InitializeSystemSkillsAsync();

        // Assert
        var systemSkills = await _context.Skills
            .Where(s => s.IsSystemManaged)
            .ToListAsync();

        var skillNames = systemSkills.Select(s => s.Name).ToList();
        var uniqueSkillNames = skillNames.Distinct().ToList();

        Assert.Equal(uniqueSkillNames.Count, skillNames.Count);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}