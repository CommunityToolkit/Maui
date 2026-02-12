using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui;

static class StatusBarBehaviorDefaults
{
	public static Color StatusBarColor { get; } = Colors.Transparent;
	public static StatusBarStyle StatusBarStyle { get; } = StatusBarStyle.Default;
	public static StatusBarApplyOn ApplyOn { get; } = StatusBarApplyOn.OnBehaviorAttachedTo;
}