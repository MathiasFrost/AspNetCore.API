using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace AspNetCore.API.OpenAPI;

public sealed class OpenApiDocument
{
    [JsonPropertyName("openapi"), UsedImplicitly]
    public required string OpenApi { get; init; }

    [JsonPropertyName("info"), UsedImplicitly]
    public required Info Info { get; init; }

    [JsonPropertyName("paths"), UsedImplicitly]
    public required Dictionary<string, Dictionary<string, Path>> Paths { get; init; }

    [JsonPropertyName("components"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public Components? Components { get; set; }
}

public sealed class Info
{
    [JsonPropertyName("title"), UsedImplicitly]
    public required string Title { get; init; }

    [JsonPropertyName("version"), UsedImplicitly]
    public required string Version { get; init; }
}

public sealed class Path
{
    [JsonPropertyName("tags"), UsedImplicitly]
    public required List<string> Tags { get; init; }

    [JsonPropertyName("parameters"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required Parameter[]? Parameters { get; init; }

    [JsonPropertyName("requestBody"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required RequestBody? RequestBody { get; init; }

    [JsonPropertyName("responses"), UsedImplicitly]
    public required Dictionary<string, Response> Responses { get; init; }
}

public sealed class Parameter
{
    [JsonPropertyName("name"), UsedImplicitly]
    public required string Name { get; init; }

    [JsonPropertyName("in"), UsedImplicitly]
    public required string In { get; init; }

    [JsonPropertyName("style"), UsedImplicitly]
    public required string Style { get; init; }

    [JsonPropertyName("required"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), UsedImplicitly]
    public required bool Required { get; init; }

    [JsonPropertyName("schema"), UsedImplicitly]
    public required Schema Schema { get; init; }
}

public sealed class RequestBody
{
    [JsonPropertyName("content"), UsedImplicitly]
    public required Dictionary<string, Content> Content { get; init; }
}

public sealed class Content
{
    [JsonPropertyName("schema"), UsedImplicitly]
    public required Schema Schema { get; init; }

    [JsonPropertyName("encoding"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required Dictionary<string, Encoding>? Encoding { get; init; }
}

public sealed class Response
{
    [JsonPropertyName("description"), UsedImplicitly]
    public required string Description { get; init; }

    [JsonPropertyName("content"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required Dictionary<string, Content>? Content { get; init; }
}

public sealed class Components
{
    [JsonPropertyName("schemas"), UsedImplicitly]
    public required Dictionary<string, Schema> Schemas { get; init; }
}

public sealed class Schema
{
    [JsonPropertyName("type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required string? Type { get; init; }

    [JsonPropertyName("format"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required string? Format { get; init; }

    [JsonPropertyName("$ref"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required string? Ref { get; init; }

    [JsonPropertyName("properties"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required Dictionary<string, Property>? Properties { get; init; }

    [JsonPropertyName("additionalProperties"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required JsonNode? AdditionalProperties { get; init; }

    [JsonPropertyName("items"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required Schema? Items { get; init; }
}

public sealed class Property
{
    [JsonPropertyName("type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required string? Type { get; init; }

    [JsonPropertyName("format"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), UsedImplicitly]
    public required string? Format { get; init; }

    [JsonPropertyName("nullable"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), UsedImplicitly]
    public required bool Nullable { get; init; }
}

public sealed class Encoding
{
    [JsonPropertyName("style"), UsedImplicitly]
    public required string Style { get; init; }
}