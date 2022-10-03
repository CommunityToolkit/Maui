using Microsoft.Maui.Controls.Internals;

namespace CommunityToolkit.Maui.MediaElement;

// TODO
//[TypeConversion(typeof(MediaSourceConverter))]
public abstract class MediaSource : Element
{
	readonly WeakEventManager weakEventManager = new WeakEventManager();

	public static MediaSource FromFile(string? file) =>
		new FileMediaSource { File = file };

	public static MediaSource? FromUri(Uri? uri)
	{
		if (uri == null)
		{ 
			return null;
		}

		return !uri.IsAbsoluteUri ? throw new ArgumentException("Uri must be be absolute", nameof(uri)) : new UriMediaSource { Uri = uri };
	}

	public static MediaSource? FromUri(string uri) => FromUri(new Uri(uri));

	[Preserve(Conditional = true)]
	public static implicit operator MediaSource?(string? source) =>
		Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme != "file"
			? FromUri(uri)
			: FromFile(source);

	[Preserve(Conditional = true)]
	public static implicit operator MediaSource?(Uri? uri) => FromUri(uri);

	protected void OnSourceChanged() =>
		weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(SourceChanged));

	internal event EventHandler SourceChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}
}
