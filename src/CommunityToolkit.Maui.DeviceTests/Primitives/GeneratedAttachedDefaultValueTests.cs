namespace CommunityToolkit.Maui.DeviceTests;

// A default such as (byte)1 used to be boxed as an int by the generated code. Reading the defaults at run time covers the packaged
// generator's output in the trimmed app and under NativeAOT.
[Category(TestCategory.BindablePropertyGenerators)]
public sealed class GeneratedAttachedDefaultValueTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public void BoxedDefaultKeepsTheTypeOfEachElement()
	{
		var host = new AttachedDefaultsHost();

		var defaultValue = Assert.IsType<object[]>(AttachedDefaultsHost.BoxedProperty.DefaultValue, exactMatch: false);
		var elementTypes = defaultValue.Select(element => element.GetType()).ToArray();

		Assert.Equal(
			[typeof(byte), typeof(short), typeof(double), typeof(sbyte), typeof(char), typeof(float), typeof(long), typeof(uint), typeof(AttachedDefaultSign)],
			elementTypes);
		Assert.Equal(defaultValue, AttachedDefaultsHost.GetBoxed(host));
	}

	[Fact]
	public void ScalarObjectDefaultIsAByte()
	{
		var host = new AttachedDefaultsHost();

		Assert.IsType<byte>(AttachedDefaultsHost.ScalarByteProperty.DefaultValue);
		Assert.Equal((byte)1, AttachedDefaultsHost.GetScalarByte(host));
	}
}

public enum AttachedDefaultSign
{
	Neg = -1,
	Pos = 1
}

[AttachedBindableProperty<object[]>("Boxed", DefaultValue = new object[] { (byte)1, (short)2, 3.0, (sbyte)-5, 'c', 4.5f, 7L, 8u, AttachedDefaultSign.Pos })]
[AttachedBindableProperty<object>("ScalarByte", DefaultValue = (byte)1)]
public partial class AttachedDefaultsHost : View
{
}