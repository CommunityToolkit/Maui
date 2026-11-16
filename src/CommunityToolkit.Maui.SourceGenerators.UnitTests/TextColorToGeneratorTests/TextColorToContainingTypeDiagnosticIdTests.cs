using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class TextColorToContainingTypeDiagnosticIdTests : BaseTest
{
	const string experimentalOuterConstraintSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using System.Diagnostics.CodeAnalysis;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace ExperimentalOuter;

		[Experimental("USR9")]
		public interface IExp { }

		#pragma warning disable USR9
		public class ExpOuter<T> where T : IExp
		{
			public class InnerText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}
		#pragma warning restore USR9
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesCleanly_WhenAContainingTypeConstraintIsExperimental(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(experimentalOuterConstraintSource, cultureName, "ExpOuter-1+InnerTextTextColorTo");
		Assert.Contains(GeneratedCodeCompilation.RunStrict(experimentalOuterConstraintSource, cultureName).Findings, static finding => finding.Id is "USR9");
	}
}