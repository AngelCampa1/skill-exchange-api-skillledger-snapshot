namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Interface for idempotency service to prevent duplicate operations
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Checks if an operation has already been processed
    /// </summary>
    /// <param name="operationKey">Unique operation identifier (format: entity:action:entityId:userId)</param>
    /// <returns>True if this is a duplicate operation, false if it's new</returns>
    Task<bool> IsDuplicateOperationAsync(string operationKey);

    /// <summary>
    /// Marks an operation as completed to prevent duplicate processing
    /// </summary>
    /// <param name="operationKey">Unique operation identifier (format: entity:action:entityId:userId)</param>
    Task MarkOperationCompletedAsync(string operationKey);
}
