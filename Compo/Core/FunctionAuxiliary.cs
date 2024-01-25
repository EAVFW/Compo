namespace Compo;

public static class FunctionAuxiliary
{
    /// <summary>
    /// Based on the types of the arguments, the function finds the best suited Execute method form the given IFunction
    /// implementation and invokes the function and returns the result as an object.
    /// </summary>
    /// <param name="invokable">IFunction implementation</param>
    /// <param name="args">Arguments to pass to the Function Execute function</param>
    /// <returns></returns>
    public static object? FunctionInvoker(IFunction invokable, object?[] args)
    {
        var invokableType = invokable.GetType();

        var interfaceTypes = invokableType.GetInterfaces()
            .Where(i => i.IsGenericType).ToArray();

        if (interfaceTypes.Length <= 0) return null;

        // Only get execute function which matches parameter length - not including params stuff yet
        // Overvej at bruge en cache
        var onLength = (
            from type in interfaceTypes
            let genericArguments = type.GetGenericArguments()
            where genericArguments.Length - 1 == args.Length
            select type
        ).ToArray();

        // Find exact match passed on parameter types
        var executeMethod = (
            from type in onLength
            let genericArguments = type.GetGenericArguments()
            let match = !args.Where((t, i) => !genericArguments[i].IsInstanceOfType(t)).Any()
            where match
            select type.GetMethod("Execute")).FirstOrDefault();

        if (executeMethod == null)
        {
            executeMethod = onLength.FirstOrDefault()?.GetMethod("Execute");
            if (executeMethod == null)
            {
                return null;
            }
        }

        // Convert parameters to the expected types
        var parameters = executeMethod.GetParameters();
        var invokeParams = new object[args.Length];

        for (var i = 0; i < args.Length; i++)
        {
            // Virker det som "normal" casting
            invokeParams[i] = Convert.ChangeType(args[i], parameters[i].ParameterType)!;
        }

        return executeMethod.Invoke(invokable, invokeParams);
    }
}
