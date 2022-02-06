using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for CommunityToolkit.Maui
/// </summary>
public static class Defaults
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
	/// Default Text Color
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly Color TextColor = Colors.Black;

	/// <summary>
	/// Default Background Color
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static readonly Color BackgroundColor = Colors.LightGray;
}