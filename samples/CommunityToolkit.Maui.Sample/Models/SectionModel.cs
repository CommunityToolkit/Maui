namespace CommunityToolkit.Maui.Sample.Models;

public sealed class SectionModel
{
	public SectionModel(in string pagePath, in string title, in string description)
		: this(pagePath, title, new Color(), description)
	{
	}

	public SectionModel(in string pagePath, in string title, in Color color, in string description)
	{
		PagePath = pagePath;
		Title = title;
		Description = description;
		Color = color;
	}

	public string PagePath { get; }

	public string Title { get; }

	public string Description { get; }

	public Color Color { get; }
}