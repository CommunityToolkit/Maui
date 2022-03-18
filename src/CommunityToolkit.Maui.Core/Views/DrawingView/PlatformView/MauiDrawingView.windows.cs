using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WBrush = Microsoft.UI.Xaml.Media.Brush;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Native Control
/// </summary>
public partial class MauiDrawingView : FrameworkElement
{
	/// <summary>
	/// Line color
	/// </summary>
	public WBrush LineColor { get; set; } = new WSolidColorBrush();

	/// <summary>
	/// Line width
	/// </summary>
	public float LineWidth { get; set; } = 5;

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
	}

	/// <summary>
	/// Clean up resources
	/// </summary>
	public void CleanUp()
	{
	}
}