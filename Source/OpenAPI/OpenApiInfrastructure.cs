using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AspNetCore.API.OpenAPI;

internal static class OpenApiInfrastructure
{
    private static bool _hasCompiled;

    public static IApplicationBuilder UseOpenApi(this IApplicationBuilder app)
    {
        return app.Use(static async (context, next) =>
        {
            if (context.Request.Path != "/openapi")
            {
                await next();
                return;
            }

            string? executing = Assembly.GetExecutingAssembly().GetName().Name;
            string? filePath = null;
            if (executing != null)
            {
                var name = $"{executing}.OpenAPI.json";
                filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
                if (!_hasCompiled && File.Exists(filePath))
                {
                    _hasCompiled = true;
                    string content = await File.ReadAllTextAsync(filePath);
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(content, context.RequestAborted);
                    return;
                }
            }

            var res = new OpenApiDocument {
                OpenApi = "3.0.1",
                Info = new Info { Title = AppDomain.CurrentDomain.FriendlyName, Version = "1.0" },
                Paths = new Dictionary<string, Dictionary<string, Path>>()
            };

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
                foreach (Type controller in types)
                {
                    // HTTP REST Controllers
                    // Check if the type inherits from ControllerBase and has the ApiController attribute
                    if (controller.IsSubclassOf(typeof(ControllerBase)) && Attribute.IsDefined(controller, typeof(ApiControllerAttribute)))
                    {
                        // Retrieve the Route attribute if it exists
                        var routeAttr = controller.GetCustomAttribute<RouteAttribute>();
                        if (routeAttr != null)
                        {
                            IEnumerable<MethodInfo> methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                            foreach (MethodInfo method in methods)
                            {
                                if (method.GetCustomAttributes().FirstOrDefault(static attribute => attribute is HttpMethodAttribute) is not
                                    HttpMethodAttribute httpMethod) continue;

                                string endpoint = OpenApiTypeHelper.GetEndpoint(controller, routeAttr.Template, method, httpMethod.Template);

                                Path path = OpenApiTypeHelper.GetPath(controller, routeAttr.Template, method, httpMethod.Template);

                                string m = httpMethod.HttpMethods.First().ToLower();
                                if (res.Paths.TryGetValue(endpoint, out Dictionary<string, Path>? value)) value.Add(m, path);
                                else res.Paths.Add(endpoint, new Dictionary<string, Path> { { m, path } });
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

            res.Components = OpenApiTypeHelper.Components;
            if (filePath != null) await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = true }));
            await context.Response.WriteAsJsonAsync(res, context.RequestAborted);
        });
    }
}