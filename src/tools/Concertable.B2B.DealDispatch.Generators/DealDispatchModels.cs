using Microsoft.CodeAnalysis;

namespace Concertable.B2B.DealDispatch.Generators;

internal enum AnchorKind
{
    Strategy,
    Variant
}

internal sealed record AnchorRequest(
    INamedTypeSymbol Anchor,
    INamedTypeSymbol Factory,
    INamedTypeSymbol? Marker,
    AnchorKind Kind);

internal sealed record SelectorModel(
    IMethodSymbol Method,
    INamedTypeSymbol Input,
    ImmutableArray<CaseModel> Cases);

internal sealed record CaseModel(
    string Stem,
    INamedTypeSymbol Type);

internal sealed record FamilyModel(
    INamedTypeSymbol Family,
    ImmutableArray<INamedTypeSymbol> Implementations);

internal sealed record StrategyModel(
    AnchorRequest Request,
    ImmutableArray<SelectorModel> Selectors,
    ImmutableArray<CaseModel> Slots,
    ImmutableArray<FamilyModel> Families,
    string RegistrationMethod);

internal sealed record VariantImplementationModel(
    INamedTypeSymbol Implementation,
    ImmutableArray<INamedTypeSymbol> Cases);

internal sealed record VariantModel(
    AnchorRequest Request,
    IMethodSymbol Method,
    INamedTypeSymbol Input,
    INamedTypeSymbol Wrapper,
    ImmutableArray<CaseModel> Cases,
    ImmutableArray<VariantImplementationModel> Implementations,
    string RegistrationMethod);
