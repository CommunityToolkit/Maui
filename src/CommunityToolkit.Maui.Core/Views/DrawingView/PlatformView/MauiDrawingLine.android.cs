using System.Collections.ObjectModel;
using AColor = Android.Graphics.Color;
using APoint = Android.Graphics.PointF;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="MauiDrawingView"/>.
/// </summary>
public partial class MauiDrawingLine
{
	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="MauiDrawingView"/>. This is a bindable property.
	/// </summary>
	public AColor LineColor { get; set; } = AColor.Black;

	/// <summary>
	/// The collection of <see cref="Point"/> that makes up this line on the <see cref="MauiDrawingView"/>. This is a bindable property.
	/// </summary>
	public ObservableCollection<APoint> Points { get; set; } = new();
}