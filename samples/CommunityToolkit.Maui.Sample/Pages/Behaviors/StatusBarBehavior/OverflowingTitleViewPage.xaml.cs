namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class OverflowingTitleViewPage : ContentPage
{
	public OverflowingTitleViewPage()
	{
		InitializeComponent();
	}

	void OnNavigateToMainPageButtonClicked(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(async () => { await Shell.Current.GoToAsync("//MainPage"); });
	}
}