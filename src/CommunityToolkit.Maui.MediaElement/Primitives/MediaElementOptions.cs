namespace CommunityToolkit.Maui;

/// <summary>
/// Construction options for MediaElement, for example, to create an Android SurfaceView or TextureView
/// </summary>
public class MediaElementOptions
{
	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	public AndroidViewType AndroidViewType { get; set; } = AndroidViewType.SurfaceView;
}