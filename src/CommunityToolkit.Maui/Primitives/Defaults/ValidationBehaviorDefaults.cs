using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui;

static class ValidationBehaviorDefaults
{
	public const bool IsNotValid = false;
	public const bool IsValid = true;
	public const bool IsRunning = false;
	public const Style? ValidStyle = null;
	public const Style? InvalidStyle = null;
	public const object? Value = null;

	public static string ValuePropertyName { get; } = Entry.TextProperty.PropertyName;
	public static ValidationFlags Flags { get; } = ValidationFlags.ValidateOnUnfocused | ValidationFlags.ForceMakeValidWhenFocused;
}