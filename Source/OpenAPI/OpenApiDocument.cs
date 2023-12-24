using System.Text.Json.Serialization;

namespace AspNetCore.API.OpenAPI;

public sealed class OpenApiDocument
{
    [JsonPropertyName("openapi")] public required string OpenApi { get; init; }
    [JsonPropertyName("info")] public required Info Info { get; init; }
    [JsonPropertyName("paths")] public required Dictionary<string, Dictionary<string, Path>> Paths { get; init; }
}

public sealed class Info
{
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("version")] public required string Version { get; init; }
}

public sealed class Path
{
    [JsonPropertyName("tags")] public required List<string> Tags { get; init; }

    [JsonPropertyName("responses")] public Dictionary<string, Response> Responses => new() { { "200", new Response { Description = "Success" } } };
}

public sealed class Response
{
    [JsonPropertyName("description")] public required string Description { get; init; }
}

public sealed class Components
{
    [JsonPropertyName("schemas")] public required List<Schema> Schemas { get; init; }
}

public sealed class Schema
{
    [JsonPropertyName("schemas")] public required string Version { get; init; }
}

public enum SchemaType : byte
{
    Object
}