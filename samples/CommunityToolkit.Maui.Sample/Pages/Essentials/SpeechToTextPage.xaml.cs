using System.Globalization;
using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class SpeechToTextPage : BasePage<SpeechToTextViewModel>
{
	public SpeechToTextPage(SpeechToTextViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}

public class PickerDisplayConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Locale locale)
		{
			return $"{locale.Language} {locale.Name}";
		}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}