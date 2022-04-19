using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for <see cref="IAlert"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AlertDefaults
{
	/// <summary>
	/// Default Font Size
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double FontSize = 14;

	/// <summary>
	/// Default Character Spacing
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double CharacterSpacing = 0.0d;

	/// <summary>
	/// Default Character Spacing
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const string ActionButtonText = "OK";

	/// <summary>
	/// Default Text Color
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color TextColor { get; } = Colors.Black;

	/// <summary>
	/// Default Background Color
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color BackgroundColor { get; } = Colors.LightGray;
}