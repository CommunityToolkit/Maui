using Microsoft.UI.Xaml;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WColor = Microsoft.UI.Colors;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Native Control
/// </summary>
public partial class MauiDrawingView : FrameworkElement
{
	/// <summary>
	/// Line color
	/// </summary>
	public WBrush LineColor { get; set; } = new WSolidColorBrush(WColor.Black);

	/// <summary>
	/// Line width
	/// </summary>
	public float LineWidth { get; set; } = 5;

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		throw new NotSupportedException("DrawingView is supported on Windows till InkCanvas is not migrated to WinUI 3");
	}

	/// <summary>
	/// Clean up resources
	/// </summary>
	public void CleanUp()
	{
	}
}