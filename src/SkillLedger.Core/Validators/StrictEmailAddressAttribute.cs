using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SkillLedger.Core.Validators;

/// <summary>
/// Strict email validation attribute that enforces more rigorous email format requirements
/// than the default EmailAddressAttribute
/// </summary>
public class StrictEmailAddressAttribute : ValidationAttribute
{
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9!#$%&'*+/=?^_`{|}~.-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override bool IsValid(object? value)
    {
        if (value == null || value is not string email)
        {
            return true; // Let [Required] handle null/empty validation
        }

        // Basic length check
        if (email.Length > 254) // RFC 5321 limit
        {
            return false;
        }

        // Check for whitespace (not allowed)
        if (email.Contains(' ') || email.Contains('\t') || email.Contains('\n'))
        {
            return false;
        }

        // Must contain exactly one @ symbol
        var atCount = email.Count(c => c == '@');
        if (atCount != 1)
        {
            return false;
        }

        // Split into local and domain parts
        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            return false;
        }

        var localPart = parts[0];
        var domainPart = parts[1];

        // Local part validation
        if (string.IsNullOrEmpty(localPart) || localPart.Length > 64)
        {
            return false;
        }

        // Domain part validation
        if (string.IsNullOrEmpty(domainPart) || domainPart.Length > 253)
        {
            return false;
        }

        // Domain must contain at least one dot and have a valid TLD
        if (!domainPart.Contains('.') || domainPart.StartsWith('.') || domainPart.EndsWith('.'))
        {
            return false;
        }

        // Check for consecutive dots
        if (domainPart.Contains(".."))
        {
            return false;
        }

        // Use regex for final validation
        return EmailRegex.IsMatch(email);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The {name} field is not a valid email address.";
    }
}