using System.ComponentModel;
using Microsoft.Maui.Controls;
using UriTypeConverter = Microsoft.Maui.Controls.UriTypeConverter;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a source, loaded from a remote URI, that can be played by <see cref="MediaElement"/>.
/// </summary>
public sealed partial class UriMediaSource : MediaSource
{
	/// <summary>
	/// An implicit operator to convert a string value into a <see cref="UriMediaSource"/>.
	/// </summary>
	/// <param name="uri">Full path to the resource file, relative to the application's resources folder.</param>
	public static implicit operator UriMediaSource?(string uri) => (UriMediaSource?)FromUri(uri);

	/// <summary>
	/// An implicit operator to convert a <see cref="UriMediaSource"/> into a string value.
	/// </summary>
	/// <param name="uriMediaSource">A <see cref="UriMediaSource"/> instance to convert to a string value.</param>
	public static implicit operator string?(UriMediaSource? uriMediaSource) => uriMediaSource?.Uri?.ToString();

	/// <summary>
	/// Gets or sets the URI to use as a media source.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>The URI has to be absolute.</remarks>
	[TypeConverter(typeof(UriTypeConverter))]
	[BindableProperty(PropertyChangedMethodName = nameof(OnUriSourceChanged), ValidateValueMethodName = nameof(UriValueValidator))]
	public partial Uri? Uri { get; set; }

	/// <inheritdoc/>
	public override string ToString() => $"Uri: {Uri}";

	static bool UriValueValidator(BindableObject bindable, object value) =>
		value is null || ((Uri)value).IsAbsoluteUri;

	static void OnUriSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
		((UriMediaSource)bindable).OnSourceChanged();
}