using System.Globalization;
using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockOneWayConverter(string[] quotes) : BaseConverterOneWay<int, string>
{
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	public override string ConvertFrom(int value, CultureInfo? culture)
	{
		return value < quotes.Length ? quotes[value] : DefaultConvertReturnValue;
	}
}