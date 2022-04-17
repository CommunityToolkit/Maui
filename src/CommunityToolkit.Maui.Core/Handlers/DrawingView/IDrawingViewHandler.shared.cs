namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// <see cref="IDrawingView"/> handler.
/// </summary>
public interface IDrawingViewHandler
{
	/// <summary>
	/// Set <see cref="DrawingLineAdapter"/>.
	/// </summary>
	/// <param name="drawingLineAdapter"><see cref="DrawingLineAdapter"/>. If null, default drawing line adapter is used.</param>
	void SetDrawingLineAdapter(DrawingLineAdapter? drawingLineAdapter = null);
}