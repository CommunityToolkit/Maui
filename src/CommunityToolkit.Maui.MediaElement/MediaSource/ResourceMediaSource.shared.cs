using System.ComponentModel;
using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a source, loaded from the application's resources, that can be played by <see cref="MediaElement"/>.
/// </summary>
[TypeConverter(typeof(FileMediaSourceConverter))]
public sealed class ResourceMediaSource : MediaSource
{
	/// <summary>
	/// Backing store for the <see cref="Path"/> property.
	/// </summary>
	public static readonly BindableProperty PathProperty
		= BindableProperty.Create(nameof(Path), typeof(string), typeof(ResourceMediaSource), propertyChanged: OnResourceMediaSourceMediaSourceChanged);

	/// <summary>
	/// An implicit operator to convert a string value into a <see cref="ResourceMediaSource"/>.
	/// </summary>
	/// <param name="path">Full path to the resource file, relative to the application's resources folder.</param>
	public static implicit operator ResourceMediaSource(string path) => (ResourceMediaSource)FromFile(path);

	/// <summary>
	/// An implicit operator to convert a <see cref="ResourceMediaSource"/> into a string value.
	/// </summary>
	/// <param name="resourceMediaSource">A <see cref="ResourceMediaSource"/> instance to convert to a string value.</param>
	public static implicit operator string?(ResourceMediaSource? resourceMediaSource) => resourceMediaSource?.Path;

	/// <summary>
	/// Gets or sets the full path to the resource file to use as a media source.
	/// This is a bindable property.
	/// </summary>
	/// <remarks>
	/// Path is relative to the application's resources folder.
	/// It can only be just a filename if the resource file is in the root of the resources folder.
	/// </remarks>
	public string? Path
	{
		get => (string?)GetValue(PathProperty);
		set => SetValue(PathProperty, value);
	}

	/// <inheritdoc/>
	public override string ToString() => $"Resource: {Path}";

	static void OnResourceMediaSourceMediaSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
		((ResourceMediaSource)bindable).OnSourceChanged();
}