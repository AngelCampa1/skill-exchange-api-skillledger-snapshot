using System.Text.RegularExpressions;

namespace SkillLedger.Core.Validators;

public static class PasswordValidator
{
    private static readonly Regex UppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercaseRegex = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex SpecialCharRegex = new(@"[^a-zA-Z0-9]", RegexOptions.Compiled);

    /// <summary>
    /// Validates password against security requirements
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Validation result with errors if any</returns>
    public static PasswordValidationResult ValidatePassword(string password)
    {
        var result = new PasswordValidationResult(password) { IsValid = true };

        if (string.IsNullOrWhiteSpace(password))
        {
            result.IsValid = false;
            result.Errors.Add("Password is required");
            return result;
        }

        // Check minimum length (12 characters)
        if (password.Length < 12)
        {
            result.IsValid = false;
            result.Errors.Add("Password must be at least 12 characters long");
        }

        // Check maximum length to prevent DoS attacks
        if (password.Length > 128)
        {
            result.IsValid = false;
            result.Errors.Add("Password cannot exceed 128 characters");
        }

        // Check for uppercase letter
        if (!UppercaseRegex.IsMatch(password))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one uppercase letter");
        }

        // Check for lowercase letter
        if (!LowercaseRegex.IsMatch(password))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one lowercase letter");
        }

        // Check for digit
        if (!DigitRegex.IsMatch(password))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one number");
        }

        // Check for special character
        if (!SpecialCharRegex.IsMatch(password))
        {
            result.IsValid = false;
            result.Errors.Add("Password must contain at least one special character");
        }

        // Check for common weak patterns
        if (ContainsCommonWeakPatterns(password))
        {
            result.IsValid = false;
            result.Errors.Add("Password contains common weak patterns");
        }

        // SECURITY FIX: Enforce minimum entropy requirement (40 bits minimum)
        // 40 bits = 1 trillion possible combinations (adequate for user passwords)
        var entropy = CalculateEntropy(password);
        if (entropy < 40)
        {
            result.IsValid = false;
            result.Errors.Add($"Password is too predictable (entropy: {entropy:F1} bits, minimum: 40 bits). Use more varied characters.");
        }

        return result;
    }

    /// <summary>
    /// Calculates password strength score (0-100) based on entropy, diversity, and patterns
    /// </summary>
    /// <param name="password">Password to evaluate</param>
    /// <returns>Strength score from 0 (weakest) to 100 (strongest)</returns>
    public static int CalculateStrengthScore(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        int score = 0;

        // SECURITY FIX: Add true entropy calculation (up to 35 points)
        var entropyBits = CalculateEntropy(password);
        // 40 bits of entropy = weak, 60 bits = good, 80+ bits = strong
        score += (int)Math.Min(entropyBits / 2.3, 35); // Scale to 0-35 points

        // Length scoring (up to 25 points)
        score += Math.Min(password.Length * 2, 25);

        // Character diversity (up to 30 points)
        if (UppercaseRegex.IsMatch(password)) score += 8;
        if (LowercaseRegex.IsMatch(password)) score += 8;
        if (DigitRegex.IsMatch(password)) score += 7;
        if (SpecialCharRegex.IsMatch(password)) score += 7;

        // Unique character ratio (up to 10 points)
        var uniqueRatio = password.Distinct().Count() / (double)password.Length;
        score += (int)(uniqueRatio * 10);

        // Penalize common weak patterns (up to -30 points)
        if (ContainsCommonWeakPatterns(password))
            score -= 30;

        // Ensure score is between 0 and 100
        return Math.Max(0, Math.Min(score, 100));
    }

    /// <summary>
    /// SECURITY FIX: Calculates Shannon entropy to measure password unpredictability
    /// Higher entropy = harder to crack via brute force
    /// </summary>
    /// <param name="password">Password to analyze</param>
    /// <returns>Entropy in bits (higher is better)</returns>
    public static double CalculateEntropy(string password)
    {
        if (string.IsNullOrEmpty(password))
            return 0;

        // Calculate character frequency distribution
        var charFrequency = new Dictionary<char, int>();
        foreach (var c in password)
        {
            if (charFrequency.ContainsKey(c))
                charFrequency[c]++;
            else
                charFrequency[c] = 1;
        }

        // Calculate Shannon entropy: H = -Σ(p(x) * log2(p(x)))
        double entropy = 0;
        int passwordLength = password.Length;

        foreach (var freq in charFrequency.Values)
        {
            var probability = (double)freq / passwordLength;
            entropy -= probability * Math.Log2(probability);
        }

        // Multiply by password length to get total entropy in bits
        return entropy * passwordLength;
    }

    private static bool ContainsCommonWeakPatterns(string password)
    {
        var lowerPassword = password.ToLower();

        // Common weak patterns
        var weakPatterns = new[]
        {
            "password", "123456", "qwerty", "abc123", "admin",
            "login", "welcome", "master", "secret", "user"
        };

        // Sequential patterns
        var sequentialPatterns = new[]
        {
            "abcd", "1234", "qwer", "asdf", "zxcv"
        };

        // Check for weak patterns
        if (weakPatterns.Any(pattern => lowerPassword.Contains(pattern)))
            return true;

        // Check for sequential patterns
        if (sequentialPatterns.Any(pattern => lowerPassword.Contains(pattern)))
            return true;

        // Check for repeated characters (more than 3 in a row)
        for (int i = 0; i < password.Length - 3; i++)
        {
            if (password[i] == password[i + 1] &&
                password[i] == password[i + 2] &&
                password[i] == password[i + 3])
                return true;
        }

        return false;
    }
}

public class PasswordValidationResult
{
    private readonly string _password;

    public PasswordValidationResult(string password = "")
    {
        _password = password;
    }

    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Password strength score (0-100)
    /// </summary>
    public int StrengthScore => PasswordValidator.CalculateStrengthScore(_password);
}