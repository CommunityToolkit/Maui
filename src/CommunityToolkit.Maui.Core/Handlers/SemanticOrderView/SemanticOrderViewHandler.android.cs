using Android.Views;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;

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
	protected override MauiSemanticOrderView CreatePlatformElement()
	{
		_ = MauiContext ?? throw new InvalidOperationException("MauiContext is null, please check your MauiApplication.");
		_ = MauiContext.Context ?? throw new InvalidOperationException("Android Context is null, please check your MauiApplication.");

		return new MauiSemanticOrderView(MauiContext.Context);
	}
}
