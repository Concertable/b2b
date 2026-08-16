using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class ControllerBoundaryTests
{
    [Fact]
    public void Controllers_do_not_depend_on_TimeProvider()
    {
        var directory = Path.GetDirectoryName(typeof(ControllerBoundaryTests).Assembly.Location)!;
        var controllers = Directory.GetFiles(directory, "Concertable.B2B.*.Api.dll")
            .Select(System.Reflection.Assembly.LoadFrom)
            .SelectMany(GetLoadableTypes)
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));

        var offenders = controllers
            .Where(type => type.GetConstructors().SelectMany(constructor => constructor.GetParameters())
                .Any(parameter => parameter.ParameterType == typeof(TimeProvider)))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(offenders);
    }

    private static IEnumerable<Type> GetLoadableTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
