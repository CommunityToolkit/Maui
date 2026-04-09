namespace CommunityToolkit.Maui.Sample.Pages.Behaviors.StatusBarBehavior;

public partial class OverflowingTitleViewPage : ContentPage
{
	int count = 0;

	public OverflowingTitleViewPage()
	{
		InitializeComponent();
	}

	void OnCounterClicked(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(async () => { await Shell.Current.GoToAsync("//MainPage"); });
	}
}