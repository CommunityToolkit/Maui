namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

// This page deliberately has no StatusBarBehavior: a behavior re-applies the window flags once the modal is visible, which would hide a bug in DialogFragmentService.
public partial class EdgeToEdgeModalPage : ContentPage
{
	public EdgeToEdgeModalPage()
	{
		InitializeComponent();
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