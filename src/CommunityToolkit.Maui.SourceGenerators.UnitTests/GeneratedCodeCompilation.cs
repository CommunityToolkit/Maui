using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Maui.SourceGenerators.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

/// <summary>
/// Runs all three generators over a source and reports everything that is wrong with the result: errors anywhere, warnings in generated files and generator diagnostics.
/// Strict mode removes the generated files' pragmas first, so the report shows every warning the shipped pragmas hide.
/// </summary>
static partial class GeneratedCodeCompilation
{
	public static readonly string[] CultureNames = ["", "tr-TR", "az-Latn-AZ"];

	// Generated code names the user's members, which may be deprecated, hidden or `field`-named; these are the only warnings the generators knowingly cause.
	static readonly string[] knownGeneratedCodeWarningIds = ["CS0108", "CS0109", "CS9258"];
	static readonly string[] deprecationWarningIds = ["CS0612", "CS0618"];

	public static GeneratedCodeResult Run(string source, string cultureName, CancellationToken cancellationToken, IReadOnlyList<MetadataReference>? extraReferences = null, bool isStrict = false)
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
				[.. GeneratorCultureTests.GetReferences(), .. extraReferences ?? []],
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, warningLevel: 9999)
					.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
					{
						{ BindablePropertyDiagnostic.BindablePropertyAttributeExperimentalDiagnosticId, ReportDiagnostic.Suppress }
					}));

			var driver = CSharpGeneratorDriver.Create(
				[new BindablePropertyAttributeSourceGenerator().AsSourceGenerator(), new AttachedBindablePropertyAttributeSourceGenerator().AsSourceGenerator(), new TextColorToGenerator().AsSourceGenerator()],
				parseOptions: parseOptions);
			driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics, cancellationToken);

			if (isStrict)
			{
				outputCompilation = RemovePragmasFromGeneratedTrees(outputCompilation, parseOptions, cancellationToken);
			}

			var generatedTrees = outputCompilation.SyntaxTrees.Where(static tree => IsGeneratedFile(tree.FilePath)).ToList();
			var generatedSources = generatedTrees.ToDictionary(static tree => GetGeneratorFolderAndFileName(tree.FilePath), tree => tree.GetText(cancellationToken).ToString(), StringComparer.Ordinal);
			var generatedPaths = generatedTrees.Select(static tree => tree.FilePath).ToHashSet(StringComparer.Ordinal);

			var diagnostics = outputCompilation.GetDiagnostics(cancellationToken)
				.Concat(generatorDiagnostics)
				.Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
				.Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error || diagnostic.Location.SourceTree is null || generatedPaths.Contains(diagnostic.Location.SourceTree.FilePath))
				.Select(static diagnostic => new ReportedDiagnostic(diagnostic.Id, diagnostic.ToString()))
				.ToList();

			return new GeneratedCodeResult(generatedSources, diagnostics);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUiCulture;
		}
	}

	public static void AssertCompilesCleanly(string source, string cultureName, params string[] expectedFileNameFragments) =>
		AssertCompilesCleanly(source, cultureName, [], [], expectedFileNameFragments);

	/// <summary>
	/// The shipped output must report nothing; the strict output may report only the known categories and the diagnostic IDs the source itself declares.
	/// </summary>
	public static void AssertCompilesCleanly(string source, string cultureName, IReadOnlyList<MetadataReference> extraReferences, IReadOnlyList<string> additionalExpectedIds, params string[] expectedFileNameFragments)
	{
		var shipped = Run(source, cultureName, TestContext.Current.CancellationToken, extraReferences);

		Assert.True(shipped.Diagnostics.Count == 0, string.Join(Environment.NewLine, shipped.Diagnostics));
		AssertContainsFiles(shipped, expectedFileNameFragments);

		var strict = RunStrict(source, cultureName, extraReferences);
		var expectedIds = GetExpectedStrictIds(source, additionalExpectedIds);
		var unexpected = strict.Findings.Where(finding => !expectedIds.Contains(finding.Id)).Select(static finding => finding.Text).ToList();

		Assert.True(unexpected.Count == 0, string.Join(Environment.NewLine, unexpected));
	}

	public static GeneratedCodeResult RunStrict(string source, string cultureName, IReadOnlyList<MetadataReference>? extraReferences = null) =>
		Run(source, cultureName, TestContext.Current.CancellationToken, extraReferences, isStrict: true);

	public static HashSet<string> GetExpectedStrictIds(string source, IEnumerable<string> additionalExpectedIds)
	{
		var declaredIds = ObsoleteDiagnosticIdRegex().Matches(source).Concat(ExperimentalIdRegex().Matches(source))
			.Select(static match => Regex.Unescape(match.Groups["id"].Value));

		var deprecationIds = source.Contains("Obsolete", StringComparison.Ordinal) ? deprecationWarningIds : [];
		return [.. knownGeneratedCodeWarningIds, .. deprecationIds, .. declaredIds, .. additionalExpectedIds];
	}

	static void AssertContainsFiles(GeneratedCodeResult result, IEnumerable<string> expectedFileNameFragments)
	{
		foreach (var fragment in expectedFileNameFragments)
		{
			Assert.True(result.Sources.Keys.Any(fileName => fileName.Contains(fragment, StringComparison.Ordinal)), $"No generated file contains \"{fragment}\". Generated: {string.Join(", ", result.Sources.Keys)}");
		}
	}

	static Compilation RemovePragmasFromGeneratedTrees(Compilation compilation, CSharpParseOptions parseOptions, CancellationToken cancellationToken)
	{
		foreach (var tree in compilation.SyntaxTrees.Where(static tree => IsGeneratedFile(tree.FilePath)).ToList())
		{
			var linesWithoutWarningPragmas = tree.GetText(cancellationToken).ToString()
				.Split('\n')
				.Where(static line => !line.StartsWith("#pragma warning", StringComparison.Ordinal));
			var strippedTree = CSharpSyntaxTree.ParseText(SourceText.From(string.Join('\n', linesWithoutWarningPragmas), Encoding.UTF8), parseOptions, tree.FilePath, cancellationToken);
			compilation = compilation.ReplaceSyntaxTree(tree, strippedTree);
		}

		return compilation;
	}

	[GeneratedRegex(@"DiagnosticId\s*=\s*""(?<id>(?:[^""\\]|\\.)*)""")]
	private static partial Regex ObsoleteDiagnosticIdRegex();

	[GeneratedRegex(@"Experimental\(\s*""(?<id>(?:[^""\\]|\\.)*)""\s*\)")]
	private static partial Regex ExperimentalIdRegex();

	static string GetGeneratorFolderAndFileName(string filePath) =>
		$"{Path.GetFileName(Path.GetDirectoryName(filePath))}/{Path.GetFileName(filePath)}";

	static bool IsGeneratedFile(string filePath) => filePath.Contains("CommunityToolkit.Maui.SourceGenerators", StringComparison.Ordinal);
}

sealed record ReportedDiagnostic(string Id, string Text);

sealed record GeneratedCodeResult(IReadOnlyDictionary<string, string> Sources, IReadOnlyList<ReportedDiagnostic> Findings)
{
	public IReadOnlyList<string> Diagnostics => [.. Findings.Select(static finding => finding.Text)];

	public bool HasFile(string fileName) => Sources.Keys.Any(key => key.EndsWith("/" + fileName, StringComparison.Ordinal));

	public string GetSource(string fileNameEnd) =>
		Sources.Single(pair => pair.Key.EndsWith(fileNameEnd, StringComparison.Ordinal)).Value;
}