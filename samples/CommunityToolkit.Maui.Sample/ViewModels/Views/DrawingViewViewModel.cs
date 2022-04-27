using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class DrawingViewViewModel : BaseViewModel
{
	[ObservableProperty]
	string logs = string.Empty;

	public DrawingViewViewModel()
	{
		DrawingLineCompletedCommand = new Command<IDrawingLine>(line => Logs = "GestureCompletedCommand executed." + Environment.NewLine + $"Line points count: {line.Points.Count}" + Environment.NewLine + Environment.NewLine + Logs);

		ClearLinesCommand = new Command(Lines.Clear);

		AddNewLineCommand = new Command<DrawingView>(drawingView =>
		{
			var width = double.IsNaN(drawingView.Width) || drawingView.Width <= 1 ? 200 : drawingView.Width;
			var height = double.IsNaN(drawingView.Height) || drawingView.Height <= 1 ? 200 : drawingView.Height;

			Lines.Add(new DrawingLine()
			{
				Points = new(GeneratePoints(10, width, height)),
				LineColor = Color.FromRgb(Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255)),
				LineWidth = 10,
				ShouldSmoothPathWhenDrawn = true,
				Granularity = 5
			});
		});
	}

	public ObservableCollection<IDrawingLine> Lines { get; } = new();

	public ICommand DrawingLineCompletedCommand { get; }
	public ICommand ClearLinesCommand { get; }
	public ICommand AddNewLineCommand { get; }

	public static IEnumerable<PointF> GeneratePoints(int count, double viewWidth, double viewHeight)
	{
		var paddedViewWidth = Math.Clamp(viewWidth - 10, 1, viewWidth);
		var paddedViewHeight = Math.Clamp(viewHeight - 10, 1, viewHeight);

		var maxWidthInt = (int)Math.Round(paddedViewWidth, MidpointRounding.AwayFromZero);
		var maxHeightInt = (int)Math.Round(paddedViewHeight, MidpointRounding.AwayFromZero);

		for (var i = 0; i < count; i++)
		{
			yield return new PointF(Random.Shared.Next(1, maxWidthInt), Random.Shared.Next(1, maxHeightInt));
		}
	}
}