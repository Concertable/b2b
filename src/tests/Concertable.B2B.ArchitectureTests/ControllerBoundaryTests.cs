using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class ControllerBoundaryTests
{
    [Fact]
    public void Controllers_do_not_depend_on_TimeProvider()
    {
        var offenders = ControllerTypes()
            .Where(type => type.GetConstructors().SelectMany(constructor => constructor.GetParameters())
                .Any(parameter => parameter.ParameterType == typeof(TimeProvider)))
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Mutating_endpoints_declare_authorization_explicitly()
    {
        var mutating = new[]
        {
            typeof(HttpPostAttribute),
            typeof(HttpPutAttribute),
            typeof(HttpPatchAttribute),
            typeof(HttpDeleteAttribute),
        };

        var offenders = ControllerTypes()
            .SelectMany(type => type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            .Where(method => mutating.Any(verb => method.IsDefined(verb, inherit: true)))
            .Where(method => !method.IsDefined(typeof(AuthorizeAttribute), inherit: true)
                && !(method.DeclaringType?.IsDefined(typeof(AuthorizeAttribute), inherit: true) ?? false)
                && !method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                && !(method.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute), inherit: true) ?? false))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .ToArray();

        Assert.Empty(offenders);
    }

    private static IEnumerable<Type> ControllerTypes()
    {
        var directory = Path.GetDirectoryName(typeof(ControllerBoundaryTests).Assembly.Location)!;
        return Directory.GetFiles(directory, "Concertable.B2B.*.Api.dll")
            .Concat(Directory.GetFiles(directory, "Concertable.B2B.Web.dll"))
            .Select(System.Reflection.Assembly.LoadFrom)
            .SelectMany(GetLoadableTypes)
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));
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
