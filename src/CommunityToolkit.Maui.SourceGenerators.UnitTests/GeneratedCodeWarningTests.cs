using System.Globalization;
using CommunityToolkit.Maui.SourceGenerators.Helpers;
using CommunityToolkit.Maui.SourceGenerators.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

// Generated code names the user's members and types, which the user may deprecate, hide or mark experimental, and cannot annotate generated members; IL codes are never listed.
// CS9258: the generated getter means the `field` keyword even when the user's class has a member named `field`.
public class GeneratedCodeWarningTests : BaseTest
{
	const string strictConsumerSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace StrictConsumer;

		/// <summary>Flavors.</summary>
		public enum Flavor
		{
			/// <summary>Sweet.</summary>
			Sweet,
			/// <summary>Sour.</summary>
			Sour
		}

		/// <summary>View.</summary>
		public partial class StrictView : View
		{
			/// <summary>Index.</summary>
			[BindableProperty]
			public partial int Index { get; set; } = -1;

			/// <summary>Caption.</summary>
			[BindableProperty]
			public partial string? Caption { get; set; }

			/// <summary>Title.</summary>
			[BindableProperty]
			public partial string Title { get; set; } = "title";

			/// <summary>Ratio.</summary>
			[BindableProperty]
			public partial double Ratio { get; set; } = 3.14;

			/// <summary>Taste.</summary>
			[BindableProperty]
			public partial Flavor Taste { get; set; } = Flavor.Sour;

