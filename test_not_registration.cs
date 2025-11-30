using Compo;
using Compo.Functions.Logical;
using Microsoft.Extensions.DependencyInjection;
using System;

var services = new ServiceCollection();
services.AddLogging();

try
{
    Console.WriteLine("Attempting to register NotFunction...");
    services.RegisterFunction<NotFunction>("not");
    Console.WriteLine("SUCCESS: NotFunction registered");
    
    var registrations = services
        .Where(sd => sd.ServiceType == typeof(FunctionRegistration))
        .Select(sd => sd.ImplementationInstance as FunctionRegistration)
        .Where(fr => fr?.FunctionName == "not")
        .ToList();
    
    Console.WriteLine($"Number of registrations for 'not': {registrations.Count}");
    foreach (var reg in registrations)
    {
        Console.WriteLine($"  - ArgumentTypes: {reg.ArgumentTypes?.Length ?? 0}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"FAILED: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
}
