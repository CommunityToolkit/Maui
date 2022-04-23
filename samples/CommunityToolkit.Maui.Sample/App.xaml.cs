using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		var layout = new Button
		{
			Text = "Navigate",
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
		};

		layout.Clicked += (sender, args) =>
		{
			MainPage!.Navigation.PushAsync(new SelectAllTextPage());
		};

		var page = new ContentPage
		{
			Content = layout,
		};
	

		MainPage = page;
	}
}