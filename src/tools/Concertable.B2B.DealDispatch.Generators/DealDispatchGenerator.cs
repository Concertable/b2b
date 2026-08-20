using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Concertable.B2B.DealDispatch.Generators;

[Generator]
public sealed partial class DealDispatchGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var anchors = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax { AttributeLists.Count: > 0 },
                static (syntaxContext, _) => GetAnchor(syntaxContext))
            .Where(static request => request is not null)
            .Select(static (request, _) => request!);

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(anchors.Collect()),
            static (sourceContext, input) => Execute(sourceContext, input.Left, input.Right));
    }

    private static AnchorRequest? GetAnchor(GeneratorSyntaxContext context)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol symbol)
            return null;

        var strategy = SymbolCatalog.GetAttribute(symbol, SymbolCatalog.StrategyAnchorAttribute);
        if (strategy is not null)
        {
            return new AnchorRequest(
                symbol,
                SymbolCatalog.GetTypeArgument(strategy, 0)!,
                SymbolCatalog.GetTypeArgument(strategy, 1),
                AnchorKind.Strategy);
        }

        var variant = SymbolCatalog.GetAttribute(symbol, SymbolCatalog.VariantAnchorAttribute);
        return variant is null
            ? null
            : new AnchorRequest(
                symbol,
                SymbolCatalog.GetTypeArgument(variant, 0)!,
                null,
                AnchorKind.Variant);
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<AnchorRequest> requests)
    {
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var request in requests)
        {
            if (!seen.Add(request.Anchor))
                continue;

            if (request.Factory is null)
            {
                Report(context, DealDispatchDiagnostics.InvalidAnchor, request.Anchor, request.Anchor.Name, "factory type is missing");
                continue;
            }

            if (request.Kind == AnchorKind.Strategy)
                GenerateStrategy(context, compilation, request);
            else
                GenerateVariant(context, compilation, request);
        }
    }

    private static void GenerateStrategy(
        SourceProductionContext context,
        Compilation compilation,
        AnchorRequest request)
    {
        if (!TryCreateStrategyModel(context, compilation, request, out var model))
            return;

        context.AddSource(
            $"{request.Anchor.Name}.DealStrategies.g.cs",
            EmitStrategy(model));
    }

    private static void GenerateVariant(
        SourceProductionContext context,
        Compilation compilation,
        AnchorRequest request)
    {
        if (!TryCreateVariantModel(context, compilation, request, out var model))
            return;

        context.AddSource(
            $"{request.Anchor.Name}.DealVariants.g.cs",
            EmitVariant(model));
    }

    private static bool TryCreateStrategyModel(
        SourceProductionContext context,
        Compilation compilation,
        AnchorRequest request,
        out StrategyModel model)
    {
        model = null!;
        var factory = request.Factory.ConstructedFrom;
        var marker = request.Marker;

        if (marker is null ||
            factory.TypeKind != TypeKind.Interface ||
            factory.TypeParameters.Length != 1 ||
            factory.TypeParameters[0].Variance != VarianceKind.None ||
            !factory.TypeParameters[0].ConstraintTypes.Any(type =>
                SymbolEqualityComparer.Default.Equals(type, marker)))
        {
            Report(context, DealDispatchDiagnostics.InvalidContract, factory, factory.Name, "the factory must be an invariant one-parameter interface constrained to its marker");
            return false;
        }

        var contract = SymbolCatalog.GetAttribute(factory, SymbolCatalog.ContractAttribute);
        if (contract is null ||
            !SymbolEqualityComparer.Default.Equals(SymbolCatalog.GetTypeArgument(contract, 0), marker))
        {
            Report(context, DealDispatchDiagnostics.InvalidContract, factory, factory.Name, "the contract attribute must declare the same marker as the generation anchor");
            return false;
        }

        var createMethods = factory.GetMembers("Create")
            .OfType<IMethodSymbol>()
            .Where(method =>
                method.Parameters.Length == 1 &&
                method.ReturnType is ITypeParameterSymbol typeParameter &&
                SymbolEqualityComparer.Default.Equals(typeParameter, factory.TypeParameters[0]))
            .ToImmutableArray();

        if (createMethods.IsEmpty)
        {
            Report(context, DealDispatchDiagnostics.InvalidContract, factory, factory.Name, "the factory must declare at least one Create(TInput) method returning TStrategy");
            return false;
        }

        var selectors = ImmutableArray.CreateBuilder<SelectorModel>();
        var slots = new Dictionary<string, CaseModel>(StringComparer.Ordinal);
        var valid = true;

        foreach (var method in createMethods)
        {
            if (method.Parameters[0].Type is not INamedTypeSymbol input)
            {
                Report(context, DealDispatchDiagnostics.InvalidCatalog, method, method.Name, "the selector input must be a named type");
                valid = false;
                continue;
            }

            var cases = DiscoverCases(input);
            if (cases.IsEmpty)
            {
                Report(context, DealDispatchDiagnostics.InvalidCatalog, input, input.Name, "no sealed direct cases were found");
                valid = false;
                continue;
            }

            foreach (var item in cases)
            {
                if (slots.TryGetValue(item.Stem, out var existing) &&
                    SymbolEqualityComparer.Default.Equals(existing.Type, item.Type))
                    continue;

                slots[item.Stem] = item;
            }

            selectors.Add(new SelectorModel(method, input, cases));
        }

        var allTypes = AllVisibleTypes(compilation).ToImmutableArray();
        var families = allTypes
            .Where(type =>
                type.TypeKind == TypeKind.Interface &&
                !SymbolEqualityComparer.Default.Equals(type, marker) &&
                type.Interfaces.Any(item => SymbolEqualityComparer.Default.Equals(item, marker)))
            .OrderBy(type => type.ToDisplayString(), StringComparer.Ordinal)
            .ToImmutableArray();

        if (families.IsEmpty)
        {
            Report(context, DealDispatchDiagnostics.InvalidCatalog, request.Anchor, factory.Name, "no strategy families directly inherit the marker");
            return false;
        }

        var orderedSlots = slots.Values.OrderBy(item => item.Stem, StringComparer.Ordinal).ToImmutableArray();
        var familyModels = ImmutableArray.CreateBuilder<FamilyModel>();

        foreach (var family in families)
        {
            var suffix = family.Name.StartsWith("I", StringComparison.Ordinal)
                ? family.Name.Substring(1)
                : family.Name;
            var implementations = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

            foreach (var slot in orderedSlots)
            {
                var expectedName = slot.Stem + suffix;
                var matches = allTypes
                    .Where(type =>
                        type.TypeKind == TypeKind.Class &&
                        !type.IsAbstract &&
                        type.Name == expectedName &&
                        type.AllInterfaces.Any(item =>
                            SymbolEqualityComparer.Default.Equals(item, family)))
                    .ToImmutableArray();

                if (matches.Length != 1)
                {
                    Report(context, DealDispatchDiagnostics.InvalidFamilyCoverage, family, family.Name, slot.Type.Name);
                    valid = false;
                    continue;
                }

                implementations.Add(matches[0]);
            }

            if (implementations.Count == orderedSlots.Length)
                familyModels.Add(new FamilyModel(family, implementations.ToImmutable()));
        }

        var registrationMethod = RegistrationMethod(request.Anchor.Name);
        var invocationCount = CountInvocations(compilation, registrationMethod);
        if (invocationCount != 1)
        {
            Report(
                context,
                DealDispatchDiagnostics.InvalidRegistrationInvocation,
                request.Anchor,
                registrationMethod,
                invocationCount);
            valid = false;
        }

        if (!valid)
            return false;

        model = new StrategyModel(
            request,
            selectors.ToImmutable(),
            orderedSlots,
            familyModels.ToImmutable(),
            registrationMethod);
        return true;
    }

    private static bool TryCreateVariantModel(
        SourceProductionContext context,
        Compilation compilation,
        AnchorRequest request,
        out VariantModel model)
    {
        model = null!;
        var factory = request.Factory;
        var method = factory.GetMembers("Create")
            .OfType<IMethodSymbol>()
            .SingleOrDefault(candidate => candidate.Parameters.Length == 1);

        if (factory.TypeKind != TypeKind.Interface ||
            factory.TypeParameters.Length != 0 ||
            method is null)
        {
            Report(context, DealDispatchDiagnostics.InvalidAnchor, request.Anchor, request.Anchor.Name, "the variant factory must be a non-generic interface with one Create(TInput) method");
            return false;
        }

        if (method.Parameters[0].Type is not INamedTypeSymbol input ||
            method.ReturnType is not INamedTypeSymbol wrapper)
        {
            Report(context, DealDispatchDiagnostics.InvalidAnchor, request.Anchor, request.Anchor.Name, "the variant factory input and wrapper must be named types");
            return false;
        }

        var cases = DiscoverCases(input);
        if (cases.IsEmpty)
        {
            Report(context, DealDispatchDiagnostics.InvalidCatalog, input, input.Name, "no sealed direct cases were found");
            return false;
        }

        var allTypes = SymbolCatalog.AllTypes(compilation.Assembly).ToImmutableArray();
        var implementations = allTypes
            .Select(type => (Type: type, Attribute: SymbolCatalog.GetAttribute(type, SymbolCatalog.VariantCasesAttribute)))
            .Where(item => item.Attribute is not null && item.Type.TypeKind == TypeKind.Class && !item.Type.IsAbstract)
            .Select(item => new VariantImplementationModel(
                item.Type,
                SymbolCatalog.GetTypeArrayArgument(item.Attribute!, 0)))
            .OrderBy(item => item.Implementation.ToDisplayString(), StringComparer.Ordinal)
            .ToImmutableArray();

        var valid = true;
        foreach (var implementation in implementations)
        {
            if (!HasWrapperMembership(wrapper, implementation.Implementation))
            {
                Report(
                    context,
                    DealDispatchDiagnostics.InvalidWrapperMembership,
                    implementation.Implementation,
                    implementation.Implementation.Name,
                    wrapper.Name);
                valid = false;
            }
        }

        foreach (var item in cases)
        {
            var count = implementations.Count(implementation =>
                implementation.Cases.Any(type =>
                    SymbolEqualityComparer.Default.Equals(type, item.Type)));
            if (count != 1)
            {
                Report(
                    context,
                    DealDispatchDiagnostics.InvalidVariantCoverage,
                    request.Anchor,
                    factory.Name,
                    item.Type.Name);
                valid = false;
            }
        }

        foreach (var implementation in implementations)
        {
            foreach (var assigned in implementation.Cases)
            {
                if (!cases.Any(item => SymbolEqualityComparer.Default.Equals(item.Type, assigned)))
                {
                    Report(
                        context,
                        DealDispatchDiagnostics.InvalidVariantCoverage,
                        implementation.Implementation,
                        factory.Name,
                        assigned.Name);
                    valid = false;
                }
            }
        }

        var registrationMethod = RegistrationMethod(request.Anchor.Name);
        var invocationCount = CountInvocations(compilation, registrationMethod);
        if (invocationCount != 1)
        {
            Report(
                context,
                DealDispatchDiagnostics.InvalidRegistrationInvocation,
                request.Anchor,
                registrationMethod,
                invocationCount);
            valid = false;
        }

        if (!valid)
            return false;

        model = new VariantModel(
            request,
            method,
            input,
            wrapper,
            cases,
            implementations,
            registrationMethod);
        return true;
    }

    private static ImmutableArray<CaseModel> DiscoverCases(INamedTypeSymbol input)
    {
        var cases = SymbolCatalog.AllTypes(input.ContainingAssembly)
            .Where(type =>
                !type.IsAbstract &&
                type.IsSealed &&
                IsDirectCase(type, input))
            .Select(type => new CaseModel(CaseStem(type.Name), type))
            .OrderBy(item => item.Stem, StringComparer.Ordinal)
            .ToImmutableArray();

        return cases;
    }

    private static bool IsDirectCase(INamedTypeSymbol type, INamedTypeSymbol input) =>
        input.TypeKind == TypeKind.Interface
            ? type.Interfaces.Any(item => SymbolEqualityComparer.Default.Equals(item, input))
            : SymbolEqualityComparer.Default.Equals(type.BaseType, input);

    private static IEnumerable<INamedTypeSymbol> AllVisibleTypes(Compilation compilation)
    {
        foreach (var type in SymbolCatalog.AllTypes(compilation.Assembly))
            yield return type;

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach (var type in SymbolCatalog.AllTypes(assembly))
                yield return type;
        }
    }

    private static bool HasWrapperMembership(INamedTypeSymbol wrapper, INamedTypeSymbol implementation) =>
        wrapper.InstanceConstructors.Any(constructor =>
            constructor.Parameters.Length == 1 &&
            SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, implementation));

    private static string CaseStem(string name)
    {
        if (name.EndsWith("DealEntity", StringComparison.Ordinal))
            return name.Substring(0, name.Length - "DealEntity".Length);
        if (name.EndsWith("Deal", StringComparison.Ordinal))
            return name.Substring(0, name.Length - "Deal".Length);
        return name;
    }

    private static string RegistrationMethod(string anchorName)
    {
        const string suffix = "Registration";
        var stem = anchorName.EndsWith(suffix, StringComparison.Ordinal)
            ? anchorName.Substring(0, anchorName.Length - suffix.Length)
            : anchorName;
        return "Add" + (
            stem.EndsWith("y", StringComparison.Ordinal)
                ? stem.Substring(0, stem.Length - 1) + "ies"
                : stem + "s");
    }

    private static int CountInvocations(Compilation compilation, string methodName) =>
        compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Count(invocation => invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText == methodName,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText == methodName,
                _ => false
            });

    private static void Report(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        ISymbol symbol,
        params object[] arguments) =>
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            symbol.Locations.FirstOrDefault(),
            arguments));
}
