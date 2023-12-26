using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCore.API.OpenAPI;

internal static class OpenApiTypeHelper
{
    private static (string? type, string? format) GetTypeAndFormat(Type type)
    {
        TypeCode code = Type.GetTypeCode(type);
        switch (code)
        {
            case TypeCode.Empty:
            case TypeCode.DBNull:
            default:
                return (null, null);
            case TypeCode.Object:
                // TODO: Find or create a schema definition which returns a link to it
                return ("object", "TODO");
            case TypeCode.Boolean:
                return ("boolean", null);
            case TypeCode.Char:
                return ("string", "char");
            case TypeCode.SByte:
                return ("integer", "sbyte");
            case TypeCode.Byte:
                return ("integer", "byte");
            case TypeCode.Int16:
                return ("integer", "int16");
            case TypeCode.UInt16:
                return ("integer", "uint16");
            case TypeCode.Int32:
                return ("integer", "int32");
            case TypeCode.UInt32:
                return ("integer", "uint32");
            case TypeCode.Int64:
                return ("integer", "int64");
            case TypeCode.UInt64:
                return ("integer", "uint64");
            case TypeCode.Single:
                return ("number", "float");
            case TypeCode.Double:
                return ("number", "double");
            case TypeCode.Decimal:
                return ("number", "decimal");
            case TypeCode.DateTime:
                return ("string", "date-time");
            case TypeCode.String:
                return ("string", null);
        }
    }

    private static Dictionary<string, Response> GetResponse(MethodInfo method)
    {
        string? contentType = null;
        Dictionary<string, Property>? properties = null;
        string? type = null;

        Type unwrapped = UnwrapActionResult(UnwrapTask(method.ReturnType));
        if (unwrapped == typeof(IActionResult) || unwrapped == typeof(ActionResult))
        {
            // Ignore
        }
        else if (unwrapped == typeof(ViewResult))
        {
            contentType = "text/html";
            type = "string";
        }
        else
        {
            
        }

        return new Dictionary<string, Response> {
            {
                "200", new Response {
                    Description = "success",
                    Content = contentType == null
                        ? null
                        : new Dictionary<string, Content> {
                            {
                                contentType, new Content {
                                    Schema = new Schema { Properties = properties, Type = type },
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
                (string? type, string? format) = GetTypeAndFormat(t.Type);
                if (type == "object")
                {
                    GetProperties(prefix == String.Empty, $"{t.Name}.", t.Type.GetProperties().Select(static prop => (prop.Name, prop.PropertyType)));
                }
                else if (type != null)
                {
                    string name = first ? t.Name : $"{prefix}{t.Name}";
                    encoding.Add(name, new Encoding { Style = "form" });
                    properties.Add(name, new Property { Format = format, Type = type, Nullable = IsNullable(t.Type) });
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
                            Properties = properties
                        }
                    }
                }
            }
        };
    }

    private static bool? IsNullable(Type type)
    {
        if (Nullable.GetUnderlyingType(type) != null) return true;

        // Check for reference types (C# 8.0 nullable reference types)
        object? nullableAttribute = type.GetCustomAttributes(false)
            .FirstOrDefault(static attr => attr.GetType().FullName == "System.Runtime.CompilerServices.NullableAttribute");

        if (nullableAttribute == null) return null;

        FieldInfo? nullableField = nullableAttribute.GetType().GetField("NullableFlags");
        if (nullableField == null) return null;

        var flags = (byte[]?)nullableField.GetValue(nullableAttribute);
        if (flags?.Length > 0 && flags[0] == 2) return true;
        return null; // Nullable reference type
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
        var formParams = new List<ParameterInfo>();
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
                    formParams.Add(info);
                }
            }
        }

        RequestBody? requestBody = null;
        if (formParams.Any()) requestBody = GetFormRequestBody(formParams);

        return new Path {
            Tags = new List<string> { GetControllerName(controller, template) },
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
}