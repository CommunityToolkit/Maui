using CommunityToolkit.Maui.Sample.Pages;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Sample;

public partial class App : Application
{
	public App(MainGalleryPage mainGalleryPage)
	{
		InitializeComponent();
		
		MainPage = new BaseShell(mainGalleryPage);
	}
}