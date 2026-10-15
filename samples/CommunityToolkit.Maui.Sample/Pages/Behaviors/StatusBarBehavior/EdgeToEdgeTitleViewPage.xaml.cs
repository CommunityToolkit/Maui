using System;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EdgeToEdgeTitleViewPage : ContentPage
{
	const string statusColorBlue = "1976D2";
	const string statusColorRed = "C0392B";

	public EdgeToEdgeTitleViewPage()
	{
		InitializeComponent();
		Core.Platform.StatusBar.SetColor(Color.FromHex(statusColorBlue));
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		Core.Platform.StatusBar.SetColor(Color.FromHex(StatusColorSwitch.IsToggled ? statusColorRed : statusColorBlue));
	}

	void OnStatusColorSwitchToggled(object? sender, ToggledEventArgs e)
	{
		Core.Platform.StatusBar.SetColor(Color.FromHex(e.Value ? statusColorRed : statusColorBlue));
	}

	async void OnBackButtonClicked(object? sender, EventArgs e)
	{
		await Shell.Current.Navigation.PopModalAsync();
	}
}
