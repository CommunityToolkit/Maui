using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

class MockValueConverter : IValueConverter
{
	readonly Func<object, object> returnValue;

	public MockValueConverter(Func<object, object> returnValue)
	{
		this.returnValue = returnValue;
	}

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return returnValue.Invoke(value);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return returnValue.Invoke(value);
	}
}