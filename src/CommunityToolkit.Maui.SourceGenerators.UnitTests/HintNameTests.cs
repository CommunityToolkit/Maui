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
			Assert.True(result.Sources.ContainsKey(hintName), $"{hintName} was not generated. Generated: {string.Join(", ", result.Sources.Keys)}");
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
}