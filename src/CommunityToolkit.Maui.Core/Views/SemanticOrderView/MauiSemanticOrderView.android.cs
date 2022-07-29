using Android.Content;
using Android.Views;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// TBD
/// </summary>
public class MauiSemanticOrderView : View
{
	public MauiSemanticOrderView(Context context)
		: base(context) { }

	/// <summary>
	/// TBD
	/// </summary>
	public ISemanticOrderView? VirtualView { get; set; }

	protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
	{
		SetAccessibilityElements();
		base.OnLayout(changed, left, top, right, bottom);
	}

	void SetAccessibilityElements()
	{
		if (VirtualView == null)
		{
			return;
		}

		var viewOrder = VirtualView.ViewOrder.OfType<IElement>().ToList();

		for (var i = 1; i < viewOrder.Count; i++)
		{
			var view1 = viewOrder[i - 1].GetViewForAccessibility();
			var view2 = viewOrder[i].GetViewForAccessibility();

			if (view1 == null || view2 == null)
			{
				return;
			}

			view2.AccessibilityTraversalAfter = view1.Id;
			view1.AccessibilityTraversalBefore = view2.Id;
		}
	}
}
