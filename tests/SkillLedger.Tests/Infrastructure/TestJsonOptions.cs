using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Provides shared JSON serialization options for integration tests.
/// Configured to match the API's JSON configuration for proper deserialization.
/// </summary>
public static class TestJsonOptions
{
    /// <summary>
    /// Default JsonSerializerOptions configured to match API settings.
    /// Includes string enum converter for proper enum deserialization.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = CreateDefaultOptions();

    /// <summary>
    /// Creates a new instance of JsonSerializerOptions with default test configuration.
    /// Use this when you need a mutable options instance.
    /// </summary>
    public static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Add JsonStringEnumConverter to handle string enum values from API
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    /// <summary>
    /// Deserialize JSON content using the default test options.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Default);
    }

    /// <summary>
    /// Serialize object using the default test options.
    /// </summary>
    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Default);
    }
}
