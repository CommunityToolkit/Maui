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
	/// <param name="mauiDrawingLine">The <see cref="MauiDrawingLine"/> to convert.</param>
	/// <returns><see cref="IDrawingLine"/></returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mauiDrawingLine"/> is null.</exception>
	public IDrawingLine ConvertMauiDrawingLine(MauiDrawingLine mauiDrawingLine)
	{
		ArgumentNullException.ThrowIfNull(mauiDrawingLine);

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