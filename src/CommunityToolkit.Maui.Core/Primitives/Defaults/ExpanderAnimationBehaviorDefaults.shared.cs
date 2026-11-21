namespace CommunityToolkit.Maui.Core;

static class ExpanderAnimationBehaviorDefaults
{
	public const uint CollapsingLength = 250u;
	public static Easing CollapsingEasing { get; } = Easing.Linear;
	public const uint ExpandingLength = 250u;
	public static Easing ExpandingEasing { get; } = Easing.Linear;
}
