using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Construction options for MediaElement, for example to create an Android SurfaceView or TextureView
/// </summary>
public class MediaElementOptions 
{
	/// <summary>
	/// Set Android View type for MediaElement as SurfaceView or TextureView on construction
	/// </summary>
	public AndroidViewType AndroidViewType = AndroidViewType.SurfaceView;
}

/// <summary>
/// Enum for Android view type, whether to make MediaElement as SurfaceView or TextureView
/// </summary>
public enum AndroidViewType {
	/// <summary>
	/// Create Android MediaElement as SurfaceView
	/// </summary>
	SurfaceView,
	/// <summary>
	/// Create Android MediaElement as TextureView
	/// </summary>
	TextureView
}

