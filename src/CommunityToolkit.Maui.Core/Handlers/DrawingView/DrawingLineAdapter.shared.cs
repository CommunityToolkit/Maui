using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// DrawingLine Adapter
/// </summary>
public sealed class DrawingLineAdapter : IDrawingLineAdapter
{
	/// <summary>
	/// Convert <see cref="MauiDrawingLine"/> to <see cref="IDrawingLine"/>.
	/// </summary>
	/// <returns><see cref="IDrawingLine"/></returns>
	public IDrawingLine ConvertMauiDrawingLine(MauiDrawingLine mauiDrawingLine)
	{
		return new DrawingLine
		{
			LineColor = mauiDrawingLine.LineColor,
			ShouldSmoothPathWhenDrawn = mauiDrawingLine.ShouldSmoothPathWhenDrawn,
			Granularity = mauiDrawingLine.Granularity,
			LineWidth = mauiDrawingLine.LineWidth,
			Points = mauiDrawingLine.Points.ToObservableCollection()
		};
	}
}