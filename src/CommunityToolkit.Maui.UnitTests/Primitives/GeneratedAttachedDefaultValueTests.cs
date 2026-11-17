using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Primitives;

public class GeneratedAttachedDefaultValueTests : BaseTest
{
	[Fact]
	public void BoxedDefault_KeepsTheTypeOfEachElement()
	{
		var host = new AttachedDefaultsHost();

		var defaultValue = Assert.IsType<object[]>(AttachedDefaultsHost.BoxedProperty.DefaultValue, exactMatch: false);
		var elementTypes = defaultValue.Select(element => element.GetType()).ToArray();

		Assert.Equal(
			[typeof(byte), typeof(short), typeof(double), typeof(sbyte), typeof(char), typeof(float), typeof(long), typeof(uint), typeof(Signed)],
			elementTypes);
		Assert.Equal(defaultValue, AttachedDefaultsHost.GetBoxed(host));
	}

	[Fact]
	public void ScalarObjectDefault_IsAByte()
	{
		var host = new AttachedDefaultsHost();

		Assert.IsType<byte>(AttachedDefaultsHost.ScalarByteProperty.DefaultValue);
		Assert.IsType<byte>(AttachedDefaultsHost.GetScalarByte(host));
		Assert.Equal((byte)1, AttachedDefaultsHost.GetScalarByte(host));
	}

	[Fact]
	public void NegativeEnumDefault_IsNeg()
	{
		var host = new AttachedDefaultsHost();

		Assert.Equal(Signed.Neg, AttachedDefaultsHost.NegativeEnumProperty.DefaultValue);
		Assert.Equal(Signed.Neg, AttachedDefaultsHost.GetNegativeEnum(host));
	}

	[Fact]
	public void NestedArrayDefault_IsAnIntArray()
	{
		var host = new AttachedDefaultsHost();

		var defaultValue = Assert.IsType<object[]>(AttachedDefaultsHost.GetNested(host), exactMatch: false);

		Assert.Equal(2, defaultValue.Length);
		Assert.Equal([1, 2], Assert.IsType<int[]>(defaultValue[0]));
		Assert.Equal("s", defaultValue[1]);
	}
}

public enum Signed
{
	Neg = -1,
	Pos = 1
}

[AttachedBindableProperty<object[]>("Boxed", DefaultValue = new object[] { (byte)1, (short)2, 3.0, (sbyte)-5, 'c', 4.5f, 7L, 8u, Signed.Pos })]
[AttachedBindableProperty<object>("ScalarByte", DefaultValue = (byte)1)]
[AttachedBindableProperty<Signed>("NegativeEnum", DefaultValue = Signed.Neg)]
[AttachedBindableProperty<object[]>("Nested", DefaultValue = new object[] { new int[] { 1, 2 }, "s" })]
public partial class AttachedDefaultsHost : View
{
}