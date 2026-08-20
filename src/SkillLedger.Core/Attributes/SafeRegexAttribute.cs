using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SkillLedger.Core.Attributes;

/// <summary>
/// VULN-018 FIX: Validates that a regex pattern is safe and won't cause ReDoS attacks
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class SafeRegexAttribute : ValidationAttribute
{
    // Whitelist of allowed common regex patterns for form validation
    private static readonly HashSet<string> AllowedPatterns = new()
    {
        @"^\d+$",                           // Numbers only
        @"^[a-zA-Z]+$",                     // Letters only
        @"^[a-zA-Z0-9]+$",                  // Alphanumeric
        @"^[a-zA-Z\s]+$",                   // Letters and spaces
        @"^\w+@\w+\.\w+$",                  // Simple email
        @"^\(\d{3}\)\s?\d{3}-\d{4}$",      // Phone number
        @"^\d{5}(-\d{4})?$",               // ZIP code
        @"^https?://\S+$",                  // URL
        @"^\d{4}-\d{2}-\d{2}$",            // Date YYYY-MM-DD
        @"^[A-Z]{2,3}\d{2,4}$",            // Product code
    };

    // Dangerous patterns that indicate potential ReDoS
    private static readonly string[] DangerousPatterns = new[]
    {
        @"(\w+)+",      // Nested quantifiers
        @"(\w*)*",      // Nested quantifiers
        @"(\w+)*",      // Nested quantifiers
        @"(\w*)+",      // Nested quantifiers
        @"(a+)+",       // Classic ReDoS pattern
        @"(a*)*",       // Classic ReDoS pattern
        @"(a|a)*",      // Alternation with overlap
        @"(a|ab)*",     // Alternation with overlap
        @"(\d+|\d+\d+)", // Overlapping alternation
    };

    public SafeRegexAttribute()
    {
        ErrorMessage = "The regex pattern is not allowed. Please use a simple, safe pattern or contact support for custom validation.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;  // Allow null/empty (use [Required] separately)
        }

        var pattern = value.ToString()!;

        // Check if pattern is in the whitelist
        if (AllowedPatterns.Contains(pattern))
        {
            return ValidationResult.Success;
        }

        // Check for dangerous patterns
        foreach (var dangerous in DangerousPatterns)
        {
            if (pattern.Contains(dangerous, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult($"The regex pattern contains a potentially dangerous construct: {dangerous}. This could cause performance issues.");
            }
        }

        // Additional checks for complexity
        if (pattern.Length > 200)
        {
            return new ValidationResult("The regex pattern is too long. Maximum length is 200 characters.");
        }

        // Count nested quantifiers (*, +, {n,m})
        var quantifierCount = Regex.Matches(pattern, @"[*+{]\d*,?\d*}?").Count;
        if (quantifierCount > 3)
        {
            return new ValidationResult("The regex pattern has too many quantifiers. Limit is 3 for safety.");
        }

        // Try to compile the regex with a timeout to catch catastrophic backtracking
        try
        {
            var testRegex = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));

            // Test with a challenging input string
            var testInput = new string('a', 30);
            var testTask = Task.Run(() =>
            {
                try
                {
                    testRegex.IsMatch(testInput);
                    return true;
                }
                catch
                {
                    return false;
                }
            });

            // BUG-HIGH-014 FIX: Replace .Wait() with WaitAsync for better async handling
            // Note: This is a synchronous validation method required by the ValidationAttribute framework.
            // We cannot make IsValid async, so we must block here. Using WaitAsync + GetAwaiter().GetResult()
            // is the recommended pattern as it properly handles timeouts and unwraps AggregateException.
            var timeoutTask = testTask.WaitAsync(TimeSpan.FromMilliseconds(500));
            bool taskCompleted;
            try
            {
                taskCompleted = timeoutTask.GetAwaiter().GetResult();
            }
            catch (TimeoutException)
            {
                return new ValidationResult("The regex pattern takes too long to execute and may cause performance issues.");
            }

            if (!taskCompleted)
            {
                return new ValidationResult("The regex pattern takes too long to execute and may cause performance issues.");
            }

            // Get the result with proper exception unwrapping
            bool testResult;
            try
            {
                testResult = testTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return new ValidationResult($"Regex validation test failed: {ex.Message}");
            }

            if (!testResult)
            {
                return new ValidationResult("The regex pattern failed validation testing.");
            }
        }
        catch (ArgumentException ex)
        {
            return new ValidationResult($"Invalid regex pattern: {ex.Message}");
        }
        catch (RegexMatchTimeoutException)
        {
            return new ValidationResult("The regex pattern takes too long to execute (timeout). This could indicate a ReDoS vulnerability.");
        }

        // If not in whitelist but passes all safety checks, allow but log warning
        // In production, you might want to log this for review
        return ValidationResult.Success;
    }
}
