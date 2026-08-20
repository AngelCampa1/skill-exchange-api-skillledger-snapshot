namespace SkillLedger.Infrastructure.Configuration;

public class SequencerOptions
{
    public const string SectionName = "Sequencer";

    public string? BaseUrl { get; set; }
    public string? CloudflareAccessClientId { get; set; }
    public string? CloudflareAccessClientSecret { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl) &&
        !string.IsNullOrWhiteSpace(CloudflareAccessClientId) &&
        !string.IsNullOrWhiteSpace(CloudflareAccessClientSecret);
}
