namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for CSRF token response
/// </summary>
public class CsrfTokenDto
{
    /// <summary>
    /// The CSRF token value
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The header name to use for the token
    /// </summary>
    public string HeaderName { get; set; } = "X-CSRF-TOKEN";
}