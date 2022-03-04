using Microsoft.UI.Xaml;
using WinRT;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// 
/// </summary>
public class DrawingNativeView : FrameworkElement
{
	public IDrawingView VirtualView { get; }

	public DrawingNativeView(IDrawingView virtualView)
	{
		VirtualView = virtualView;
	}
}