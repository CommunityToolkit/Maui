namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EdgeToEdgeModalPage : ContentPage
{
	public EdgeToEdgeModalPage(bool isStatusBarSolidRed)
	{
		InitializeComponent();

		// Subscribed after the initial state is set, so that opening this page never sets the status bar color: it must come from the page that opened it.
		SolidStatusBarSwitch.IsToggled = isStatusBarSolidRed;
		SolidStatusBarSwitch.Toggled += OnSolidStatusBarSwitchToggled;
	}

	void OnSolidStatusBarSwitchToggled(object? sender, ToggledEventArgs e)
	{
#if ANDROID
		Core.Platform.StatusBar.SetColor(e.Value ? Colors.Red : Colors.Transparent);
		Core.Platform.StatusBar.SetStyle(e.Value ? Core.StatusBarStyle.LightContent : Core.StatusBarStyle.DarkContent);
#endif
	}

	async void OnBackButtonClicked(object? sender, EventArgs e)
	{
		await Navigation.PopModalAsync();
	}
}