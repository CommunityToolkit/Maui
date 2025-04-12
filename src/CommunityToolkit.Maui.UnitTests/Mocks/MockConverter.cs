using System.Globalization;
using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockConverter(string[] quotes) : BaseConverter<int, string>
{
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	public override int DefaultConvertBackReturnValue { get; set; }

	public override string ConvertFrom(int value, CultureInfo? culture)
	{
		return value < quotes.Length ? quotes[value] : DefaultConvertReturnValue;
	}

	public override int ConvertBackTo(string value, CultureInfo? culture)
	{
		var matchingQuoteIndex = quotes.IndexOf(value);

		return matchingQuoteIndex == -1 ? DefaultConvertBackReturnValue : matchingQuoteIndex;
	}
}