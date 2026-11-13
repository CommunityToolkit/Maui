using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class TextColorToSharedClassTests : BaseTest
{
	const string textStyleMembers =
		"""
		public Color TextColor { get; set; } = Colors.Black;
		public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
		public double CharacterSpacing => 0;
		""";

	const string publicAndPublicSource =
		/* language=C#-test */
		//lang=csharp
		$$"""
		using System.Threading.Tasks;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace SharedClass;

		public class FooText : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}

		public class FooText<T> : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}

		public static class Callers
		{
			public static Task<bool> CallNonGeneric() => new FooText().TextColorTo(Colors.Red);
			public static Task<bool> CallGeneric() => new FooText<int>().TextColorTo(Colors.Red);
		}
		""";

	const string publicAndInternalSource =
		/* language=C#-test */
		//lang=csharp
		$$"""
		using System.Threading.Tasks;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace SharedClass;

		public class FooText : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}

		internal class FooText<T> : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}

		public static class Callers
		{
			public static Task<bool> CallNonGeneric() => new FooText().TextColorTo(Colors.Red);
			internal static Task<bool> CallGeneric() => new FooText<int>().TextColorTo(Colors.Red);
		}
		""";

	const string internalAndPublicSource =
		/* language=C#-test */
		//lang=csharp
		$$"""
		using System.Threading.Tasks;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		namespace SharedClass;

		internal class FooText : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}

		public class FooText<T> : VisualElement, ITextStyle
		{
			{{textStyleMembers}}
		}

		public static class Callers
		{
			internal static Task<bool> CallNonGeneric() => new FooText().TextColorTo(Colors.Red);
			public static Task<bool> CallGeneric() => new FooText<int>().TextColorTo(Colors.Red);
		}
		""";

	[Theory]
	[InlineData("", publicAndPublicSource)]
	[InlineData("tr-TR", publicAndPublicSource)]
	[InlineData("az-Latn-AZ", publicAndPublicSource)]
	[InlineData("", publicAndInternalSource)]
	[InlineData("tr-TR", publicAndInternalSource)]
	[InlineData("az-Latn-AZ", publicAndInternalSource)]
	[InlineData("", internalAndPublicSource)]
	[InlineData("tr-TR", internalAndPublicSource)]
	[InlineData("az-Latn-AZ", internalAndPublicSource)]
	public void GeneratedCode_CompilesWithoutDiagnostics_WhenAGenericAndANonGenericTextTypeShareAnExtensionClass(string cultureName, string source)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(source, cultureName, "SharedClass.FooText-1TextColorTo", "SharedClass.FooTextTextColorTo");
	}

	[Fact]
	public void GeneratedCode_PutsTheGeneratedCodeAttributesOnTheMethod_NotOnTheSharedClass()
	{
		var result = GeneratedCodeCompilation.Run(publicAndPublicSource, "", TestContext.Current.CancellationToken);
		var source = result.GetSource("SharedClass.FooTextTextColorTo.g.shared.cs");

		var classLineIndex = source.IndexOf("public static partial class ColorAnimationExtensions_FooText", StringComparison.Ordinal);
		var generatedCodeAttributeIndex = source.IndexOf("[global::System.CodeDom.Compiler.GeneratedCode(", StringComparison.Ordinal);
		var methodIndex = source.IndexOf("Task<bool> TextColorTo(", StringComparison.Ordinal);
		Assert.True(classLineIndex >= 0);
		Assert.True(generatedCodeAttributeIndex > classLineIndex);
		Assert.True(generatedCodeAttributeIndex < methodIndex);
		Assert.Contains("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]", source[classLineIndex..methodIndex], StringComparison.Ordinal);
	}

	[Fact]
	public void GeneratedCode_StatesNoAccessibilityOnTheExtensionClassOfInternalTextTypes()
	{
		var result = GeneratedCodeCompilation.Run(publicAndInternalSource, "", TestContext.Current.CancellationToken);
		var source = result.GetSource("SharedClass.FooText-1TextColorTo.g.shared.cs");

		var classLine = source.Split('\n').Single(static line => line.Contains(" class ColorAnimationExtensions_FooText", StringComparison.Ordinal)).Trim();
		Assert.Equal("static partial class ColorAnimationExtensions_FooText", classLine);
		Assert.Contains("internal static Task<bool> TextColorTo<T>(", source, StringComparison.Ordinal);
	}
}