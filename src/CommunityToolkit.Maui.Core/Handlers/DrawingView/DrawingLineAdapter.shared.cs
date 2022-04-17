using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// DrawingLine Adapter
/// </summary>
public class DrawingLineAdapter
{
	/// <summary>
	/// Convert <see cref="MauiDrawingLine"/> to <see cref="IDrawingLine"/>.
	/// </summary>
	/// <returns></returns>
	public virtual IDrawingLine GetDrawingLine(MauiDrawingLine mauiDrawingLine)
	{
		return new DrawingLine
		{
			LineColor = mauiDrawingLine.LineColor,
			EnableSmoothedPath = mauiDrawingLine.EnableSmoothedPath,
			Granularity = mauiDrawingLine.Granularity,
			LineWidth = mauiDrawingLine.LineWidth,
			Points = mauiDrawingLine.Points.ToObservableCollection()
		};
	}
}