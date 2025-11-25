using System.ComponentModel;
using CommunityToolkit.Maui.Converters;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a source that can be played by <see cref="MediaElement"/>.
/// </summary>
[TypeConverter(typeof(MediaSourceConverter))]
public abstract class MediaSource : Element
{
	static readonly bool isAndroid = OperatingSystem.IsAndroid();
	readonly WeakEventManager weakEventManager = new();

	internal event EventHandler SourceChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// An implicit operator to convert a string value into a <see cref="MediaSource"/>.
	/// </summary>
	/// <param name="source">Full path to a local file (starting with <c>file://</c>) or an absolute URI.</param>
	public static implicit operator MediaSource?(string? source) =>
		Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme != "file"
			? FromUri(uri)
			: FromFile(source);

	/// <summary>
	/// An implicit operator to convert a <see cref="Uri"/> object into a <see cref="UriMediaSource"/>.
	/// </summary>
	/// <param name="uri">Absolute URI to load.</param>
	public static implicit operator MediaSource?(Uri? uri) => FromUri(uri);

	/// <summary>
	/// Creates a <see cref="ResourceMediaSource"/> from an absolute URI.
	/// </summary>
	/// <param name="path">Full path to the resource file, relative to the application's resources folder.</param>
	/// <returns>A <see cref="ResourceMediaSource"/> instance.</returns>
	public static MediaSource FromResource(string? path) => new ResourceMediaSource { Path = isAndroid ? $"Assets/{path}" : path };

	/// <summary>
	/// Creates a <see cref="UriMediaSource"/> from a string that contains an absolute URI.
	/// </summary>
	/// <param name="uri">String representation or an absolute URI to load.</param>
	/// <returns>A <see cref="UriMediaSource"/> instance.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="uri"/> is not an absolute URI.</exception>
	public static MediaSource? FromUri(string uri) => FromUri(new Uri(uri));

	/// <summary>
	/// Creates a <see cref="FileMediaSource"/> from a local path.
	/// </summary>
	/// <param name="path">Full path to the file to load.</param>
	/// <returns>A <see cref="FileMediaSource"/> instance.</returns>
	public static MediaSource FromFile(string? path) => new FileMediaSource { Path = path };

	/// <summary>
	/// Creates a <see cref="StreamMediaSource"/> from a <see cref="Stream"/>.
	/// </summary>
	/// <param name="stream">The stream to use as a media source.</param>
	/// <returns>A <see cref="StreamMediaSource"/> instance.</returns>
	public static StreamMediaSource FromStream(Stream stream) => new StreamMediaSource { Stream = stream };

	/// <summary>
	/// Creates a <see cref="UriMediaSource"/> from an absolute URI.
	/// </summary>
	/// <param name="uri">Absolute URI to load.</param>
	/// <returns>A <see cref="UriMediaSource"/> instance.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="uri"/> is not an absolute URI.</exception>
	public static MediaSource? FromUri(Uri? uri)
	{
		if (uri is null)
		{
			return null;
		}

		if (!uri.IsAbsoluteUri)
		{
			throw new ArgumentException("Uri must be absolute", nameof(uri));
		}

		return new UriMediaSource { Uri = uri };
	}

	/// <summary>
	/// Triggers the <see cref="SourceChanged"/> event.
	/// </summary>
	protected void OnSourceChanged() => weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(SourceChanged));
}