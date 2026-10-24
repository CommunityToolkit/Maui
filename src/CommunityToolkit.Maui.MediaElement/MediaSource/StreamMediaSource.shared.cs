namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a source, loaded from a <see cref="System.IO.Stream"/>, that can be played by <see cref="MediaElement"/>.
/// </summary>
public sealed partial class StreamMediaSource : MediaSource
{
	/// <summary>
	/// Backing store for the <see cref="Stream"/> property.
	/// </summary>
	public static readonly BindableProperty StreamProperty
		= BindableProperty.Create(nameof(Stream), typeof(Stream), typeof(StreamMediaSource), propertyChanged: OnStreamMediaSourceChanged);

	/// <summary>
	/// Gets or sets the stream to use as a media source.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>
	/// <para>
	/// On iOS, Mac Catalyst and Windows the stream must support seeking (<see cref="System.IO.Stream.CanSeek"/>): a stream that cannot seek is not
	/// supported there, and the media fails to open (<see cref="MediaElement.MediaFailed"/>).
	/// </para>
	/// <para>
	/// On Android a stream that cannot seek is read once, from its start: the media cannot be sought outside of what the player holds, looped or
	/// played again after <see cref="MediaElement.Stop"/>, and a seek that needs another part of the stream raises
	/// <see cref="MediaElement.MediaFailed"/>.
	/// </para>
	/// <para>
	/// Not supported on Tizen: setting the source throws <see cref="NotSupportedException"/>.
	/// </para>
	/// </remarks>
	public Stream? Stream
	{
		get => (Stream?)GetValue(StreamProperty);
		set => SetValue(StreamProperty, value);
	}

	/// <summary>
	/// An implicit operator to convert a <see cref="System.IO.Stream"/> value into a <see cref="StreamMediaSource"/>.
	/// </summary>
	/// <param name="stream">The stream to use as a media source.</param>
	public static implicit operator StreamMediaSource?(Stream? stream) => stream is not null ? FromStream(stream) : null;

	/// <summary>
	/// An implicit operator to convert a <see cref="StreamMediaSource"/> into a <see cref="System.IO.Stream"/> value.
	/// </summary>
	/// <param name="streamMediaSource">A <see cref="StreamMediaSource"/> instance to convert to a <see cref="System.IO.Stream"/> value.</param>
	public static implicit operator Stream?(StreamMediaSource? streamMediaSource) => streamMediaSource?.Stream;


	/// <inheritdoc/>
	public override string ToString() => $"Stream: {Stream?.GetType().Name ?? "null"}";

	static void OnStreamMediaSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
		((StreamMediaSource)bindable).OnSourceChanged();
}