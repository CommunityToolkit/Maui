using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class AttachedNestedArrayDefaultTests : BaseTest
{
	const string nestedArraySource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace NestedArrays;

		[AttachedBindableProperty<object>("Scalar", DefaultValue = new[] { 1, 2 })]
		public partial class ScalarHost : View { }

		[AttachedBindableProperty<object[]>("Mixed", DefaultValue = new object[] { new[] { 1, 2 }, "s" })]
		public partial class MixedHost : View { }

		[AttachedBindableProperty<object[]>("Nested", DefaultValue = new object[] { new object[] { "a", 1 } })]
		public partial class NestedHost : View { }

		[AttachedBindableProperty<object[]>("Empty", DefaultValue = new object[] { new int[0] })]
		public partial class EmptyHost : View { }

		[AttachedBindableProperty<int>("Plain", DefaultValue = 3)]
		public partial class PlainHost : View { }
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForArraysInsideObjectDefaults(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(nestedArraySource, cultureName, "ScalarHost.g.cs", "MixedHost.g.cs", "NestedHost.g.cs", "EmptyHost.g.cs", "PlainHost.g.cs");
	}

	[Theory]
	[InlineData("", "ScalarHost.g.cs", "(object)new int[] { 1, 2 }")]
	[InlineData("tr-TR", "ScalarHost.g.cs", "(object)new int[] { 1, 2 }")]
	[InlineData("az-Latn-AZ", "ScalarHost.g.cs", "(object)new int[] { 1, 2 }")]
	[InlineData("", "MixedHost.g.cs", "new object[] { new int[] { 1, 2 }, \"s\" }")]
	[InlineData("tr-TR", "MixedHost.g.cs", "new object[] { new int[] { 1, 2 }, \"s\" }")]
	[InlineData("az-Latn-AZ", "MixedHost.g.cs", "new object[] { new int[] { 1, 2 }, \"s\" }")]
	[InlineData("", "NestedHost.g.cs", "new object[] { new object[] { \"a\", (int)(1) } }")]
	[InlineData("tr-TR", "NestedHost.g.cs", "new object[] { new object[] { \"a\", (int)(1) } }")]
	[InlineData("az-Latn-AZ", "NestedHost.g.cs", "new object[] { new object[] { \"a\", (int)(1) } }")]
	[InlineData("", "EmptyHost.g.cs", "new object[] { (int[])[] }")]
	[InlineData("tr-TR", "EmptyHost.g.cs", "new object[] { (int[])[] }")]
	[InlineData("az-Latn-AZ", "EmptyHost.g.cs", "new object[] { (int[])[] }")]
	public void GeneratedCode_WritesNestedArraysWithTheirOwnType(string cultureName, string fileName, string expectedText)
	{
		var source = GeneratedCodeCompilation.Run(nestedArraySource, cultureName, TestContext.Current.CancellationToken).GetSource(fileName);

		Assert.Contains(expectedText, source, StringComparison.Ordinal);
	}
}