using CommunityToolkit.Maui.Sample.Pages;

namespace CommunityToolkit.Maui.Sample.Models;

public sealed class SectionModel
{
	// Use a private constructor to enforce the Factory Pattern
	// This requires us to use the `Create` methods to instantiate SectionModel
	// The Create methods allow us to 
	SectionModel(in Type pageType, in string title, in Color color, in string description)
	{
		PageType = pageType;
		Title = title;
		Description = description;
		Color = color;
	}

	// Use constrained generic to ensure TPage inherits from BasePage
	public static SectionModel Create<TPage>(in string title, in string description) where TPage : BasePage
		=> Create<TPage>(title, new Color(), description);

	// Use constrained generic to ensure TPage inherits from BasePage
	public static SectionModel Create<TPage>(in string title, in Color color, in string description) where TPage : BasePage
	{
		return new SectionModel(typeof(TPage), title, color, description);
	}

	public Type PageType { get; }

	public string Title { get; }

	public string Description { get; }

	public Color Color { get; }
}