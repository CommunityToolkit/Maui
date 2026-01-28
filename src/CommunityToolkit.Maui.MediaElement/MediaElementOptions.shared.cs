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
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	public void SetDefaultAndroidViewType(AndroidViewType androidViewType) => DefaultAndroidViewType = androidViewType;

	/// <summary>
	/// Set Android Foreground Service for MediaElement on construction
	/// </summary>
	/// <remarks>When <c>true</c>, the following permissions are automatically added to the Android Manifest by CommunityToolkit.Maui.MediaElement: FOREGROUND_SERVICE, MEDIA_CONTENT_CONTROL, POST_NOTIFICATION, FOREGROUND_SERVICE_MEDIA_PLAYBACK</remarks>
	/// <param name="isEnabled"></param>
	public void SetIsAndroidForegroundServiceEnabled(bool isEnabled) => IsAndroidForegroundServiceEnabled = isEnabled;
}