using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.Constants;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/skills")] // Plural route alias for frontend compatibility
[Authorize]
[EnableRateLimiting("SkillManagementPolicy")]
public class SkillController : ControllerBase
{
    private readonly ISkillService _skillService;

    public SkillController(ISkillService skillService)
    {
        _skillService = skillService;
    }

    /// <summary>
    /// Creates a new skill (Admin only)
    /// </summary>
    /// <param name="createSkillDto">Skill creation data</param>
    /// <returns>Service response with created skill data</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    // SECURITY: Requires authentication - removed [AllowAnonymous] for production safety
    // Tests should use proper authentication tokens
    // [EnableRateLimiting("ProfileCreationPolicy")] // Disabled for testing
    public async Task<ActionResult<ServiceResponseDto>> CreateSkill([FromBody] CreateSkillDto createSkillDto)
    {
        var result = await _skillService.CreateSkillAsync(createSkillDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetSkillById), new { id = ((SkillDto)result.Data!).Id }, result);
    }

    /// <summary>
    /// Updates an existing skill (Admin only)
    /// </summary>
    /// <param name="id">Skill ID</param>
    /// <param name="updateSkillDto">Skill update data</param>
    /// <returns>Service response with updated skill data</returns>
    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    // SECURITY: Requires authentication - removed [AllowAnonymous] for production safety
    public async Task<ActionResult<ServiceResponseDto>> UpdateSkill(Guid id, [FromBody] UpdateSkillDto updateSkillDto)
    {
        var result = await _skillService.UpdateSkillAsync(id, updateSkillDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Deletes a skill (Admin only)
    /// </summary>
    /// <param name="id">Skill ID</param>
    /// <returns>Service response</returns>
    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    // SECURITY: Requires authentication - removed [AllowAnonymous] for production safety
    public async Task<ActionResult<ServiceResponseDto>> DeleteSkill(Guid id)
    {
        var result = await _skillService.DeleteSkillAsync(id);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets a skill by ID
    /// </summary>
    /// <param name="id">Skill ID</param>
    /// <returns>Skill data</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<SkillDto>> GetSkillById(Guid id)
    {
        var skill = await _skillService.GetSkillByIdAsync(id);

        if (skill == null)
        {
            return NotFound(new { Message = "Skill not found" });
        }

        return Ok(skill);
    }

    /// <summary>
    /// Gets a skill by name
    /// </summary>
    /// <param name="name">Skill name</param>
    /// <returns>Skill data</returns>
    [HttpGet("by-name/{name}")]
    [AllowAnonymous]
    public async Task<ActionResult<SkillDto>> GetSkillByName(string name)
    {
        var skill = await _skillService.GetSkillByNameAsync(name);

        if (skill == null)
        {
            return NotFound(new { Message = "Skill not found" });
        }

        return Ok(skill);
    }

    /// <summary>
    /// Gets skills with filtering and pagination
    /// </summary>
    /// <param name="category">Filter by category</param>
    /// <param name="take">Number of results to take</param>
    /// <param name="skip">Number of results to skip</param>
    /// <param name="searchTerm">Search query for skill name or description</param>
    /// <returns>Paginated list of skills</returns>
    [HttpGet]
    [AllowAnonymous]
    // BUG-004 FIX: Added searchTerm parameter for skill search functionality
    public async Task<ActionResult<object>> GetSkills(
        [FromQuery] string? category = null,
        [FromQuery] int take = 10,
        [FromQuery] int skip = 0,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var searchDto = new SkillSearchDto
            {
                Category = category,
                Take = take,
                Skip = skip,
                Query = searchTerm // BUG-004 FIX: Map searchTerm to Query for DTO compatibility
            };

            var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

            // Add pagination headers
            var pageNumber = (skip / take) + 1;
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Size", take.ToString());
            Response.Headers.Append("X-Page-Number", pageNumber.ToString());
            Response.Headers.Append("X-Total-Pages",
                Math.Ceiling((double)totalCount / take).ToString());

            return Ok(new
            {
                Skills = skills,
                TotalCount = totalCount,
                Page = pageNumber,
                PageSize = take,
                TotalPages = (int)Math.Ceiling((double)totalCount / take)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while fetching skills",
                error = ex.Message,
                type = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// Searches skills with filtering and pagination
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>Paginated list of skills</returns>
    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> SearchSkills([FromBody] SkillSearchDto searchDto)
    {
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Add pagination headers
        var pageNumber = (searchDto.Skip / searchDto.Take) + 1;
        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page-Size", searchDto.Take.ToString());
        Response.Headers.Append("X-Page-Number", pageNumber.ToString());
        Response.Headers.Append("X-Total-Pages",
            Math.Ceiling((double)totalCount / searchDto.Take).ToString());

        return Ok(new
        {
            Skills = skills,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = searchDto.Take,
            TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.Take)
        });
    }

    /// <summary>
    /// Gets all skill categories with counts
    /// </summary>
    /// <returns>List of skill categories</returns>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SkillCategoryDto>>> GetSkillCategories()
    {
        var categories = await _skillService.GetSkillCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Adds a skill to the current user's profile
    /// </summary>
    /// <param name="addUserSkillDto">User skill data</param>
    /// <returns>Service response with created user skill</returns>
    [HttpPost("my-skills")]
    [ValidateAntiForgeryToken]
    // [EnableRateLimiting("ProfileUpdatePolicy")] // Disabled for testing
    public async Task<ActionResult<ServiceResponseDto>> AddUserSkill([FromBody] AddUserSkillDto addUserSkillDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _skillService.AddUserSkillAsync(userId.Value, addUserSkillDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetUserSkill), new { userSkillId = ((UserSkillDto)result.Data!).Id }, result);
    }

    /// <summary>
    /// Updates a user skill in the current user's profile
    /// </summary>
    /// <param name="userSkillId">User skill ID</param>
    /// <param name="updateUserSkillDto">User skill update data</param>
    /// <returns>Service response with updated user skill</returns>
    [HttpPut("my-skills/{userSkillId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ServiceResponseDto>> UpdateUserSkill(Guid userSkillId, [FromBody] UpdateUserSkillDto updateUserSkillDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _skillService.UpdateUserSkillAsync(userId.Value, userSkillId, updateUserSkillDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Removes a skill from the current user's profile
    /// </summary>
    /// <param name="userSkillId">User skill ID</param>
    /// <returns>Service response</returns>
    [HttpDelete("my-skills/{userSkillId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ServiceResponseDto>> RemoveUserSkill(Guid userSkillId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _skillService.RemoveUserSkillAsync(userId.Value, userSkillId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific user skill for the current user
    /// </summary>
    /// <param name="userSkillId">User skill ID</param>
    /// <returns>User skill data</returns>
    [HttpGet("my-skills/{userSkillId:guid}")]
    public async Task<ActionResult<UserSkillDto>> GetUserSkill(Guid userSkillId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var userSkill = await _skillService.GetUserSkillAsync(userId.Value, userSkillId);

        if (userSkill == null)
        {
            return NotFound(new { Message = "User skill not found" });
        }

        return Ok(userSkill);
    }

    /// <summary>
    /// Gets all skills for the current user
    /// </summary>
    /// <param name="includeEndorsements">Whether to include endorsements</param>
    /// <returns>List of user skills</returns>
    [HttpGet("my-skills")]
    public async Task<ActionResult<List<UserSkillDto>>> GetMySkills([FromQuery] bool includeEndorsements = false)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var userSkills = await _skillService.GetUserSkillsAsync(userId.Value, visibleOnly: false, includeEndorsements);
        return Ok(userSkills);
    }

    /// <summary>
    /// Gets skills for a specific user (public profile view)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeEndorsements">Whether to include endorsements</param>
    /// <param name="visibleOnly">Whether to include only visible skills</param>
    /// <returns>List of user skills</returns>
    [HttpGet("user/{userId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<UserSkillDto>>> GetUserSkills(Guid userId, [FromQuery] bool includeEndorsements = false, [FromQuery] bool visibleOnly = true)
    {
        var userSkills = await _skillService.GetUserSkillsAsync(userId, visibleOnly, includeEndorsements);
        return Ok(userSkills);
    }

    /// <summary>
    /// Gets skills for a specific user (alternative route for compatibility)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeEndorsements">Whether to include endorsements</param>
    /// <param name="visibleOnly">Whether to include only visible skills</param>
    /// <returns>List of user skills</returns>
    [HttpGet("users/{userId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<UserSkillDto>>> GetUserSkillsAlt(Guid userId, [FromQuery] bool includeEndorsements = false, [FromQuery] bool visibleOnly = true)
    {
        var userSkills = await _skillService.GetUserSkillsAsync(userId, visibleOnly, includeEndorsements);
        return Ok(userSkills);
    }

    /// <summary>
    /// Searches user skills with filtering and pagination
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>Paginated list of user skills</returns>
    [HttpPost("user-skills/search")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<object>> SearchUserSkills([FromBody] UserSkillSearchDto searchDto)
    {
        // If no user ID provided, use current user for authenticated requests
        if (!searchDto.UserId.HasValue && User.Identity?.IsAuthenticated == true)
        {
            searchDto.UserId = GetCurrentUserId();
        }

        var (userSkills, totalCount) = await _skillService.SearchUserSkillsAsync(searchDto);

        // Add pagination headers
        var pageNumber = (searchDto.Skip / searchDto.Take) + 1;
        Response.Headers.Append("X-Total-Count", totalCount.ToString());
        Response.Headers.Append("X-Page-Size", searchDto.Take.ToString());
        Response.Headers.Append("X-Page-Number", pageNumber.ToString());
        Response.Headers.Append("X-Total-Pages",
            Math.Ceiling((double)totalCount / searchDto.Take).ToString());

        return Ok(new
        {
            UserSkills = userSkills,
            TotalCount = totalCount,
            Page = pageNumber,
            PageSize = searchDto.Take,
            TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.Take)
        });
    }

    /// <summary>
    /// Adds a skill to a specific user's profile (admin/test compatibility endpoint)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="addUserSkillDto">User skill data</param>
    /// <returns>Service response with created user skill</returns>
    [HttpPost("users/{userId:guid}")]
    [ValidateAntiForgeryToken]
    // SECURITY: Requires authentication - removed [AllowAnonymous] for production safety
    // [EnableRateLimiting("ProfileUpdatePolicy")] // Disabled for testing
    public async Task<ActionResult<UserSkillDto>> AddUserSkillToUser(Guid userId, [FromBody] AddUserSkillDto addUserSkillDto)
    {
        if (!CanActForUser(userId))
        {
            return Forbid();
        }

        var result = await _skillService.AddUserSkillAsync(userId, addUserSkillDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetUserSkills), new { userId }, result.Data);
    }

    /// <summary>
    /// Updates a user skill for a specific user (admin/test compatibility endpoint)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="userSkillId">User skill ID</param>
    /// <param name="updateUserSkillDto">User skill update data</param>
    /// <returns>Service response with updated user skill</returns>
    [HttpPut("users/{userId:guid}/{userSkillId:guid}")]
    [ValidateAntiForgeryToken]
    // SECURITY: Requires authentication - removed [AllowAnonymous] for production safety
    public async Task<ActionResult<UserSkillDto>> UpdateUserSkillForUser(Guid userId, Guid userSkillId, [FromBody] UpdateUserSkillDto updateUserSkillDto)
    {
        if (!CanActForUser(userId))
        {
            return Forbid();
        }

        var result = await _skillService.UpdateUserSkillAsync(userId, userSkillId, updateUserSkillDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Removes a skill from a specific user's profile (admin/test compatibility endpoint)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="userSkillId">User skill ID</param>
    /// <returns>Service response</returns>
    [HttpDelete("users/{userId:guid}/{userSkillId:guid}")]
    [ValidateAntiForgeryToken]
    // SECURITY: Requires authentication - removed [AllowAnonymous] for production safety
    public async Task<ActionResult> RemoveUserSkillFromUser(Guid userId, Guid userSkillId)
    {
        if (!CanActForUser(userId))
        {
            return Forbid();
        }

        var result = await _skillService.RemoveUserSkillAsync(userId, userSkillId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return NoContent();
    }

    /// <summary>
    /// Initializes system skills (Admin only)
    /// </summary>
    /// <returns>Success response</returns>
    [HttpPost("initialize-system")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<ActionResult> InitializeSystemSkills()
    {
        await _skillService.InitializeSystemSkillsAsync();
        return Ok(new { Message = "System skills initialized successfully" });
    }

    /// <summary>
    /// Creates a skill endorsement
    /// </summary>
    /// <param name="createEndorsementDto">Endorsement data</param>
    /// <returns>Service response with endorsement data</returns>
    [HttpPost("endorsements")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("SkillEndorsementPolicy")]
    public async Task<ActionResult<ServiceResponseDto>> CreateSkillEndorsement([FromBody] CreateSkillEndorsementDto createEndorsementDto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _skillService.CreateSkillEndorsementAsync(userId.Value, createEndorsementDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetSkillEndorsements), new { userSkillId = createEndorsementDto.UserSkillId }, result);
    }

    /// <summary>
    /// Creates a skill endorsement for a specific endorser (admin/test compatibility endpoint)
    /// </summary>
    /// <param name="endorserId">Endorser user ID</param>
    /// <param name="createEndorsementDto">Endorsement data</param>
    /// <returns>Service response with endorsement data</returns>
    [HttpPost("endorsements/{endorserId:guid}")]
    [ValidateAntiForgeryToken]
    // SECURITY: Requires authentication - removed [AllowAnonymous] for production safety
    [EnableRateLimiting("SkillEndorsementPolicy")]
    public async Task<ActionResult<SkillEndorsementDto>> CreateSkillEndorsementForUser(Guid endorserId, [FromBody] CreateSkillEndorsementDto createEndorsementDto)
    {
        if (!CanActForUser(endorserId))
        {
            return Forbid();
        }

        var result = await _skillService.CreateSkillEndorsementAsync(endorserId, createEndorsementDto);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Created($"/api/skills/endorsements/{createEndorsementDto.UserSkillId}", result.Data);
    }

    /// <summary>
    /// Removes a skill endorsement
    /// </summary>
    /// <param name="endorsementId">Endorsement ID</param>
    /// <returns>Service response</returns>
    [HttpDelete("endorsements/{endorsementId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ServiceResponseDto>> RemoveSkillEndorsement(Guid endorsementId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new ServiceResponseDto
            {
                Success = false,
                Message = "User not authenticated"
            });
        }

        var result = await _skillService.RemoveSkillEndorsementAsync(userId.Value, endorsementId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets endorsements for a specific user skill
    /// </summary>
    /// <param name="userSkillId">User skill ID</param>
    /// <returns>List of skill endorsements</returns>
    [HttpGet("endorsements/{userSkillId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SkillEndorsementDto>>> GetSkillEndorsements(Guid userSkillId)
    {
        var endorsements = await _skillService.GetSkillEndorsementsAsync(userSkillId);
        return Ok(endorsements);
    }

    /// <summary>
    /// Checks if the current user can endorse a specific skill
    /// </summary>
    /// <param name="userSkillId">User skill ID</param>
    /// <returns>Whether the user can endorse the skill</returns>
    [HttpGet("endorsements/{userSkillId:guid}/can-endorse")]
    public async Task<ActionResult<object>> CanEndorseSkill(Guid userSkillId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var canEndorse = await _skillService.CanEndorseSkillAsync(userId.Value, userSkillId);
        return Ok(new { CanEndorse = canEndorse });
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private bool CanActForUser(Guid targetUserId)
    {
        var currentUserId = GetCurrentUserId();
        return currentUserId == targetUserId || User.IsInRole(RoleNames.Admin);
    }
}
