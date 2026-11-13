using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class TextColorToGenericOuterTests : BaseTest
{
	const string textStyleMembers =
		"""
		public Color TextColor { get; set; } = Colors.Black;
		public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
		public double CharacterSpacing => 0;
		""";

	const string genericOuterSource =
		/* language=C#-test */
		//lang=csharp
		$$"""
		using System;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace GenericOuters;

		public class GenericOuter<T>
		{
			public class InnerText : VisualElement, ITextStyle
			{
				{{textStyleMembers}}
			}
		}

		public class TwoLevels<T1>
		{
			public class Middle<T2>
			{
				public class InnerText<T3> : VisualElement, ITextStyle where T3 : class
				{
					{{textStyleMembers}}
				}
			}
		}

		public class ConstrainedOuter<T> where T : IComparable<T>
		{
			public class InnerText<U> : VisualElement, ITextStyle where U : T, new()
			{
				{{textStyleMembers}}
			}
		}

		internal class InternalGenericOuter<T> where T : struct
		{
			public class InnerText : VisualElement, ITextStyle
			{
				{{textStyleMembers}}
			}
		}

		public class ShadowingOuter<T>
		{
			public class InnerText<T> : VisualElement, ITextStyle
			{
				{{textStyleMembers}}
			}
		}

		public static class Usage
		{
			public static void Run(GenericOuter<int>.InnerText plain, TwoLevels<int>.Middle<string>.InnerText<object> deep, ConstrainedOuter<int>.InnerText<int> constrained)
			{
				_ = plain.TextColorTo(Colors.Red);
				_ = deep.TextColorTo(Colors.Red);
				_ = constrained.TextColorTo(Colors.Red);
			}
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForTextTypesNestedInGenericTypes(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(
			genericOuterSource,
			cultureName,
			"GenericOuter-1+InnerTextTextColorTo",
			"TwoLevels-1+Middle-1+InnerText-1TextColorTo",
			"ConstrainedOuter-1+InnerText-1TextColorTo",
			"InternalGenericOuter-1+InnerTextTextColorTo");
	}

	[Fact]
	public void GeneratedCode_RepeatsTheTypeParametersAndConstraintsOfAllContainingTypes()
	{
		var result = GeneratedCodeCompilation.Run(genericOuterSource, "", TestContext.Current.CancellationToken);

		var deep = result.GetSource("GenericOuters.TwoLevels-1+Middle-1+InnerText-1TextColorTo.g.shared.cs");
		Assert.Contains("TextColorTo<T1, T2, T3>(this global::GenericOuters.TwoLevels<T1>.Middle<T2>.InnerText<T3> element", deep, StringComparison.Ordinal);
		Assert.Contains("where T3 : class", deep, StringComparison.Ordinal);

		var constrained = result.GetSource("GenericOuters.ConstrainedOuter-1+InnerText-1TextColorTo.g.shared.cs");
		Assert.Contains("where T : global::System.IComparable<T>", constrained, StringComparison.Ordinal);
		Assert.Contains("where U : T, new()", constrained, StringComparison.Ordinal);

		Assert.Contains("internal static partial class", result.GetSource("GenericOuters.InternalGenericOuter-1+InnerTextTextColorTo.g.shared.cs"), StringComparison.Ordinal);
	}

	[Fact]
	public void Generator_EmitsNothing_ForTextTypesThatRedeclareATypeParameterOfTheirContainingType()
	{
		var result = GeneratedCodeCompilation.Run(genericOuterSource, "", TestContext.Current.CancellationToken);

		Assert.DoesNotContain(result.Sources.Keys, static fileName => fileName.Contains("ShadowingOuter", StringComparison.Ordinal));
	}
}