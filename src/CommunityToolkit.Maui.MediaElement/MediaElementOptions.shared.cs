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
	/// Set Android Foreground Service for MediaElement on construction
	/// </summary>
	internal static bool IsAndroidForegroundServiceEnabled { get; private set; } = true;

	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	internal static AndroidViewType DefaultAndroidViewType { get; private set; } = AndroidViewType.SurfaceView;

	/// <summary>
	/// Set Android Foreground Service for MediaElement on construction
	/// </summary>
	/// <param name="isEnabled"></param>
	public void SetDefaultAndroidForegroundServiceEnabled(bool isEnabled) => IsAndroidForegroundServiceEnabled = isEnabled;

	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	public void SetDefaultAndroidViewType(AndroidViewType androidViewType) => DefaultAndroidViewType = androidViewType;
}