using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.OpenAPI;

internal static class OpenApiTypeHelper
{
    private sealed record class SchemaDefinition(
        string? Type,
        string? Format = null,
        string? Ref = null,
        SchemaDefinition? Items = null,
        SchemaDefinition? AdditionalProperties = null,
        bool IsDynamic = false)
    {
        public JsonNode? GetAdditionalProperties()
        {
            if (AdditionalProperties == null) return null;
            if (IsDynamic) return true;
            string format = Format == null ? String.Empty : $", \"format\": \"{Format}\"";
            return JsonNode.Parse($"{{\"type\": \"{AdditionalProperties.Type}\"{format}}}");
        }

        public Schema? GetItemsSchema()
        {
            if (Items == null) return null;
            return new Schema {
                Type = Items.Type,
                Format = Items.Format,
                Ref = Items.Ref,
                Properties = null,
                AdditionalProperties = Items.GetAdditionalProperties(),
                Items = Items.GetItemsSchema()
            };
        }
    }

    private static SchemaDefinition? GetSchemaDefinition(Type type)
    {
        TypeCode code = Type.GetTypeCode(type);
        switch (code)
        {
            case TypeCode.Empty:
            case TypeCode.DBNull:
            default:
                return null;
            case TypeCode.Object:
                if (IsDictionary(type, out Type? generic))
                    return new SchemaDefinition("object", null, null, null, generic == null ? null : GetSchemaDefinition(generic), generic == null);

                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (IsArray(type, out generic))
                    return new SchemaDefinition("array", null, null, GetSchemaDefinition(generic!));

                return new SchemaDefinition(null, null, GetOrCreateSchema(type));
            case TypeCode.Boolean:
                return new SchemaDefinition("boolean");
            case TypeCode.Char:
                return new SchemaDefinition("string", "char");
            case TypeCode.SByte:
                return new SchemaDefinition("integer", "sbyte");
            case TypeCode.Byte:
                return new SchemaDefinition("integer", "byte");
            case TypeCode.Int16:
                return new SchemaDefinition("integer", "int16");
            case TypeCode.UInt16:
                return new SchemaDefinition("integer", "uint16");
            case TypeCode.Int32:
                return new SchemaDefinition("integer", "int32");
            case TypeCode.UInt32:
                return new SchemaDefinition("integer", "uint32");
            case TypeCode.Int64:
                return new SchemaDefinition("integer", "int64");
            case TypeCode.UInt64:
                return new SchemaDefinition("integer", "uint64");
            case TypeCode.Single:
                return new SchemaDefinition("number", "float");
            case TypeCode.Double:
                return new SchemaDefinition("number", "double");
            case TypeCode.Decimal:
                return new SchemaDefinition("number", "decimal");
            case TypeCode.DateTime:
                return new SchemaDefinition("string", "date-time");
            case TypeCode.String:
                return new SchemaDefinition("string");
        }
    }

    public static Components Components { get; } = new() { Schemas = new Dictionary<string, Schema>() };

    private static string GetOrCreateSchema(Type type)
    {
        var name = $"{Char.ToLower(type.Name[0])}{type.Name[1..]}";
        // ReSharper disable once InvertIf
        if (!Components.Schemas.TryGetValue(name, out Schema? schema))
        {
            schema = new Schema {
                Type = "object",
                Format = null,
                Ref = null,
                Properties = type.GetProperties()
                    .ToDictionary(static info => info.Name, static info =>
                    {
                        SchemaDefinition? definition = GetSchemaDefinition(info.PropertyType);
                        return new Property {
                            Type = definition?.Type,
                            Format = definition?.Format,
                            Nullable = IsNullable(info.PropertyType)
                        };
                    }),
                AdditionalProperties = false,
                Items = null
            };
            Components.Schemas.Add(name, schema);
        }

        return $"#/components/schemas/{name}";
    }

