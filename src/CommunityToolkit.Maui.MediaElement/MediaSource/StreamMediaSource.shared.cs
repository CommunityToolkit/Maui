using Microsoft.Maui.Controls;

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
	/// An implicit operator to convert a <see cref="System.IO.Stream"/> value into a <see cref="StreamMediaSource"/>.
	/// </summary>
	/// <param name="stream">The stream to use as a media source.</param>
	public static implicit operator StreamMediaSource?(Stream? stream) => stream is not null ? FromStream(stream) : null;

	/// <summary>
	/// An implicit operator to convert a <see cref="StreamMediaSource"/> into a <see cref="System.IO.Stream"/> value.
	/// </summary>
	/// <param name="streamMediaSource">A <see cref="StreamMediaSource"/> instance to convert to a <see cref="System.IO.Stream"/> value.</param>
	public static implicit operator Stream?(StreamMediaSource? streamMediaSource) => streamMediaSource?.Stream;

	/// <summary>
	/// Gets or sets the stream to use as a media source.
	/// This is a bindable property.
	/// </summary>
	public Stream? Stream
	{
		get => (Stream?)GetValue(StreamProperty);
		set => SetValue(StreamProperty, value);
	}

	/// <inheritdoc/>
	public override string ToString() => $"Stream: {Stream?.GetType().Name ?? "null"}";

	static void OnStreamMediaSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
		((StreamMediaSource)bindable).OnSourceChanged();
}
