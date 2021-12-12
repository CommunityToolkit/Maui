using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Hosting;

namespace CommunityToolkit.Maui;

/// <summary>
/// Extensions for MauiAppBuilder
/// </summary>
public static class AppBuilderExtensions
{
	/// <summary>
	/// Initializes the .NET Maui Community Toolkit Library
	/// </summary>
	/// <param name="builder">MauiAppBuilder</param>
	/// <returns></returns>
	public static MauiAppBuilder UseMauiCommunityToolkit(this MauiAppBuilder builder)
	{
		return builder.UseMauiCommunityToolkitCore();
	}
}
