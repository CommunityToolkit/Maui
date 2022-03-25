using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class DrawingViewViewModel : BaseViewModel
{
	readonly IDeviceDisplay deviceDisplay;

	[ObservableProperty]
	string logs = string.Empty;

	public DrawingViewViewModel(IDeviceDisplay deviceDisplay)
	{
		this.deviceDisplay = deviceDisplay;

		DrawingLineCompletedCommand = new Command<DrawingLine>(line => Logs += $"GestureCompletedCommand executed. Line points count: {line.Points.Count}" + Environment.NewLine);

		ClearLinesCommand = new Command(Lines.Clear);

		AddNewLineCommand = new Command<DrawingView>(drawingView =>
		{
			var width = double.IsNaN(drawingView.Width) ? 200 : drawingView.Width;
			var height = double.IsNaN(drawingView.Height) ? 200 : drawingView.Height;

			Lines.Add(new DrawingLine()
			{
				Points = new(GeneratePoints(10, width, height)),
				LineColor = Color.FromRgb(Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255)),
				LineWidth = 10,
				EnableSmoothedPath = true,
				Granularity = 5
			});
		});
	}

	public ObservableCollection<DrawingLine> Lines { get; } = new();

	public ICommand DrawingLineCompletedCommand { get; }
	public ICommand ClearLinesCommand { get; }
	public ICommand AddNewLineCommand { get; }

	public IEnumerable<Point> GeneratePoints(int count, double viewWidth, double viewHeight)
	{
		var maxWidthInt = (int)Math.Round(viewWidth * deviceDisplay.GetMainDisplayInfo().Density, MidpointRounding.ToZero);
		var maxHeightInt = (int)Math.Round(viewHeight * deviceDisplay.GetMainDisplayInfo().Density, MidpointRounding.ToZero);

		for (var i = 0; i < count; i++)
		{
			yield return new Point(Random.Shared.Next(1, maxWidthInt), Random.Shared.Next(1, maxHeightInt));
		}
	}
}