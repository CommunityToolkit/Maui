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
		Assert.Contains("newobject?[]{typeof(string),(int)(1),null}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newint[]{1,2,3}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newstring?[]{\"a\",null}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newglobal::System.Type[]{typeof(string),typeof(int)}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newglobal::ArrayDefaults.Flavor[]{(global::ArrayDefaults.Flavor)(1),(global::ArrayDefaults.Flavor)(0)}", sourceWithoutSpaces, StringComparison.Ordinal);
	}

	const string ownTypeDefaultsSource =
		/* language=C#-test */
		//lang=csharp
		"""
		using CommunityToolkit.Maui;
		using Microsoft.Maui.Controls;

		namespace OwnTypeDefaults;

		public enum Signed
		{
			Neg = -1,
			Pos = 1
		}

		[AttachedBindableProperty<object[]>("Boxed", DefaultValue = new object[] { (byte)1, (short)2, 3.0, (sbyte)-5, (ushort)6, 'c', 4.5f, 7L, 8u, Signed.Pos })]
		[AttachedBindableProperty<object>("ScalarByte", DefaultValue = (byte)1)]
		[AttachedBindableProperty<object>("ScalarDouble", DefaultValue = 2.0)]
		[AttachedBindableProperty<object>("ScalarEnum", DefaultValue = Signed.Neg)]
		[AttachedBindableProperty<Signed>("NegativeEnum", DefaultValue = Signed.Neg)]
		[AttachedBindableProperty<Signed[]>("NegativeEnums", DefaultValue = new[] { Signed.Neg, Signed.Pos })]
		[AttachedBindableProperty<double>("Plain", DefaultValue = 1.5)]
		public partial class OwnTypeHost : View
		{
		}
		""";

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CompilesWithoutDiagnostics_ForDefaultsThatNeedTheirOwnType(string cultureName)
	{
		GeneratedCodeCompilation.AssertCompilesCleanly(ownTypeDefaultsSource, cultureName, "OwnTypeHost");
	}

	[Theory]
	[InlineData("")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void GeneratedCode_CastsDefaultsToTheirOwnType_BeforeBoxingThem(string cultureName)
	{
		var source = GeneratedCodeCompilation.Run(ownTypeDefaultsSource, cultureName, TestContext.Current.CancellationToken).GetSource("OwnTypeHost.g.cs");

		var sourceWithoutSpaces = source.Replace(" ", string.Empty, StringComparison.Ordinal);
		Assert.Contains("newobject[]{(byte)(1),(short)(2),(double)(3),(sbyte)(-5),(ushort)(6),(char)('c'),(float)(4.5f),(long)(7L),(uint)(8U),(global::OwnTypeDefaults.Signed)(1)}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("(object)(byte)(1)", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("(object)(double)(2)", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("(object)(global::OwnTypeDefaults.Signed)(-1)", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("(global::OwnTypeDefaults.Signed)(-1)", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("newglobal::OwnTypeDefaults.Signed[]{(global::OwnTypeDefaults.Signed)(-1),(global::OwnTypeDefaults.Signed)(1)}", sourceWithoutSpaces, StringComparison.Ordinal);
		Assert.Contains("(double)1.5", sourceWithoutSpaces, StringComparison.Ordinal);
	}
}