using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// DrawingLine Adapter
/// </summary>
public interface IDrawingLineAdapter
{
	/// <summary>
	/// Convert <see cref="MauiDrawingLine"/> to <see cref="IDrawingLine"/>.
	/// </summary>
	/// <returns><see cref="IDrawingLine"/></returns>
	IDrawingLine ConvertMauiDrawingLine(MauiDrawingLine mauiDrawingLine);
}