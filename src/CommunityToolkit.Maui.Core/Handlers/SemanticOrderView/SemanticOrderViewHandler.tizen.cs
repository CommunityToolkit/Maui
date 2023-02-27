using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class SemanticOrderViewHandler
{
	/// <inheritdoc/>
	protected override ContentViewGroup CreatePlatformView()
	{
		_ = MauiContext ?? throw new InvalidOperationException("MauiContext is null, please check your MauiApplication.");

		return new MauiSemanticOrderView(VirtualView);
	}

}
