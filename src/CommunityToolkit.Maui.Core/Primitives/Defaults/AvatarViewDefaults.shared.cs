using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>Default Values for <see cref="IAvatarView"/></summary>
static class AvatarViewDefaults
{
	/// <summary>Default avatar border width.</summary>
	public const double DefaultBorderWidth = 1;

	/// <summary>Default height request.</summary>
	public const double DefaultHeightRequest = 48;

	/// <summary>Default avatar text.</summary>
	public const string DefaultText = "?";

	/// <summary>Default width request.</summary>
	public const double DefaultWidthRequest = 48;

	/// <summary>default avatar border colour.</summary>
	public static Color DefaultBorderColor { get; } = Colors.White;

	/// <summary>Default corner radius.</summary>
	public static CornerRadius DefaultCornerRadius { get; } = new(24, 24, 24, 24);

	/// <summary>Default padding.</summary>
	public static Thickness DefaultPadding { get; } = new(1);
}