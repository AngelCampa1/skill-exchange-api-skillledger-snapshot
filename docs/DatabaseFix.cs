using Microsoft.Data.SqlClient;

await RunSqlScriptAsync();

static async Task RunSqlScriptAsync()
{
    // BUG-MED-002 FIX: Use environment variable or configuration instead of hardcoded connection string
    // For development utility scripts, read from environment variable or default to local dev
    var connectionString = Environment.GetEnvironmentVariable("SKILLLEDGER_CONNECTION_STRING")
        ?? "Server=localhost\\SQLEXPRESS01;Database=SkillLedgerDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;";

    try
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sqlCommand = @"
            IF NOT EXISTS (
                SELECT 1
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'RefreshTokens'
                AND COLUMN_NAME = 'AutoRefreshAttempts'
            )
            BEGIN
                ALTER TABLE RefreshTokens
                ADD AutoRefreshAttempts INT NOT NULL DEFAULT 0;

                PRINT 'AutoRefreshAttempts column added successfully to RefreshTokens table.';
            END
            ELSE
            BEGIN
                PRINT 'AutoRefreshAttempts column already exists in RefreshTokens table.';
            END";

        using var command = new SqlCommand(sqlCommand, connection);
        var result = await command.ExecuteNonQueryAsync();

        Console.WriteLine("SQL script executed successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error executing SQL script: {ex.Message}");
        throw;
    }
}