using Microsoft.Extensions.DependencyInjection;
using Compo.Functions.Logical;

namespace Compo.Test.Registration;

/// <summary>
/// Debug test to understand why NotFunction is not being discovered/registered.
/// </summary>
public class NotFunctionDiscoveryTest
{
    [Fact]
    public void NotFunction_ShouldBeDiscovered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();

        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "not")
            .ToList();

        registrations.Should().HaveCountGreaterThan(0, "NotFunction should be discovered by DiscoverFunctions");
    }

    [Fact]
    public void NotFunction_ShouldRegisterManually()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterFunction<NotFunction>("not");

        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "not")
            .ToList();

        registrations.Should().HaveCount(1, "NotFunction should register successfully");
        registrations[0].ArgumentTypes.Should().HaveCount(1);
        registrations[0].ArgumentTypes![0].Should().Be(typeof(bool));
    }

    [Fact]
    public void IfFunction_ShouldBeDiscovered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.DiscoverFunctions();

        var registrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .Where(fr => fr?.FunctionName == "if")
            .ToList();

        registrations.Should().HaveCountGreaterThan(0, "IfFunction should be discovered");
    }

    [Fact]
    public void AddExpressionEngine_ShouldDiscoverBuiltInFunctions()
    {
        var services = new ServiceCollection();
        services.AddExpressionEngine();

        var allRegistrations = services
            .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
            .Select(sd => sd.ImplementationInstance as FunctionRegistration)
            .ToList();

        allRegistrations.Should().HaveCountGreaterThan(0, "Should discover some functions");

        var functionNames = allRegistrations.Select(r => r.FunctionName).Distinct().OrderBy(x => x).ToList();

        // Output for debugging
        var output = string.Join(", ", functionNames);
        output.Should().NotBeNullOrEmpty($"Discovered functions: {output}");
    }
}
