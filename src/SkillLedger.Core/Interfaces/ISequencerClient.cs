namespace SkillLedger.Core.Interfaces;

public interface ISequencerClient
{
    Task EnrollAsync(
        string email,
        string sequenceSlug,
        string source,
        IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken cancellationToken = default);
}
