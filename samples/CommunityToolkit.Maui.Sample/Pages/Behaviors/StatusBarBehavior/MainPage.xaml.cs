namespace CommunityToolkit.Maui.Sample.Pages.Behaviors.StatusBarBehavior;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	void OnCounterClicked(object? sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(async () => { await Shell.Current.GoToAsync("//MainPage"); });
	}
}