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
	/// <param name="mauiDrawingLine">The <see cref="MauiDrawingLine"/> to convert.</param>
	/// <returns><see cref="IDrawingLine"/></returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mauiDrawingLine"/> is null.</exception>
	IDrawingLine ConvertMauiDrawingLine(MauiDrawingLine mauiDrawingLine);
}