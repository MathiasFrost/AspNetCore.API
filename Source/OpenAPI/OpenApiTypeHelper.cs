using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.OpenAPI;

internal static class OpenApiTypeHelper
{
    private static Property GetProperty(ParameterInfo info)
    {
        bool? nullable = null;
        string? format = null;
        string? type = null;
        TypeCode code = Type.GetTypeCode(info.ParameterType);
        switch (code)
        {
            case TypeCode.Empty:
                type = "null";
                break;
            case TypeCode.Object:
                // TODO: Find or create a schema definition which returns a link to it
                break;
            case TypeCode.DBNull:
                type = "null";
                break;
            case TypeCode.Boolean:
                type = "boolean";
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
            default:
                throw new ArgumentOutOfRangeException();
        }

        return new Property {
            Format = format,
            Nullable = nullable,
            Type = type ?? String.Empty
        };
    }

    private static void AddOrCreateRequestBody(ref RequestBody? requestBody, ParameterInfo info, string contentType, string type, string encoding)
    {
        if (requestBody?.Content.TryGetValue(contentType, out Content? content) is true)
            content.Schema.Properties.Add(contentType, GetProperty(info));
        else
            requestBody = new RequestBody {
                Content = new Dictionary<string, Content> {
                    {
                        contentType, new Content {
                            Schema = new Schema {
                                Type = type,
                                Properties = new Dictionary<string, Property> { { info.Name!, GetProperty(info) } },
                            },
                            Encoding = new Dictionary<string, Encoding> { { info.Name!, new Encoding { Style = encoding } } }
                        }
                    }
                }
            };
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
        RequestBody? requestBody = null;

        ParameterInfo[] @params = method.GetParameters();
        foreach (ParameterInfo info in @params)
        {
            foreach (Attribute attribute in info.GetCustomAttributes())
            {
                if (attribute is FromFormAttribute)
                {
                    const string contentType = "multipart/form-data";
                    AddOrCreateRequestBody(ref requestBody, info, contentType, "object", "form");
                }
            }
        }

        var content = new Dictionary<string, Content>();
        Type response = method.ReturnType;

        return new Path {
            Tags = new List<string> { GetControllerName(controller, template) },
            RequestBody = requestBody,
            Responses = new Dictionary<string, Response> {
                {
                    "200", new Response {
                        Description = "success",
                        Content = content
                    }
                }
            }
        };
    }
}