using SkillLedger.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace SkillLedger.Core.Entities
{
    public class ProjectWorkspace
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required]
        public Guid ClientId { get; set; }
        public User Client { get; set; } = null!;

        [Required]
        public Guid ProviderId { get; set; }
        public User Provider { get; set; } = null!;

        [Required]
        [StringLength(256)]
        public string WorkspaceKey { get; set; } = GenerateWorkspaceKey();

        public WorkspaceStatus Status { get; set; } = WorkspaceStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }

        // Timeline and milestone data (stored as JSON)
        public string? TimelineData { get; set; }
        public string? MilestoneData { get; set; }

        // Integration tracking
        public DateTime? LastSyncedAt { get; set; }
        public string? IntegrationStatus { get; set; }

        public void ArchiveWorkspace()
        {
            if (Status != WorkspaceStatus.Archived)
            {
                Status = WorkspaceStatus.Archived;
                ArchivedAt = DateTime.UtcNow;
            }
        }

        public bool IsAccessibleBy(Guid userId)
        {
            return userId == ClientId || userId == ProviderId;
        }

        private static string GenerateWorkspaceKey()
        {
            // Generate a cryptographically secure workspace key
            using var rng = RandomNumberGenerator.Create();
            var keyBytes = new byte[32]; // 256-bit key
            rng.GetBytes(keyBytes);

            // Convert to base64 and make it URL-safe
            return Convert.ToBase64String(keyBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }
    }
}