using System.Diagnostics;
using FluentAssertions;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TravelAssistant.Core.Telemetry;
using Xunit;

namespace TravelAssistant.Tests.Telemetry;

public sealed class TelemetryPipelineTests
{
    [Fact]
    public void StartAgentSession_EmitsSpanWithSessionIdTag()
    {
        // Arrange — in-memory exporter captures spans without requiring Jaeger
        var exportedActivities = new List<Activity>();
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(TravelActivitySource.ServiceName))
            .AddSource(TravelActivitySource.ServiceName)
            .AddInMemoryExporter(exportedActivities)
            .Build();

        var sessionId = Guid.NewGuid().ToString();

        // Act
        using (TravelActivitySource.StartAgentSession(sessionId))
        {
            // span is exported on Dispose
        }

        // Assert — AC-4: at least one trace emitted; AC-5: span count > 0
        exportedActivities.Should().NotBeEmpty();
        exportedActivities.Count.Should().BeGreaterThan(0);

        // AC-2: session ID attribute present on the span
        var span = exportedActivities.Single();
        span.DisplayName.Should().Be("AgentSession");
        span.GetTagItem("agent.session_id").Should().Be(sessionId);
    }
}
