using System.Globalization;
using CommunityToolkit.Maui.SourceGenerators.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class TextColorToNestedTypeTests : BaseTest
{
	const string nestedTextTypesSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace Nested;

		public class PublicInPublicOuter
		{
			public class InnerText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}

		public class InternalInPublicOuter
		{
			internal class InnerText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}

		internal class PublicInInternalOuter
		{
			public class InnerText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}

		public class ProtectedInternalInPublicOuter
		{
			protected internal class InnerText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}

		public class DeeplyNestedOuter
		{
			public class Middle
			{
				public class InnerText : VisualElement, ITextStyle
				{
					public Color TextColor { get; set; } = Colors.Black;
					public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
					public double CharacterSpacing => 0;
				}
			}
		}

		public class GenericInnerInPublicOuter
		{
			public class InnerText<T> : VisualElement, ITextStyle where T : class
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}

		public class PrivateNestedOuter
		{
			class InnerText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}

		public class ProtectedNestedOuter
		{
			protected class InnerText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}

		public class PrivateProtectedNestedOuter
		{
			private protected class InnerText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}

		public class PublicInPrivateOuterHolder
		{
			class PrivateOuter
			{
				public class InnerText : VisualElement, ITextStyle
				{
					public Color TextColor { get; set; } = Colors.Black;
					public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
					public double CharacterSpacing => 0;
				}
			}
		}

		public class GenericOuter<T>
		{
			public class InnerText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}
		""";

	[Theory]
	[InlineData("en-US", "PublicInPublicOuter.InnerTextTextColorTo.g.shared.cs", "public static partial class ColorAnimationExtensions_PublicInPublicOuter_InnerText")]
	[InlineData("tr-TR", "PublicInPublicOuter.InnerTextTextColorTo.g.shared.cs", "public static partial class ColorAnimationExtensions_PublicInPublicOuter_InnerText")]
	[InlineData("az-Latn-AZ", "PublicInPublicOuter.InnerTextTextColorTo.g.shared.cs", "public static partial class ColorAnimationExtensions_PublicInPublicOuter_InnerText")]
	[InlineData("tr-TR", "InternalInPublicOuter.InnerTextTextColorTo.g.shared.cs", "internal static partial class ColorAnimationExtensions_InternalInPublicOuter_InnerText")]
	[InlineData("tr-TR", "PublicInInternalOuter.InnerTextTextColorTo.g.shared.cs", "internal static partial class ColorAnimationExtensions_PublicInInternalOuter_InnerText")]
	[InlineData("tr-TR", "ProtectedInternalInPublicOuter.InnerTextTextColorTo.g.shared.cs", "internal static partial class ColorAnimationExtensions_ProtectedInternalInPublicOuter_InnerText")]
	[InlineData("tr-TR", "DeeplyNestedOuter.Middle.InnerTextTextColorTo.g.shared.cs", "public static partial class ColorAnimationExtensions_DeeplyNestedOuter_Middle_InnerText")]
	[InlineData("tr-TR", "GenericInnerInPublicOuter.InnerTextTextColorTo.g.shared.cs", "public static partial class ColorAnimationExtensions_GenericInnerInPublicOuter_InnerText")]
	public void Generator_NamesTheContainingTypesAndUsesTheEffectiveAccessibility_ForNestedTextTypes(string cultureName, string expectedHintName, string expectedClassDeclaration)
	{
		var (sources, diagnostics) = Generate(cultureName, TestContext.Current.CancellationToken);

		Assert.True(sources.TryGetValue(expectedHintName, out var source), $"{expectedHintName} was not generated. Generated: {string.Join(", ", sources.Keys)}");
		Assert.Contains(expectedClassDeclaration, source, StringComparison.Ordinal);
		Assert.True(diagnostics.Count == 0, string.Join(Environment.NewLine, diagnostics));
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void Generator_EmitsNothing_ForNestedTextTypesThatAStaticClassCannotNameOrSee(string cultureName)
	{
		var (sources, diagnostics) = Generate(cultureName, TestContext.Current.CancellationToken);

		Assert.DoesNotContain(sources.Keys, static hintName => hintName.StartsWith("PrivateNestedOuter", StringComparison.Ordinal));
		Assert.DoesNotContain(sources.Keys, static hintName => hintName.StartsWith("ProtectedNestedOuter", StringComparison.Ordinal));
		Assert.DoesNotContain(sources.Keys, static hintName => hintName.StartsWith("PrivateProtectedNestedOuter", StringComparison.Ordinal));
		Assert.DoesNotContain(sources.Keys, static hintName => hintName.StartsWith("PublicInPrivateOuterHolder", StringComparison.Ordinal));
		Assert.DoesNotContain(sources.Keys, static hintName => hintName.StartsWith("GenericOuter", StringComparison.Ordinal));
		Assert.True(diagnostics.Count == 0, string.Join(Environment.NewLine, diagnostics));
	}

	static (Dictionary<string, string> Sources, List<string> Diagnostics) Generate(string cultureName, CancellationToken cancellationToken)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUiCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo(cultureName);
			CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

			var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
			var compilation = CSharpCompilation.Create(
				"NestedTextTypesAssembly",
				[CSharpSyntaxTree.ParseText(nestedTextTypesSource, parseOptions, cancellationToken: cancellationToken)],
				GeneratorCultureTests.GetReferences(),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

			var driver = CSharpGeneratorDriver.Create([new TextColorToGenerator().AsSourceGenerator()], parseOptions: parseOptions);
			driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics, cancellationToken);

			var generatedTrees = outputCompilation.SyntaxTrees.Where(static tree => tree.FilePath.Contains("TextColorToGenerator", StringComparison.Ordinal)).ToList();
			var sources = generatedTrees.ToDictionary(static tree => Path.GetFileName(tree.FilePath), tree => tree.GetText(cancellationToken).ToString(), StringComparer.Ordinal);
			var diagnostics = outputCompilation.GetDiagnostics(cancellationToken)
				.Concat(generatorDiagnostics)
				.Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error || diagnostic.Location.SourceTree?.FilePath.Contains("TextColorToGenerator", StringComparison.Ordinal) is true && diagnostic.Severity >= DiagnosticSeverity.Warning)
				.Select(static diagnostic => diagnostic.ToString())
				.ToList();

			return (sources, diagnostics);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUiCulture;
		}
	}
}