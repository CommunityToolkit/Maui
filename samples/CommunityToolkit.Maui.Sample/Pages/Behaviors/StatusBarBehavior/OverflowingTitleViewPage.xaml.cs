namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class OverflowingTitleViewPage : ContentPage
{
	public OverflowingTitleViewPage()
	{
		InitializeComponent();
	}

	async void OnNavigateToMainPageButtonClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//MainPage");
	}
}