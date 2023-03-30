using Android.Content;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
public class MauiSemanticOrderView : ContentViewGroup
{
	ISemanticOrderView? virtualView;

	public MauiSemanticOrderView(Context context) : base(context)
	{
	}

	internal ISemanticOrderView? VirtualView
	{
		get => virtualView;
		set
		{
			virtualView = value;
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
			var view1 = (viewOrder[i - 1]?.Handler as IPlatformViewHandler)?.PlatformView;
			var view2 = (viewOrder[i]?.Handler as IPlatformViewHandler)?.PlatformView;

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

			if (OperatingSystem.IsAndroidVersionAtLeast(22))
			{
				view2.AccessibilityTraversalAfter = view1.Id;
				view1.AccessibilityTraversalBefore = view2.Id;
			}
		}
	}

	protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
	{
		UpdateViewOrder();
		base.OnLayout(changed, left, top, right, bottom);
	}
}