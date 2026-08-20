using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Concertable.B2B.DealDispatch.Generators.UnitTests;

internal static class GeneratorTestHarness
{
    public static GeneratorResult Run(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.CSharp14));
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "Fixture",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new DealDispatchGenerator());

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var output,
            out _);

        var runResult = driver.GetRunResult();
        return new GeneratorResult(
            runResult.Diagnostics,
            output.GetDiagnostics(),
            string.Join(
                Environment.NewLine,
                runResult.Results.SelectMany(result => result.GeneratedSources)
                    .Select(generated => generated.SourceText.ToString())));
    }
}

internal sealed record GeneratorResult(
    ImmutableArray<Diagnostic> GeneratorDiagnostics,
    ImmutableArray<Diagnostic> CompilationDiagnostics,
    string GeneratedSource)
{
    public ImmutableArray<Diagnostic> Errors =>
        GeneratorDiagnostics
            .Concat(CompilationDiagnostics)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();
}
