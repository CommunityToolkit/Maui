using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>Default Values for <see cref="IAvatarView"/></summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AvatarViewDefaults
{
	/// <summary>Default avatar border width.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double DefaultBorderWidth = 1;

	/// <summary>Default height request.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double DefaultHeightRequest = 48;

	/// <summary>Default avatar text.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const string DefaultText = "?";

	/// <summary>Default width request.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double DefaultWidthRequest = 48;

	/// <summary>default avatar border colour.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color DefaultBorderColor { get; } = Colors.White;

	/// <summary>Default corner radius.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static CornerRadius DefaultCornerRadius { get; } = new(24, 24, 24, 24);

	/// <summary>Default padding.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Thickness DefaultPadding { get; } = new(1);
}