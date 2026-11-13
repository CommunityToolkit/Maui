using System.Globalization;
using CommunityToolkit.Maui.SourceGenerators.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

/// <summary>
/// Runs all three generators over a source and reports everything that is wrong with the result: errors anywhere, warnings in generated files and generator diagnostics.
/// </summary>
static class GeneratedCodeCompilation
{
	public static readonly string[] CultureNames = ["", "tr-TR", "az-Latn-AZ"];

	public static GeneratedCodeResult Run(string source, string cultureName, CancellationToken cancellationToken)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUiCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo(cultureName);
			CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

			var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Diagnose);
			var compilation = CSharpCompilation.Create(
				"GeneratedCodeAssembly",
				[CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: cancellationToken)],
				GeneratorCultureTests.GetReferences(),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, warningLevel: 9999)
					.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
					{
						{ BindablePropertyDiagnostic.BindablePropertyAttributeExperimentalDiagnosticId, ReportDiagnostic.Suppress }
					}));

			var driver = CSharpGeneratorDriver.Create(
				[new BindablePropertyAttributeSourceGenerator().AsSourceGenerator(), new AttachedBindablePropertyAttributeSourceGenerator().AsSourceGenerator(), new TextColorToGenerator().AsSourceGenerator()],
				parseOptions: parseOptions);
			driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics, cancellationToken);

			var generatedTrees = outputCompilation.SyntaxTrees.Where(static tree => IsGeneratedFile(tree.FilePath)).ToList();
			var generatedSources = generatedTrees.ToDictionary(static tree => Path.GetFileName(tree.FilePath), tree => tree.GetText(cancellationToken).ToString(), StringComparer.Ordinal);
			var generatedPaths = generatedTrees.Select(static tree => tree.FilePath).ToHashSet(StringComparer.Ordinal);

			var diagnostics = outputCompilation.GetDiagnostics(cancellationToken)
				.Concat(generatorDiagnostics)
				.Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
				.Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error || diagnostic.Location.SourceTree is null || generatedPaths.Contains(diagnostic.Location.SourceTree.FilePath))
				.Select(static diagnostic => diagnostic.ToString())
				.ToList();

			return new GeneratedCodeResult(generatedSources, diagnostics);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUiCulture;
		}
	}

	public static void AssertCompilesCleanly(string source, string cultureName, params string[] expectedFileNameFragments)
	{
		var result = Run(source, cultureName, TestContext.Current.CancellationToken);

		Assert.True(result.Diagnostics.Count == 0, string.Join(Environment.NewLine, result.Diagnostics));

		foreach (var fragment in expectedFileNameFragments)
		{
			Assert.True(result.Sources.Keys.Any(fileName => fileName.Contains(fragment, StringComparison.Ordinal)), $"No generated file contains \"{fragment}\". Generated: {string.Join(", ", result.Sources.Keys)}");
		}
	}

	static bool IsGeneratedFile(string filePath) => filePath.Contains("CommunityToolkit.Maui.SourceGenerators", StringComparison.Ordinal);
}

sealed record GeneratedCodeResult(IReadOnlyDictionary<string, string> Sources, IReadOnlyList<string> Diagnostics)
{
	public string GetSource(string fileNameEnd) =>
		Sources.Single(pair => pair.Key.EndsWith(fileNameEnd, StringComparison.Ordinal)).Value;
}