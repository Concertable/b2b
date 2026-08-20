using Microsoft.CodeAnalysis;

namespace Concertable.B2B.DealDispatch.Generators;

internal static class DealDispatchDiagnostics
{
    public static readonly DiagnosticDescriptor InvalidContract = Create(
        "DDD001",
        "Invalid Deal strategy factory contract",
        "Factory contract '{0}' is invalid: {1}");

    public static readonly DiagnosticDescriptor InvalidFactoryUse = Create(
        "DDD002",
        "Invalid Deal strategy factory use",
        "Factory '{0}' cannot be used with strategy '{1}'");

    public static readonly DiagnosticDescriptor InvalidAnchor = Create(
        "DDD003",
        "Invalid Deal dispatch generation anchor",
        "Generation anchor '{0}' is invalid: {1}");

    public static readonly DiagnosticDescriptor InvalidCatalog = Create(
        "DDD004",
        "Invalid Deal case catalog",
        "Selector '{0}' has an invalid Deal case catalog: {1}");

    public static readonly DiagnosticDescriptor InvalidFamilyCoverage = Create(
        "DDD005",
        "Invalid Deal strategy family coverage",
        "Strategy family '{0}' must have exactly one implementation for Deal case '{1}'");

    public static readonly DiagnosticDescriptor InvalidImplementation = Create(
        "DDD006",
        "Invalid Deal strategy implementation",
        "Implementation '{0}' is invalid: {1}");

    public static readonly DiagnosticDescriptor InvalidRegistrationInvocation = Create(
        "DDD007",
        "Invalid generated registration invocation",
        "Generated registration method '{0}' must be invoked exactly once; found {1} invocations");

    public static readonly DiagnosticDescriptor InvalidVariantCoverage = Create(
        "DDD008",
        "Invalid Deal variant coverage",
        "Variant factory '{0}' must assign Deal case '{1}' exactly once");

    public static readonly DiagnosticDescriptor InvalidWrapperMembership = Create(
        "DDD009",
        "Invalid Deal variant wrapper membership",
        "Variant implementation '{0}' is not a case of wrapper '{1}'");

    private static DiagnosticDescriptor Create(string id, string title, string messageFormat) =>
        new(
            id,
            title,
            messageFormat,
            "DealDispatch",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
}
