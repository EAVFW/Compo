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

        // Handle True/False bool conversion
        // If any parameter expects True or False struct and we have a bool argument, convert it
        var processedArgs = ProcessBooleanTypeArguments(args, interfaceTypes);

        // Only get execute function which matches parameter length - not including params stuff yet
        // Overvej at bruge en cache
        var onLength = (
            from type in interfaceTypes
            let genericArguments = type.GetGenericArguments()
            where genericArguments.Length - 1 == processedArgs.Length
            select type
        ).ToArray();

        // Find exact match passed on parameter types (including Lazy<T> matching)
        var executeMethod = (
            from type in onLength
            let genericArguments = type.GetGenericArguments()
            let match = !processedArgs.Where((t, i) => !IsParameterMatch(t, genericArguments[i])).Any()
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
                let match = processedArgs.Any(genericArgument.IsInstanceOfType)
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
        var invokeParams = new object[processedArgs.Length];

        // If it is a a params function, we need to handle it differently
        if (paramsFunction)
        {
            var target = parameters.First().ParameterType.GetElementType() ?? throw new Exception();
            var targetArray = Array.CreateInstance(target, processedArgs.Length);
            for (var i = 0; i < processedArgs.Length; i++)
            {
                targetArray.SetValue(Convert.ChangeType(processedArgs[i], target), i);
            }
            return executeMethod.Invoke(invokable, [targetArray]);
        }

        for (var i = 0; i < processedArgs.Length; i++)
        {
            var argType = processedArgs[i]?.GetType();
            var paramType = parameters[i].ParameterType;

            // Handle Lazy<T> conversion
            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                var targetType = paramType.GetGenericArguments()[0];

                if (processedArgs[i] is Lazy<object?> lazyObj)
                {
                    // Convert Lazy<object?> to Lazy<T>
                    invokeParams[i] = ConvertLazy(lazyObj, targetType);
                }
                else
                {
                    // Wrap non-lazy value in Lazy<T>
                    invokeParams[i] = WrapInLazy(processedArgs[i], targetType);
                }
            }
            else if (processedArgs[i]?.GetType() == paramType)
            {
                invokeParams[i] = processedArgs[i]!;
            }
            else if (argType?.IsAssignableTo(paramType) ?? false)
            {
                invokeParams[i] = processedArgs[i]!;
            }
            else if (processedArgs[i] is IConvertible convertible)
            {
                invokeParams[i] = Convert.ChangeType(convertible, paramType)!;
            }
            else
            {
                invokeParams[i] = processedArgs[i]!;
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

        // Check for True/False struct matching
        if (parameterType.Name == "True" && argType.Name == "True")
        {
            return true;
        }

        if (parameterType.Name == "False" && argType.Name == "False")
        {
            return true;
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
            if (value is IConvertible) return (T) Convert.ChangeType(value, typeof(T));
            return (T) value;
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
            if (value is IConvertible) return (T) Convert.ChangeType(value, typeof(T));
            return (T) value;
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

    /// <summary>
    /// Processes arguments to convert bool values to True/False structs when needed.
    /// This enables type-safe conditional returns in generic functions.
    /// </summary>
    private static object?[] ProcessBooleanTypeArguments(object?[] args, Type[] interfaceTypes)
    {
        // Check if any interface has True or False as first parameter
        var hasBooleanTypes = interfaceTypes.Any(iface =>
        {
            var genericArgs = iface.GetGenericArguments();
            if (genericArgs.Length == 0) return false;

            var firstParamType = genericArgs[0];
            return firstParamType.Name == "True" || firstParamType.Name == "False";
        });

        if (!hasBooleanTypes || args.Length == 0)
        {
            return args; // No conversion needed
        }

        // Check if first argument is a bool
        if (args[0] is not bool boolValue)
        {
            return args; // First arg is not bool, no conversion needed
        }

        // Convert bool to True or False struct
        var processedArgs = new object?[args.Length];

        // Get the True and False types from the Compo assembly
        var trueType = interfaceTypes
            .SelectMany(i => i.GetGenericArguments())
            .FirstOrDefault(t => t.Name == "True");

        var falseType = interfaceTypes
            .SelectMany(i => i.GetGenericArguments())
            .FirstOrDefault(t => t.Name == "False");

        if (trueType == null || falseType == null)
        {
            return args; // Types not found, fallback
        }

        // Create instance of True or False based on bool value
        processedArgs[0] = boolValue
            ? Activator.CreateInstance(trueType)!
            : Activator.CreateInstance(falseType)!;

        // Copy remaining arguments
        Array.Copy(args, 1, processedArgs, 1, args.Length - 1);

        return processedArgs;
    }
}