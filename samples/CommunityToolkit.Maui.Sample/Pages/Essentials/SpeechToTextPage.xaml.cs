using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class SpeechToTextPage : BasePage<SpeechToTextViewModel>
{
	public SpeechToTextPage(SpeechToTextViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		await BindingContext.SetLocalesCommand.ExecuteAsync(null);
	}
}

class PickerLocaleDisplayConverter : BaseConverterOneWay<Locale, string>
{
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	public override string ConvertFrom(Locale value, CultureInfo? culture)
	{
		return $"{value.Language} {value.Name}";
	}
}