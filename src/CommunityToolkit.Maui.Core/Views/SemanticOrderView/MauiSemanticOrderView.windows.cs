using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
public partial class MauiSemanticOrderView : ContentPanel
{
	ISemanticOrderView? virtualView;

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

		var i = 1;
		foreach (var element in VirtualView.ViewOrder)
		{
			if (element.Handler?.PlatformView is FrameworkElement platformView)
			{
				platformView.TabIndex = i++;
			}
		}
	}
}