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

		var result = indexToArrayConverter.Convert(position, typeof(Array), value, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(null, 100)]
	public void IndexToArrayInValidValuesThrowArgumenException(object value, object position)
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();
		Assert.Throws<ArgumentException>(() => indexToArrayConverter.Convert(position, typeof(Array), value, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(new int[] { 1, 2, 3, 4, 5 }, 100)]
	[InlineData(new int[] { 1, 2, 3, 4, 5 }, -1)]
	public void IndexToArrayInValidValuesThrowArgumenOutOfRangeException(object value, object position)
	{
		var indexToArrayConverter = new IndexToArrayItemConverter();
		Assert.Throws<ArgumentOutOfRangeException>(() => indexToArrayConverter.Convert(position, typeof(Array), value, CultureInfo.CurrentCulture));
	}
}