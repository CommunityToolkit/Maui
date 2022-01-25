using CommunityToolkit.Maui.Sample.Pages;

namespace CommunityToolkit.Maui.Sample.Models;

public sealed class SectionModel
{
	public SectionModel(in ContentPage page, in string title, in string description)
		: this(page, title, new Color(), description)
	{
	}

	public SectionModel(in Type type, in string title, in Color color, in string description)
	{
		Type = type;
		Title = title;
		Description = description;
		Color = color;
		Page = new();
	}

	public SectionModel(in ContentPage page, in string title, in Color color, in string description)
	{
		if (string.IsNullOrWhiteSpace(page.Title))
		{
			page.Title = title;
		}

		Page = page;
		Title = title;
		Description = description;
		Color = color;
		Type = page.GetType();
	}

	public ContentPage Page { get; }

	public string Title { get; }

	public string Description { get; }

	public Color Color { get; }
	public Type Type { get; }
}