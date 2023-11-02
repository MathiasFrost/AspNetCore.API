using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AspNetCore.API.OpenAPI;

public static class OpenApiInfrastructure
{
    public static IApplicationBuilder UseOpenApi(this IApplicationBuilder app)
    {
        return app.Use(static _ =>
        {
            return static async context =>
            {
                if (context.Request.Path != "/openapi") return;

                var builder = new StringBuilder();

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        types = e.Types.OfType<Type>().ToArray();
                    }

                    // Iterate through each type in the assembly
                    foreach (Type type in types)
                    {
                        // HTTP REST Controllers
                        // Check if the type inherits from ControllerBase and has the ApiController attribute
                        if (type.IsSubclassOf(typeof(ControllerBase)) && Attribute.IsDefined(type, typeof(ApiControllerAttribute)))
                        {
                            // Retrieve the Route attribute if it exists
                            var routeAttr = type.GetCustomAttribute<RouteAttribute>();
                            if (routeAttr != null)
                            {
                                // Display the type name and the Route attribute value
                                string route = "/" + routeAttr.Template.Replace("[controller]", type.Name);
                                builder.AppendLine($"Controller: {type.Name}: {route}");

                                IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                                foreach (MethodInfo method in methods)
                                {
                                    if (method.GetCustomAttributes().FirstOrDefault(static attribute => attribute is HttpMethodAttribute) is not
                                        HttpMethodAttribute httpMethod) continue;

                                    builder.AppendLine($"\tEndpoint: {httpMethod.HttpMethods.FirstOrDefault()}: /{method.Name}/{httpMethod.Template}");
                                }
                            }
                        }

                        // WebSocket Hubs
                        // if (type.IsSubclassOf(typeof(Hub<>)))
                        // {
                        // builder.AppendLine($"Hub: {type.Name}");
                        // }
                    }
                }

                await context.Response.WriteAsync(builder.ToString(), context.RequestAborted);
            };
        });
    }
}