namespace CommunityToolkit.Maui.Core.Handlers;

/// <summary>
/// <see cref="IDrawingView"/> handler.
/// </summary>
public interface IDrawingViewHandler
{
	/// <summary>
	/// Set <see cref="IDrawingLineAdapter"/>.
	/// </summary>
	/// <param name="drawingLineAdapter"><see cref="IDrawingLineAdapter"/></param>
	void SetDrawingLineAdapter(IDrawingLineAdapter drawingLineAdapter);
}