    private static Parameter[] GetParameters(List<ParameterInfo> routeParams, List<ParameterInfo> queryParams)
    {
        var res = new List<Parameter>();

        foreach (ParameterInfo param in queryParams)
        {
            SchemaDefinition? definition = GetSchemaDefinition(param.ParameterType);
            if (definition == null) continue;
            var parameter = new Parameter {
                In = "query",
                Name = param.Name!,
                Style = "form",
                Required = !IsNullable(param.ParameterType),
                Schema = new Schema {
                    Properties = null,
                    Type = definition.Type,
                    Format = definition.Format,
                    Ref = definition.Ref,
                    AdditionalProperties = null,
                    Items = null
                }
            };
            res.Add(parameter);
        }

        foreach (ParameterInfo param in routeParams)
        {
            SchemaDefinition? definition = GetSchemaDefinition(param.ParameterType);
            if (definition == null) continue;
            var parameter = new Parameter {
                In = "path",
                Name = param.Name!,
                Style = "simple",
                Required = !IsNullable(param.ParameterType),
                Schema = new Schema {
                    Properties = null,
                    Type = definition.Type,
                    Format = definition.Format,
                    Ref = definition.Ref,
                    AdditionalProperties = null,
                    Items = null
                }
            };
            res.Add(parameter);
        }

        return res.ToArray();
    }

    private static Dictionary<string, Response> GetResponse(MethodInfo method)
    {
        string? contentType = null;
        Dictionary<string, Property>? properties = null;
        SchemaDefinition? definition = null;

        if (typeof(void) == method.ReturnType || (method.ReturnType.GenericTypeArguments.Length == 0 && typeof(Task).IsAssignableFrom(method.ReturnType)))
            goto ret;

        Type unwrapped = UnwrapActionResult(UnwrapTask(method.ReturnType));
        if (UnwrapIAsyncEnumerable(unwrapped, out Type? generic))
        {
            contentType = "text/event-stream";
            definition = GetSchemaDefinition(generic!);
        }
        else if (UnwrapValueType(unwrapped, out IEnumerable<FieldInfo>? items))
        {
            contentType = "application/json";
            definition = new SchemaDefinition("object", "tuple");
            properties = items!.ToDictionary(static info => info.Name, info =>
            {
                SchemaDefinition? def = GetSchemaDefinition(info.FieldType);
                return new Property {
                    Type = def?.Type ?? "null",
                    Format = def?.Format,
                    Nullable = IsNullable(info.FieldType)
                };
            });
        }
        else
        {
            if (unwrapped == typeof(IActionResult) || unwrapped == typeof(ActionResult))
            {
                // Ignore
            }
            else if (unwrapped == typeof(ViewResult))
            {
                contentType = "text/html";
                definition = new SchemaDefinition("string");
            }
            else if (unwrapped == typeof(RedirectResult))
            {
                // 301 response or similar
            }
            else if (!typeof(IActionResult).IsAssignableFrom(unwrapped)) // Action results are special. If we missed one we have to ignore
            {
                definition = GetSchemaDefinition(unwrapped);
                if (definition != null) contentType = definition.Type == "object" ? "application/json" : "text/plain";
            }
        }

    ret:
        return new Dictionary<string, Response> {
            {
                "200", new Response {
                    Description = "success",
                    Content = contentType == null || definition == null
                        ? null
                        : new Dictionary<string, Content> {
                            {
                                contentType, new Content {
                                    Schema = new Schema {
                                        Properties = properties,
                                        Type = definition.Type,
                                        Format = definition.Format,
                                        Ref = definition.Ref,
                                        Items = definition.GetItemsSchema(),
                                        AdditionalProperties = definition.GetAdditionalProperties()
                                    },
                                    Encoding = null
                                }
                            }
                        }
                }
            }
        };
    }

