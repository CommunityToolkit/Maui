using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ItemSelectedEventArgsConverter_Tests : BaseTest
{
	[Theory]
	[InlineData("Random String")]
	public void InvalidConverterValuesThrowsArgumenException(object value)
	{
		var itemSelectedEventArgsConverter = new ItemSelectedEventArgsConverter();
		Assert.Throws<ArgumentException>(() => itemSelectedEventArgsConverter.Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}
}