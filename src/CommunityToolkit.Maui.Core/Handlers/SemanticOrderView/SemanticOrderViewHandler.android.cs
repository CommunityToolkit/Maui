using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class SemanticOrderViewHandler
{
	/// <inheritdoc/>
	protected override ContentViewGroup CreatePlatformView()
	{
		_ = MauiContext ?? throw new InvalidOperationException("MauiContext is null, please check your MauiApplication.");
		_ = MauiContext.Context ?? throw new InvalidOperationException("Android Context is null, please check your MauiApplication.");

		return new MauiSemanticOrderView(MauiContext.Context);
	}
}