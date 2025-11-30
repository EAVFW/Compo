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
                            i.GetGenericTypeDefinition() == typeof(IFunction<>) ||
                            i.GetGenericTypeDefinition() == typeof(IFunction<,>) ||
                            i.GetGenericTypeDefinition() == typeof(IFunction<,,>) ||
                            i.GetGenericTypeDefinition() == typeof(IFunction<,,,>) ||
                            i.GetGenericTypeDefinition() == typeof(IFunctionParams<,>))))
                    .ToList();

                foreach (var functionType in functionTypes)
                {
                    // Get all FunctionRegistration attributes (can have multiple)
                    var functionNames = functionType.GetCustomAttributes<FunctionRegistrationAttribute>()
                        .Select(attr => attr.FunctionName)
                        .ToArray();

                    if (functionNames.Length == 0)
                    {
                        continue; // Skip if no attributes
                    }

                    serviceCollection.RegisterFunction(functionType, functionNames);
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
        params string[] names) where T : class, IFunction
    {
        var functionType = typeof(T);

        // T is always a closed type when using generic method
        serviceCollection.AddTransient<T>();

        // Register for each IFunction interface the type implements
        var functionInterfaces = GetFunctionInterfaces(functionType);
        foreach (var funcInterface in functionInterfaces)
        {
            var argumentTypes = ExtractArgumentTypesFromInterface(funcInterface);
            var parameters = ExtractParametersForInterface(functionType, argumentTypes);

            foreach (var name in names)
                serviceCollection.AddSingleton(new FunctionRegistration
                {
                    FunctionType = functionType,
                    FunctionName = name,
                    ArgumentTypes = argumentTypes,
                    Parameters = parameters,
                    IsOpenGeneric = false  // Generic method parameter is always closed
                });
        }

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
        var isOpenGeneric = function.IsGenericTypeDefinition;

        // Only register closed generic types in DI
        if (!isOpenGeneric)
        {
            serviceCollection.AddTransient(function);
        }

        // Register for each IFunction interface the type implements
        var functionInterfaces = GetFunctionInterfaces(function);
        foreach (var funcInterface in functionInterfaces)
        {
            var argumentTypes = ExtractArgumentTypesFromInterface(funcInterface);
            var parameters = ExtractParametersForInterface(function, argumentTypes);

            foreach (var name in names)
                serviceCollection.AddSingleton(new FunctionRegistration
                {
                    FunctionType = function,
                    FunctionName = name,
                    ArgumentTypes = argumentTypes,
                    Parameters = parameters,
                    IsOpenGeneric = isOpenGeneric
                });
        }

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

    /// <summary>
    /// Gets all IFunction interfaces implemented by a type.
    /// </summary>
    private static Type[] GetFunctionInterfaces(Type functionType)
    {
        return functionType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IFunction"))
            .ToArray();
    }

    /// <summary>
    /// Extracts argument types from a specific IFunction interface.
    /// Returns array of argument types excluding the return type.
    /// For IFunction&lt;T1, T2, ..., TN, TResult&gt;, this contains [T1, T2, ..., TN] (excluding TResult).
    /// </summary>
    private static Type[] ExtractArgumentTypesFromInterface(Type functionInterface)
    {
        var genericArgs = functionInterface.GetGenericArguments();
        // Last argument is return type, exclude it
        return genericArgs.Take(genericArgs.Length - 1).ToArray();
    }

    /// <summary>
    /// Extracts parameter information from the Execute method that matches the given argument types.
    /// Used for checking attributes like [NestedExpression].
    /// </summary>
    private static ParameterInfo[]? ExtractParametersForInterface(Type functionType, Type[] argumentTypes)
    {
        var executeMethod = functionType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance, null, argumentTypes, null);
        return executeMethod?.GetParameters();
    }
}