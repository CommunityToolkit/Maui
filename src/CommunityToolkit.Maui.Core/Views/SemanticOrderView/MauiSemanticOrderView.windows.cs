using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Microsoft.Maui.Platform;
using Windows.UI.Xaml;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The native implementation of the <see href="SemanticOrderView"/> control.
/// </summary>
public class MauiSemanticOrderView : ContentPanel
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
			if (element.Handler is IPlatformViewHandler platformViewHandler
				&& platformViewHandler.PlatformView is not null)
			{
				platformViewHandler.PlatformView.TabIndex = i++;
			}
		}
	}
}