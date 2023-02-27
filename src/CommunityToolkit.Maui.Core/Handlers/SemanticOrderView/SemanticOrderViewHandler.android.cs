using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class SemanticOrderViewHandler
{
	/// <inheritdoc/>
	protected override ContentViewGroup CreatePlatformView()
	{
		if (MauiContext?.Context is null)
		{
			throw new InvalidOperationException("Android Context is null, please ensure your MauiApplication is initialized.");
		}

		return new MauiSemanticOrderView(MauiContext.Context);
	}
}