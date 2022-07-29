using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class SemanticOrderViewHandler : ElementHandler<ISemanticOrderView, MauiSemanticOrderView>
{
	/// <summary>
	/// TBD
	/// </summary>
	public static void MapViewOrder(SemanticOrderViewHandler handler, ISemanticOrderView view)
	{
		handler.PlatformView.SetViewOrder(view);
	}

	/// <inheritdoc/>
	protected override MauiSemanticOrderView CreatePlatformElement() => new();

	/// <inheritdoc/>
	protected override void ConnectHandler(MauiSemanticOrderView platformView)
	{
		base.ConnectHandler(platformView);

		UpdateViewOrder();
	}

	void UpdateViewOrder()
	{
		var i = 1;
		foreach (var element in VirtualView.ViewOrder)
		{
			if (element is FrameworkElement ve)
			{
				ve.TabIndex = i++;
			}
		}
	}
}
