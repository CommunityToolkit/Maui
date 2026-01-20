using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Construction options for MediaElement, for example, to create an Android SurfaceView or TextureView
/// </summary>
public class MediaElementOptions
{
	readonly MauiAppBuilder? builder;

	internal MediaElementOptions()
	{

	}

	internal MediaElementOptions(in MauiAppBuilder builder) : this()
	{
		this.builder = builder;
	}

	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	internal static AndroidViewType DefaultAndroidViewType { get; private set; } = AndroidViewType.SurfaceView;

	/// <summary>
	/// Set whether Android Foreground Service is enabled for MediaElement
	/// </summary>
	internal static bool IsAndroidForegroundServiceEnabled { get; private set; }

	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	public void SetDefaultAndroidViewType(AndroidViewType androidViewType) => DefaultAndroidViewType = androidViewType;

	/// <summary>
	/// Enable Android Foreground Service for MediaElement
	/// </summary>
	/// <param name="enabled">True to enable foreground service, false to disable</param>
	internal void EnableAndroidForegroundService(bool enabled) => IsAndroidForegroundServiceEnabled = enabled;
}