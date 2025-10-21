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

        var paramsFunction = interfaceTypes.Any(x => x.GetGenericTypeDefinition() == typeof(IFunctionParams<,>));

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

        if (paramsFunction)
        {
            var t =
                from type in interfaceTypes
                where type.GetGenericTypeDefinition() == typeof(IFunctionParams<,>)
                    select type;
            // I need to find the one that fits the best, i.e. if there is a decimal, it should be decimal
            var executeMethod1 = (
                from type in t
                let genericArgument = type.GetGenericArguments().First()
                let match = args.Any(genericArgument.IsInstanceOfType)
                where match
                orderby Order(genericArgument) descending
                select type.GetMethod("Execute"));
            executeMethod = executeMethod1.FirstOrDefault();
        }

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

        // If it is a a params function, we need to handle it differently
        if (paramsFunction)
        {
            var target = parameters.First().ParameterType.GetElementType() ?? throw new Exception();
            var targetArray = Array.CreateInstance(target, args.Length);
            for (var i = 0; i < args.Length; i++)
            {
                targetArray.SetValue(Convert.ChangeType(args[i], target), i);
            }
            return executeMethod.Invoke(invokable, [targetArray]);
        }

        for (var i = 0; i < args.Length; i++)
        {
            var argType = args[i]?.GetType();
            if (args[i]?.GetType() == parameters[i].ParameterType)
            {
                invokeParams[i] = args[i]!;               
            }
            else if(argType?.IsAssignableTo(parameters[i].ParameterType) ?? false)
            {
                invokeParams[i] = args[i]!;
            }
            else if (args[i] is IConvertible convertible)
            {
                invokeParams[i] = Convert.ChangeType(convertible, parameters[i].ParameterType)!;
            }else
            {
                invokeParams[i] = args[i]!;
            }
        }

        return executeMethod.Invoke(invokable, invokeParams);
    }

    // TODO: Determine a better way of handling function calls with split typed arguments.
    private static int Order(Type t)
    {
        return t.Name switch
        {
            "Int32" => 1,
            "Decimal" => 2,
            "String" => 3,
            _ => 0
        };
    }
}
