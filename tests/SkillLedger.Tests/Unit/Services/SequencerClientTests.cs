using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Services;

namespace SkillLedger.Tests.Unit.Services;

public class SequencerClientTests
{
    [Fact]
    public async Task EnrollAsync_WhenConfigured_PostsEnrollmentWithCloudflareAccessHeaders()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://sequencer.test")
        };
        var sut = new SequencerClient(
            client,
            Options.Create(new SequencerOptions
            {
                BaseUrl = "https://sequencer.test",
                CloudflareAccessClientId = "client-id",
                CloudflareAccessClientSecret = "client-secret"
            }),
            NullLogger<SequencerClient>.Instance);

        await sut.EnrollAsync(
            "NewUser@Example.com",
            "skillledger-fulfillment-welcome",
            "skillledger_signup",
            new Dictionary<string, object?>
            {
                ["first_name"] = "Ada",
                ["last_name"] = "Lovelace"
            });

        handler.Requests.Should().ContainSingle();
        var request = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.PathAndQuery.Should().Be("/api/v1/enrollments");
        request.Headers.GetValues("CF-Access-Client-Id").Should().ContainSingle("client-id");
        request.Headers.GetValues("CF-Access-Client-Secret").Should().ContainSingle("client-secret");

        var payload = JsonSerializer.Deserialize<JsonElement>(handler.Bodies.Single());
        payload.GetProperty("product").GetString().Should().Be("skillledger");
        payload.GetProperty("email").GetString().Should().Be("NewUser@Example.com");
        payload.GetProperty("sequence_slug").GetString().Should().Be("skillledger-fulfillment-welcome");
        payload.GetProperty("source").GetString().Should().Be("skillledger_signup");
        payload.GetProperty("properties").GetProperty("first_name").GetString().Should().Be("Ada");
    }

    [Fact]
    public async Task EnrollAsync_WhenNotConfigured_DoesNotSendHttpRequest()
    {
        var handler = new RecordingHandler();
        var sut = new SequencerClient(
            new HttpClient(handler),
            Options.Create(new SequencerOptions()),
            NullLogger<SequencerClient>.Instance);

        await sut.EnrollAsync("user@example.com", "skillledger-nurture-value-1", "skillledger_signup");

        handler.Requests.Should().BeEmpty();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"run_id":"run_1","status":"enrolled"}""")
            };
        }
    }
}
