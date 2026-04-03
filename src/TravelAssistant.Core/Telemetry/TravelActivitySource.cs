using System.Diagnostics;

namespace TravelAssistant.Core.Telemetry;

public static class TravelActivitySource
{
    public const string ServiceName = "TravelAssistant";

    private static readonly ActivitySource Source = new(ServiceName);

    /// <summary>
    /// Starts a root span for one agent session and stamps the session ID as a trace
    /// attribute (AC-2: agent.session_id). Caller is responsible for disposing the returned Activity.
    /// </summary>
    public static Activity? StartAgentSession(string sessionId)
    {
        var activity = Source.StartActivity("AgentSession");
        activity?.SetTag("agent.session_id", sessionId);
        return activity;
    }
}
