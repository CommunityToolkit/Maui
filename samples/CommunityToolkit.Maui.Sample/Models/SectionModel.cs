using System;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Sample.Models;

public sealed class SectionModel
{
	public SectionModel(Type type, string title, string description)
		: this(type, title, new Color(), description)
	{
	}

	public SectionModel(Type type, string title, Color color, string description)
	{
		Type = type;
		Title = title;
		Description = description;
		Color = color;
	}

	public Type Type { get; }

	public string Title { get; } = string.Empty;

	public string Description { get; } = string.Empty;

	public Color Color { get; } = new Color();
}