using System.Collections.ObjectModel;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WColor = Microsoft.UI.Colors;
using WPoint = Windows.Foundation.Point;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="MauiDrawingView"/>.
/// </summary>
public partial class MauiDrawingLine
{
	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="MauiDrawingView"/>.
	/// </summary>
	public WBrush LineColor { get; set; } = new WSolidColorBrush(WColor.Black);

	/// <summary>
	/// The collection of <see cref="WPoint"/> that makes up this line on the <see cref="MauiDrawingView"/>.
	/// </summary>
	public ObservableCollection<WPoint> Points { get; set; } = new();
}