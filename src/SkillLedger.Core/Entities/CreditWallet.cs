using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Encrypted credit wallet for secure storage of user collaboration credits
/// Implements AES-256 encryption for all financial data with Azure Key Vault integration
/// </summary>
public class CreditWallet
{
    public CreditWallet()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier for the credit wallet
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the wallet owner
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the wallet owner
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Encrypted current balance of collaboration credits
    /// Stored as encrypted string, decrypted to int for display
    /// </summary>
    [Required]
    [MaxLength(512)] // Allow for encrypted data expansion
    public string EncryptedBalance { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted pending balance (credits in escrow or pending transactions)
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string EncryptedPendingBalance { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted total credits earned throughout wallet lifetime
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string EncryptedTotalEarned { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted total credits spent throughout wallet lifetime
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string EncryptedTotalSpent { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the last transaction affecting this wallet
    /// Used for fraud detection and pattern analysis
    /// </summary>
    public DateTime? LastTransactionAt { get; set; }

    /// <summary>
    /// When the wallet was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the wallet was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version number for optimistic concurrency control
    /// Critical for preventing race conditions in financial operations
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Encryption key identifier used for this wallet
    /// Used for key rotation and cryptographic operations
    /// </summary>
    [MaxLength(128)]
    public string KeyIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether this wallet has been flagged for suspicious activity
    /// </summary>
    public bool IsBlocked { get; set; } = false;

    /// <summary>
    /// Reason for blocking if the wallet is blocked
    /// </summary>
    [MaxLength(500)]
    public string? BlockedReason { get; set; }

    /// <summary>
    /// When the wallet was blocked
    /// </summary>
    public DateTime? BlockedAt { get; set; }

    // Note: Transaction navigation properties are accessed via service layer queries
    // due to nullable foreign key complexity in EF Core

    // Non-mapped properties for decrypted values (populated by service layer)

    /// <summary>
    /// Decrypted balance for application use
    /// Not stored in database - populated by CreditWalletService
    /// </summary>
    [NotMapped]
    public int Balance { get; set; }

    /// <summary>
    /// Decrypted pending balance for application use
    /// </summary>
    [NotMapped]
    public int PendingBalance { get; set; }

    /// <summary>
    /// Decrypted total earned for application use
    /// </summary>
    [NotMapped]
    public int TotalEarned { get; set; }

    /// <summary>
    /// Decrypted total spent for application use
    /// </summary>
    [NotMapped]
    public int TotalSpent { get; set; }

    /// <summary>
    /// Available balance (Balance - PendingBalance)
    /// </summary>
    [NotMapped]
    public int AvailableBalance => Balance - PendingBalance;

    /// <summary>
    /// Update the UpdatedAt timestamp
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Block the wallet for security reasons
    /// </summary>
    /// <param name="reason">Reason for blocking</param>
    public void Block(string reason)
    {
        IsBlocked = true;
        BlockedReason = reason;
        BlockedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Unblock the wallet
    /// </summary>
    public void Unblock()
    {
        IsBlocked = false;
        BlockedReason = null;
        BlockedAt = null;
        UpdateTimestamp();
    }
}