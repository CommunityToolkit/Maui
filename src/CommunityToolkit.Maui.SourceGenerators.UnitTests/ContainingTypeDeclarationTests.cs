using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class ContainingTypeDeclarationTests : BaseTest
{
	const string nestedInGenericTypesSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace GenericContainers;

		public partial class Outer<T>
		{
			public partial class Inner<U> : View
			{
				[BindableProperty]
				public partial T? First { get; set; }

				[BindableProperty]
				public partial U? Second { get; set; }

				[BindableProperty]
				public partial string Label { get; set; } = "label";
			}

			public partial class NonGenericInner : View
			{
				[BindableProperty]
				public partial string Title { get; set; } = "title";

				[BindableProperty]
				public partial T? Item { get; set; }
			}
		}

		public partial class Constrained<T> where T : class
		{
			public partial class Inner<U> : View where U : struct
			{
				[BindableProperty]
				public partial T? Reference { get; set; }

				[BindableProperty]
				public partial U Value { get; set; }
			}
		}

		public partial class DeeplyGeneric<T1>
		{
			public partial class Middle<T2>
			{
				public partial class Innermost<T3> : View
				{
					[BindableProperty]
					public partial T1? Outermost { get; set; }

					[BindableProperty]
					public partial T3? Own { get; set; }
				}
			}
		}

		public partial class AttachedOuter<T>
		{
			[AttachedBindableProperty<string>("Tag")]
			public partial class AttachedInner<U> : View
			{
			}

			public partial class AttachedConstructorInner : View
			{
				[AttachedBindableProperty<int>("Rank")]
				public AttachedConstructorInner()
				{
				}
			}
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForTypesNestedInGenericTypes(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(nestedInGenericTypesSource, cultureName, "Outer", "Constrained", "DeeplyGeneric", "AttachedOuter");
	}

	[Fact]
	public void GeneratedCode_DeclaresTheTypeParametersOfEveryContainingType()
	{
		var result = GeneratedCodeCompilation.Run(nestedInGenericTypesSource, "", TestContext.Current.CancellationToken);

		var deeplyGeneric = result.GetSource("Innermost.g.cs");
		Assert.Contains("partial class DeeplyGeneric<T1>", deeplyGeneric, StringComparison.Ordinal);
		Assert.Contains("partial class Middle<T2>", deeplyGeneric, StringComparison.Ordinal);
		Assert.Contains("partial class Innermost<T3>", deeplyGeneric, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("", "internal partial class Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("", "public partial class Outer { internal partial class Inner : View { [BindableProperty] internal partial string Text { get; set; } = \"\"; } }")]
	[InlineData("", "public static partial class Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("", "public abstract partial class Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("", "public partial struct Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("", "public partial record Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("", "public partial record struct Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("", "public partial interface Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("", "internal partial struct Outer { [AttachedBindableProperty<string>(\"Tag\")] public partial class Inner : View { } }")]
	[InlineData("", "public static partial class Outer { public partial class Inner : View { [AttachedBindableProperty<int>(\"Rank\")] public Inner() { } } }")]
	[InlineData("tr-TR", "internal partial class Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("tr-TR", "public partial struct Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("tr-TR", "public static partial class Outer { public partial class Inner : View { [AttachedBindableProperty<int>(\"Rank\")] public Inner() { } } }")]
	[InlineData("az-Latn-AZ", "public partial record Outer { public partial class Inner : View { [BindableProperty] public partial string Text { get; set; } = \"\"; } }")]
	[InlineData("az-Latn-AZ", "internal partial struct Outer { [AttachedBindableProperty<string>(\"Tag\")] public partial class Inner : View { } }")]
	public void GeneratedCode_CompilesWithoutDiagnostics_WhenContainingTypesDifferInKindOrAccessibility(string cultureName, string declarations)
	{
		var source = "using CommunityToolkit.Maui; using Microsoft.Maui.Controls; namespace Containers; " + declarations;

		GeneratedCodeCompilation.AssertCompilesCleanly(source, cultureName, "Outer");
	}
}