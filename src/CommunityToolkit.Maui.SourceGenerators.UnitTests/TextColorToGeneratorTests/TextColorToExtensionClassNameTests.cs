using System.Text.RegularExpressions;
using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public partial class TextColorToExtensionClassNameTests : BaseTest
{
	const string textStyleMembers =
		"""
		public Color TextColor { get; set; } = Colors.Black;
		public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
		public double CharacterSpacing => 0;
		""";

	const string collidingNamesSource =
		/* language=C#-test */
		//lang=csharp
		$$"""
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace Colliding;

		public class Outer
		{
			public class Inner : VisualElement, ITextStyle
			{
				{{textStyleMembers}}
			}

			public class Other : VisualElement, ITextStyle
			{
				{{textStyleMembers}}
			}
		}

		internal class Outer_Inner : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}

		public class Plain : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_WhenANestedTypeAndATopLevelTypeShareTheirJoinedName(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(collidingNamesSource, cultureName, "Outer+InnerTextColorTo", "Outer_InnerTextColorTo");
	}

	[Fact]
	public void GeneratedCode_KeepsTheNamesOfTopLevelExtensionClasses_AndAddsAStableSuffixToNestedOnes()
	{
		var result = GeneratedCodeCompilation.Run(collidingNamesSource, "", TestContext.Current.CancellationToken);

		Assert.Contains("static partial class ColorAnimationExtensions_Plain\r\n", result.GetSource("Colliding.PlainTextColorTo.g.shared.cs"), StringComparison.Ordinal);
		Assert.Contains("static partial class ColorAnimationExtensions_Outer_Inner\r\n", result.GetSource("Colliding.Outer_InnerTextColorTo.g.shared.cs"), StringComparison.Ordinal);
		var nestedClassName = NestedExtensionClassName().Match(result.GetSource("Colliding.Outer+InnerTextColorTo.g.shared.cs"));
		Assert.True(nestedClassName.Success);
		Assert.Matches(NestedExtensionClassNamePattern(), nestedClassName.Value);
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_AppendsTheFnv1aHashOfTheTypeIdentity_ToNestedExtensionClassNames(string cultureName)
	{
		var result = GeneratedCodeCompilation.Run(collidingNamesSource, cultureName, TestContext.Current.CancellationToken);

		Assert.Contains("static partial class ColorAnimationExtensions_Outer_Inner_9dcde9d0\r\n", result.GetSource("Colliding.Outer+InnerTextColorTo.g.shared.cs"), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_NamesNestedExtensionClassesTheSameInEveryCulture(string cultureName)
	{
		var invariant = GeneratedCodeCompilation.Run(collidingNamesSource, "", TestContext.Current.CancellationToken);
		var culture = GeneratedCodeCompilation.Run(collidingNamesSource, cultureName, TestContext.Current.CancellationToken);

		Assert.Equal(
			NestedExtensionClassName().Match(invariant.GetSource("Colliding.Outer+InnerTextColorTo.g.shared.cs")).Value,
			NestedExtensionClassName().Match(culture.GetSource("Colliding.Outer+InnerTextColorTo.g.shared.cs")).Value);
	}

	[Fact]
	public void GeneratedCode_GivesDifferentNestedTypesDifferentExtensionClassNames()
	{
		var result = GeneratedCodeCompilation.Run(collidingNamesSource, "", TestContext.Current.CancellationToken);

		var inner = NestedExtensionClassName().Match(result.GetSource("Colliding.Outer+InnerTextColorTo.g.shared.cs")).Value;
		var other = NestedExtensionClassName().Match(result.GetSource("Colliding.Outer+OtherTextColorTo.g.shared.cs")).Value;
		Assert.NotEqual(inner, other);
	}

	[GeneratedRegex(@"ColorAnimationExtensions_Outer_\w+(?=\r\n)")]
	private static partial Regex NestedExtensionClassName();

	[GeneratedRegex(@"^ColorAnimationExtensions_Outer_(Inner|Other)_[0-9a-f]{8}$")]
	private static partial Regex NestedExtensionClassNamePattern();
}