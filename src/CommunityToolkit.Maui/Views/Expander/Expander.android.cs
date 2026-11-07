using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using Microsoft.Maui.Platform;
using AccessibilityActionCompat = AndroidX.Core.View.Accessibility.AccessibilityNodeInfoCompat.AccessibilityActionCompat;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Views;

// MAUI makes the header's platform view the node that TalkBack focuses for the header's TapGestureRecognizer. TalkBack announces a node that
// offers the expand action as collapsed and one that offers the collapse action as expanded, and lists that action in its actions menu.
public partial class Expander
{
	// MAUI clears the PlatformView of a handler before it disconnects it, so the Expander keeps the view it gave its actions to.
	AView? headerViewWithActions;

	partial void OnHeaderChanged(View? oldHeader, View? newHeader)
	{
		if (oldHeader is not null)
		{
			oldHeader.HandlerChanged -= OnHeaderHandlerChanged;
		}

		if (newHeader is not null)
		{
			newHeader.HandlerChanged += OnHeaderHandlerChanged;
		}
	}

	partial void UpdateHeaderAccessibility()
	{
		if (headerViewWithActions is not null)
		{
			ViewCompat.RemoveAccessibilityAction(headerViewWithActions, AccessibilityNodeInfoCompat.ActionExpand);
			ViewCompat.RemoveAccessibilityAction(headerViewWithActions, AccessibilityNodeInfoCompat.ActionCollapse);
			headerViewWithActions = null;
		}

		if (Header?.Handler?.PlatformView is not AView headerView
			|| !IsHeaderActivatable
			|| (IsExpanded ? AccessibilityActionCompat.ActionCollapse : AccessibilityActionCompat.ActionExpand) is not AccessibilityActionCompat action)
		{
			return;
		}

		// ViewCompat gives a view without a delegate a plain AccessibilityDelegateCompat, and MAUI wraps only a missing delegate or one of its own
		// wrappers with the delegate that offers the click of the header's TapGestureRecognizer, which it adds after the HandlerChanged event.
		if (ViewCompat.GetAccessibilityDelegate(headerView) is null)
		{
			ViewCompat.SetAccessibilityDelegate(headerView, new AccessibilityDelegateCompatWrapper(null));
		}

		ViewCompat.ReplaceAccessibilityAction(headerView, action, (string?)null, new ExpandOrCollapseCommand(this, !IsExpanded));
		headerViewWithActions = headerView;
	}

	void OnHeaderHandlerChanged(object? sender, EventArgs e) => UpdateHeaderAccessibility();

	sealed class ExpandOrCollapseCommand : Java.Lang.Object, IAccessibilityViewCommand
	{
		// The header's platform view keeps the command for as long as Android keeps the view.
		readonly WeakReference<Expander> expanderReference;
		readonly bool isExpanded;

		public ExpandOrCollapseCommand(Expander expander, bool isExpanded)
		{
			expanderReference = new WeakReference<Expander>(expander);
			this.isExpanded = isExpanded;
		}

		public bool Perform(AView? view, AccessibilityViewCommandCommandArguments? arguments)
		{
			if (!expanderReference.TryGetTarget(out var expander) || !expander.IsHeaderActivatable)
			{
				return false;
			}

			expander.SetIsExpandedFromHeader(isExpanded, new TappedEventArgs(null));
			return true;
		}
	}
}
