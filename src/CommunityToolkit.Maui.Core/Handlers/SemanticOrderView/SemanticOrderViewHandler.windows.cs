using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class SemanticOrderViewHandler
{
	/// <inheritdoc/>
	protected override ContentPanel CreatePlatformView() => new MauiSemanticOrderView();
}