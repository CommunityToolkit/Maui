using Microsoft.Maui.Platform;
using Tizen.NUI.Accessibility;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
public class MauiSemanticOrderView : ContentViewGroup
{
	ISemanticOrderView? virtualView;

	/// <summary>
	/// Constructor for MauiSemanticOrderView.
	/// </summary>
	public MauiSemanticOrderView(IView virtualView) : base(virtualView)
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

		var accessibilityManager = AccessibilityManager.Instance;
		var order = accessibilityManager.GenerateNewFocusOrder();

		foreach (var view in VirtualView.ViewOrder)
		{
			if (view.Handler is IPlatformViewHandler platformViewHandler && platformViewHandler.PlatformView is not null)
			{
				if (platformViewHandler.PlatformView is WrapperView wrapperView)
				{
					if (wrapperView.Content is not null)
					{
						accessibilityManager.SetFocusOrder(wrapperView.Content, order);
					}
				}
				else
				{
					accessibilityManager.SetFocusOrder(platformViewHandler.PlatformView, order);
				}

				order++;
			}
		}
	}
}
