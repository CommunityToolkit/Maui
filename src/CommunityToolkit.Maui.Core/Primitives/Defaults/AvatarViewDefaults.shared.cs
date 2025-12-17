using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>Default Values for <see cref="IAvatarView"/></summary>
static class AvatarViewDefaults
{
	/// <summary>Default avatar border width.</summary>
	public const double BorderWidth = 1;

	/// <summary>Default height request.</summary>
	public const double HeightRequest = 48;

	/// <summary>Default avatar text.</summary>
	public const string Text = "?";

	/// <summary>Default width request.</summary>
	public const double WidthRequest = 48;

	/// <summary>default avatar border colour.</summary>
	public static Color BorderColor { get; } = Colors.White;

	/// <summary>Default corner radius.</summary>
	public static CornerRadius CornerRadius { get; } = new(24, 24, 24, 24);

	/// <summary>Default padding.</summary>
	public static Thickness Padding { get; } = new(1);
}