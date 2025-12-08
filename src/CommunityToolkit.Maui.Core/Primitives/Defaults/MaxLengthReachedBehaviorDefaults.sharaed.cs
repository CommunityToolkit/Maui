using System.Windows.Input;

namespace CommunityToolkit.Maui.Core;

static class MaxLengthReachedBehaviorDefaults
{
	public const bool ShouldDismissKeyboardAutomatically = false;
	public static ICommand? Command { get; } = null;
}