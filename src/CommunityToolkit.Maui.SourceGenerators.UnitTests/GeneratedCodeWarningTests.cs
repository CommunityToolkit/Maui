using CommunityToolkit.Maui.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

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

	// The fixture's own declarations use its experimental types, as a consumer's would; its pragma covers only its own file, never the generated files.
	const string userChosenDiagnosticIdsSource =
		/* language=C#-test */
		//lang=csharp
		"""
		#pragma warning disable USR014, USR015, EXPASM1
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

		/// <summary>Obsolete with an ID that becomes a trim warning ID once the lexer drops its invisible character.</summary>
		[Obsolete("x", DiagnosticId = "IL\u200B2026")]
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

	const string directiveWordPragmaTokensSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using System;
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace DirectiveWordTokens;

		/// <summary>Obsolete with an ID that is a pragma directive word.</summary>
		[Obsolete("old", DiagnosticId = "default")]
		public class DefaultIdItem
		{
		}

		/// <summary>Obsolete with an ID that is a pragma directive word.</summary>
		[Obsolete("old", DiagnosticId = "disable")]
		public class DisableIdItem
		{
		}

		/// <summary>Obsolete with an ID that is a pragma directive word.</summary>
		[Obsolete("old", DiagnosticId = "restore")]
		public class RestoreIdItem
		{
		}

		/// <summary>Obsolete with an ID that is a pragma directive word.</summary>
		[Obsolete("old", DiagnosticId = "enable")]
		public class EnableIdItem
		{
		}

		/// <summary>Obsolete with a keyword that Roslyn accepts as an error code.</summary>
		[Obsolete("old", DiagnosticId = "class")]
		public class ClassIdItem
		{
		}

		/// <summary>Obsolete with an ordinary user ID.</summary>
		[Obsolete("old", DiagnosticId = "USR1")]
		public class UserIdItem
		{
		}

		/// <summary>Uses every type.</summary>
		public partial class DirectiveWordIdView : View
		{
			/// <summary>Default.</summary>
			[BindableProperty]
			public partial DefaultIdItem? DefaultItem { get; set; }

			/// <summary>Disable.</summary>
			[BindableProperty]
			public partial DisableIdItem? DisableItem { get; set; }

			/// <summary>Restore.</summary>
			[BindableProperty]
			public partial RestoreIdItem? RestoreItem { get; set; }

			/// <summary>Enable.</summary>
			[BindableProperty]
			public partial EnableIdItem? EnableItem { get; set; }

			/// <summary>Class.</summary>
			[BindableProperty]
			public partial ClassIdItem? ClassItem { get; set; }

			/// <summary>User.</summary>
			[BindableProperty]
			public partial UserIdItem? UserItem { get; set; }
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
		GeneratedCodeCompilation.AssertCompilesCleanly(strictConsumerSource, cultureName, "StrictView");
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutWarnings_WhenUserDeprecatesOrHidesMembersAndDeclaresPublicTextTypes(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(deprecationAndHidingSource, cultureName, "CaseEFancyTextTextColorTo", "CaseEGenericText-1TextColorTo", "CaseFObsoleteTextTextColorTo");
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_WhenTypeConstraintsReferToThemselves(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(selfReferencingConstraintsSource, cultureName, "SelfReferencing.CmpView-1.g.cs", "SelfReferencing.NodeView-1.g.cs", "SelfReferencing.ExpCmpView-1.g.cs");
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void TextColorTo_Completes_WhenTheTextTypeConstraintRefersToItself(string cultureName)
	{
		var result = GeneratedCodeCompilation.Run(selfReferencingTextSource, cultureName, TestContext.Current.CancellationToken);

		Assert.DoesNotContain(result.Findings, static finding => finding.Id is "CS8785");
		Assert.True(result.HasFile("SelfReferencingText.ComparableText-1TextColorTo.g.shared.cs"), string.Join(", ", result.Sources.Keys));
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_WhenUserChoosesOwnDiagnosticIds(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(userChosenDiagnosticIdsSource, cultureName, [CreateExperimentalAssemblyReference()], ["EXPASM1"], "P4ExperimentalTextTextColorTo", "P13ObsoleteIdTextTextColorTo");
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_WhenUserIdsAreNotPragmaTokensOrDirectiveWords(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(unusablePragmaTokensSource, cultureName, "SpacedIdView.g.cs", "ZwspIdView.g.cs");
		GeneratedCodeCompilation.AssertCompilesCleanly(directiveWordPragmaTokensSource, cultureName, "DirectiveWordIdView.g.cs");
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void StrictMode_ReportsTheIdsTheUserDeclared_SoItProvesTheShippedPragmasHideThem(string cultureName)
	{
		var strict = GeneratedCodeCompilation.RunStrict(userChosenDiagnosticIdsSource, cultureName, [CreateExperimentalAssemblyReference()]);
		var reportedIds = strict.Findings.Select(static finding => finding.Id).ToHashSet(StringComparer.Ordinal);

		Assert.Superset(new HashSet<string>(["USR001", "USR002", "USR003", "USR004", "USR005", "USR014", "USR015", "USR016", "USROBS1", "USROBS2", "EXPASM1", "CS9258"], StringComparer.Ordinal), reportedIds);
	}

	[Fact]
	public void StrictMode_ReportsDirectiveWordIds()
	{
		var strict = GeneratedCodeCompilation.RunStrict(directiveWordPragmaTokensSource, "");

		Assert.Contains(strict.Findings, static finding => finding.Id is "default");
		Assert.Contains(strict.Findings, static finding => finding.Id is "disable");
	}

	[Theory]
	[InlineData(deprecationAndHidingSource)]
	[InlineData(userChosenDiagnosticIdsSource)]
	public void GeneratedCode_StartsEveryFileWithTheSharedHeader(string source)
	{
		var result = GeneratedCodeCompilation.Run(source, "", TestContext.Current.CancellationToken, [CreateExperimentalAssemblyReference()]);

		Assert.NotEmpty(result.Sources);
		foreach (var generatedSource in result.Sources.Values)
		{
			Assert.Equal(["#pragma warning disable", $"#pragma warning restore {GeneratedCodePragmas.TrimAndAotWarnings}"], GetPragmaLines(generatedSource));
		}
	}

	static List<string> GetPragmaLines(string source) =>
		[.. source.Split('\n').Select(static line => line.TrimEnd('\r')).Where(static line => line.StartsWith("#pragma", StringComparison.Ordinal))];

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
}