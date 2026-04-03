using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TravelAssistant.Core.Configuration;
using Xunit;

namespace TravelAssistant.Tests.Configuration;

public class AssistantOptionsTests
{
    [Fact]
    public void AssistantOptions_BindsFromConfiguration()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["OpenAI:Endpoint"] = "https://my-resource.openai.azure.com/",
            ["OpenAI:ApiKey"] = "test-key",
            ["OpenAI:ChatModelId"] = "gpt-4.1-nano",
            ["OpenAI:EmbeddingModelId"] = "text-embedding-3-small",
            ["PostgreSQL:ConnectionString"] = "Host=localhost;Database=travel",
            ["Qdrant:Endpoint"] = "http://localhost:6333",
            ["Jaeger:Endpoint"] = "http://localhost:4317",
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        AssistantOptions options = new();
        config.Bind(options);

        options.OpenAI.Endpoint.Should().Be("https://my-resource.openai.azure.com/");
        options.OpenAI.ApiKey.Should().Be("test-key");
        options.OpenAI.ChatModelId.Should().Be("gpt-4.1-nano");
        options.OpenAI.EmbeddingModelId.Should().Be("text-embedding-3-small");
        options.PostgreSQL.ConnectionString.Should().Be("Host=localhost;Database=travel");
        options.Qdrant.Endpoint.Should().Be("http://localhost:6333");
        options.Jaeger.Endpoint.Should().Be("http://localhost:4317");
    }

    [Fact]
    public void AssistantOptions_HasSensibleDefaults()
    {
        AssistantOptions options = new();

        options.OpenAI.ChatModelId.Should().Be("gpt-4.1-nano");
        options.OpenAI.EmbeddingModelId.Should().Be("text-embedding-3-small");
    }
}
