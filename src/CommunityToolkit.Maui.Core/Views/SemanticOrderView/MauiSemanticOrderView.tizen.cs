using Microsoft.Maui.Platform;
using Tizen.NUI.Accessibility;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
public class MauiSemanticOrderView : ContentViewGroup
{
	/// <summary>
	/// Constructor for MauiSemanticOrderView.
	/// </summary>
	public MauiSemanticOrderView(IView virtualView) : base(virtualView)
	{
	}

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
		
		var order = 0;

		foreach (var view in VirtualView.ViewOrder)
		{
			if (view.Handler is IPlatformViewHandler platformViewHandler && platformViewHandler.PlatformView is not null)
			{
				if (platformViewHandler.PlatformView is WrapperView wrapperView)
				{
					if (wrapperView.Content is not null)
					{
						wrapperView.Content.SiblingOrder = order;
					}
				}
				else
				{
					platformViewHandler.PlatformView.SiblingOrder = order;
				}

				order++;
			}
		}
	}
}
