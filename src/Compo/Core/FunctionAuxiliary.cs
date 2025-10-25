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

        // Find exact match passed on parameter types (including Lazy<T> matching)
        var executeMethod = (
            from type in onLength
            let genericArguments = type.GetGenericArguments()
            let match = !args.Where((t, i) => !IsParameterMatch(t, genericArguments[i])).Any()
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
            var paramType = parameters[i].ParameterType;

            // Handle Lazy<T> conversion
            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                var targetType = paramType.GetGenericArguments()[0];

                if (args[i] is Lazy<object?> lazyObj)
                {
                    // Convert Lazy<object?> to Lazy<T>
                    invokeParams[i] = ConvertLazy(lazyObj, targetType);
                }
                else
                {
                    // Wrap non-lazy value in Lazy<T>
                    invokeParams[i] = WrapInLazy(args[i], targetType);
                }
            }
            else if (args[i]?.GetType() == paramType)
            {
                invokeParams[i] = args[i]!;
            }
            else if(argType?.IsAssignableTo(paramType) ?? false)
            {
                invokeParams[i] = args[i]!;
            }
            else if (args[i] is IConvertible convertible)
            {
                invokeParams[i] = Convert.ChangeType(convertible, paramType)!;
            }
            else
            {
                invokeParams[i] = args[i]!;
            }
        }

        return executeMethod.Invoke(invokable, invokeParams);
    }

    private static bool IsParameterMatch(object? arg, Type parameterType)
    {
        if (arg == null) return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;

        var argType = arg.GetType();

        // Check if parameter expects Lazy<T> and arg is Lazy<object?>
        if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Lazy<>))
        {
            return arg is Lazy<object?>;
        }

        return parameterType.IsInstanceOfType(arg);
    }

    private static object ConvertLazy(Lazy<object?> source, Type targetType)
    {
        // Create Lazy<T> from Lazy<object?>
        var lazyType = typeof(Lazy<>).MakeGenericType(targetType);

        // Create a factory method dynamically
        var factoryMethod = typeof(FunctionAuxiliary)
            .GetMethod(nameof(CreateLazyFactory), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(targetType);

        var factory = factoryMethod.Invoke(null, new object[] { source })!;
        return Activator.CreateInstance(lazyType, factory)!;
    }

    private static Func<T> CreateLazyFactory<T>(Lazy<object?> source)
    {
        return () =>
        {
            var value = source.Value;
            if (value == null) return default!;
            if (value is T typed) return typed;
            if (value is IConvertible) return (T)Convert.ChangeType(value, typeof(T));
            return (T)value;
        };
    }

    private static object WrapInLazy(object? value, Type targetType)
    {
        var lazyType = typeof(Lazy<>).MakeGenericType(targetType);

        // Create a factory method dynamically
        var factoryMethod = typeof(FunctionAuxiliary)
            .GetMethod(nameof(CreateValueFactory), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(targetType);

        var factory = factoryMethod.Invoke(null, new object?[] { value })!;
        return Activator.CreateInstance(lazyType, factory)!;
    }

    private static Func<T> CreateValueFactory<T>(object? value)
    {
        return () =>
        {
            if (value == null) return default!;
            if (value is T typed) return typed;
            if (value is IConvertible) return (T)Convert.ChangeType(value, typeof(T));
            return (T)value;
        };
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
