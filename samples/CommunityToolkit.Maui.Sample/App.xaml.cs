using CommunityToolkit.Maui.Sample.Pages;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		var mainGalleryPage = ServiceProvider.GetRequiredService<MainGalleryPage>();
		MainPage = new BaseNavigationPage(mainGalleryPage);
	}
}