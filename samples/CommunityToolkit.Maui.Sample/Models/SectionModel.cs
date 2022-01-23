using CommunityToolkit.Maui.Sample.ViewModels;

namespace CommunityToolkit.Maui.Sample.Models;

public sealed class SectionModel
{
	public SectionModel(in Type viewModelType, in string title, in string description)
		: this(viewModelType, title, new Color(), description)
	{
	}

	public SectionModel(in Type viewModelType, in string title, in Color color, in string description)
	{
		ViewModelType = viewModelType;
		Title = title;
		Description = description;
		Color = color;
	}

	public Type ViewModelType { get; }

	public string Title { get; }

	public string Description { get; }

	public Color Color { get; }
}