namespace SkillLedger.Core.Enums;

/// <summary>
/// Status values for provider selections
/// </summary>
public enum ProviderSelectionStatus
{
    /// <summary>
    /// Provider has been selected but contract is pending
    /// </summary>
    Selected = 0,

    /// <summary>
    /// Contract has been signed by both parties
    /// </summary>
    ContractSigned = 1,

    /// <summary>
    /// Work has started on the project
    /// </summary>
    WorkInProgress = 2,

    /// <summary>
    /// Project has been completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Selection was cancelled before work started
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Project was terminated early
    /// </summary>
    Terminated = 5,

    /// <summary>
    /// Dispute raised, selection on hold
    /// </summary>
    Disputed = 6
}