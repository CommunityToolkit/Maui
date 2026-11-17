using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class MixedRankArrayTests : BaseTest
{
	const string mixedRankArraysSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace MixedRank;

		public partial class MixedRankView : View
		{
			[BindableProperty]
			public partial int[][,] JaggedOfRectangular { get; set; }

			[BindableProperty]
			public partial int[,][] RectangularOfJagged { get; set; }

			[BindableProperty]
			public partial string[,][]? NullableRectangularOfJagged { get; set; }

			[BindableProperty]
			public partial string?[][,] NullableElements { get; set; }

			[BindableProperty]
			public partial int[,,][][] ThreeLevels { get; set; }

			[BindableProperty]
			public partial int[][,]? NullableJaggedOfRectangular { get; private set; }
		}

		[AttachedBindableProperty<int[][,]>("Grid")]
		[AttachedBindableProperty<string[,][]>("Rows")]
		[AttachedBindableProperty<long[,,][][]>("Cube")]
		public partial class MixedRankHost : View
		{
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForArraysOfDifferentRanks(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(mixedRankArraysSource, cultureName, "MixedRankView", "MixedRankHost");
	}

	[Fact]
	public void GeneratedCode_KeepsTheRankOrderAndNullableAnnotationsOfTheArrayType()
	{
		var result = GeneratedCodeCompilation.Run(mixedRankArraysSource, "", TestContext.Current.CancellationToken);

		var viewSource = result.GetSource("MixedRankView.g.cs");
		AssertContains("public partial int[][,] JaggedOfRectangular", viewSource);
		AssertContains("public partial int[,][] RectangularOfJagged", viewSource);
		AssertContains("public partial string[,][]? NullableRectangularOfJagged", viewSource);
		AssertContains("public partial string?[][,] NullableElements", viewSource);
		AssertContains("public partial int[,,][][] ThreeLevels", viewSource);
		AssertContains("typeof(int[][,])", viewSource);
		AssertContains("typeof(string[,][])", viewSource);
	}

	// The generator normalizes whitespace, which writes int[][,] as "int[][, ]".
	static void AssertContains(string expected, string generatedSource) =>
		Assert.True(generatedSource.Replace(" ", string.Empty, StringComparison.Ordinal).Contains(expected.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal), $"Not found: {expected}{Environment.NewLine}{generatedSource}");
}