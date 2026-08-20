namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Test categories for selective test execution and filtering
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Fast unit tests that should run in under 100ms each
    /// </summary>
    public const string Fast = "Fast";

    /// <summary>
    /// Integration tests that involve database operations
    /// </summary>
    public const string Integration = "Integration";

    /// <summary>
    /// Performance tests that measure timing and throughput
    /// </summary>
    public const string Performance = "Performance";

    /// <summary>
    /// Security tests that validate authentication, authorization, and security features
    /// </summary>
    public const string Security = "Security";

    /// <summary>
    /// API tests that make HTTP requests to endpoints
    /// </summary>
    public const string Api = "Api";

    /// <summary>
    /// Database tests that require database setup and cleanup
    /// </summary>
    public const string Database = "Database";

    /// <summary>
    /// File operation tests that involve file system operations
    /// </summary>
    public const string FileSystem = "FileSystem";

    /// <summary>
    /// Messaging tests that involve SignalR or real-time communication
    /// </summary>
    public const string Messaging = "Messaging";

    /// <summary>
    /// Slow tests that may take more than 1 second to execute
    /// </summary>
    public const string Slow = "Slow";

    /// <summary>
    /// Core business logic tests that should always pass
    /// </summary>
    public const string Core = "Core";

    /// <summary>
    /// Tests that require external services or network access
    /// </summary>
    public const string External = "External";

    /// <summary>
    /// Reputation system specific tests
    /// </summary>
    public const string Reputation = "Reputation";
}

/// <summary>
/// Attribute for marking test categories
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class TestCategoryAttribute : Attribute
{
    public string Category { get; }

    public TestCategoryAttribute(string category)
    {
        Category = category;
    }
}

/// <summary>
/// Trait attribute for xUnit test filtering
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class TestTraitAttribute : Attribute
{
    public string Name { get; }
    public string Value { get; }

    public TestTraitAttribute(string name, string value)
    {
        Name = name;
        Value = value;
    }
}

/// <summary>
/// Convenience attributes for common test categories
/// </summary>
public class FastTestAttribute : TestTraitAttribute
{
    public FastTestAttribute() : base("Category", TestCategories.Fast) { }
}

public class IntegrationTestAttribute : TestTraitAttribute
{
    public IntegrationTestAttribute() : base("Category", TestCategories.Integration) { }
}

public class PerformanceTestAttribute : TestTraitAttribute
{
    public PerformanceTestAttribute() : base("Category", TestCategories.Performance) { }
}

public class SecurityTestAttribute : TestTraitAttribute
{
    public SecurityTestAttribute() : base("Category", TestCategories.Security) { }
}

public class ApiTestAttribute : TestTraitAttribute
{
    public ApiTestAttribute() : base("Category", TestCategories.Api) { }
}

public class SlowTestAttribute : TestTraitAttribute
{
    public SlowTestAttribute() : base("Category", TestCategories.Slow) { }
}

public class CoreTestAttribute : TestTraitAttribute
{
    public CoreTestAttribute() : base("Category", TestCategories.Core) { }
}

public class ReputationTestAttribute : TestTraitAttribute
{
    public ReputationTestAttribute() : base("Category", TestCategories.Reputation) { }
}

public class UnitTestAttribute : TestTraitAttribute
{
    public UnitTestAttribute() : base("Category", "Unit") { }
}

public class FinancialTestAttribute : TestTraitAttribute
{
    public FinancialTestAttribute() : base("Category", "Financial") { }
}

public class DocumentTestAttribute : TestTraitAttribute
{
    public DocumentTestAttribute() : base("Category", "Document") { }
}

public class MessagingTestAttribute : TestTraitAttribute
{
    public MessagingTestAttribute() : base("Category", TestCategories.Messaging) { }
}

public class ValidationTestAttribute : TestTraitAttribute
{
    public ValidationTestAttribute() : base("Category", "Validation") { }
}

public class EndToEndTestAttribute : TestTraitAttribute
{
    public EndToEndTestAttribute() : base("Category", "EndToEnd") { }
}

public class BDDTestAttribute : TestTraitAttribute
{
    public BDDTestAttribute() : base("Category", "BDD") { }
}

public class ConfigurationTestAttribute : TestTraitAttribute
{
    public ConfigurationTestAttribute() : base("Category", "Configuration") { }
}

public class FileManagementTestAttribute : TestTraitAttribute
{
    public FileManagementTestAttribute() : base("Category", "FileManagement") { }
}

public class StorageTestAttribute : TestTraitAttribute
{
    public StorageTestAttribute() : base("Category", "Storage") { }
}

public class RealTimeTestAttribute : TestTraitAttribute
{
    public RealTimeTestAttribute() : base("Category", "RealTime") { }
}