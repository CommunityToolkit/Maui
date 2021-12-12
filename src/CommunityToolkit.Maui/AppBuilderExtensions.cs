using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Hosting;

namespace CommunityToolkit.Maui;

public static class AppBuilderExtensions
{
	public static MauiAppBuilder UseMauiCommunityToolkit(this MauiAppBuilder builder)
	{
		return builder.UseMauiCommunityToolkitCore();
	}
}
