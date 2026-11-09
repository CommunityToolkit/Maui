namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for <see cref="IAlert"/>
/// </summary>
static class AlertDefaults
{
	/// <summary>
	/// Default Font Size
	/// </summary>
	public const double FontSize = 14;

	/// <summary>
	/// Default Character Spacing
	/// </summary>
	public const double CharacterSpacing = 0.0d;

	/// <summary>
	/// Default Character Spacing
	/// </summary>
	public const string ActionButtonText = "OK";

	/// <summary>
	/// Default Text Color
	/// </summary>
	public static Color TextColor { get; } = Colors.Black;

	/// <summary>
	/// Default Background Color
	/// </summary>
	public static Color BackgroundColor { get; } = Colors.LightGray;
}