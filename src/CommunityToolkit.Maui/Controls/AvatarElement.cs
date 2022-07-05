namespace CommunityToolkit.Maui.Controls;

/// <summary>Avatar content view element.</summary>
public static class AvatarElement
{
	/// <summary>Default height request.</summary>
	public const double DefaultHeightRequest = 64;

	/// <summary>Default width request.</summary>
	public const double DefaultWidthRequest = 64;

	/// <summary>Default corner radius.</summary>
	public static readonly CornerRadius DefaultCornerRadius = new(50, 50, 50, 50);

	/// <summary>Default avatar text.</summary>
	public const string DefaultText = "?";

	/// <summary>default avatar border colour.</summary>
	public static readonly Color DefaultBorderColor = Colors.White;

	/// <summary>Default avatar border width.</summary>
	public const double DefaultBorderWidth = 1;
}