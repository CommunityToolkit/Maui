using System;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EdgeToEdgeTitleViewPage : ContentPage
{
	const string statusColorBlue = "1976D2";
	const string statusColorRed = "C0392B";

	// A Shell must be a window root and cannot be pushed onto navigation, so the previous page is saved
	// here and restored by OnBackButtonClicked.
	public static Page? PreviousPage { get; set; }

	public EdgeToEdgeTitleViewPage()
	{
		InitializeComponent();
		Core.Platform.StatusBar.SetColor(Color.FromHex(statusColorBlue));
	}

	void OnStatusColorSwitchToggled(object? sender, ToggledEventArgs e)
	{
		Core.Platform.StatusBar.SetColor(Color.FromHex(e.Value ? statusColorRed : statusColorBlue));
	}

	void OnBackButtonClicked(object? sender, EventArgs e)
	{
		if (Application.Current?.Windows.Count > 0 && PreviousPage is not null)
		{
			Application.Current.Windows[0].Page = PreviousPage;
		}
	}
}
