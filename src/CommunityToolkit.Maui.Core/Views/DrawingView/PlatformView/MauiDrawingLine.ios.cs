using System.Collections.ObjectModel;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="MauiDrawingView"/>.
/// </summary>
public partial class MauiDrawingLine
{
	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="MauiDrawingView"/>.
	/// </summary>
	public UIColor LineColor { get; set; } = UIColor.Black;

	/// <summary>
	/// The collection of <see cref="Point"/> that makes up this line on the <see cref="MauiDrawingView"/>.
	/// </summary>
	public ObservableCollection<CGPoint> Points { get; set; } = new();
}