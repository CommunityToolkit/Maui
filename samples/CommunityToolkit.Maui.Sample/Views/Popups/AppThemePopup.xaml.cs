using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class AppThemePopup : Popup
{
	public AppThemePopup()
	{
		InitializeComponent();
	}

	static Application CurrentApplication => Application.Current ?? throw new InvalidOperationException("Application cannot be null");

	void HandleLightThemeClicked(object? sender, EventArgs e) => CurrentApplication.UserAppTheme = AppTheme.Light;

	void HandleDarkThemeClicked(object? sender, EventArgs e) => CurrentApplication.UserAppTheme = AppTheme.Dark;

	void HandleSystemThemeClicked(object? sender, EventArgs e) => CurrentApplication.UserAppTheme = AppTheme.Unspecified;

	async void HandleCloseClicked(object? sender, EventArgs e) => await CloseAsync(CancellationToken.None);
}
