using Microsoft.Maui.Handlers;
using CommunityToolkit.Maui.Core.Extensions;
namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Line handler
/// </summary>
public partial class LineHandler : ElementHandler<ILine, MauiDrawingLine>
{
	/// <inheritdoc/>
	protected override MauiDrawingLine CreatePlatformElement() => new(MauiContext?.Context);
}