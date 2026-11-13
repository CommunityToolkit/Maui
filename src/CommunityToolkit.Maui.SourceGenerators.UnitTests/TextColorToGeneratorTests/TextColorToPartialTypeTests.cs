using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class TextColorToPartialTypeTests : BaseTest
{
	const string textTypeWithTwoBaseListsSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using System;
		using System.Threading.Tasks;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace ProbeDup;

		public partial class DupText : VisualElement, ITextStyle
		{
			public Color TextColor { get; set; } = Colors.Black;
			public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
			public double CharacterSpacing => 0;
		}

		public partial class DupText : IDisposable
		{
			public void Dispose() { }
		}

		public static class Callers
		{
			public static Task<bool> CallDupText() => new DupText().TextColorTo(Colors.Red);
			public static Task<bool> CallLabel() => new Label().TextColorTo(Colors.Red);
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_WhenATextTypeIsPartialWithABaseListOnTwoParts(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(textTypeWithTwoBaseListsSource, cultureName, "ProbeDup.DupTextTextColorTo.g.shared.cs");
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void Generator_EmitsOneFile_WhenATextTypeIsPartialWithABaseListOnTwoParts(string cultureName)
	{
		var result = GeneratedCodeCompilation.Run(textTypeWithTwoBaseListsSource, cultureName, TestContext.Current.CancellationToken);

		Assert.Single(result.Sources.Keys, static fileName => fileName.EndsWith("ProbeDup.DupTextTextColorTo.g.shared.cs", StringComparison.Ordinal));
	}
}