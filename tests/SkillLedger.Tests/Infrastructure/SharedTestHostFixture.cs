using Xunit;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// xUnit collection fixture that creates a single shared WebApplicationFactory instance
/// for all integration tests in the collection, preventing the 5-minute IHost timeout issue.
/// Implements IAsyncLifetime for proper async initialization.
/// </summary>
public class SharedTestHostFixture : IAsyncLifetime
{
    private SharedWebApplicationFactory? _factory;

    /// <summary>
    /// Gets the shared WebApplicationFactory instance.
    /// This factory is created once and reused across all tests in the collection.
    /// </summary>
    public SharedWebApplicationFactory Factory => _factory
        ?? throw new InvalidOperationException("Factory not initialized. Ensure InitializeAsync has been called.");

    /// <summary>
    /// Initializes the shared WebApplicationFactory asynchronously.
    /// This is called once by xUnit before any tests in the collection run.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            System.Console.WriteLine("SharedTestHostFixture: Starting initialization...");
            _factory = new SharedWebApplicationFactory();
            System.Console.WriteLine("SharedTestHostFixture: Factory created");

            // CRITICAL FIX: Force the IHost to be created now, during fixture initialization
            // This prevents each test from triggering a rebuild when calling CreateClient()
            // Accessing the Server property forces WebApplicationFactory to build the IHost
            System.Console.WriteLine("SharedTestHostFixture: Accessing Server property to trigger IHost build...");
            _ = _factory.Server;
            System.Console.WriteLine("SharedTestHostFixture: Server property accessed successfully");

            // Give the host a moment to fully initialize
            await Task.Delay(100);

            System.Console.WriteLine("SharedTestHostFixture: Factory initialized successfully");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"SharedTestHostFixture: INITIALIZATION FAILED: {ex.GetType().Name}: {ex.Message}");
            System.Console.WriteLine($"StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    /// <summary>
    /// Disposes the shared WebApplicationFactory asynchronously.
    /// This is called once by xUnit after all tests in the collection have completed.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_factory != null)
        {
            _factory.Dispose();
            System.Diagnostics.Debug.WriteLine("SharedTestHostFixture: Factory disposed");
        }

        await Task.CompletedTask;
    }
}
