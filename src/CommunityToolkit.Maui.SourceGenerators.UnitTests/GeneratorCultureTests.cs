using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

// Source generators run inside the app developer's compiler or IDE, so whatever they emit must not depend on that machine's culture.
// Comparing the output across cultures needs no expected text, runs on every OS, and catches a culture leak in any generated line.
public class GeneratorCultureTests
{
	const string testSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace TestNamespace;

		internal partial class TestView : View
		{
			[BindableProperty]
			public partial bool IsBusy { get; private set; }

			[BindableProperty]
			public partial int Index { get; set; } = -1;

			[BindableProperty]
			public partial long Big { get; set; } = -1L;

			[BindableProperty]
			public partial short Small { get; set; } = -1;

			[BindableProperty]
			public partial double Ratio { get; set; } = 3.14;

			[BindableProperty]
			public partial float Scale { get; set; } = 1.5f;

			// A folded constant goes through FormatConstantValue; literal initializers keep their syntax text
			[BindableProperty]
			public partial int Folded { get; set; } = unchecked((int)uint.MaxValue);

			[BindableProperty]
			public partial long FoldedLong { get; set; } = unchecked((long)ulong.MaxValue);
		}

		[AttachedBindableProperty<double>("Pi", DefaultValue = 3.14)]
		[AttachedBindableProperty<float>("Scale", DefaultValue = 1.5f)]
		[AttachedBindableProperty<int>("Index", DefaultValue = -1)]
		[AttachedBindableProperty<long>("Big", DefaultValue = -1L)]
		[AttachedBindableProperty<sbyte>("Tiny", DefaultValue = (sbyte)-1)]
		[AttachedBindableProperty<bool>("IsOpen", SetterAccessibility = AccessModifier.Private)]
		internal partial class AttachedHost : View
		{
		}
		""";

	const string nestedProtectedInternalSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace TestNamespace;

		public partial class Outer : View
		{
			protected internal partial class Inner : View
			{
				[BindableProperty]
				public partial string Text { get; set; }
			}
		}
		""";

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	[InlineData("de-DE")]
	[InlineData("sv-SE")]
	[InlineData("ar-SA")]
	public void GeneratedSourceIsIdenticalInEveryCulture(string cultureName)
	{
		var invariantSources = RunGenerators(testSource, CultureInfo.InvariantCulture, out var invariantErrors);
		var cultureSources = RunGenerators(testSource, new CultureInfo(cultureName), out var cultureErrors);

		Assert.NotEmpty(invariantSources);
		Assert.Empty(invariantErrors);
		Assert.Empty(cultureErrors);
		Assert.Equal(invariantSources.Count, cultureSources.Count);

		foreach (var (hintName, text) in invariantSources)
		{
			Assert.True(cultureSources.TryGetValue(hintName, out var cultureText), $"{hintName} was not generated in {cultureName}");
			Assert.Equal(text, cultureText);
		}
	}

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCodeUsesAsciiKeywordsAndIdentifiers(string cultureName)
	{
		var sources = RunGenerators(testSource, new CultureInfo(cultureName), out _);
		var allText = string.Concat(sources.Values);

		Assert.Contains("internal partial class TestView", allText, StringComparison.Ordinal);
		Assert.Contains("isBusyPropertyKey", allText, StringComparison.Ordinal);
		Assert.DoesNotContain("ı", allText, StringComparison.Ordinal);
		Assert.DoesNotContain("İ", allText, StringComparison.Ordinal);
	}

	[Fact]
	public void NestedProtectedInternalClass_EmitsCSharpAccessibilityKeywords()
	{
		// Only the emitted keyword is asserted: the generator repeats the nested class's accessibility on its containing types, so compiling this shape fails with CS0262 independent of the culture.
		var allText = string.Concat(RunGenerators(nestedProtectedInternalSource, CultureInfo.InvariantCulture, out _).Values);

		Assert.Contains("protected internal partial class Inner", allText, StringComparison.Ordinal);
		Assert.DoesNotContain("protectedorinternal", allText, StringComparison.Ordinal);
	}

	// The generated code is CRLF-formatted, but a literal copied from the user's code is a value and must keep the line endings of the user's source
	[Theory]
	[InlineData("\n")]
	[InlineData("\r\n")]
	public void MultiLineVerbatimInitializer_KeepsItsLineEndings(string eol)
	{
		var source = "using CommunityToolkit.Maui;" + eol
			+ "using Microsoft.Maui.Controls;" + eol
			+ "namespace TestNamespace;" + eol
			+ "public partial class TestView : View" + eol
			+ "{" + eol
			+ "\t[BindableProperty]" + eol
			+ "\tpublic partial string Text { get; set; } = @\"line1" + eol + "line2\";" + eol
			+ "}" + eol;

		var generatedText = string.Concat(RunGenerators(source, CultureInfo.InvariantCulture, out var errors).Values);

		Assert.Empty(errors);
		Assert.Contains("@\"line1" + eol + "line2\"", generatedText, StringComparison.Ordinal);
	}

	static Dictionary<string, string> RunGenerators(string source, CultureInfo culture, out List<Diagnostic> errors)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUiCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;

			var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
			var compilation = CSharpCompilation.Create(
				"CultureTestAssembly",
				[syntaxTree],
				GetReferences(),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
					.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
					{
						{ BindablePropertyDiagnostic.BindablePropertyAttributeExperimentalDiagnosticId, ReportDiagnostic.Suppress }
					}));

			var driver = CSharpGeneratorDriver.Create(
				[new BindablePropertyAttributeSourceGenerator().AsSourceGenerator(), new AttachedBindablePropertyAttributeSourceGenerator().AsSourceGenerator()],
				parseOptions: new CSharpParseOptions(LanguageVersion.Preview));

			driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

			errors = [.. generatorDiagnostics, .. outputCompilation.GetDiagnostics()];
			errors.RemoveAll(static x => x.Severity is not DiagnosticSeverity.Error);

			var generatedTrees = outputCompilation.SyntaxTrees.Where(static x => !string.IsNullOrEmpty(x.FilePath) && x.FilePath.Contains("CommunityToolkit.Maui.SourceGenerators", StringComparison.Ordinal));
			return generatedTrees.ToDictionary(static x => x.FilePath, static x => x.GetText().ToString());
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUiCulture;
		}
	}

	static List<MetadataReference> GetReferences()
	{
		var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is not available");

		var references = new List<MetadataReference>();
		foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
		{
			try
			{
				// Skips native libraries in the list
				System.Reflection.AssemblyName.GetAssemblyName(path);
				references.Add(MetadataReference.CreateFromFile(path));
			}
			catch (BadImageFormatException)
			{
			}
		}

		return references;
	}
}