using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class HintNameTests : BaseTest
{
	const string textStyleMembers =
		"""
		public Color TextColor { get; set; } = Colors.Black;
		public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
		public double CharacterSpacing => 0;
		""";

	const string sameNamedTypesSource =
		/* language=C#-test */
		//lang=csharp
		$$"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		public partial class DupView : View
		{
			[BindableProperty]
			public partial string Title { get; set; } = "global";
		}

		namespace First
		{
			public partial class DupView : View
			{
				[BindableProperty]
				public partial string Title { get; set; } = "first";
			}

			[AttachedBindableProperty<string>("Tag")]
			public partial class DupHost : View
			{
			}

			public class DupText : VisualElement, ITextStyle
			{
				{{textStyleMembers}}
			}
		}

		namespace Second
		{
			public partial class DupView : View
			{
				[BindableProperty]
				public partial string Title { get; set; } = "second";
			}

			[AttachedBindableProperty<string>("Tag")]
			public partial class DupHost : View
			{
			}

			public class DupText : VisualElement, ITextStyle
			{
				{{textStyleMembers}}
			}
		}

		namespace Nesting
		{
			public partial class OuterOne
			{
				public partial class Inner : View
				{
					[BindableProperty]
					public partial string Title { get; set; } = "one";
				}

				[AttachedBindableProperty<string>("Tag")]
				public partial class InnerHost : View
				{
				}

				public class InnerText : VisualElement, ITextStyle
				{
					{{textStyleMembers}}
				}
			}

			public partial class OuterTwo
			{
				public partial class Inner : View
				{
					[BindableProperty]
					public partial string Title { get; set; } = "two";
				}

				[AttachedBindableProperty<string>("Tag")]
				public partial class InnerHost : View
				{
				}

				public class InnerText : VisualElement, ITextStyle
				{
					{{textStyleMembers}}
				}
			}
		}

		namespace Arity
		{
			public partial class Same : View
			{
				[BindableProperty]
				public partial string Title { get; set; } = "none";
			}

			public partial class Same<T> : View
			{
				[BindableProperty]
				public partial string Title { get; set; } = "one";
			}

			public partial class Same<T, U> : View
			{
				[BindableProperty]
				public partial string Title { get; set; } = "two";
			}
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForSameNamedTypesInDifferentContainers(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(sameNamedTypesSource, cultureName);
	}

	[Fact]
	public void GeneratedCode_GivesEveryTypeItsOwnHintName()
	{
		var result = GeneratedCodeCompilation.Run(sameNamedTypesSource, "", TestContext.Current.CancellationToken);

		Assert.Empty(result.Diagnostics);
		string[] expectedHintNames =
		[
			"DupView.g.cs",
			"First.DupView.g.cs",
			"Second.DupView.g.cs",
			"First.DupHost.g.cs",
			"Second.DupHost.g.cs",
			"First.DupTextTextColorTo.g.shared.cs",
			"Second.DupTextTextColorTo.g.shared.cs",
			"Nesting.OuterOne+Inner.g.cs",
			"Nesting.OuterTwo+Inner.g.cs",
			"Nesting.OuterOne+InnerHost.g.cs",
			"Nesting.OuterTwo+InnerHost.g.cs",
			"Nesting.OuterOne+InnerTextTextColorTo.g.shared.cs",
			"Nesting.OuterTwo+InnerTextTextColorTo.g.shared.cs",
			"Arity.Same.g.cs",
			"Arity.Same-1.g.cs",
			"Arity.Same-2.g.cs",
		];
		foreach (var hintName in expectedHintNames)
		{
			Assert.True(result.HasFile(hintName), $"{hintName} was not generated. Generated: {string.Join(", ", result.Sources.Keys)}");
		}
	}

	[Theory]
	[InlineData("", "public partial class Host : View { [AttachedBindableProperty<int>(\"First\")] public Host() { } [AttachedBindableProperty<int>(\"Second\")] public Host(int value) { } }")]
	[InlineData("tr-TR", "[AttachedBindableProperty<int>(\"First\")] public partial class Host : View { [AttachedBindableProperty<int>(\"Second\")] public Host() { } }")]
	[InlineData("az-Latn-AZ", "[AttachedBindableProperty<int>(\"First\")] [AttachedBindableProperty<string>(\"Second\")] public partial class Host : View { [AttachedBindableProperty<bool>(\"Third\")] public Host() { } [AttachedBindableProperty<long>(\"Fourth\")] public Host(int value) { } }")]
	public void GeneratedCode_MergesAttachedPropertiesOfOneTypeIntoOneFile(string cultureName, string declarations)
	{
		var source = "using CommunityToolkit.Maui; using Microsoft.Maui.Controls; namespace Merged; " + declarations;

		var result = GeneratedCodeCompilation.Run(source, cultureName, TestContext.Current.CancellationToken);

		Assert.Empty(result.Diagnostics);
		var hostSource = result.GetSource("Merged.Host.g.cs");
		Assert.Contains("FirstProperty", hostSource, StringComparison.Ordinal);
		Assert.Contains("SecondProperty", hostSource, StringComparison.Ordinal);
	}

	const string caseOnlyDifferencesSource =
		/* language=C#-test */
		//lang=csharp
		$$"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace CaseOnly;

		public partial class CaseView : View { [BindableProperty] public partial string Upper { get; set; } = "a"; }
		public partial class caseView : View { [BindableProperty] public partial string Lower { get; set; } = "b"; }

		[AttachedBindableProperty<int>("Upper")] public partial class CaseHost : View { }
		[AttachedBindableProperty<int>("Lower")] public partial class caseHost : View { }

		public class CaseText : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}

		public class caseText : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForTypesThatDifferOnlyByCase(string cultureName)
	{
		var result = GeneratedCodeCompilation.Run(caseOnlyDifferencesSource, cultureName, TestContext.Current.CancellationToken);

		Assert.Empty(result.Diagnostics);
		var caseOnlyFileNames = result.Sources.Keys.Select(static key => key.Substring(key.LastIndexOf('/') + 1)).Where(static fileName => fileName.StartsWith("CaseOnly.", StringComparison.Ordinal)).ToList();
		Assert.Equal(6, caseOnlyFileNames.Count);
		Assert.Equal(6, caseOnlyFileNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
		Assert.Contains("UpperProperty", result.Sources.Single(pair => pair.Value.Contains("partial class CaseView", StringComparison.Ordinal)).Value, StringComparison.Ordinal);
		Assert.Contains("LowerProperty", result.Sources.Single(pair => pair.Value.Contains("partial class caseView", StringComparison.Ordinal)).Value, StringComparison.Ordinal);
	}

	// A combining accent is a valid identifier character, so "A\u0301View" and "A_View" are two types that must not share a file.
	static readonly string accentedNamesSource =
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace Accented;

		public partial class A
		""" + "\u0301" + """
		View : View { [BindableProperty] public partial string Accent { get; set; } = "a"; }
		public partial class A_View : View { [BindableProperty] public partial string Underscore { get; set; } = "b"; }

		[AttachedBindableProperty<int>("Accent")] public partial class A
		""" + "\u0301" + """
		Host : View { }
		[AttachedBindableProperty<int>("Underscore")] public partial class A_Host : View { }

		public class A
		""" + "\u0301" + $$"""
		Text : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}

		public class A_Text : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_KeepsTypesApart_WhenTheirNamesDifferOnlyByACombiningCharacter(string cultureName)
	{
		var result = GeneratedCodeCompilation.Run(accentedNamesSource, cultureName, TestContext.Current.CancellationToken);

		Assert.Empty(result.Diagnostics);
		var accentedView = result.Sources.Single(pair => pair.Value.Contains("partial class A\u0301View", StringComparison.Ordinal)).Value;
		var plainView = result.Sources.Single(pair => pair.Value.Contains("partial class A_View", StringComparison.Ordinal)).Value;
		Assert.Contains("AccentProperty", accentedView, StringComparison.Ordinal);
		Assert.DoesNotContain("UnderscoreProperty", accentedView, StringComparison.Ordinal);
		Assert.Contains("UnderscoreProperty", plainView, StringComparison.Ordinal);

		var accentedHost = result.Sources.Single(pair => pair.Value.Contains("partial class A\u0301Host", StringComparison.Ordinal)).Value;
		var plainHost = result.Sources.Single(pair => pair.Value.Contains("partial class A_Host", StringComparison.Ordinal)).Value;
		Assert.Contains("AccentProperty", accentedHost, StringComparison.Ordinal);
		Assert.DoesNotContain("UnderscoreProperty", accentedHost, StringComparison.Ordinal);
		Assert.Contains("UnderscoreProperty", plainHost, StringComparison.Ordinal);

		Assert.Equal(2, result.Sources.Keys.Count(key => key.EndsWith("TextTextColorTo.g.shared.cs", StringComparison.Ordinal)));
	}
}