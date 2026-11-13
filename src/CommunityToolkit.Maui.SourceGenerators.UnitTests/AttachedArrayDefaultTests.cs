using Xunit;

namespace CommunityToolkit.Maui.SourceGenerators.UnitTests;

public class AttachedArrayDefaultTests : BaseTest
{
	const string arrayDefaultsSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace ArrayDefaults;

		public enum Flavor
		{
			Sweet,
			Sour
		}

		[AttachedBindableProperty<object[]>("Mixed", DefaultValue = new object[] { typeof(string), 1, null })]
		[AttachedBindableProperty<object[]>("Boxed", DefaultValue = new object[] { "text", 2.5, 'c', true })]
		[AttachedBindableProperty<object[]>("Types", DefaultValue = new object[] { typeof(string), typeof(int) })]
		[AttachedBindableProperty<int[]>("Numbers", DefaultValue = new[] { 1, 2, 3 })]
		[AttachedBindableProperty<string[]>("Names", DefaultValue = new[] { "a", "b" })]
		[AttachedBindableProperty<string[]>("NullableNames", DefaultValue = new[] { "a", null })]
		[AttachedBindableProperty<Flavor[]>("Flavors", DefaultValue = new[] { Flavor.Sour, Flavor.Sweet })]
		[AttachedBindableProperty<System.Type[]>("Kinds", DefaultValue = new[] { typeof(string), typeof(int) })]
		[AttachedBindableProperty<double[]>("Empty", DefaultValue = new double[0])]
		public partial class ArrayHost : View
		{
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForArrayDefaultValues(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(arrayDefaultsSource, cultureName, "ArrayHost");
	}

	[Fact]
	public void GeneratedCode_CreatesTheArrayWithItsElementType_InsteadOfInferringIt()
	{
		var source = GeneratedCodeCompilation.Run(arrayDefaultsSource, "", TestContext.Current.CancellationToken).GetSource("ArrayHost.g.cs");

		var sourceWithoutSpaces = source.Replace(" ", string.Empty, StringComparison.Ordinal);
		Assert.DoesNotContain("new[]", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newobject?[]{typeof(string),1,null}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newint[]{1,2,3}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newstring?[]{\"a\",null}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newglobal::System.Type[]{typeof(string),typeof(int)}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newglobal::ArrayDefaults.Flavor[]{(global::ArrayDefaults.Flavor)1,(global::ArrayDefaults.Flavor)0}", sourceWithoutSpaces, StringComparison.Ordinal);
	}
}