using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

sealed class CultureSpyConverter : IValueConverter
{
	public CultureInfo? ReceivedCulture { get; private set; }

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		ReceivedCulture = culture;
		return new object();
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}
