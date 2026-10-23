using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
#if WINDOWS
using Windows.Media.Playback;
#endif

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Create a http-link list.
/// </summary>
public sealed partial class HttpListMediaSource : MediaSource
{
	/// <summary>
	/// Gets the HTTP headers to include in the request when loading the media from <see cref="Uri"/>.
	/// It will cover the headers of the items' of the Sources.
	/// </summary>
	/// <remarks>
	/// Use this to provide authentication tokens (e.g. <c>Authorization: Bearer &lt;token&gt;</c>) or other custom HTTP headers.
	/// Mutating the contents of the returned dictionary triggers a source update on the underlying platform player.
	/// Not supported on Tizen.
	/// </remarks>
	public IDictionary<string, string>? DefaultHttpHeaders { get; set; }

	/// <summary>
	/// Http sources.
	/// </summary>
	public IList<MetaMediaSource>? Sources { get; set; }

	/// <summary>
	/// Index
	/// </summary>
	public int Index { get; set; } = 0;

#if WINDOWS
	/// <summary>
	/// List.
	/// </summary>
	public MediaPlaybackList? MediaPlaybackList { get; set; }
#endif
	/// <summary>
	/// Init.
	/// </summary>
	public HttpListMediaSource()
	{

	}
}

