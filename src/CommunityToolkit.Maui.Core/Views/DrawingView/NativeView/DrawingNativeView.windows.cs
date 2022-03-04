using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Native Control
/// </summary>
public class DrawingNativeView : FrameworkElement
{
	readonly IDrawingView virtualView;

	/// <summary>
	/// Initialize a new instance of <see cref="DrawingNativeView" />.
	/// </summary>
	public DrawingNativeView(IDrawingView virtualView)
	{
		this.virtualView = virtualView;
	}
}