using System.Diagnostics;
using System.Runtime.Versioning;
using Android.Content;
using Microsoft.Maui.Platform;

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
			var view1 = (viewOrder[i - 1].Handler as IPlatformViewHandler)?.PlatformView;
			var view2 = (viewOrder[i].Handler as IPlatformViewHandler)?.PlatformView;

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
			else
			{
				Trace.WriteLine($"{nameof(ISemanticOrderView)} is only supported on Android 22.0 and higher");
			}
		}
	}

	/// <inheritdoc />
	protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
	{
		UpdateViewOrder();
		base.OnLayout(changed, left, top, right, bottom);
	}
}