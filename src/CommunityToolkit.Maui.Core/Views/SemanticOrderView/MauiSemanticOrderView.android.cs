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
public class MauiSemanticOrderView(Context context) : ContentViewGroup(context)
{
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

		var viewOrder = VirtualView.ViewOrder.ToList();

		for (var i = 1; i < viewOrder.Count; i++)
		{
			var view1 = viewOrder[i - 1].Handler?.PlatformView as AView;
			var view2 = viewOrder[i].Handler?.PlatformView as AView;

			if (view1 is null || view2 is null)
			{
				return;
			}

			if (view1.Id <= 0)
			{
				view1.Id = GenerateViewId();
			}

			if (view2.Id <= 0)
			{
				view2.Id = GenerateViewId();
			}

			view2.AccessibilityTraversalAfter = view1.Id;
			view1.AccessibilityTraversalBefore = view2.Id;
		}
	}

	/// <inheritdoc />
	protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
	{
		UpdateViewOrder();
		base.OnLayout(changed, left, top, right, bottom);
	}
}