using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;

namespace SkillLedger.Infrastructure.Services;

public class SequencerClient : ISequencerClient
{
    private readonly HttpClient _httpClient;
    private readonly SequencerOptions _options;
    private readonly ILogger<SequencerClient> _logger;

    public SequencerClient(
        HttpClient httpClient,
        IOptions<SequencerOptions> options,
        ILogger<SequencerClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (_httpClient.BaseAddress is null &&
            Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseAddress))
        {
            _httpClient.BaseAddress = baseAddress;
        }
    }

    public async Task EnrollAsync(
        string email,
        string sequenceSlug,
        string source,
        IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogDebug("Sequencer enrollment skipped because sequencer is not configured");
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/enrollments")
        {
            Content = JsonContent.Create(new
            {
                product = "skillledger",
                email,
                sequence_slug = sequenceSlug,
                source,
                properties
            })
        };

        request.Headers.TryAddWithoutValidation("CF-Access-Client-Id", _options.CloudflareAccessClientId);
        request.Headers.TryAddWithoutValidation("CF-Access-Client-Secret", _options.CloudflareAccessClientSecret);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Sequencer enrollment requested for {Email} into {SequenceSlug}", email, sequenceSlug);
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Sequencer enrollment failed for {Email} into {SequenceSlug}: {StatusCode} {Body}",
            email,
            sequenceSlug,
            (int)response.StatusCode,
            body);
    }
}
