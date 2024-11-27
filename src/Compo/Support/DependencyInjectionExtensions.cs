using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Compo;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Discover functions in the current AppDomain attributed with the <see cref="FunctionRegistration"/> attribute
    /// and register them to the Service Collection
    /// </summary>
    /// <param name="serviceCollection">Service Collection where the functions is registered</param>
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

                    serviceCollection.RegisterFunctions(functionType, functionName!);
                }
            }
            catch
            {
            }
        }
       

        return serviceCollection;
    }


    /// <summary>
    /// Register a function with a or multiple function names
    /// </summary>
    /// <param name="serviceCollection">Service Collection where the functions is registered</param>
    /// <param name="function">Function implementation</param>
    /// <param name="names">Function invocation names</param>
    /// <returns></returns>
    public static IServiceCollection RegisterFunctions(this IServiceCollection serviceCollection, Type function,
        params string[] names)
    {
        serviceCollection.AddTransient(function);

        foreach (var name in names)
            serviceCollection.AddSingleton( new FunctionRegistration
                { FunctionType = function, FunctionName = name });

        return serviceCollection;
    }

    public static IServiceCollection AddExpressionEngine(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddLogging();
        serviceCollection.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();
        serviceCollection.DiscoverFunctions();

        return serviceCollection;
    }
    public static IServiceCollection AddCompo(this IServiceCollection services)
    {
        return services.AddExpressionEngine();
    }
}
