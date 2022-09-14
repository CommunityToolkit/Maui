using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IndexToArrayItemConverterTests : BaseConverterTest<IndexToArrayItemConverter>
{
	[Theory]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 2, 3)]
	[InlineData(new[] { "Val1", "Val2", "Val3" }, 0, "Val1")]
	[InlineData(new[] { 1.3, 4.3, 4.3 }, 1, 4.3)]
	public void IndexToArrayConverter(Array value, int position, object expectedResult)
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();

		var convertResult = ((ICommunityToolkitValueConverter)indexToArrayConverter).Convert(position, typeof(object), value, CultureInfo.CurrentCulture);
		var convertFromResult = indexToArrayConverter.ConvertFrom(position, value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void IndexToArrayNullValuesThrowArgumentNullException()
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)indexToArrayConverter).Convert(null, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 100)]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, -1)]
	public void IndexToArrayInvalidValuesThrowArgumentOutOfRangeException(int[] value, int position)
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();
		Assert.Throws<ArgumentOutOfRangeException>(() => ((ICommunityToolkitValueConverter)indexToArrayConverter).Convert(position, typeof(object), value, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentOutOfRangeException>(() => indexToArrayConverter.ConvertFrom(position, value));
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 2, 3)]
	[InlineData(new[] { "Val1", "Val2", "Val3" }, 0, "Val1")]
	[InlineData(new[] { 1.3, 4.3, 4.3 }, 1, 4.3)]
	public void IndexToArrayConverterConvertBack(Array value, int position, object expectedResult)
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();

		var convertResult = ((ICommunityToolkitValueConverter)indexToArrayConverter).ConvertBack(expectedResult, typeof(object), value, CultureInfo.CurrentCulture);
		var convertFromResult = indexToArrayConverter.ConvertBackTo(expectedResult, value);

		Assert.Equal(position, convertResult);
		Assert.Equal(position, convertFromResult);
	}

	[Fact]
	public void ConvertBackIndexToArrayNullValuesThrowArgumentNullException()
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)indexToArrayConverter).ConvertBack(null, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, 100)]
	[InlineData(new[] { 1, 2, 3, 4, 5 }, -1)]
	public void ConvertBackIndexToArrayInvalidValuesThrowArgumentOutOfRangeException(int[] value, int position)
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)indexToArrayConverter).ConvertBack(position, typeof(object), value, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentException>(() => indexToArrayConverter.ConvertBackTo(position, value));
	}

	[Fact]
	public void IndexToArrayItemConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new IndexToArrayItemConverter().ConvertFrom(1, null));
		Assert.Throws<ArgumentNullException>(() => new IndexToArrayItemConverter().ConvertBackTo(1, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IndexToArrayItemConverter()).Convert(1, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IndexToArrayItemConverter()).Convert(null, typeof(object), Array.Empty<string>(), null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IndexToArrayItemConverter()).ConvertBack(new object(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}