using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.OpenAPI;

internal static class OpenApiTypeHelper
{
    enum Source : byte
    {
        Return,
        Form,
        Body,
        Route,
        Query
    }

    private static Dictionary<string, Content>? GetContent(Source source, params Type[] types)
    {
        string contentType = source switch {
            Source.Body or Source.Return when types is [{ IsPrimitive: true }] && types[0].IsAssignableTo(typeof(string)) => "text/plain",
            Source.Return => "application/json",
            Source.Form => "multipart/form-data",
            Source.Body => "application/json",
            Source.Route => "simple",
            Source.Query => "application/xxx-form-urlencoded",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unexpected")
        };
        Dictionary<string, Encoding>? encoding = null;
        Dictionary<string, Property>? properties = null;
        string? mainType = null;

        foreach (Type type1 in types)
        {
            if (type1 == typeof(CancellationToken)) continue;

            string? type = null;
            string? format = null;
            Type unwrapped = source == Source.Return ? UnwrapActionResult(UnwrapTask(type1)) : type1;

            if (source == Source.Return)
            {
                if (unwrapped == typeof(ViewResult))
                {
                    contentType = "text/html";
                    mainType = "string";
                    break;
                }
            }

            string style = source switch {
                Source.Body or Source.Return when types is [{ IsPrimitive: true }] && types[0].IsAssignableTo(typeof(string)) => "text/plain",
                Source.Return => "text/plain",
                Source.Form => "multipart/form-data",
                Source.Body => "application/json",
                Source.Route => "simple",
                Source.Query => "application/xxx-form-urlencoded",
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unexpected")
            };

            TypeCode code = Type.GetTypeCode(unwrapped);
            mainType = "object";
            switch (code)
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                default:
                    return null;
                case TypeCode.Object:
                    // TODO: Find or create a schema definition which returns a link to it
                    break;
                case TypeCode.Boolean:
                    type = "boolean";
                    format = "simple";
                    break;
                case TypeCode.Char:
                    type = "string";
                    format = "char";
                    break;
                case TypeCode.SByte:
                    type = "integer";
                    format = "sbyte";
                    break;
                case TypeCode.Byte:
                    type = "integer";
                    format = "byte";
                    break;
                case TypeCode.Int16:
                    type = "integer";
                    format = "int16";
                    break;
                case TypeCode.UInt16:
                    type = "integer";
                    format = "uint16";
                    break;
                case TypeCode.Int32:
                    type = "integer";
                    format = "int32";
                    break;
                case TypeCode.UInt32:
                    type = "integer";
                    format = "uint32";
                    break;
                case TypeCode.Int64:
                    type = "integer";
                    format = "int64";
                    break;
                case TypeCode.UInt64:
                    type = "integer";
                    format = "uint64";
                    break;
                case TypeCode.Single:
                    type = "number";
                    format = "float";
                    break;
                case TypeCode.Double:
                    type = "number";
                    format = "double";
                    break;
                case TypeCode.Decimal:
                    type = "number";
                    format = "decimal";
                    break;
                case TypeCode.DateTime:
                    type = "date";
                    format = "datetime";
                    break;
                case TypeCode.String:
                    type = "string";
                    break;
            }

            if (type == null) continue;

            properties ??= new Dictionary<string, Property>();
            encoding ??= new Dictionary<string, Encoding>();
            properties.Add(unwrapped.Name, new Property { Format = format, Type = type, Nullable = IsNullable(unwrapped) });
            encoding.Add(unwrapped.Name, new Encoding { Style = style });
        }

        return mainType == null
            ? null
            : new Dictionary<string, Content> {
                { contentType, new Content { Encoding = encoding, Schema = new Schema { Properties = properties, Type = mainType } } }
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
        return flags?.Length > 0 && flags[0] == 2; // Nullable reference type
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

        string res = replacements.Aggregate(@base, static (current, pair) => current.Replace(pair.Key, pair.Value));
        return res.StartsWith('/') ? res : "/" + res;
    }

    public static Path GetPath(MemberInfo controller, string template, MethodInfo method)
    {
        var formParams = new List<Type>();
        var queryParams = new List<Type>();
        var bodyParams = new List<Type>();
        var routeParams = new List<Type>();
        ParameterInfo[] @params = method.GetParameters();
        foreach (ParameterInfo info in @params)
        {
            foreach (Attribute attribute in info.GetCustomAttributes())
            {
                if (attribute is FromFormAttribute)
                {
                    formParams.Add(info.ParameterType);
                }
            }
        }

        RequestBody? requestBody = null;
        Dictionary<string, Content>? content = GetContent(Source.Form, formParams.ToArray());
        if (content != null) requestBody = new RequestBody { Content = content };

        return new Path {
            Tags = new List<string> { GetControllerName(controller, template) },
            RequestBody = requestBody,
            Responses = new Dictionary<string, Response> {
                {
                    "200", new Response {
                        Description = "success",
                        Content = GetContent(Source.Return, method.ReturnType)
                    }
                }
            }
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
}