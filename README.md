# Compo

Compo is from function composition, which is what the language is based on, besides being one-to-one with the language known as Expression from Power Automate, Logic Apps, Synapse and possibly more. Each function can take any number of arguments, and return any type, where the arguments either are values or other functions. This way expressions can be expressed in a simple yet powerful and extendable language. 


## Why

This tool makes the process of having a Domain Specific Language (DSL) in you application easier, but having the foundation of the language with pre-defined functions, and the ability to extend it with your own functions.

This is a re-implementation of [ExpressionEngine](https://github.com/delegateas/expressionengine), but with some of the "problems" fixed.

Functions are no longer built upon `ValueContainer` (`ValueContainer.cs`) and the function are parsed to an AST before execution.
Furthermore, Sprache have been replaced with Pidgin, which is a parser combinator library like Sprache, but with better performance.

## Usage

```csharp
var serviceCollection = new ServiceCollection();
serviceCollection.AddExpressionEngine();
var serviceProvider = serviceCollection.BuildServiceProvider();
var expressionEngine = serviceProvider.GetRequiredService<IExpressionEngine>();

var result = expressionEngine.Execute("add(1, 2");
```
`result` is a `Result` object, which contains the result of the execution, and a list of errors if any.

The `IExpressionEngine` is added as scoped, but out-of-box functions are transient.

## Functions

Functions are following the functions defined by Microsoft from Power Automate, where the language also have its roots. TODO: Link to own docs over available functions.

It is easy to add your own functions, or overwrite existing ones.

### Function implementation

Functions are implemented by implementing the `IFunction`, such as

```csharp
public class MultiplicationFunction :
    IFunction<double, double, double>
{
    public double Execute(double t1, double t2)
    {
        return t1 * t2;
    }
}
```

A function, here `MultiplicationFunction`, can implement `IFunction<_>` more than once and the best match will be picked by the Expression Engine, see `ExpressionEvaluator.cs`.

The last generic type is the function return type, and the first generic type(s) are the argument types. IFunction currently supports up to 3 arguments and a `IFunctionParams` which takes a parameter of the same type. _See `IFunction.cs`_.

#### Function state

Functions are transient and they can be stateful be depending on a scoped or singleton _service_.

See "reference to unit test" as an example.

_Likewise can a function depend on a HttpClient to make a HTTP request_.

### Registering functions

A function can be registered with multiple _aliases_, such as `mult` and `multiply` for the `MultiplicationFunction`.
Either by using the `RegisterFunction` extension method on `IServiceCollection` or by using the `RegisterFunction` attribute :

```csharp
[RegisterFunction("mult", "multiply")
public class MultiplicationFunction :
    IFunction<double, double, double>
```

```csharp
services.RegisterFunctions(typeof(<IFunction implementaiton>), "mult", "multiply");
```

### Overriding functions

Standard Dependency Injection is used to register and resolve functions and the last registered function will be used.
To override a function, simply register a new function with the same alias(es) and do it later, to this the extension method is required guarantee the registering order.

## Value conversions

Given the "loose" relation of functions, some conversions might happen.
For example, if you have `add(1.2, 3)`, then the IFunction<double, double, double> will be used, and `3` will be converted to a double.

This is done `Convert` from the `System` namespace.

// Consider:
This is done by the `IValueConverter` interface, which can be implemented and registered in the `ServiceCollection`:


## Performance comparison

// TODO: Create performance comparison between Compo and ExpressionEngine, and also for Combo versions going forward.
// label: performance
