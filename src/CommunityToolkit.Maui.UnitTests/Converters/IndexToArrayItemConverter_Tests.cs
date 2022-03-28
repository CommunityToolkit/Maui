using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IndexToArrayItemConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(new int[] { 1, 2, 3, 4, 5 }, 2, 3)]
	[InlineData(new string[] { "Val1", "Val2", "Val3" }, 0, "Val1")]
	[InlineData(new double[] { 1.3, 4.3, 4.3 }, 1, 4.3)]
	public void IndexToArrayConverter(Array value, int position, object expectedResult)
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();

		var convertResult = ((ICommunityToolkitValueConverter)indexToArrayConverter).Convert(position, typeof(object), value, CultureInfo.CurrentCulture);
		var convertFromResult = indexToArrayConverter.ConvertFrom(position, typeof(object), value, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(null, 100)]
	public void IndexToArrayInvalidValuesThrowArgumentException(object? value, object? position)
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)indexToArrayConverter).Convert(position, typeof(object), value, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void IndexToArrayNullValuesThrowArgumentNullException()
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)indexToArrayConverter).Convert(null, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(new int[] { 1, 2, 3, 4, 5 }, 100)]
	[InlineData(new int[] { 1, 2, 3, 4, 5 }, -1)]
	public void IndexToArrayInvalidValuesThrowArgumenOutOfRangeException(int[] value, int position)
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();
		Assert.Throws<ArgumentOutOfRangeException>(() => ((ICommunityToolkitValueConverter)indexToArrayConverter).Convert(position, typeof(object), value, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentOutOfRangeException>(() => indexToArrayConverter.ConvertFrom(position, typeof(object), value, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void IndexToArrayItemConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IndexToArrayItemConverter()).Convert(1, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IndexToArrayItemConverter()).Convert(null, typeof(object), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IndexToArrayItemConverter()).ConvertBack(new object(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}