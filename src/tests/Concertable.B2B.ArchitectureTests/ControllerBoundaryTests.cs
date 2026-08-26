using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class ControllerBoundaryTests
{
    [Fact]
    public void Controllers_do_not_depend_on_TimeProvider()
    {
        var offenders = GetControllers()
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

        var offenders = GetControllers()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => mutating.Any(verb => method.IsDefined(verb, inherit: true)))
            .Where(method => !method.IsDefined(typeof(AuthorizeAttribute), inherit: true)
                && !(method.DeclaringType?.IsDefined(typeof(AuthorizeAttribute), inherit: true) ?? false)
                && !method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                && !(method.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute), inherit: true) ?? false))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Controller_route_segments_match_controller_names_and_routes()
    {
        var offenders = GetControllers()
            .Select(type => new
            {
                Type = type,
                Field = type.GetField(
                    "RouteSegment",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
            })
            .Where(candidate => candidate.Field is not null)
            .Where(candidate =>
            {
                var segment = candidate.Field!.GetRawConstantValue() as string;
                var controllerName = candidate.Type.Name[..^"Controller".Length];
                var expectedSegment = Regex.Matches(controllerName, "[A-Z][a-z0-9]*")
                    .Select(match => match.Value.ToLowerInvariant())
                    .Last();
                var route = candidate.Type.GetCustomAttribute<RouteAttribute>()?.Template;

                return segment != expectedSegment || route?.Split('/').Last() != segment;
            })
            .Select(candidate => candidate.Type.FullName)
            .ToArray();

        Assert.Empty(offenders);
    }

    private static IEnumerable<Type> GetControllers()
    {
        var directory = Path.GetDirectoryName(typeof(ControllerBoundaryTests).Assembly.Location)!;
        return Directory.GetFiles(directory, "Concertable.B2B.*.Api.dll")
            .Concat(Directory.GetFiles(directory, "Concertable.B2B.Web.dll"))
            .Select(Assembly.LoadFrom)
            .SelectMany(GetLoadableTypes)
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
