using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Compo.Test.Registration;

/// <summary>
/// Tests for function registration mechanics, especially for functions with multiple Execute overloads.
/// Validates that ExtractParameters doesn't throw AmbiguousMatchException.
/// </summary>
public class FunctionRegistrationTest
{
    /// <summary>
    /// Test function with multiple Execute overloads (similar to LookupFunction).
    /// This would previously cause AmbiguousMatchException in ExtractParameters.
    /// </summary>
    public class MultiOverloadFunction
        : IFunction<string, string>
        , IFunction<string, string, string>
        , IFunction<string, string, string, string>
    {
        public string Execute(string t1)
            => $"1:{t1}";

        public string Execute(string t1, string t2)
            => $"2:{t1},{t2}";

        public string Execute(string t1, string t2, string t3)
            => $"3:{t1},{t2},{t3}";
    }

    /// <summary>
    /// Test function with [NestedExpression] attributes on some parameters.
    /// This validates that ExtractParameters can find the right overload to inspect parameter attributes.
    /// </summary>
    public class NestedExpressionFunction
        : IFunction<string, object, string>
        , IFunction<string, object, object, string>
    {
        public string Execute(string condition, [NestedExpression] object trueBranch)
            => condition == "true" ? trueBranch.ToString()! : "false";

        public string Execute(string condition, [NestedExpression] object trueBranch, [NestedExpression] object falseBranch)
            => condition == "true" ? trueBranch.ToString()! : falseBranch.ToString()!;
    }

    [Fact]
    public void RegisterFunction_WithMultipleOverloads_ShouldNotThrowAmbiguousMatch()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // This should not throw AmbiguousMatchException
        var action = () => services.RegisterFunction<MultiOverloadFunction>("multi");

        action.Should().NotThrow<AmbiguousMatchException>();
    }

    [Fact]
    public void RegisterFunction_WithMultipleOverloads_ShouldCreateMultipleRegistrations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MultiOverloadFunction>("multi");

        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "multi")
            .ToList();

        // Should create 3 registrations - one for each IFunction interface
        registrations.Should().HaveCount(3);
    }

    [Fact]
    public void RegisterFunction_WithMultipleOverloads_ShouldHaveCorrectArgumentTypes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MultiOverloadFunction>("multi");

        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "multi")
            .OrderBy(fr => fr.ArgumentTypes?.Length ?? 0)
            .ToList();

        // First overload: IFunction<string, string> - 1 argument
        registrations[0].ArgumentTypes.Should().HaveCount(1);
        registrations[0].ArgumentTypes![0].Should().Be(typeof(string));

        // Second overload: IFunction<string, string, string> - 2 arguments
        registrations[1].ArgumentTypes.Should().HaveCount(2);
        registrations[1].ArgumentTypes![0].Should().Be(typeof(string));
        registrations[1].ArgumentTypes![1].Should().Be(typeof(string));

        // Third overload: IFunction<string, string, string, string> - 3 arguments
        registrations[2].ArgumentTypes.Should().HaveCount(3);
        registrations[2].ArgumentTypes![0].Should().Be(typeof(string));
        registrations[2].ArgumentTypes![1].Should().Be(typeof(string));
        registrations[2].ArgumentTypes![2].Should().Be(typeof(string));
    }

    [Fact]
    public void RegisterFunction_WithMultipleOverloads_ShouldHaveCorrectParameters()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MultiOverloadFunction>("multi");

        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "multi")
            .OrderBy(fr => fr.ArgumentTypes?.Length ?? 0)
            .ToList();

        // Each registration should have ParameterInfo matching its argument count
        registrations[0].Parameters.Should().HaveCount(1);
        registrations[0].Parameters![0].Name.Should().Be("t1");

        registrations[1].Parameters.Should().HaveCount(2);
        registrations[1].Parameters![0].Name.Should().Be("t1");
        registrations[1].Parameters![1].Name.Should().Be("t2");

        registrations[2].Parameters.Should().HaveCount(3);
        registrations[2].Parameters![0].Name.Should().Be("t1");
        registrations[2].Parameters![1].Name.Should().Be("t2");
        registrations[2].Parameters![2].Name.Should().Be("t3");
    }

    [Fact]
    public void RegisterFunction_WithNestedExpressionAttribute_ShouldPreserveAttributes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<NestedExpressionFunction>("nested");

        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "nested")
            .OrderBy(fr => fr.ArgumentTypes?.Length ?? 0)
            .ToList();

        registrations.Should().HaveCount(2);

        // First overload: IFunction<string, object, string> - 2 arguments
        // Second parameter should have [NestedExpression] attribute
        var firstOverload = registrations[0];
        firstOverload.Parameters.Should().HaveCount(2);
        var trueBranchParam = firstOverload.Parameters![1];
        trueBranchParam.Name.Should().Be("trueBranch");
        trueBranchParam.GetCustomAttribute<NestedExpressionAttribute>().Should().NotBeNull();

        // Second overload: IFunction<string, object, object, string> - 3 arguments
        // Both second and third parameters should have [NestedExpression] attribute
        var secondOverload = registrations[1];
        secondOverload.Parameters.Should().HaveCount(3);
        secondOverload.Parameters![1].GetCustomAttribute<NestedExpressionAttribute>().Should().NotBeNull();
        secondOverload.Parameters![2].GetCustomAttribute<NestedExpressionAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void RegisterFunction_NonGenericOverload_WithMultipleInterfaces_ShouldWork()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Test non-generic RegisterFunction overload
        var action = () => services.RegisterFunction(typeof(MultiOverloadFunction), "multi");

        action.Should().NotThrow<AmbiguousMatchException>();

        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "multi")
            .ToList();

        registrations.Should().HaveCount(3);
    }

    [Fact]
    public void MultiOverloadFunction_ShouldExecuteCorrectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<MultiOverloadFunction>("multi");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        // Test 1-arg overload
        var result1 = evaluator.Evaluate(engine.BuildAst("@multi('a')").Value!);
        result1.Should().Be("1:a");

        // Test 2-arg overload
        var result2 = evaluator.Evaluate(engine.BuildAst("@multi('a', 'b')").Value!);
        result2.Should().Be("2:a,b");

        // Test 3-arg overload
        var result3 = evaluator.Evaluate(engine.BuildAst("@multi('a', 'b', 'c')").Value!);
        result3.Should().Be("3:a,b,c");
    }

    /// <summary>
    /// Regression test: Ensure that the old single-interface pattern still works.
    /// </summary>
    public class SingleInterfaceFunction : IFunction<string, string, string>
    {
        public string Execute(string t1, string t2) => $"{t1}+{t2}";
    }

    [Fact]
    public void RegisterFunction_WithSingleInterface_ShouldStillWork()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<SingleInterfaceFunction>("single");

        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "single")
            .ToList();

        // Should create exactly 1 registration
        registrations.Should().HaveCount(1);
        registrations[0].ArgumentTypes.Should().HaveCount(2);
    }

    [Fact]
    public void SingleInterfaceFunction_ShouldExecuteCorrectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<SingleInterfaceFunction>("single");
        services.DiscoverFunctions();
        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        var serviceProvider = services.BuildServiceProvider();
        var evaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();
        var engine = new ExpressionParser();

        var result = evaluator.Evaluate(engine.BuildAst("@single('foo', 'bar')").Value!);
        result.Should().Be("foo+bar");
    }
}
