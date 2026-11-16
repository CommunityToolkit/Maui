using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.Views;

// On Windows, iOS and Mac Catalyst, keyboard and screen reader users reach the header through the platform view of the Expander's layout,
// because the header's own platform view cannot be made to toggle the Expander. On Android, MAUI already makes the header's platform view
// the node that TalkBack focuses, and the Expander adds its actions there, so the layout keeps MAUI's platform view, or the one that an app's
// LayoutHandler.PlatformViewFactory creates.
sealed partial class ExpanderLayoutHandler : LayoutHandler
{
}