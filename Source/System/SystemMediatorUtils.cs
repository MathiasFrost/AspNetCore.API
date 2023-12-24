using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspNetCore.API.System;

internal static class SystemMediatRHelper
{
    public static IServiceCollection AddSystemMediatR<TEnum>(this IServiceCollection services, Action<SystemHandlerConfiguration<TEnum>> configuration)
        where TEnum : Enum
    {
        services.AddMediatR(serviceConfiguration =>
        {
            serviceConfiguration.MediatorImplementationType = typeof(SystemMediator<TEnum>);
            var config = new SystemHandlerConfiguration<TEnum>(services, serviceConfiguration);
            configuration(config);
        });

        return services;
    }

    private static void ConnectImplementationsToTypesClosing(Type openRequestInterface,
        IServiceCollection services,
        Assembly assemblyToScan,
        bool addIfAlreadyExists,
        MediatRServiceConfiguration configuration)
    {
        var concretions = new List<Type>();
        var interfaces = new List<Type>();
        foreach (Type type in assemblyToScan.DefinedTypes.Where(static t => !t.IsOpenGeneric()).Where(configuration.TypeEvaluator))
        {
            Type[] interfaceTypes = type.FindInterfacesThatClose(openRequestInterface).ToArray();
            if (!interfaceTypes.Any()) continue;

            if (type.IsConcrete()) concretions.Add(type);

            foreach (Type? interfaceType in interfaceTypes) interfaces.Fill(interfaceType);
        }

        foreach (Type @interface in interfaces)
        {
            List<Type> exactMatches = concretions.Where(x => x.CanBeCastTo(@interface)).ToList();
            if (addIfAlreadyExists)
            {
                foreach (Type type in exactMatches) services.AddTransient(@interface, type);
            }
            else
            {
                if (exactMatches.Count > 1) exactMatches.RemoveAll(m => !IsMatchingWithInterface(m, @interface));

                foreach (Type type in exactMatches) services.TryAddTransient(@interface, type);
            }

            if (!@interface.IsOpenGeneric()) AddConcretionsThatCouldBeClosed(@interface, concretions, services);
        }
    }

    private static bool IsMatchingWithInterface(Type? handlerType, Type? handlerInterface)
    {
        if (handlerType == null || handlerInterface == null) return false;
        if (handlerType.IsInterface)
        {
            if (handlerType.GenericTypeArguments.SequenceEqual(handlerInterface.GenericTypeArguments)) return true;
        }
        else
        {
            return IsMatchingWithInterface(handlerType.GetInterface(handlerInterface.Name), handlerInterface);
        }

        return false;
    }

    private static void AddConcretionsThatCouldBeClosed(Type @interface, List<Type> concretions, IServiceCollection services)
    {
        foreach (Type type in concretions.Where(x => x.IsOpenGeneric() && x.CouldCloseTo(@interface)))
        {
            try
            {
                services.TryAddTransient(@interface, type.MakeGenericType(@interface.GenericTypeArguments));
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    private static bool CouldCloseTo(this Type openConcretion, Type closedInterface)
    {
        Type openInterface = closedInterface.GetGenericTypeDefinition();
        Type[] arguments = closedInterface.GenericTypeArguments;

        Type[] concreteArguments = openConcretion.GenericTypeArguments;
        return arguments.Length == concreteArguments.Length && openConcretion.CanBeCastTo(openInterface);
    }

    private static bool CanBeCastTo(this Type? pluggedType, Type pluginType)
    {
        if (pluggedType == null) return false;
        return pluggedType == pluginType || pluginType.IsAssignableFrom(pluggedType);
    }

    private static bool IsOpenGeneric(this Type type) => type.IsGenericTypeDefinition || type.ContainsGenericParameters;

    private static IEnumerable<Type> FindInterfacesThatClose(this Type? pluggedType, Type templateType) =>
        FindInterfacesThatClosesCore(pluggedType, templateType).Distinct();

    private static IEnumerable<Type> FindInterfacesThatClosesCore(Type? pluggedType, Type templateType)
    {
        if (pluggedType == null) yield break;

        if (!pluggedType.IsConcrete()) yield break;

        if (templateType.IsInterface)
            foreach (Type interfaceType in pluggedType.GetInterfaces().Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == templateType))
                yield return interfaceType;

        else if (pluggedType.BaseType!.IsGenericType && pluggedType.BaseType!.GetGenericTypeDefinition() == templateType) yield return pluggedType.BaseType!;

        if (pluggedType.BaseType == typeof(object)) yield break;

        foreach (Type interfaceType in FindInterfacesThatClosesCore(pluggedType.BaseType!, templateType)) yield return interfaceType;
    }

    private static bool IsConcrete(this Type? type) => type is { IsAbstract: false, IsInterface: false };

    private static void Fill<T>(this ICollection<T> list, T value)
    {
        if (list.Contains(value)) return;
        list.Add(value);
    }

    public sealed class SystemHandlerConfiguration<TEnum> where TEnum : Enum
    {
        internal static readonly IDictionary<TEnum, Type> HandlerTypes = new Dictionary<TEnum, Type>();
        private readonly MediatRServiceConfiguration _configuration;
        private readonly IServiceCollection _services;

        public SystemHandlerConfiguration(IServiceCollection services, MediatRServiceConfiguration configuration)
        {
            _services = services;
            _configuration = configuration;
        }

        public void AddHandlerType(TEnum system, Type handlerType) => HandlerTypes.Add(system, handlerType);

        public void RegisterSystemServicesFromAssemblyContaining<T>()
        {
            Type type = typeof(T);
            _configuration.RegisterServicesFromAssemblyContaining(type);
            foreach (Type handlerType in HandlerTypes.Values)
                ConnectImplementationsToTypesClosing(handlerType, _services, type.Assembly, false, _configuration);
        }
    }
}