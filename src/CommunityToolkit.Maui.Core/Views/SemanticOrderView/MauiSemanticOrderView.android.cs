using System.Diagnostics;
using System.Runtime.Versioning;
using Android.Content;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
/// <remarks>
/// Initialize <see cref="MauiSemanticOrderView"/>
/// </remarks>
/// <param name="context">Android Context</param>
[SupportedOSPlatform("Android22.0")]
public class MauiSemanticOrderView(Context context) : ContentViewGroup(context)
{
	// The views that the last update ordered: the next update removes the links between them that the new order does not have
	readonly List<AView> orderedViews = [];

	internal ISemanticOrderView? VirtualView
	{
		get;
		set
		{
			field = value;
			UpdateViewOrder();
		}
	}

	internal void UpdateViewOrder()
	{
		if (VirtualView is null)
		{
			return;
		}

		// Entries without an Android platform view (not realized yet, or with a platform view of another type) cannot be ordered:
		// they are left out, and the other views keep their relative order
		var viewOrder = VirtualView.ViewOrder.Select(static view => view.Handler?.PlatformView).OfType<AView>().ToList();

		if (OperatingSystem.IsAndroidVersionAtLeast(22))
		{
			RemoveLinksOfThePreviousOrder(viewOrder);
		}

		for (var i = 1; i < viewOrder.Count; i++)
		{
			var view1 = viewOrder[i - 1];
			var view2 = viewOrder[i];

			if (view1.Id <= 0)
			{
				view1.Id = GenerateViewId();
			}

			if (view2.Id <= 0)
			{
				view2.Id = GenerateViewId();
			}

			if (OperatingSystem.IsAndroidVersionAtLeast(22))
			{
				view2.AccessibilityTraversalAfter = view1.Id;
				view1.AccessibilityTraversalBefore = view2.Id;
			}
			else
			{
				Trace.WriteLine($"{nameof(ISemanticOrderView)} is only supported on Android 22.0 and higher");
			}
		}

		orderedViews.Clear();
		orderedViews.AddRange(viewOrder);
	}

	/// <inheritdoc />
	protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
	{
		UpdateViewOrder();
		base.OnLayout(changed, left, top, right, bottom);
	}

	static bool AreNextToEachOther(List<AView> viewOrder, AView view1, AView view2)
	{
		for (var i = 1; i < viewOrder.Count; i++)
		{
			if (viewOrder[i - 1] == view1 && viewOrder[i] == view2)
			{
				return true;
			}
		}

		return false;
	}

	// The native order describes the current order only: a link that the previous order made between two views that are no longer next to
	// each other is removed (from views dropped from the order, and from the ends of the new order), unless it was changed since. Links
	// that the new order keeps, and links that the app made, are left as they are, so applying an unchanged order again (OnLayout) changes
	// nothing.
	void RemoveLinksOfThePreviousOrder(List<AView> viewOrder)
	{
		for (var i = 1; i < orderedViews.Count; i++)
		{
			var view1 = orderedViews[i - 1];
			var view2 = orderedViews[i];

			// A view of the previous order may have been disposed since
			if (view1.Handle == IntPtr.Zero || view2.Handle == IntPtr.Zero || AreNextToEachOther(viewOrder, view1, view2))
			{
				continue;
			}

			if (view2.AccessibilityTraversalAfter == view1.Id)
			{
				view2.AccessibilityTraversalAfter = NoId;
			}

			if (view1.AccessibilityTraversalBefore == view2.Id)
			{
				view1.AccessibilityTraversalBefore = NoId;
			}
		}
	}
}