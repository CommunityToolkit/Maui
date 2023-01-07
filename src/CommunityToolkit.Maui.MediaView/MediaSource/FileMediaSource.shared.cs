using System.ComponentModel;
using CommunityToolkit.Maui.MediaView.Converters;

namespace CommunityToolkit.Maui.MediaView;

/// <summary>
/// Represents a source, loaded from local filesystem, that can be played by <see cref="MediaView"/>.
/// </summary>
[TypeConverter(typeof(FileMediaSourceConverter))]
public sealed class FileMediaSource : MediaSource
{
	/// <summary>
	/// Backing store for the <see cref="Path"/> property.
	/// </summary>
	public static readonly BindableProperty PathProperty
		= BindableProperty.Create(nameof(Path), typeof(string), typeof(FileMediaSource), propertyChanged: OnFileMediaSourceChanged);

	/// <summary>
	/// Gets or sets the full path to the local file to use as a media source.
	/// This is a bindable property.
	/// </summary>
	public string? Path
	{
		get => (string?)GetValue(PathProperty);
		set => SetValue(PathProperty, value);
	}

	/// <summary>
	/// An implicit operator to convert a string value into a <see cref="FileMediaSource"/>.
	/// </summary>
	/// <param name="path">Full path to the local file. Can be a relative or absolute path.</param>
	public static implicit operator FileMediaSource(string path) => (FileMediaSource)FromFile(path);

	/// <summary>
	/// An implicit operator to convert a <see cref="FileMediaSource"/> into a string value.
	/// </summary>
	/// <param name="fileMediaSource">A <see cref="FileMediaSource"/> instance to convert to a string value.</param>
	public static implicit operator string?(FileMediaSource? fileMediaSource) => fileMediaSource?.Path;

	/// <inheritdoc/>
	public override string ToString() => $"File: {Path}";

	static void OnFileMediaSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
		((FileMediaSource)bindable).OnSourceChanged();
}