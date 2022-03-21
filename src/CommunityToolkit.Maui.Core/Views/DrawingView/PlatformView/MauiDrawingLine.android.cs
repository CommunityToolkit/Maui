using System.Collections.ObjectModel;
using Android.Content;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="MauiDrawingView"/>.
/// </summary>
public  partial class MauiDrawingLine : Android.Views.View
{
	public MauiDrawingLine(Context? context) :base(context)
	{
		Points = new ObservableCollection<Point>();
		LineColor = Colors.Black;
		LineWidth = 5;
		Granularity = minValueGranularity;
	}
}