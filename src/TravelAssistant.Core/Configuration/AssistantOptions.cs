namespace TravelAssistant.Core.Configuration;

public sealed class AssistantOptions
{
    public OpenAIOptions OpenAI { get; set; } = new();
    public PostgreSQLOptions PostgreSQL { get; set; } = new();
    public QdrantOptions Qdrant { get; set; } = new();
    public JaegerOptions Jaeger { get; set; } = new();
}

public sealed class OpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModelId { get; set; } = "gpt-4.1-nano";
    public string EmbeddingModelId { get; set; } = "text-embedding-3-small";
}

public sealed class PostgreSQLOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class QdrantOptions
{
    public string Endpoint { get; set; } = "http://localhost:6333";
}

public sealed class JaegerOptions
{
    public string Endpoint { get; set; } = "http://localhost:4317";
}
