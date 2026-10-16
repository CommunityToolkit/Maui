namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EdgeToEdgeTitleViewPage : ContentPage
{
	public EdgeToEdgeTitleViewPage()
	{
		InitializeComponent();
	}

	void OnCloseButtonClicked(object? sender, EventArgs e)
	{
#if ANDROID
		Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Finish();
#endif
	}
}