using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UriTypeConverter = Microsoft.Maui.Controls.UriTypeConverter;


namespace CommunityToolkit.Maui.Views;

/// <summary>
/// MetaMediaSource
/// </summary>
public sealed partial class MetaMediaSource : MediaSource
{
	/// <summary>
	/// Gets the HTTP headers to include in the request when loading the media from <see cref="Uri"/>.
	/// </summary>
	/// <remarks>
	/// Use this to provide authentication tokens (e.g. <c>Authorization: Bearer &lt;token&gt;</c>) or other custom HTTP headers.
	/// Mutating the contents of the returned dictionary triggers a source update on the underlying platform player.
	/// Not supported on Tizen.
	/// </remarks>
	public IDictionary<string, string>? HttpHeaders { get; set; }

	/// <summary>
	/// Bindable property for the <see cref="Uri"/> property.
	/// </summary>
	public static readonly BindableProperty UriProperty =
		BindableProperty.Create(nameof(Uri), typeof(Uri), typeof(MetaMediaSource));

	/// <summary>
	/// Gets or sets the URI to use as a media source.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>The URI has to be absolute.</remarks>
	[TypeConverter(typeof(UriTypeConverter))]
	public Uri? Uri
	{
		get => (Uri?)GetValue(UriProperty);
		set => SetValue(UriProperty, value);
	}

	/// <summary>
	/// Title to display.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	/// Artist's name to display.
	/// </summary>
	public string? Artist { get; set; }

	/// <summary>
	/// Artwork-picture to display using Url.
	/// </summary>
	public Uri? ArtworkUrl { get; set; }


	/// <summary>
	/// Init.
	/// </summary>
	public MetaMediaSource() { }
}
