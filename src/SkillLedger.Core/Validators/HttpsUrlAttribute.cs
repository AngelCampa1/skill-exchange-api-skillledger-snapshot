using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Validators;

/// <summary>
/// Validates that a URL uses HTTPS scheme only (no HTTP allowed for security)
/// </summary>
public class HttpsUrlAttribute : ValidationAttribute
{
    public HttpsUrlAttribute()
    {
        ErrorMessage = "URL must use HTTPS protocol for security. HTTP URLs are not allowed.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            // Allow null/empty values - use [Required] for mandatory fields
            return ValidationResult.Success;
        }

        var urlString = value.ToString()!;

        // Check if URL is valid
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
        {
            return new ValidationResult("Invalid URL format.");
        }

        // Check if scheme is HTTPS
        if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Success;
        }

        // Reject HTTP and any other non-HTTPS schemes
        return new ValidationResult(ErrorMessage ?? "URL must use HTTPS protocol.");
    }
}
