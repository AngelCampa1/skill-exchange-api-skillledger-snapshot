using FluentAssertions;
using SkillLedger.Api.Hubs;
using SkillLedger.Tests.Infrastructure;
using System.Reflection;
using Xunit;

namespace SkillLedger.Tests.Security;

[SecurityTest]
public class SignalRHubSecurityTests
{
    [Fact]
    public void ServiceBroadcastMethods_AreNotClientCallableHubMethods()
    {
        var hubTypes = new[]
        {
            typeof(MessagingHub),
            typeof(FinancialAnalyticsHub),
            typeof(MilestoneTrackingHub)
        };

        var exposedBroadcastMethods = hubTypes
            .SelectMany(hubType => hubType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.Name.StartsWith("Broadcast", StringComparison.Ordinal))
                .Select(method => $"{hubType.Name}.{method.Name}"))
            .ToList();

        exposedBroadcastMethods.Should().BeEmpty(
            "service-only SignalR broadcast helpers must not be invokable by connected clients");
    }
}