			/// <summary>Callbacks.</summary>
			[BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnAmountChanged), ValidateValueMethodName = nameof(ValidateAmount), CoerceValueMethodName = nameof(CoerceAmount))]
			public partial int Amount { get; set; }

			/// <summary>Creator.</summary>
			[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateLabel))]
			public partial string Label { get; set; }

			/// <summary>Nullable with initializer.</summary>
			[BindableProperty]
			public partial System.Windows.Input.ICommand? Command { get; set; } = null;

			/// <summary>Private setter.</summary>
			[BindableProperty]
			public partial bool IsBusy { get; private set; }

			/// <summary>Non-null reference initializer.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.IList<string> Items { get; set; } = new System.Collections.Generic.List<string>();

			/// <summary>Nullable string array.</summary>
			[BindableProperty]
			public partial string[]? NullableStrings { get; set; }

			/// <summary>Nullable int array.</summary>
			[BindableProperty]
			public partial int[]? NullableInts { get; set; }

			/// <summary>Nullable jagged int array.</summary>
			[BindableProperty]
			public partial int[][]? NullableJagged { get; set; }

			/// <summary>Nullable reference initializer.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.IList<string>? MaybeItems { get; set; } = new System.Collections.Generic.List<string>();

			static void OnAmountChanged(BindableObject bindable, object oldValue, object newValue)
			{
			}

			static bool ValidateAmount(BindableObject bindable, object value) => value is int;

			static object CoerceAmount(BindableObject bindable, object value) => value;

			static string CreateLabel(BindableObject bindable) => "label";
		}

		/// <summary>Generic view.</summary>
		/// <typeparam name="T">Item type.</typeparam>
		public partial class StrictGenericView<T> : View
		{
			/// <summary>Items.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.IList<T>? Items { get; set; } = new System.Collections.Generic.List<T>();
		}

		/// <summary>Host.</summary>
		[AttachedBindableProperty<int>("Rank", DefaultValue = 3)]
		[AttachedBindableProperty<string>("Tag", IsNullable = true)]
		[AttachedBindableProperty<double>("Pi", DefaultValue = 3.14)]
		[AttachedBindableProperty<Flavor>("Taste", DefaultValue = Flavor.Sour)]
		[AttachedBindableProperty<bool>("IsOpen", SetterAccessibility = AccessModifier.Private)]
		[AttachedBindableProperty<System.Type>("Kind", DefaultValue = typeof(StrictView))]
		[AttachedBindableProperty<System.Type[]>("Kinds", DefaultValue = new[] { typeof(StrictView), typeof(Flavor) })]
		public partial class StrictHost : View
		{
		}
		""";

	const string deprecationAndHidingSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using System;
		using CommunityToolkit.Maui;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace DeprecationConsumer;

		/// <summary>Case A: deprecated control whose property needs the default-value helper.</summary>
		[Obsolete("Use another view")]
		public partial class CaseAObsoleteView : View
		{
			/// <summary>Items.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.IList<string> Items { get; set; } = new System.Collections.Generic.List<string>();
		}

		/// <summary>Case B: deprecated property that needs the helper.</summary>
		public partial class CaseBView : View
		{
			/// <summary>Legacy items.</summary>
			[Obsolete("Use Items")]
			[BindableProperty]
			public partial System.Collections.Generic.IList<string> LegacyItems { get; set; } = new System.Collections.Generic.List<string>();
		}

		/// <summary>Case C: deprecated changed callback by string.</summary>
		public partial class CaseCView : View
		{
			/// <summary>Old.</summary>
			[BindableProperty(PropertyChangedMethodName = "OnOldChanged")]
			public partial int Old { get; set; }

			[Obsolete("Do not call")]
			static void OnOldChanged(BindableObject bindable, object oldValue, object newValue)
			{
			}
		}

		/// <summary>Case K: deprecated changed callback by nameof.</summary>
		public partial class CaseKView : View
		{
			/// <summary>Old.</summary>
			[BindableProperty(PropertyChangedMethodName = nameof(OnOldChanged))]
			public partial int Old { get; set; }

			[Obsolete("Do not call")]
			static void OnOldChanged(BindableObject bindable, object oldValue, object newValue)
			{
			}
		}

		/// <summary>Case J: hides a plain CLR property of the base type.</summary>
		public partial class CaseJView : View
		{
			/// <summary>Style id.</summary>
			[BindableProperty]
			public new partial string? StyleId { get; set; }
		}

		/// <summary>Case L: base type has a static RowProperty but no instance Row property.</summary>
		public partial class CaseLGrid : Grid
		{
			/// <summary>Row.</summary>
			[BindableProperty]
			public partial int Row { get; set; }
		}

		/// <summary>Case M: attached host that hides the Grid attached Row members.</summary>
		[AttachedBindableProperty<int>("Row")]
		public partial class CaseMGridHost : Grid
		{
		}

		/// <summary>Case N: attached property with a deprecated changed callback.</summary>
		[AttachedBindableProperty<int>("Level", PropertyChangedMethodName = nameof(OnLevelChanged))]
		public partial class CaseNHost : View
		{
			[Obsolete("Do not call")]
			static void OnLevelChanged(BindableObject bindable, object oldValue, object newValue)
			{
			}
		}

		/// <summary>Case E: public custom text type that MAUI does not already cover.</summary>
		public class CaseEFancyText : VisualElement, ITextStyle
		{
			/// <inheritdoc/>
			public Color TextColor { get; set; } = Colors.Black;

			/// <inheritdoc/>
			public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;

			/// <inheritdoc/>
			public double CharacterSpacing => 0;
		}

		/// <summary>Case E2: public generic custom text type.</summary>
		/// <typeparam name="T">Payload.</typeparam>
		public class CaseEGenericText<T> : VisualElement, ITextStyle where T : class
		{
			/// <inheritdoc/>
			public Color TextColor { get; set; } = Colors.Black;

			/// <inheritdoc/>
			public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;

			/// <inheritdoc/>
			public double CharacterSpacing => 0;
		}

		/// <summary>Case F: deprecated custom text type.</summary>
		[Obsolete("Use CaseEFancyText")]
		public class CaseFObsoleteText : VisualElement, ITextStyle
		{
			/// <inheritdoc/>
			public Color TextColor { get; set; } = Colors.Black;

			/// <inheritdoc/>
			public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;

			/// <inheritdoc/>
			public double CharacterSpacing => 0;
		}
		""";

	const string userChosenDiagnosticIdsSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using System;
		using System.Diagnostics.CodeAnalysis;
		using CommunityToolkit.Maui;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace UserChosenIds;

		/// <summary>P1: experimental control whose property needs the default-value helper.</summary>
		[Experimental("USR001")]
		public partial class P1ExperimentalView : View
		{
			/// <summary>Items.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.IList<string> Items { get; set; } = new System.Collections.Generic.List<string>();
		}

		/// <summary>P2: experimental property that needs the helper.</summary>
		public partial class P2View : View
		{
			/// <summary>Items.</summary>
			[Experimental("USR002")]
			[BindableProperty]
			public partial System.Collections.Generic.IList<string> NewItems { get; set; } = new System.Collections.Generic.List<string>();
		}

		/// <summary>P3: experimental changed callback.</summary>
		public partial class P3View : View
		{
			/// <summary>Level.</summary>
			[BindableProperty(PropertyChangedMethodName = nameof(OnLevelChanged))]
			public partial int Level { get; set; }

			[Experimental("USR003")]
			static void OnLevelChanged(BindableObject bindable, object oldValue, object newValue)
			{
			}
		}

		/// <summary>P4: experimental custom text type.</summary>
		[Experimental("USR004")]
		public class P4ExperimentalText : VisualElement, ITextStyle
		{
			/// <inheritdoc/>
			public Color TextColor { get; set; } = Colors.Black;

			/// <inheritdoc/>
			public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;

			/// <inheritdoc/>
			public double CharacterSpacing => 0;
		}

		/// <summary>P5: attached property with an experimental changed callback.</summary>
		[AttachedBindableProperty<int>("Depth", PropertyChangedMethodName = nameof(OnDepthChanged))]
		public partial class P5Host : View
		{
			[Experimental("USR005")]
			static void OnDepthChanged(BindableObject bindable, object oldValue, object newValue)
			{
			}
		}

		/// <summary>P6: member named field.</summary>
		public partial class P6View : View
		{
			readonly int field = 1;

			/// <summary>Items.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.IList<string> Items { get; set; } = new System.Collections.Generic.List<string>();

			/// <summary>Uses the field.</summary>
			public int UseField() => field;
		}

		/// <summary>P12: control deprecated with its own diagnostic ID.</summary>
		[Obsolete("Use another view", DiagnosticId = "USROBS1")]
		public partial class P12ObsoleteIdView : View
		{
			/// <summary>Items.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.IList<string> Items { get; set; } = new System.Collections.Generic.List<string>();
		}

		/// <summary>P13: text type deprecated with its own diagnostic ID.</summary>
		[Obsolete("Use another text", DiagnosticId = "USROBS2")]
		public class P13ObsoleteIdText : VisualElement, ITextStyle
		{
			/// <inheritdoc/>
			public Color TextColor { get; set; } = Colors.Black;

			/// <inheritdoc/>
			public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;

			/// <inheritdoc/>
			public double CharacterSpacing => 0;
		}

		/// <summary>P14: item type that is experimental.</summary>
		[Experimental("USR014")]
		public class P14ExperimentalItem
		{
		}

		/// <summary>P14: property typed as a generic over an experimental type, an array of it and a nullable of a struct of it.</summary>
		public partial class P14View : View
		{
			/// <summary>Items.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.List<P14ExperimentalItem>? Items { get; set; }

			/// <summary>Array.</summary>
			[BindableProperty]
			public partial P14ExperimentalItem[] Array { get; set; } = [];
		}

		/// <summary>P15: struct that is experimental.</summary>
		[Experimental("USR015")]
		public struct P15ExperimentalStruct
		{
		}

		/// <summary>P15: nullable of an experimental struct as a property type.</summary>
		public partial class P15View : View
		{
			/// <summary>Marker.</summary>
			[BindableProperty]
			public partial P15ExperimentalStruct? Mark { get; set; }
		}

		/// <summary>P15: an experimental struct as an attached property type.</summary>
		[AttachedBindableProperty<P15ExperimentalStruct>("Marker")]
		public partial class P15Host : View
		{
		}

		/// <summary>P16: experimental container of a nested view.</summary>
		[Experimental("USR016")]
		public partial class P16Outer
		{
			/// <summary>Nested view.</summary>
			public partial class P16Inner : View
			{
				/// <summary>Items.</summary>
				[BindableProperty]
				public partial System.Collections.Generic.IList<string> Items { get; set; } = new System.Collections.Generic.List<string>();
			}
		}

		/// <summary>P18: nested type of a generic over an experimental type.</summary>
		public class Box<T>
		{
			/// <summary>Inner.</summary>
			public class Inner
			{
			}
		}

		/// <summary>P18: attached property typed as a nested type of a generic over an experimental type.</summary>
		[AttachedBindableProperty<Box<P15ExperimentalStruct>.Inner>("Boxed")]
		public partial class P18Host : View
		{
		}

		/// <summary>P18: property typed as a nested type of a generic over an experimental type.</summary>
		public partial class P18View : View
		{
			/// <summary>Inner.</summary>
			[BindableProperty]
			public partial Box<P14ExperimentalItem>.Inner? Inner { get; set; }
		}

		/// <summary>P17: view whose property type comes from an assembly that is experimental as a whole.</summary>
		public partial class P17View : View
		{
			/// <summary>Item.</summary>
			[BindableProperty]
			public partial ExperimentalAssembly.ExternalItem? Item { get; set; }

			/// <summary>Items.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.List<ExperimentalAssembly.ExternalItem> Items { get; set; } = new System.Collections.Generic.List<ExperimentalAssembly.ExternalItem>();
		}
		""";

	const string experimentalAssemblySource =
		/* language=C#-test */
		//lang=csharp
		"""
		using System.Diagnostics.CodeAnalysis;

		[assembly: Experimental("EXPASM1")]

		namespace ExperimentalAssembly;

		public class ExternalItem
		{
		}
		""";

	const string unusablePragmaTokensSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using System;
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace UnusableTokens;

		/// <summary>Obsolete with an ID that is not a pragma token.</summary>
		[Obsolete("old", DiagnosticId = "not a token")]
		public partial class SpacedIdView : View
		{
			/// <summary>Items.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.IList<string> Items { get; set; } = new System.Collections.Generic.List<string>();
		}

		/// <summary>Obsolete with a trim warning ID.</summary>
		[Obsolete("old", DiagnosticId = "IL2026")]
		public partial class TrimIdView : View
		{
			/// <summary>Items.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.IList<string> Items { get; set; } = new System.Collections.Generic.List<string>();
		}

		/// <summary>Experimental with an ID that becomes a trim warning ID once the lexer drops its invisible character.</summary>
		[System.Diagnostics.CodeAnalysis.Experimental("IL\u200B2026")]
		public class ZwspItem
		{
		}

		/// <summary>Obsolete with an ID that becomes a trim warning ID once the lexer drops its soft hyphen.</summary>
		[Obsolete("x", DiagnosticId = "IL\u00AD3050")]
		public class SoftHyphenItem
		{
		}

		/// <summary>Uses both types.</summary>
		public partial class ZwspIdView : View
		{
			/// <summary>Zwsp.</summary>
			[BindableProperty]
			public partial ZwspItem? Zwsp { get; set; }

			/// <summary>Soft hyphen.</summary>
			[BindableProperty]
			public partial SoftHyphenItem? SoftHyphen { get; set; }
		}
		""";

	const string selfReferencingConstraintsSource =
		/* language=C#-test */
		//lang=csharp
		"""
		#pragma warning disable SELF1
		using System;
		using System.Diagnostics.CodeAnalysis;
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace SelfReferencing;

		/// <summary>Experimental self-referencing interface.</summary>
		/// <typeparam name="T">Self.</typeparam>
		[Experimental("SELF1")]
		public interface IExpCmp<T>
		{
		}

		/// <summary>Comparable view.</summary>
		/// <typeparam name="T">Value type.</typeparam>
		public partial class CmpView<T> : View where T : IComparable<T>
		{
			/// <summary>Value.</summary>
			[BindableProperty]
			public partial T? Value { get; set; }

			/// <summary>Values.</summary>
			[BindableProperty]
			public partial System.Collections.Generic.List<T>? Values { get; set; }
		}

		/// <summary>Curiously recurring view.</summary>
		/// <typeparam name="TSelf">Self.</typeparam>
		public partial class NodeView<TSelf> : View where TSelf : NodeView<TSelf>
		{
			/// <summary>Parent.</summary>
			[BindableProperty]
			public partial TSelf? ParentNode { get; set; }
		}

		/// <summary>Experimental constraint view.</summary>
		/// <typeparam name="T">Value type.</typeparam>
		public partial class ExpCmpView<T> : View where T : IExpCmp<T>
		{
			/// <summary>Value.</summary>
			[BindableProperty]
			public partial T? Value { get; set; }
		}
		""";

	const string selfReferencingTextSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using System;
		using CommunityToolkit.Maui;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace SelfReferencingText;

		/// <summary>Generic text type constrained by itself.</summary>
		/// <typeparam name="T">Payload.</typeparam>
		public class ComparableText<T> : VisualElement, ITextStyle where T : IComparable<T>
		{
			/// <inheritdoc/>
			public Color TextColor { get; set; } = Colors.Black;

			/// <inheritdoc/>
			public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;

			/// <inheritdoc/>
			public double CharacterSpacing => 0;
		}
		""";

	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutWarnings_UnderNullableAndDocumentationChecks(string cultureName)
	{
		var (generatedFilePaths, diagnosticsInGeneratedCode) = CompileWithGenerators(strictConsumerSource, cultureName, null, TestContext.Current.CancellationToken);

		Assert.NotEmpty(generatedFilePaths);
		Assert.True(diagnosticsInGeneratedCode.Count == 0, string.Join(Environment.NewLine, diagnosticsInGeneratedCode));
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutWarnings_WhenUserDeprecatesOrHidesMembersAndDeclaresPublicTextTypes(string cultureName)
	{
		var (generatedFilePaths, diagnosticsInGeneratedCode) = CompileWithGenerators(deprecationAndHidingSource, cultureName, null, TestContext.Current.CancellationToken);

		Assert.Contains(generatedFilePaths, static path => path.Contains("CaseEFancyTextTextColorTo", StringComparison.Ordinal));
		Assert.Contains(generatedFilePaths, static path => path.Contains("CaseEGenericText-1TextColorTo", StringComparison.Ordinal));
		Assert.Contains(generatedFilePaths, static path => path.Contains("CaseFObsoleteTextTextColorTo", StringComparison.Ordinal));
		Assert.True(diagnosticsInGeneratedCode.Count == 0, string.Join(Environment.NewLine, diagnosticsInGeneratedCode));
	}

	[Fact]
	public void GeneratedCode_NeverSuppressesTrimOrAotWarnings()
	{
		var generatedSources = GenerateSources(deprecationAndHidingSource, TestContext.Current.CancellationToken);

		Assert.NotEmpty(generatedSources);
		foreach (var source in generatedSources)
		{
			var pragmaLines = source.Split('\n').Where(static line => line.StartsWith("#pragma", StringComparison.Ordinal));
			Assert.DoesNotContain(pragmaLines, static line => line.Contains("IL2", StringComparison.Ordinal) || line.Contains("IL3", StringComparison.Ordinal));
		}
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_WhenTypeConstraintsReferToThemselves(string cultureName)
	{
		var (generatedFilePaths, diagnosticsInGeneratedCode) = CompileWithGenerators(selfReferencingConstraintsSource, cultureName, null, TestContext.Current.CancellationToken);

		Assert.Contains(generatedFilePaths, static path => string.Equals(Path.GetFileName(path), "SelfReferencing.CmpView-1.g.cs", StringComparison.Ordinal));
		Assert.Contains(generatedFilePaths, static path => string.Equals(Path.GetFileName(path), "SelfReferencing.NodeView-1.g.cs", StringComparison.Ordinal));
		Assert.Contains(generatedFilePaths, static path => string.Equals(Path.GetFileName(path), "SelfReferencing.ExpCmpView-1.g.cs", StringComparison.Ordinal));
		Assert.True(diagnosticsInGeneratedCode.Count == 0, string.Join(Environment.NewLine, diagnosticsInGeneratedCode));
	}

	[Fact]
	public void GeneratedCode_NamesTheExperimentalIdOfASelfReferencingConstraint()
	{
		var pragmaLine = GetPragmaLine(selfReferencingConstraintsSource, "ExpCmpView-1.g.cs", []);

		Assert.Equal("#pragma warning disable CS0108, CS0109, CS0612, CS0618, CS9258, SELF1", pragmaLine);
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void TextColorTo_Completes_WhenTheTextTypeConstraintRefersToItself(string cultureName)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUiCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo(cultureName);
			CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

			var (compilation, parseOptions) = CreateCompilation(selfReferencingTextSource, [], TestContext.Current.CancellationToken);
			var driver = CreateDriver(parseOptions);
			driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics, TestContext.Current.CancellationToken);

			Assert.DoesNotContain(generatorDiagnostics, static diagnostic => diagnostic.Id == "CS8785");
			var generatedTree = outputCompilation.SyntaxTrees.Single(static tree => IsGeneratedFile(tree.FilePath) && tree.FilePath.Contains("ComparableText-1TextColorTo", StringComparison.Ordinal));
			var pragmaLine = generatedTree.GetText(TestContext.Current.CancellationToken).ToString().Split('\n').Single(static line => line.StartsWith("#pragma", StringComparison.Ordinal)).TrimEnd('\r');
			Assert.Equal("#pragma warning disable CS0612, CS0618", pragmaLine);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUiCulture;
		}
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_WhenUserChoosesOwnDiagnosticIds(string cultureName)
	{
		var (generatedFilePaths, diagnosticsInGeneratedCode) = CompileWithGenerators(userChosenDiagnosticIdsSource, cultureName, [CreateExperimentalAssemblyReference()], TestContext.Current.CancellationToken);

		Assert.Contains(generatedFilePaths, static path => path.Contains("P4ExperimentalTextTextColorTo", StringComparison.Ordinal));
		Assert.Contains(generatedFilePaths, static path => path.Contains("P13ObsoleteIdTextTextColorTo", StringComparison.Ordinal));
		Assert.True(diagnosticsInGeneratedCode.Count == 0, string.Join(Environment.NewLine, diagnosticsInGeneratedCode));
	}

	[Theory]
	[InlineData("P1ExperimentalView.g.cs", "USR001")]
	[InlineData("P2View.g.cs", "USR002")]
	[InlineData("P3View.g.cs", "USR003")]
	[InlineData("P4ExperimentalTextTextColorTo.g.shared.cs", "USR004")]
	[InlineData("P5Host.g.cs", "USR005")]
	[InlineData("P12ObsoleteIdView.g.cs", "USROBS1")]
	[InlineData("P13ObsoleteIdTextTextColorTo.g.shared.cs", "USROBS2")]
	[InlineData("P14View.g.cs", "USR014")]
	[InlineData("P15View.g.cs", "USR015")]
	[InlineData("P16Outer+P16Inner.g.cs", "USR016")]
	[InlineData("P17View.g.cs", "EXPASM1")]
	[InlineData("P15Host.g.cs", "USR015")]
	[InlineData("P18View.g.cs", "USR014")]
	[InlineData("P18Host.g.cs", "USR015")]
	public void GeneratedCode_SuppressesTheUserChosenDiagnosticId(string generatedFileName, string expectedDiagnosticId)
	{
		var pragmaLine = GetPragmaLine(userChosenDiagnosticIdsSource, generatedFileName, [CreateExperimentalAssemblyReference()]);

		Assert.Contains(", " + expectedDiagnosticId, pragmaLine + ",", StringComparison.Ordinal);
	}

	[Fact]
	public void GeneratedCode_ListsUserChosenDiagnosticIdsDistinctAndSortedAfterTheFixedCodes()
	{
		var pragmaLine = GetPragmaLine(userChosenDiagnosticIdsSource, "P15View.g.cs", [CreateExperimentalAssemblyReference()]);

		Assert.Equal("#pragma warning disable CS0108, CS0109, CS0612, CS0618, CS9258, USR015", pragmaLine);
	}

	[Fact]
	public void GeneratedCode_NamesCs9258ForBindableProperties_ButNotForOtherGenerators()
	{
		Assert.Equal("#pragma warning disable CS0108, CS0109, CS0612, CS0618, CS9258", GetPragmaLine(userChosenDiagnosticIdsSource, "P6View.g.cs", []));
		Assert.Equal("#pragma warning disable CS0108, CS0109, CS0612, CS0618, USR005", GetPragmaLine(userChosenDiagnosticIdsSource, "P5Host.g.cs", []));
		Assert.Equal("#pragma warning disable CS0612, CS0618, USR004", GetPragmaLine(userChosenDiagnosticIdsSource, "P4ExperimentalTextTextColorTo.g.shared.cs", []));
	}

	[Fact]
	public void GeneratedCode_SkipsDiagnosticIdsThatAreNotPragmaTokensAndTrimWarnings()
	{
		Assert.Equal("#pragma warning disable CS0108, CS0109, CS0612, CS0618, CS9258", GetPragmaLine(unusablePragmaTokensSource, "SpacedIdView.g.cs", []));
		Assert.Equal("#pragma warning disable CS0108, CS0109, CS0612, CS0618, CS9258", GetPragmaLine(unusablePragmaTokensSource, "TrimIdView.g.cs", []));
		Assert.Equal("#pragma warning disable CS0108, CS0109, CS0612, CS0618, CS9258", GetPragmaLine(unusablePragmaTokensSource, "ZwspIdView.g.cs", []));
	}

	[Theory]
	[InlineData("IL2026", true)]
	[InlineData("IL3050", true)]
	[InlineData("IL", false)]
	[InlineData("ILX1", false)]
	[InlineData("USR001", false)]
	public void GeneratedCodePragmas_RecognizesTrimAndAotWarnings(string id, bool expected)
	{
		Assert.Equal(expected, GeneratedCodePragmas.IsTrimOrAotWarning(id));
	}

	[Theory]
	[InlineData("USR001", true)]
	[InlineData("_x1", true)]
	[InlineData("not a token", false)]
	[InlineData("A-B", false)]
	[InlineData("1ABC", false)]
	[InlineData("IL\u200B2026", false)]
	[InlineData("USR\u00AD1", false)]
	[InlineData("", false)]
	public void GeneratedCodePragmas_AcceptsOnlyIdentifierCharacters(string id, bool expected)
	{
		Assert.Equal(expected, GeneratedCodePragmas.IsValidPragmaToken(id));
	}

	static string GetPragmaLine(string source, string generatedFileName, IReadOnlyList<MetadataReference> extraReferences)
	{
		var (compilation, parseOptions) = CreateCompilation(source, extraReferences, TestContext.Current.CancellationToken);
		var driver = CreateDriver(parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _, TestContext.Current.CancellationToken);

		var generatedTree = outputCompilation.SyntaxTrees.Single(tree => IsGeneratedFile(tree.FilePath) && tree.FilePath.EndsWith(generatedFileName, StringComparison.Ordinal));
		return generatedTree.GetText(TestContext.Current.CancellationToken).ToString().Split('\n').Single(static line => line.StartsWith("#pragma", StringComparison.Ordinal)).TrimEnd('\r');
	}

	static MetadataReference CreateExperimentalAssemblyReference()
	{
		var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
		var experimentalAssembly = CSharpCompilation.Create(
			"ExperimentalAssembly",
			[CSharpSyntaxTree.ParseText(experimentalAssemblySource, parseOptions, cancellationToken: TestContext.Current.CancellationToken)],
			GeneratorCultureTests.GetReferences(),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		using var stream = new MemoryStream();
		var emitResult = experimentalAssembly.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));

		return MetadataReference.CreateFromImage(stream.ToArray());
	}

	static List<string> GenerateSources(string source, CancellationToken cancellationToken)
	{
		var (compilation, parseOptions) = CreateCompilation(source, [], cancellationToken);
		var driver = CreateDriver(parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _, cancellationToken);

		return outputCompilation.SyntaxTrees
			.Where(static tree => IsGeneratedFile(tree.FilePath))
			.Select(tree => tree.GetText(cancellationToken).ToString())
			.ToList();
	}

	static (HashSet<string> GeneratedFilePaths, List<string> DiagnosticsInGeneratedCode) CompileWithGenerators(string source, string cultureName, IReadOnlyList<MetadataReference>? extraReferences, CancellationToken cancellationToken)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUiCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo(cultureName);
			CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

			var (compilation, parseOptions) = CreateCompilation(source, extraReferences ?? [], cancellationToken);
			var driver = CreateDriver(parseOptions);
			driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics, cancellationToken);

			var generatedFilePaths = outputCompilation.SyntaxTrees
				.Where(static tree => IsGeneratedFile(tree.FilePath))
				.Select(static tree => tree.FilePath)
				.ToHashSet(StringComparer.Ordinal);
			var diagnosticsInGeneratedCode = outputCompilation.GetDiagnostics(cancellationToken)
				.Concat(generatorDiagnostics)
				.Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
				.Where(diagnostic => diagnostic.Location.SourceTree is not null && generatedFilePaths.Contains(diagnostic.Location.SourceTree.FilePath))
				.Select(static diagnostic => diagnostic.ToString())
				.ToList();

			return (generatedFilePaths, diagnosticsInGeneratedCode);
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUiCulture;
		}
	}

	static bool IsGeneratedFile(string filePath) => filePath.Contains("CommunityToolkit.Maui.SourceGenerators", StringComparison.Ordinal);

	static (CSharpCompilation Compilation, CSharpParseOptions ParseOptions) CreateCompilation(string source, IReadOnlyList<MetadataReference> extraReferences, CancellationToken cancellationToken)
	{
		var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Diagnose);
		var compilation = CSharpCompilation.Create(
			"StrictConsumerAssembly",
			[CSharpSyntaxTree.ParseText(source, parseOptions, cancellationToken: cancellationToken)],
			[.. GeneratorCultureTests.GetReferences(), .. extraReferences],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, warningLevel: 9999)
				.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
				{
					{ BindablePropertyDiagnostic.BindablePropertyAttributeExperimentalDiagnosticId, ReportDiagnostic.Suppress }
				}));
		return (compilation, parseOptions);
	}

	static GeneratorDriver CreateDriver(CSharpParseOptions parseOptions) => CSharpGeneratorDriver.Create(
		[new BindablePropertyAttributeSourceGenerator().AsSourceGenerator(), new AttachedBindablePropertyAttributeSourceGenerator().AsSourceGenerator(), new TextColorToGenerator().AsSourceGenerator()],
		parseOptions: parseOptions);
}