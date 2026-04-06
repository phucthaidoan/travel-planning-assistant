using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace TravelAssistant.Core.Agents;

public static class SpecialistStub
{
    public static ChatClientAgent Create(IChatClient chatClient, string name, string instructions)
        => chatClient.AsAIAgent(name: name, instructions: instructions);
}
