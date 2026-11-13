using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class OfflineSpeechToTextPage : BasePage<OfflineSpeechToTextViewModel>
{
	public OfflineSpeechToTextPage(OfflineSpeechToTextViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
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