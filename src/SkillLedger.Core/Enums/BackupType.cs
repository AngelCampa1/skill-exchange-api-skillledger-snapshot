namespace SkillLedger.Core.Enums;

public enum BackupType
{
    Manual = 0,
    Automatic = 1,
    Scheduled = 2,
    SystemInitiated = 3
}

public enum BackupFrequency
{
    Hourly = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}