using CommunityToolkit.Maui.Sample.Pages.Views;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		var page = new SemanticOrderViewPage(new ViewModels.Views.SemanticOrderViewViewModel());

		var btn = new Button { Text = "Click" , VerticalOptions= LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };

		MainPage = new NavigationPage(new ContentPage
		{
			Content = btn
		});

		btn.Clicked += (_, __) =>
		{
			MainPage.Navigation.PushAsync(page);
		};
	}
}