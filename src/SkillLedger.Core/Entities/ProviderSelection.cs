using System.ComponentModel.DataAnnotations;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Represents a provider selection for a project by a client
/// </summary>
public class ProviderSelection
{
    public ProviderSelection()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the provider selection
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the project for which provider is selected
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Reference to the selected service provider
    /// </summary>
    public Guid SelectedProviderId { get; set; }

    /// <summary>
    /// Reference to the project application that was selected
    /// </summary>
    public Guid SelectedApplicationId { get; set; }

    /// <summary>
    /// Reason for selecting this provider
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string SelectionReason { get; set; } = null!;

    /// <summary>
    /// Contract terms agreed upon with the provider
    /// </summary>
    [MaxLength(5000)]
    public string? ContractTerms { get; set; }

    /// <summary>
    /// Escrow amount in credits to be held for the project
    /// </summary>
    [Range(50, 5000)]
    public int EscrowAmount { get; set; }

    /// <summary>
    /// When the selection was made
    /// </summary>
    public DateTime SelectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Expected project start date
    /// </summary>
    public DateTime? ExpectedStartDate { get; set; }

    /// <summary>
    /// Expected project completion date
    /// </summary>
    public DateTime? ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Current status of the provider selection
    /// </summary>
    public ProviderSelectionStatus Status { get; set; } = ProviderSelectionStatus.Selected;

    /// <summary>
    /// Notes from contract negotiation
    /// </summary>
    [MaxLength(2000)]
    public string? NegotiationNotes { get; set; }

    /// <summary>
    /// IP address from which the selection was made
    /// </summary>
    [MaxLength(45)]
    public string? SelectedFromIP { get; set; }

    /// <summary>
    /// Whether escrow has been funded
    /// </summary>
    public bool IsEscrowFunded { get; set; } = false;

    /// <summary>
    /// Whether the contract has been signed by both parties
    /// </summary>
    public bool IsContractSigned { get; set; } = false;

    /// <summary>
    /// Navigation property to the project
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Navigation property to the selected provider
    /// </summary>
    public virtual User SelectedProvider { get; set; } = null!;

    /// <summary>
    /// Navigation property to the selected application
    /// </summary>
    public virtual ProjectApplication SelectedApplication { get; set; } = null!;

    /// <summary>
    /// Helper property to check if selection is active
    /// </summary>
    public bool IsActive => Status == ProviderSelectionStatus.Selected || Status == ProviderSelectionStatus.ContractSigned;

    /// <summary>
    /// Helper property to check if ready for work to begin
    /// </summary>
    public bool IsReadyToStart => IsContractSigned && IsEscrowFunded;
}