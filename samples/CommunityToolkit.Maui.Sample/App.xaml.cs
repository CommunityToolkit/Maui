using CommunityToolkit.Maui.Views;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		var page = new ContentPage();
		var stack = new VerticalStackLayout
		{
			new MediaElement
			{
				Source = MediaSource.FromUri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"),
				ShouldAutoPlay = true,
				WidthRequest = 400,
				HeightRequest = 300
			}
		};

		page.Content = stack;

		var page1 = new ContentPage();
		var button = new Button();
		button.Text = "Click";
		button.Clicked += async (s, a) =>
		{
			await Application.Current!.MainPage!.Navigation.PushAsync(page);
		};

		page1.Content = button;
		MainPage = new NavigationPage(page1); //new AppShell();
	}
}