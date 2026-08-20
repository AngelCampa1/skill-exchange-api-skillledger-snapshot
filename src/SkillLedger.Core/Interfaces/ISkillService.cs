using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

public interface ISkillService
{
    /// <summary>
    /// Creates a new skill
    /// </summary>
    Task<ServiceResponseDto> CreateSkillAsync(CreateSkillDto dto);

    /// <summary>
    /// Updates an existing skill
    /// </summary>
    Task<ServiceResponseDto> UpdateSkillAsync(Guid skillId, UpdateSkillDto dto);

    /// <summary>
    /// Deletes a skill (soft delete - marks as inactive)
    /// </summary>
    Task<ServiceResponseDto> DeleteSkillAsync(Guid skillId);

    /// <summary>
    /// Gets a skill by ID
    /// </summary>
    Task<SkillDto?> GetSkillByIdAsync(Guid skillId);

    /// <summary>
    /// Gets a skill by name
    /// </summary>
    Task<SkillDto?> GetSkillByNameAsync(string name);

    /// <summary>
    /// Searches skills with filtering and pagination
    /// </summary>
    Task<(List<SkillDto> Skills, int TotalCount)> SearchSkillsAsync(SkillSearchDto searchDto);

    /// <summary>
    /// Gets all skill categories with counts
    /// </summary>
    Task<List<SkillCategoryDto>> GetSkillCategoriesAsync();

    /// <summary>
    /// Adds a skill to a user's profile
    /// </summary>
    Task<ServiceResponseDto> AddUserSkillAsync(Guid userId, AddUserSkillDto dto);

    /// <summary>
    /// Updates a user's skill
    /// </summary>
    Task<ServiceResponseDto> UpdateUserSkillAsync(Guid userId, Guid userSkillId, UpdateUserSkillDto dto);

    /// <summary>
    /// Removes a skill from a user's profile
    /// </summary>
    Task<ServiceResponseDto> RemoveUserSkillAsync(Guid userId, Guid userSkillId);

    /// <summary>
    /// Gets a user's specific skill
    /// </summary>
    Task<UserSkillDto?> GetUserSkillAsync(Guid userId, Guid userSkillId);

    /// <summary>
    /// Searches a user's skills with filtering and pagination
    /// </summary>
    Task<(List<UserSkillDto> UserSkills, int TotalCount)> SearchUserSkillsAsync(UserSkillSearchDto searchDto);

    /// <summary>
    /// Gets all skills for a specific user
    /// </summary>
    Task<List<UserSkillDto>> GetUserSkillsAsync(Guid userId, bool visibleOnly = true, bool includeEndorsements = false);

    /// <summary>
    /// Creates a skill endorsement
    /// </summary>
    Task<ServiceResponseDto> CreateSkillEndorsementAsync(Guid endorserId, CreateSkillEndorsementDto dto);

    /// <summary>
    /// Removes a skill endorsement
    /// </summary>
    Task<ServiceResponseDto> RemoveSkillEndorsementAsync(Guid endorserId, Guid endorsementId);

    /// <summary>
    /// Gets endorsements for a specific user skill
    /// </summary>
    Task<List<SkillEndorsementDto>> GetSkillEndorsementsAsync(Guid userSkillId);

    /// <summary>
    /// Checks if a user can endorse a skill (hasn't already endorsed it)
    /// </summary>
    Task<bool> CanEndorseSkillAsync(Guid endorserId, Guid userSkillId);

    /// <summary>
    /// Gets system-managed skills for initialization
    /// </summary>
    Task InitializeSystemSkillsAsync();
}