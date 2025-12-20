namespace CommunityToolkit.Maui.Core;

static class BaseAnimationDefaults
{
	public const uint Length = 250u;
	public static Easing Easing { get; } = Easing.Linear;
}