    private static RequestBody GetFormRequestBody(IEnumerable<ParameterInfo> parameters)
    {
        var encoding = new Dictionary<string, Encoding>();
        var properties = new Dictionary<string, Property>();

        void GetProperties(bool first, string prefix, IEnumerable<(string Name, Type Type)> ts)
        {
            foreach ((string Name, Type Type) t in ts)
            {
                SchemaDefinition? definition = GetSchemaDefinition(t.Type);
                if (definition?.Type == "object")
                {
                    GetProperties(prefix == String.Empty, $"{t.Name}.", t.Type.GetProperties().Select(static prop => (prop.Name, prop.PropertyType)));
                }
                else if (definition != null)
                {
                    string name = first ? t.Name : $"{prefix}{t.Name}";
                    encoding.Add(name, new Encoding { Style = "form" });
                    properties.Add(name, new Property { Format = definition.Format, Type = definition.Type, Nullable = IsNullable(t.Type) });
                }
            }
        }

        GetProperties(true, String.Empty, parameters.Select(static info => (info.Name!, info.ParameterType)));

        return new RequestBody {
            Content = new Dictionary<string, Content> {
                {
                    "multipart/form-data", new Content {
                        Encoding = encoding,
                        Schema = new Schema {
                            Type = "object",
                            Format = null,
                            Properties = properties,
                            Ref = null,
                            Items = null,
                            AdditionalProperties = null
                        }
                    }
                }
            }
        };
    }

    private static RequestBody GetBodyRequestBody(IReadOnlyCollection<ParameterInfo> parameters)
    {
        // FromBody only makes sense with one param
        ParameterInfo first = parameters.First();
        SchemaDefinition? definition = GetSchemaDefinition(first.ParameterType);
        string contentType = definition?.Type == "object" ? "application/json" : "text/plain";

        return new RequestBody {
            Content = new Dictionary<string, Content> {
                {
                    contentType, new Content {
                        Encoding = null,
                        Schema = new Schema {
                            Type = definition?.Type,
                            Format = definition?.Format,
                            Properties = null,
                            Ref = definition?.Ref,
                            Items = definition?.GetItemsSchema(),
                            AdditionalProperties = definition?.GetAdditionalProperties()
                        }
                    }
                }
            }
        };
    }

    private static bool IsNullable(Type type)
    {
        if (Nullable.GetUnderlyingType(type) != null) return true;

        // Check for reference types (C# 8.0 nullable reference types)
        object? nullableAttribute = type.GetCustomAttributes(false)
            .FirstOrDefault(static attr => attr.GetType().FullName == "System.Runtime.CompilerServices.NullableAttribute");

        if (nullableAttribute == null) return false;

        FieldInfo? nullableField = nullableAttribute.GetType().GetField("NullableFlags");
        if (nullableField == null) return false;

        var flags = (byte[]?)nullableField.GetValue(nullableAttribute);
        return flags?.Length > 0 && flags[0] == 2;
    }

    private static string GetControllerName(MemberInfo controller, string template) =>
        controller.Name.EndsWith("Controller") && template.Contains("[controller]")
            ? controller.Name[..^10]
            : controller.Name;

    public static string GetEndpoint(MemberInfo controller, string template, MemberInfo method, string? methodTemplate)
    {
        var replacements = new Dictionary<string, string> {
            { "[controller]", GetControllerName(controller, template) },
            { "[action]", method.Name }
        };

        string @base;
        if (methodTemplate?.StartsWith('/') is true) @base = methodTemplate;
        else if (String.IsNullOrEmpty(methodTemplate)) @base = template;
        else if (template.EndsWith('/')) @base = template + methodTemplate;
        else @base = $"{template}/{methodTemplate}";

        string res = replacements.Aggregate(@base, (current, pair) => current.Replace(pair.Key, pair.Value));
        return res.StartsWith('/') ? res : "/" + res;
    }

