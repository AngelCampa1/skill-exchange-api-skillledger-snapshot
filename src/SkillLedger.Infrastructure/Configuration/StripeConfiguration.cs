namespace SkillLedger.Infrastructure.Configuration;

public class StripeSettings
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsTestMode { get; set; } = true;
}