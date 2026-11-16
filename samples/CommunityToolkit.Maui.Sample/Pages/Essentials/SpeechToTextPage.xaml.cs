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

		await BindingContext.SetLocalesCommand.ExecuteAsync(CancellationToken.None);
	}

	// When another flyout item is selected (NavigationType.Replace), Shell keeps this page in its section and raises OnNavigatedFrom and OnNavigatedTo for it,
	// but not OnDisappearing and OnAppearing
	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		BindingContext.SubscribeToSpeechToText();
	}

	protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		base.OnNavigatedFrom(args);

		await BindingContext.StopListeningAndUnsubscribeFromSpeechToText();
	}
}

[AcceptEmptyServiceProvider]
partial class PickerLocaleDisplayConverter : BaseConverterOneWay<Locale, string>
{
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	public override string ConvertFrom(Locale value, CultureInfo? culture)
	{
		return $"{value.Language} {value.Name}";
	}
}