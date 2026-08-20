using SkillLedger.Core.DTOs.TestData;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service for seeding test data into the database for E2E testing
/// </summary>
public interface ITestDataSeederService
{
    /// <summary>
    /// Seeds the entire database with comprehensive test data
    /// </summary>
    /// <param name="fullSeed">If true, seeds all data. If false, seeds only basic data</param>
    /// <returns>Results of the seeding operation</returns>
    Task<SeedResult> SeedAsync(bool fullSeed = true);

    /// <summary>
    /// Cleans all test data from the database
    /// </summary>
    /// <returns>Task representing the cleanup operation</returns>
    Task CleanTestDataAsync();

    /// <summary>
    /// Seeds only user-related data (users, profiles, wallets, skills, experiences)
    /// </summary>
    /// <returns>Results of the user seeding operation</returns>
    Task<SeedResult> SeedUsersAsync();

    /// <summary>
    /// Seeds only project-related data (projects, deliverables, applications)
    /// </summary>
    /// <returns>Results of the project seeding operation</returns>
    Task<SeedResult> SeedProjectsAsync();

    /// <summary>
    /// Seeds only financial data (escrow, transactions, transfers)
    /// </summary>
    /// <returns>Results of the financial seeding operation</returns>
    Task<SeedResult> SeedFinancialDataAsync();

    /// <summary>
    /// Seeds only collaboration data (workspaces, messages, documents)
    /// </summary>
    /// <returns>Results of the collaboration seeding operation</returns>
    Task<SeedResult> SeedCollaborationDataAsync();

    /// <summary>
    /// Seeds only reputation data (reviews, scores, endorsements)
    /// </summary>
    /// <returns>Results of the reputation seeding operation</returns>
    Task<SeedResult> SeedReputationDataAsync();
}
