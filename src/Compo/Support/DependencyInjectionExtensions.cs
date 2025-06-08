using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Compo;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Discover functions in the current AppDomain attributed with the <see cref="FunctionRegistration"/> attribute
    /// and register them to the Service Collection
    /// </summary>
    /// <param name="serviceCollection">Service Collection where the functions are registered</param>
    /// <returns></returns>
    public static IServiceCollection DiscoverFunctions(this IServiceCollection serviceCollection)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var functionTypes = assembly.GetTypes()
                    .Where(t => t.GetInterfaces()
                        .Any(i => i.IsGenericType && (
                            i.GetGenericTypeDefinition() == typeof(IFunction<,>) ||
                            i.GetGenericTypeDefinition() == typeof(IFunction<,,>) ||
                            i.GetGenericTypeDefinition() == typeof(IFunction<,,,>) ||
                            i.GetGenericTypeDefinition() == typeof(IFunctionParams<,>))))
                    .ToList();

                foreach (var functionType in functionTypes)
                {
                    var functionName = functionType.GetCustomAttribute<FunctionRegistrationAttribute>()?.FunctionName;
                    if (functionName == null)
                    {
                        continue; // Skip no attribute
                    }

                    serviceCollection.RegisterFunction(functionType, functionName!);
                }
            }
            catch
            {
                // ignored
            }
        }

        return serviceCollection;
    }

    /// <summary>
    /// Register a function with an or multiple function names
    /// </summary>
    /// <param name="serviceCollection">Service Collection where the function is registered</param>
    /// <param name="T">Function implementation</param>
    /// <param name="names">Function invocation names</param>
    /// <returns></returns>
    public static IServiceCollection RegisterFunction<T>(this IServiceCollection serviceCollection,
        params string[] names)  where T : class, IFunction
    {
        serviceCollection.AddTransient<T>();

        foreach (var name in names)
            serviceCollection.AddSingleton(new FunctionRegistration
                { FunctionType = typeof(T), FunctionName = name });

        return serviceCollection;
    }
    /// <summary>
    /// Register a function with an or multiple function names
    /// </summary>
    /// <param name="serviceCollection">Service Collection where the function is registered</param>
    /// <param name="function">Function implementation</param>
    /// <param name="names">Function invocation names</param>
    /// <returns></returns>
    public static IServiceCollection RegisterFunction(this IServiceCollection serviceCollection, Type function,
        params string[] names)
    {
        serviceCollection.AddTransient(function);

        foreach (var name in names)
            serviceCollection.AddSingleton(new FunctionRegistration
                { FunctionType = function, FunctionName = name });

        return serviceCollection;
    }

    public static IServiceCollection AddExpressionEngine(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddLogging();
        serviceCollection.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();
        serviceCollection.AddScoped<ExpressionParser>();
        serviceCollection.DiscoverFunctions();

        return serviceCollection;
    }

    public static IServiceCollection AddCompo(this IServiceCollection services)
    {
        return services.AddExpressionEngine();
    }
}
