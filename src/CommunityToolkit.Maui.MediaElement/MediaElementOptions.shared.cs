namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Construction options for MediaElement, for example, to create an Android SurfaceView or TextureView
/// </summary>
public class MediaElementOptions()
{
	readonly MauiAppBuilder? builder;

	internal MediaElementOptions(in MauiAppBuilder builder) : this()
	{
		this.builder = builder;
	}

	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	internal static AndroidViewType DefaultAndroidViewType { get; private set; } = AndroidViewType.SurfaceView;

	/// <summary>
	/// Set Android Foreground Service for MediaElement on construction
	/// </summary>
	internal static bool AndroidForegroundServiceEnabled { get; private set; } = false;

	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	public void SetDefaultAndroidViewType(AndroidViewType androidViewType) => DefaultAndroidViewType = androidViewType;

	/// <summary>
	/// Set Android Foreground Service for MediaElement on construction
	/// </summary>
	/// <param name="androidForegroundServiceEnabled"></param>
	public void SetDefaultAndroidForegroundService(bool androidForegroundServiceEnabled) => AndroidForegroundServiceEnabled = androidForegroundServiceEnabled;
}