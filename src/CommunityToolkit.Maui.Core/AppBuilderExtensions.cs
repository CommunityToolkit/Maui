using System;
using Microsoft.Maui.Hosting;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="MauiAppBuilder"/> Extensions
/// </summary>
public static class AppBuilderExtensions
{
	/// <summary>
	/// Initializes the .NET MAUI Community Toolkit Core Library
	/// </summary>
	/// <param name="builder"></param>
	/// <returns><see cref="MauiAppBuilder"/> initialized for <see cref="CommunityToolkit.Maui.Core"/></returns>
	public static MauiAppBuilder UseMauiCommunityToolkitCore(this MauiAppBuilder builder)
	{
		return builder;
	}
}