    public static Path GetPath(MemberInfo controller, string template, MethodInfo method, string? methodTemplate)
    {
        var formParams = new List<ParameterInfo>();
        var bodyParams = new List<ParameterInfo>();
        var queryParams = new List<ParameterInfo>();
        var routeParams = new List<ParameterInfo>();
        ParameterInfo[] @params = method.GetParameters();
        foreach (ParameterInfo info in @params)
        {
            if (typeof(CancellationToken).IsAssignableFrom(info.ParameterType)) continue;

            var found = false;
            foreach (Attribute attribute in info.GetCustomAttributes())
            {
                switch (attribute)
                {
                    case FromBodyAttribute:
                        found = true;
                        bodyParams.Add(info);
                        break;
                    case FromFormAttribute:
                        found = true;
                        formParams.Add(info);
                        break;
                    case FromRouteAttribute:
                        found = true;
                        routeParams.Add(info);
                        break;
                    case FromQueryAttribute:
                        found = true;
                        queryParams.Add(info);
                        break;
                }
            }

            if (found) continue;

            string combined = template + methodTemplate;
            if (combined.Contains($"{{{info.Name}}}") || combined.Contains($"{{{info.Name}:"))
            {
                routeParams.Add(info);
            }
            else
            {
                if (Type.GetTypeCode(info.ParameterType) == TypeCode.Object) bodyParams.Add(info);
                queryParams.Add(info);
            }
        }

        Parameter[]? parameters = null;
        if (queryParams.Any() || routeParams.Any()) parameters = GetParameters(routeParams, queryParams);

        RequestBody? requestBody = null;
        if (formParams.Any()) requestBody = GetFormRequestBody(formParams);

        if (bodyParams.Any()) requestBody = GetBodyRequestBody(bodyParams);

        return new Path {
            Tags = new List<string> { GetControllerName(controller, template) },
            Parameters = parameters,
            RequestBody = requestBody,
            Responses = GetResponse(method)
        };
    }

    private static Type UnwrapTask(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>)) return type.GetGenericArguments()[0];
        return type;
    }

    private static Type UnwrapActionResult(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ActionResult<>)) return type.GetGenericArguments()[0];
        return type;
    }

    private static bool UnwrapValueType(Type type, out IEnumerable<FieldInfo>? items)
    {
        items = null;

        // Check if the type is a generic type
        if (!type.IsGenericType || type is not TypeInfo info)
            return false;

        // Get the generic type definition
        Type genericDefinition = type.GetGenericTypeDefinition();

        // Check if the type is a subclass of System.ValueTuple
        bool isValueType = genericDefinition == typeof(ValueTuple<>)
            || genericDefinition == typeof(ValueTuple<,>)
            || genericDefinition == typeof(ValueTuple<,,>)
            || genericDefinition == typeof(ValueTuple<,,,>)
            || genericDefinition == typeof(ValueTuple<,,,,>)
            || genericDefinition == typeof(ValueTuple<,,,,,>)
            || genericDefinition == typeof(ValueTuple<,,,,,,>)
            || genericDefinition == typeof(ValueTuple<,,,,,,,>);

        if (!isValueType) return false;

        items = info.DeclaredFields;
        return true;
    }

    private static bool IsDictionary(Type type, out Type? generic)
    {
        generic = null;

        // Check if the type itself is IDictionary (non-generic)
        if (typeof(IDictionary).IsAssignableFrom(type))
            return true;

        Type[] generics = type.GenericTypeArguments;
        if (generics.Length > 1) generic = type.GenericTypeArguments[1];

        // Check if the type implements IDictionary<TKey, TValue>
        if (type.IsGenericType
            && (typeof(IDictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition())
                || typeof(IReadOnlyDictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition())))
            return true;

        // Check for implemented interfaces
        return type.GetInterfaces()
            .Any(static @interface => @interface.IsGenericType
                && (@interface.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                    || @interface.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
    }

    private static bool IsArray(Type type, out Type? generic)
    {
        generic = null;

        Type[] generics = type.GenericTypeArguments;
        if (generics.Length > 0) generic = type.GenericTypeArguments[0];

        return type.IsArray || typeof(IEnumerable).IsAssignableFrom(type);
    }

    public static bool UnwrapIAsyncEnumerable(Type type, out Type? generic)
    {
        generic = null;
        if (type.GenericTypeArguments.Length <= 0) return false;

        generic = type.GenericTypeArguments[0];
        return type.IsGenericType && typeof(IAsyncEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition());
    }
}