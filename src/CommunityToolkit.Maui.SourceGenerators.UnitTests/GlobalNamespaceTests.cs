using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class GlobalNamespaceTests : BaseTest
{
	const string globalNamespaceSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui;
		using Microsoft.Maui.Controls;
		using Microsoft.Maui.Graphics;

		public partial class GlobalView : View
		{
			[BindableProperty]
			public partial string Title { get; set; } = "title";

			[BindableProperty]
			public partial int Count { get; set; }
		}

		public partial class GlobalGenericView<T> : View
		{
			[BindableProperty]
			public partial T? Item { get; set; }
		}

		public partial class GlobalOuter
		{
			public partial class NestedView : View
			{
				[BindableProperty]
				public partial string Caption { get; set; } = "caption";
			}
		}

		[AttachedBindableProperty<string>("Tag")]
		public partial class GlobalHost : View
		{
		}

		public partial class GlobalConstructorHost : View
		{
			[AttachedBindableProperty<int>("Rank")]
			public GlobalConstructorHost()
			{
			}
		}

		public class GlobalText : VisualElement, ITextStyle
		{
			public Color TextColor { get; set; } = Colors.Black;
			public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
			public double CharacterSpacing => 0;
		}

		public class GlobalGenericText<T> : VisualElement, ITextStyle where T : class
		{
			public Color TextColor { get; set; } = Colors.Black;
			public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
			public double CharacterSpacing => 0;
		}

		public class GlobalTextOuter
		{
			public class NestedText : VisualElement, ITextStyle
			{
				public Color TextColor { get; set; } = Colors.Black;
				public Microsoft.Maui.Font Font => Microsoft.Maui.Font.Default;
				public double CharacterSpacing => 0;
			}
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForTypesInTheGlobalNamespace(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(
			globalNamespaceSource,
			cultureName,
			"GlobalView",
			"GlobalGenericView",
			"NestedView",
			"GlobalHost",
			"GlobalConstructorHost",
			"GlobalTextTextColorTo",
			"GlobalGenericTextTextColorTo",
			"NestedTextTextColorTo");
	}

	[Fact]
	public void GeneratedCode_DeclaresNoNamespace_ForTypesInTheGlobalNamespace()
	{
		var result = GeneratedCodeCompilation.Run(globalNamespaceSource, "", TestContext.Current.CancellationToken);

		foreach (var fileName in new[] { "GlobalView.g.cs", "GlobalHost.g.cs", "GlobalText" + "TextColorTo.g.shared.cs", "NestedTextTextColorTo.g.shared.cs" })
		{
			var source = result.GetSource(fileName);
			Assert.DoesNotContain("namespace", source, StringComparison.Ordinal);
			Assert.DoesNotContain("<global namespace>", source, StringComparison.Ordinal);
		}
	}
}