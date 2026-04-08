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

		// Tizen needs to provide IPlatformViewHandler to update view order.
	}
}
