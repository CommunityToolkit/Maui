using CommunityToolkit.Maui;
using CommunityToolkit.Maui;

namespace DivStart;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<Application>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); })
			.UseMauiCommunityToolkitMediaElement(false, null);
		return builder.Build();
	}
}