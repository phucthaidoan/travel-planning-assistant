using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace TravelAssistant.Core.Agents;

public static class OrchestratorAgentFactory
{
    private const string OrchestratorInstructions = """
        You are the Intelligent Travel Planning Assistant orchestrator.
        Analyze the user's travel query and delegate to the appropriate specialist agent using the available tools.

        Routing rules:
        - Itinerary requests (trip planning, day-by-day schedule, destinations): call itinerary_agent
        - For general travel questions you can answer directly, respond without calling a tool.

        Always respond in the language the user uses. Be concise and helpful.
        """;

    public static AIAgent Create(
        IChatClient chatClient,
        IEnumerable<ChatClientAgent> specialists,
        Action<string>? onToolInvoked = null)
    {
        AITool[] tools = specialists
            .Select(s => (AITool)s.AsAIFunction())
            .ToArray();

        return chatClient
            .AsAIAgent(
                name: "OrchestratorAgent",
                instructions: OrchestratorInstructions,
                tools: tools)
            .AsBuilder()
            .Use((agent, ctx, next, ct) =>
            {
                onToolInvoked?.Invoke(ctx.Function.Name);
                return next(ctx, ct);
            })
            .Build();
    }
}
