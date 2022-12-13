using System.ComponentModel;
using CommunityToolkit.Maui.MediaElement.Converters;

namespace CommunityToolkit.Maui.MediaElement;

[TypeConverter(typeof(FileMediaSourceConverter))]
public sealed class ResourceMediaSource : MediaSource
{
	public static readonly BindableProperty PathProperty
		= BindableProperty.Create(nameof(File), typeof(string), typeof(ResourceMediaSource), propertyChanged: OnResourceMediaSourceMediaSourceChanged);

	public string? Path
	{
		get => (string?)GetValue(PathProperty);
		set => SetValue(PathProperty, value);
	}

	public override string ToString() => $"Resource: {Path}";

	public static implicit operator ResourceMediaSource(string file) => (ResourceMediaSource)FromFile(file);

	public static implicit operator string?(ResourceMediaSource? resource) => resource?.Path;

	static void OnResourceMediaSourceMediaSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
		((ResourceMediaSource)bindable).OnSourceChanged();
}