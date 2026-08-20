using Microsoft.CodeAnalysis;

namespace Concertable.B2B.DealDispatch.Generators;

internal static class SymbolCatalog
{
    public const string ContractAttribute =
        "Concertable.B2B.DealDispatch.DealStrategyFactoryContractAttribute";

    public const string StrategyAnchorAttribute =
        "Concertable.B2B.DealDispatch.GenerateDealStrategyFactoryAttribute";

    public const string VariantAnchorAttribute =
        "Concertable.B2B.DealDispatch.GenerateDealVariantFactoryAttribute";

    public const string VariantCasesAttribute =
        "Concertable.B2B.DealDispatch.DealVariantCasesAttribute";

    public static bool HasAttribute(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == metadataName);

    public static AttributeData? GetAttribute(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass?.ToDisplayString() == metadataName);

    public static INamedTypeSymbol? GetTypeArgument(AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.Length <= index)
            return null;

        return attribute.ConstructorArguments[index].Value as INamedTypeSymbol;
    }

    public static ImmutableArray<INamedTypeSymbol> GetTypeArrayArgument(
        AttributeData attribute,
        int index)
    {
        if (attribute.ConstructorArguments.Length <= index)
            return [];

        return attribute.ConstructorArguments[index].Values
            .Select(value => value.Value)
            .OfType<INamedTypeSymbol>()
            .ToImmutableArray();
    }

    public static IEnumerable<INamedTypeSymbol> AllTypes(IAssemblySymbol assembly) =>
        AllTypes(assembly.GlobalNamespace);

    private static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;

            foreach (var nested in AllTypes(type))
                yield return nested;
        }

        foreach (var child in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in AllTypes(child))
                yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> AllTypes(INamedTypeSymbol containingType)
    {
        foreach (var nested in containingType.GetTypeMembers())
        {
            yield return nested;

            foreach (var descendant in AllTypes(nested))
                yield return descendant;
        }
    }
}
