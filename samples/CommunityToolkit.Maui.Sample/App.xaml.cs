using CommunityToolkit.Maui.Sample.Pages;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new BaseNavigationPage(new MainPage());
	}
}