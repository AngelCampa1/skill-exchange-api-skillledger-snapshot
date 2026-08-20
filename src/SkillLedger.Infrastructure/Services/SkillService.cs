using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

public class SkillService : ISkillService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<SkillService> _logger;

    // Cache TTL constants
    private static readonly TimeSpan SkillCacheTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SkillListCacheTtl = TimeSpan.FromMinutes(15);

    // BUG-HIGH-001 FIX: Add logger for cache operation observability
    public SkillService(
        SkillLedgerDbContext context,
        IAuditLogService auditLogService,
        ICacheService cacheService,
        ILogger<SkillService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ServiceResponseDto> CreateSkillAsync(CreateSkillDto dto)
    {
        try
        {
            // Check if skill with same name already exists
            var existingSkill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Name.ToLower() == dto.Name.ToLower());

            if (existingSkill != null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "A skill with this name already exists"
                };
            }

            var skill = new Skill
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Category = dto.Category.Trim(),
                IsSystemManaged = false,
                IsActive = true
            };

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            // Invalidate skill lists cache
            await InvalidateSkillListsCacheAsync();

            await _auditLogService.LogEventAsync(
                null,
                "SKILL_CREATED",
                "127.0.0.1",
                null,
                true,
                $"Created skill '{skill.Name}' in category '{skill.Category}'"
            );

            var skillDto = await MapToSkillDto(skill);

            // Cache the new skill
            await CacheSkillAsync(skill.Id, skillDto);

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Skill created successfully",
                Data = skillDto
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                null,
                "SKILL_CREATION_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to create skill '{dto.Name}': {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to create skill",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServiceResponseDto> UpdateSkillAsync(Guid skillId, UpdateSkillDto dto)
    {
        try
        {
            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == skillId);
            if (skill == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Skill not found"
                };
            }

            // Check if skill is system-managed
            if (skill.IsSystemManaged)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "System-managed skills cannot be modified"
                };
            }

            // Check for name conflicts if name is being changed
            if (!string.IsNullOrWhiteSpace(dto.Name) &&
                dto.Name.Trim().ToLower() != skill.Name.ToLower())
            {
                var existingSkill = await _context.Skills
                    .FirstOrDefaultAsync(s => s.Name.ToLower() == dto.Name.ToLower() && s.Id != skillId);

                if (existingSkill != null)
                {
                    return new ServiceResponseDto
                    {
                        Success = false,
                        Message = "A skill with this name already exists"
                    };
                }
            }

            // Update properties
            if (!string.IsNullOrWhiteSpace(dto.Name))
                skill.Name = dto.Name.Trim();
            if (dto.Description != null)
                skill.Description = dto.Description.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Category))
                skill.Category = dto.Category.Trim();

            skill.IsActive = dto.IsActive;
            skill.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Invalidate caches
            await InvalidateSkillCacheAsync(skillId);
            await InvalidateSkillListsCacheAsync();

            await _auditLogService.LogEventAsync(
                null,
                "SKILL_UPDATED",
                "127.0.0.1",
                null,
                true,
                $"Updated skill '{skill.Name}'"
            );

            var skillDto = await MapToSkillDto(skill);

            // Update cache
            await CacheSkillAsync(skillId, skillDto);

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Skill updated successfully",
                Data = skillDto
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                null,
                "SKILL_UPDATE_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to update skill {skillId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to update skill",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServiceResponseDto> DeleteSkillAsync(Guid skillId)
    {
        try
        {
            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == skillId);
            if (skill == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Skill not found"
                };
            }

            // Check if skill is system-managed
            if (skill.IsSystemManaged)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "System-managed skills cannot be deleted"
                };
            }

            // Check if skill is being used by users
            var userSkillCount = await _context.UserSkills.CountAsync(us => us.SkillId == skillId);
            if (userSkillCount > 0)
            {
                // Soft delete - mark as inactive
                skill.IsActive = false;
                skill.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Invalidate caches
                await InvalidateSkillCacheAsync(skillId);
                await InvalidateSkillListsCacheAsync();

                await _auditLogService.LogEventAsync(
                    null,
                    "SKILL_DEACTIVATED",
                    "127.0.0.1",
                    null,
                    true,
                    $"Deactivated skill '{skill.Name}' (was being used by {userSkillCount} users)"
                );

                return new ServiceResponseDto
                {
                    Success = true,
                    Message = $"Skill deactivated successfully (was being used by {userSkillCount} users)"
                };
            }

            // Hard delete if no users have this skill
            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync();

            // Invalidate caches
            await InvalidateSkillCacheAsync(skillId);
            await InvalidateSkillListsCacheAsync();

            await _auditLogService.LogEventAsync(
                null,
                "SKILL_DELETED",
                "127.0.0.1",
                null,
                true,
                $"Permanently deleted skill '{skill.Name}'"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Skill deleted successfully"
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                null,
                "SKILL_DELETION_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to delete skill {skillId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to delete skill",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<SkillDto?> GetSkillByIdAsync(Guid skillId)
    {
        // Try cache first
        var cacheKey = GetSkillCacheKey(skillId);
        var cachedSkill = await _cacheService.GetAsync<SkillDto>(cacheKey);
        if (cachedSkill != null)
        {
            return cachedSkill;
        }

        var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == skillId);
        if (skill == null)
            return null;

        var skillDto = await MapToSkillDto(skill);

        // Cache it
        await CacheSkillAsync(skillId, skillDto);

        return skillDto;
    }

    public async Task<SkillDto?> GetSkillByNameAsync(string name)
    {
        var skill = await _context.Skills
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
        return skill != null ? await MapToSkillDto(skill) : null;
    }

    public async Task<(List<SkillDto> Skills, int TotalCount)> SearchSkillsAsync(SkillSearchDto searchDto)
    {
        var query = _context.Skills.AsQueryable();

        // Apply filters
        if (searchDto.ActiveOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        if (searchDto.SystemManagedOnly.HasValue)
        {
            query = query.Where(s => s.IsSystemManaged == searchDto.SystemManagedOnly.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Category))
        {
            query = query.Where(s => s.Category.ToLower() == searchDto.Category.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Query))
        {
            var searchQuery = searchDto.Query.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(searchQuery) ||
                (s.Description != null && s.Description.ToLower().Contains(searchQuery))
            );
        }

        var totalCount = await query.CountAsync();

        var skills = await query
            .OrderBy(s => s.Name)
            .Skip(searchDto.Skip)
            .Take(searchDto.Take)
            .ToListAsync();

        var skillDtos = new List<SkillDto>();
        foreach (var skill in skills)
        {
            skillDtos.Add(await MapToSkillDto(skill));
        }

        return (skillDtos, totalCount);
    }

    public async Task<List<SkillCategoryDto>> GetSkillCategoriesAsync()
    {
        // Try cache first
        var cacheKey = "skills:categories";
        var cachedCategories = await _cacheService.GetAsync<List<SkillCategoryDto>>(cacheKey);
        if (cachedCategories != null)
        {
            return cachedCategories;
        }

        var categories = await _context.Skills
            .Where(s => s.IsActive)
            .GroupBy(s => s.Category)
            .Select(g => new SkillCategoryDto
            {
                Name = g.Key,
                SkillCount = g.Count(),
                UserCount = g.SelectMany(s => s.UserSkills).Select(us => us.UserId).Distinct().Count()
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Cache the categories
        await _cacheService.SetAsync(cacheKey, categories, SkillListCacheTtl);

        return categories;
    }

    public async Task<ServiceResponseDto> AddUserSkillAsync(Guid userId, AddUserSkillDto dto)
    {
        try
        {
            // Check if user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Check if skill exists
            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == dto.SkillId && s.IsActive);
            if (skill == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Skill not found or inactive"
                };
            }

            // Check if user already has this skill
            var existingUserSkill = await _context.UserSkills
                .FirstOrDefaultAsync(us => us.UserId == userId && us.SkillId == dto.SkillId);

            if (existingUserSkill != null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "User already has this skill"
                };
            }

            var userSkill = new UserSkill
            {
                UserId = userId,
                SkillId = dto.SkillId,
                Proficiency = dto.Proficiency,
                YearsOfExperience = dto.YearsOfExperience,
                Notes = dto.Notes?.Trim(),
                IsFeatured = dto.IsFeatured,
                IsVisible = dto.IsVisible
            };

            _context.UserSkills.Add(userSkill);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "USER_SKILL_ADDED",
                "127.0.0.1",
                null,
                true,
                $"Added skill '{skill.Name}' to user profile with proficiency '{dto.Proficiency}'"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Skill added to profile successfully",
                Data = await MapToUserSkillDto(userSkill)
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                userId,
                "USER_SKILL_ADDITION_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to add skill {dto.SkillId} to user {userId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to add skill to profile",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServiceResponseDto> UpdateUserSkillAsync(Guid userId, Guid userSkillId, UpdateUserSkillDto dto)
    {
        try
        {
            var userSkill = await _context.UserSkills
                .Include(us => us.Skill)
                .FirstOrDefaultAsync(us => us.Id == userSkillId && us.UserId == userId);

            if (userSkill == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "User skill not found"
                };
            }

            // Update properties
            if (dto.Proficiency.HasValue)
                userSkill.Proficiency = dto.Proficiency.Value;
            if (dto.YearsOfExperience.HasValue)
                userSkill.YearsOfExperience = dto.YearsOfExperience.Value;
            if (dto.Notes != null)
                userSkill.Notes = dto.Notes.Trim();
            if (dto.IsFeatured.HasValue)
                userSkill.IsFeatured = dto.IsFeatured.Value;
            if (dto.IsVisible.HasValue)
                userSkill.IsVisible = dto.IsVisible.Value;

            userSkill.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "USER_SKILL_UPDATED",
                "127.0.0.1",
                null,
                true,
                $"Updated skill '{userSkill.Skill.Name}' in user profile"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "User skill updated successfully",
                Data = await MapToUserSkillDto(userSkill)
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                userId,
                "USER_SKILL_UPDATE_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to update user skill {userSkillId} for user {userId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to update user skill",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServiceResponseDto> RemoveUserSkillAsync(Guid userId, Guid userSkillId)
    {
        try
        {
            var userSkill = await _context.UserSkills
                .Include(us => us.Skill)
                .FirstOrDefaultAsync(us => us.Id == userSkillId && us.UserId == userId);

            if (userSkill == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "User skill not found"
                };
            }

            _context.UserSkills.Remove(userSkill);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                "USER_SKILL_REMOVED",
                "127.0.0.1",
                null,
                true,
                $"Removed skill '{userSkill.Skill.Name}' from user profile"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Skill removed from profile successfully"
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                userId,
                "USER_SKILL_REMOVAL_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to remove user skill {userSkillId} for user {userId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to remove skill from profile",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<UserSkillDto?> GetUserSkillAsync(Guid userId, Guid userSkillId)
    {
        var userSkill = await _context.UserSkills
            .Include(us => us.Skill)
            .FirstOrDefaultAsync(us => us.Id == userSkillId && us.UserId == userId);

        return userSkill != null ? await MapToUserSkillDto(userSkill) : null;
    }

    public async Task<(List<UserSkillDto> UserSkills, int TotalCount)> SearchUserSkillsAsync(UserSkillSearchDto searchDto)
    {
        var query = _context.UserSkills
            .Include(us => us.Skill)
            .AsQueryable();

        // Apply filters
        if (searchDto.UserId.HasValue)
        {
            query = query.Where(us => us.UserId == searchDto.UserId.Value);
        }

        if (searchDto.VisibleOnly)
        {
            query = query.Where(us => us.IsVisible);
        }

        if (searchDto.FeaturedOnly.HasValue)
        {
            query = query.Where(us => us.IsFeatured == searchDto.FeaturedOnly.Value);
        }

        if (searchDto.Proficiency.HasValue)
        {
            query = query.Where(us => us.Proficiency == searchDto.Proficiency.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Category))
        {
            query = query.Where(us => us.Skill.Category.ToLower() == searchDto.Category.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Query))
        {
            var searchQuery = searchDto.Query.ToLower();
            query = query.Where(us =>
                us.Skill.Name.ToLower().Contains(searchQuery) ||
                (us.Notes != null && us.Notes.ToLower().Contains(searchQuery))
            );
        }

        var totalCount = await query.CountAsync();

        var userSkills = await query
            .OrderBy(us => us.Skill.Category)
            .ThenBy(us => us.Skill.Name)
            .Skip(searchDto.Skip)
            .Take(searchDto.Take)
            .ToListAsync();

        var userSkillDtos = new List<UserSkillDto>();
        foreach (var userSkill in userSkills)
        {
            var dto = await MapToUserSkillDto(userSkill);
            if (searchDto.IncludeEndorsements)
            {
                dto.Endorsements = await GetSkillEndorsementsAsync(userSkill.Id);
            }
            userSkillDtos.Add(dto);
        }

        return (userSkillDtos, totalCount);
    }

    public async Task<List<UserSkillDto>> GetUserSkillsAsync(Guid userId, bool visibleOnly = true, bool includeEndorsements = false)
    {
        var query = _context.UserSkills
            .Include(us => us.Skill)
            .Where(us => us.UserId == userId);

        if (visibleOnly)
        {
            query = query.Where(us => us.IsVisible);
        }

        var userSkills = await query
            .OrderBy(us => us.Skill.Category)
            .ThenBy(us => us.Skill.Name)
            .ToListAsync();

        var userSkillDtos = new List<UserSkillDto>();
        foreach (var userSkill in userSkills)
        {
            var dto = await MapToUserSkillDto(userSkill);
            if (includeEndorsements)
            {
                dto.Endorsements = await GetSkillEndorsementsAsync(userSkill.Id);
            }
            userSkillDtos.Add(dto);
        }

        return userSkillDtos;
    }

    public async Task<ServiceResponseDto> CreateSkillEndorsementAsync(Guid endorserId, CreateSkillEndorsementDto dto)
    {
        try
        {
            // Check if endorser exists
            var endorserExists = await _context.Users.AnyAsync(u => u.Id == endorserId);
            if (!endorserExists)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Endorser not found"
                };
            }

            // Check if user skill exists
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var userSkill = await _context.UserSkills
                .Include(us => us.User)
                .Include(us => us.Skill)
                .AsSplitQuery()
                .FirstOrDefaultAsync(us => us.Id == dto.UserSkillId);

            if (userSkill == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "User skill not found"
                };
            }

            // Check if user is trying to endorse their own skill
            if (userSkill.UserId == endorserId)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Cannot endorse your own skills"
                };
            }

            // Check if endorsement already exists
            var existingEndorsement = await _context.SkillEndorsements
                .FirstOrDefaultAsync(se => se.UserSkillId == dto.UserSkillId && se.EndorsedByUserId == endorserId);

            if (existingEndorsement != null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "You have already endorsed this skill"
                };
            }

            var endorsement = new SkillEndorsement
            {
                UserSkillId = dto.UserSkillId,
                EndorsedByUserId = endorserId,
                Comment = dto.Comment?.Trim(),
                IsVisible = dto.IsVisible
            };

            _context.SkillEndorsements.Add(endorsement);
            await _context.SaveChangesAsync();

            // Reload the endorsement with navigation properties for mapping
            // PERFORMANCE FIX: Add AsSplitQuery to prevent cartesian explosion with multi-level Include
            var endorsementWithNav = await _context.SkillEndorsements
                .Include(e => e.EndorsedByUser)
                .ThenInclude(u => u.Profile)
                .AsSplitQuery()
                .FirstAsync(e => e.Id == endorsement.Id);

            await _auditLogService.LogEventAsync(
                endorserId,
                "SKILL_ENDORSED",
                "127.0.0.1",
                null,
                true,
                $"Endorsed skill '{userSkill.Skill.Name}' for user {userSkill.UserId}"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Skill endorsed successfully",
                Data = await MapToSkillEndorsementDto(endorsementWithNav)
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                endorserId,
                "SKILL_ENDORSEMENT_FAILED",
                "127.0.0.1",
                null,
                true,
                "Failed to endorse skill {dto.UserSkillId} by user {endorserId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to endorse skill",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<ServiceResponseDto> RemoveSkillEndorsementAsync(Guid endorserId, Guid endorsementId)
    {
        try
        {
            // PERFORMANCE FIX: Add AsSplitQuery to prevent cartesian explosion with multi-level Include
            var endorsement = await _context.SkillEndorsements
                .Include(se => se.UserSkill)
                .ThenInclude(us => us.Skill)
                .AsSplitQuery()
                .FirstOrDefaultAsync(se => se.Id == endorsementId && se.EndorsedByUserId == endorserId);

            if (endorsement == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Endorsement not found"
                };
            }

            _context.SkillEndorsements.Remove(endorsement);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                endorserId,
                "SKILL_ENDORSEMENT_REMOVED",
                "127.0.0.1",
                null,
                true,
                $"Removed endorsement for skill '{endorsement.UserSkill.Skill.Name}'"
            );

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Endorsement removed successfully"
            };
        }
        catch (Exception ex)
        {
            await _auditLogService.LogEventAsync(
                endorserId,
                "SKILL_ENDORSEMENT_REMOVAL_FAILED",
                "127.0.0.1",
                null,
                false,
                null,
                $"Failed to remove endorsement {endorsementId} by user {endorserId}: {ex.Message}"
            );

            return new ServiceResponseDto
            {
                Success = false,
                Message = "Failed to remove endorsement",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<List<SkillEndorsementDto>> GetSkillEndorsementsAsync(Guid userSkillId)
    {
        // PERFORMANCE FIX: Add AsSplitQuery to prevent cartesian explosion with multi-level Include
        var endorsements = await _context.SkillEndorsements
            .Include(se => se.EndorsedByUser)
            .ThenInclude(u => u.Profile)
            .AsSplitQuery()
            .Where(se => se.UserSkillId == userSkillId && se.IsVisible)
            .OrderByDescending(se => se.CreatedAt)
            .ToListAsync();

        var endorsementDtos = new List<SkillEndorsementDto>();
        foreach (var endorsement in endorsements)
        {
            endorsementDtos.Add(await MapToSkillEndorsementDto(endorsement));
        }

        return endorsementDtos;
    }

    public async Task<bool> CanEndorseSkillAsync(Guid endorserId, Guid userSkillId)
    {
        var userSkill = await _context.UserSkills.FirstOrDefaultAsync(us => us.Id == userSkillId);
        if (userSkill == null || userSkill.UserId == endorserId)
        {
            return false; // Skill doesn't exist or user is trying to endorse own skill
        }

        var existingEndorsement = await _context.SkillEndorsements
            .FirstOrDefaultAsync(se => se.UserSkillId == userSkillId && se.EndorsedByUserId == endorserId);

        return existingEndorsement == null; // Can endorse if no existing endorsement
    }

    public async Task InitializeSystemSkillsAsync()
    {
        var systemSkills = GetSystemSkills();
        var addedSkillsCount = 0;

        foreach (var skillData in systemSkills)
        {
            // Check if skill already exists (by name, since there's a unique constraint)
            var existingSkill = await _context.Skills
                .FirstOrDefaultAsync(s => s.Name.ToLower() == skillData.Name.ToLower());

            if (existingSkill == null)
            {
                // Skill doesn't exist, create it
                var skill = new Skill
                {
                    Name = skillData.Name,
                    Description = skillData.Description,
                    Category = skillData.Category,
                    IsSystemManaged = true,
                    IsActive = true
                };

                _context.Skills.Add(skill);
                addedSkillsCount++;
            }
            else
            {
                // Skill exists, update it to be system-managed if it isn't already
                if (!existingSkill.IsSystemManaged)
                {
                    existingSkill.IsSystemManaged = true;
                    existingSkill.IsActive = true;
                    addedSkillsCount++;
                }
            }
        }

        if (addedSkillsCount > 0)
        {
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                    null,
                    "SYSTEM_SKILLS_INITIALIZED",
                    "127.0.0.1",
                    null,
                    true,
                    $"Initialized/updated {addedSkillsCount} system-managed skills"
                );
        }
    }

    private async Task<SkillDto> MapToSkillDto(Skill skill)
    {
        var userCount = await _context.UserSkills.CountAsync(us => us.SkillId == skill.Id);
        var endorsementCount = await _context.SkillEndorsements
            .CountAsync(se => se.UserSkill.SkillId == skill.Id);

        return new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Description = skill.Description,
            Category = skill.Category,
            IsSystemManaged = skill.IsSystemManaged,
            IsActive = skill.IsActive,
            CreatedAt = skill.CreatedAt,
            UpdatedAt = skill.UpdatedAt,
            UserCount = userCount,
            EndorsementCount = endorsementCount
        };
    }

    private async Task<UserSkillDto> MapToUserSkillDto(UserSkill userSkill)
    {
        var endorsementCount = await _context.SkillEndorsements
            .CountAsync(se => se.UserSkillId == userSkill.Id);

        return new UserSkillDto
        {
            Id = userSkill.Id,
            UserId = userSkill.UserId,
            Skill = await MapToSkillDto(userSkill.Skill),
            Proficiency = userSkill.Proficiency,
            YearsOfExperience = userSkill.YearsOfExperience,
            Notes = userSkill.Notes,
            IsFeatured = userSkill.IsFeatured,
            IsVisible = userSkill.IsVisible,
            CreatedAt = userSkill.CreatedAt,
            UpdatedAt = userSkill.UpdatedAt,
            EndorsementCount = endorsementCount,
            Endorsements = new List<SkillEndorsementDto>() // Populated separately if requested
        };
    }

    private async Task<SkillEndorsementDto> MapToSkillEndorsementDto(SkillEndorsement endorsement)
    {
        return new SkillEndorsementDto
        {
            Id = endorsement.Id,
            UserSkillId = endorsement.UserSkillId,
            EndorsedByUser = new UserSummaryDto
            {
                Id = endorsement.EndorsedByUser.Id,
                DisplayName = endorsement.EndorsedByUser.Profile?.FirstName != null && endorsement.EndorsedByUser.Profile?.LastName != null
                    ? $"{endorsement.EndorsedByUser.Profile.FirstName} {endorsement.EndorsedByUser.Profile.LastName}".Trim()
                    : endorsement.EndorsedByUser.Email ?? "Unknown User",
                Title = endorsement.EndorsedByUser.Profile?.Title,
                Company = endorsement.EndorsedByUser.Profile?.Company,
                AvatarUrl = endorsement.EndorsedByUser.Profile?.AvatarUrl
            },
            Comment = endorsement.Comment,
            IsVisible = endorsement.IsVisible,
            CreatedAt = endorsement.CreatedAt
        };
    }

    private List<(string Name, string Description, string Category)> GetSystemSkills()
    {
        return new List<(string Name, string Description, string Category)>
        {
            // Programming Languages
            ("C#", "Microsoft's object-oriented programming language", "Programming"),
            ("JavaScript", "Dynamic programming language for web development", "Programming"),
            ("TypeScript", "Typed superset of JavaScript", "Programming"),
            ("Python", "High-level programming language", "Programming"),
            ("Java", "Object-oriented programming language", "Programming"),
            ("C++", "General-purpose programming language", "Programming"),
            ("Go", "Open source programming language by Google", "Programming"),
            ("Rust", "Systems programming language", "Programming"),
            ("PHP", "Server-side scripting language", "Programming"),
            ("Ruby", "Dynamic, object-oriented programming language", "Programming"),

            // Web Technologies
            ("React", "JavaScript library for building user interfaces", "Web Development"),
            ("Angular", "TypeScript-based web application framework", "Web Development"),
            ("Vue.js", "Progressive JavaScript framework", "Web Development"),
            ("Node.js", "JavaScript runtime for server-side development", "Web Development"),
            ("HTML", "Markup language for web pages", "Web Development"),
            ("CSS", "Style sheet language for web presentation", "Web Development"),
            ("ASP.NET Core", "Cross-platform web framework for .NET", "Web Development"),
            ("Express.js", "Web application framework for Node.js", "Web Development"),

            // Databases
            ("SQL Server", "Microsoft's relational database management system", "Database"),
            ("PostgreSQL", "Open-source relational database", "Database"),
            ("MySQL", "Open-source relational database management system", "Database"),
            ("MongoDB", "Document-oriented NoSQL database", "Database"),
            ("Redis", "In-memory data structure store", "Database"),
            ("Oracle Database", "Multi-model database management system", "Database"),

            // Cloud Platforms
            ("Microsoft Azure", "Microsoft's cloud computing platform", "Cloud"),
            ("Amazon Web Services", "Amazon's cloud computing platform", "Cloud"),
            ("Google Cloud Platform", "Google's cloud computing platform", "Cloud"),
            ("Docker", "Platform for developing, shipping, and running applications", "DevOps"),
            ("Kubernetes", "Container orchestration platform", "DevOps"),

            // Design
            ("UI/UX Design", "User interface and user experience design", "Design"),
            ("Adobe Photoshop", "Raster graphics editing software", "Design"),
            ("Figma", "Web-based design and prototyping tool", "Design"),
            ("Adobe Illustrator", "Vector graphics editing software", "Design"),

            // Project Management
            ("Agile", "Iterative approach to project management", "Project Management"),
            ("Scrum", "Framework for Agile project management", "Project Management"),
            ("Kanban", "Visual system for managing work", "Project Management"),

            // Marketing
            ("Digital Marketing", "Marketing using digital channels", "Marketing"),
            ("SEO", "Search engine optimization", "Marketing"),
            ("Content Marketing", "Creating and distributing valuable content", "Marketing"),
            ("Social Media Marketing", "Marketing on social media platforms", "Marketing"),

            // Business
            ("Business Analysis", "Analyzing business needs and solutions", "Business"),
            ("Data Analysis", "Examining datasets to draw conclusions", "Business"),
            ("Financial Analysis", "Evaluating financial data for decision-making", "Business"),
            ("Strategic Planning", "Defining strategy and making decisions", "Business"),

            // E2E-012 FIX: Add Data Science skills
            ("Machine Learning", "Building and deploying ML models", "Data Science"),
            ("Data Engineering", "Building data pipelines and infrastructure", "Data Science"),
            ("Natural Language Processing", "Processing and analyzing text data", "Data Science"),
            ("Computer Vision", "Image and video analysis using AI", "Data Science"),

            // E2E-012 FIX: Add Mobile Development skills
            ("React Native", "Cross-platform mobile development with React", "Mobile Development"),
            ("Flutter", "Google's UI toolkit for mobile apps", "Mobile Development"),
            ("iOS Development", "Native iOS app development with Swift", "Mobile Development"),
            ("Android Development", "Native Android app development with Kotlin", "Mobile Development"),

            // E2E-012 FIX: Add Security skills
            ("Penetration Testing", "Security testing and vulnerability assessment", "Security"),
            ("Security Auditing", "Comprehensive security reviews", "Security"),
            ("Cybersecurity", "Protecting systems from cyber threats", "Security"),

            // E2E-012 FIX: Add Writing/Documentation skills
            ("Technical Writing", "Creating technical documentation", "Writing"),
            ("API Documentation", "Documenting APIs and developer guides", "Writing"),
            ("Content Writing", "Creating engaging written content", "Writing")
        };
    }

    // Cache helper methods
    private static string GetSkillCacheKey(Guid skillId) => $"skill:{skillId}";

    private async Task CacheSkillAsync(Guid skillId, SkillDto skillDto)
    {
        try
        {
            var cacheKey = GetSkillCacheKey(skillId);
            await _cacheService.SetAsync(cacheKey, skillDto, SkillCacheTtl);
        }
        catch (Exception ex)
        {
            // BUG-HIGH-001 FIX: Cache failures should not break the application, but log for observability
            _logger.LogWarning(ex, "Cache SET operation failed for skill {SkillId}. Continuing without cache.", skillId);
        }
    }

    private async Task InvalidateSkillCacheAsync(Guid skillId)
    {
        try
        {
            var cacheKey = GetSkillCacheKey(skillId);
            await _cacheService.RemoveAsync(cacheKey);
        }
        catch (Exception ex)
        {
            // BUG-HIGH-001 FIX: Cache failures should not break the application, but log for observability
            _logger.LogWarning(ex, "Cache REMOVE operation failed for skill {SkillId}. Continuing without cache invalidation.", skillId);
        }
    }

    private async Task InvalidateSkillListsCacheAsync()
    {
        try
        {
            // Invalidate skill category list cache
            await _cacheService.RemoveAsync("skills:categories");

            // Invalidate all skill list caches by pattern (if Redis is available)
            await _cacheService.RemoveByPatternAsync("skills:list:*");
        }
        catch (Exception ex)
        {
            // BUG-HIGH-001 FIX: Cache failures should not break the application, but log for observability
            _logger.LogWarning(ex, "Cache invalidation failed for skill lists. Continuing without cache invalidation.");
        }
    }
}