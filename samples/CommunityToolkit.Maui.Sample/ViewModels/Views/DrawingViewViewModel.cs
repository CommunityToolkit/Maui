using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public class DrawingViewViewModel : BaseViewModel
{
	readonly IDeviceDisplay deviceDisplay;

	string logs = string.Empty;

	public DrawingViewViewModel(IDeviceDisplay deviceDisplay)
	{
		this.deviceDisplay = deviceDisplay;

		DrawingLineCompletedCommand = new Command<ILine>(line =>
		{
			Logs += $"GestureCompletedCommand executed. Line points count: {line.Points.Count}" + Environment.NewLine;
		});

		ClearLinesCommand = new Command(Lines.Clear);

		AddNewLineCommand = new Command<DrawingView>(drawingView => Lines.Add(new Line()
		{
			Points = new(GeneratePoints(10, drawingView.Width, drawingView.Height)),
			LineColor = Color.FromRgb(Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255)),
			LineWidth = 10,
			EnableSmoothedPath = true,
			Granularity = 5
		}));
	}

	public ObservableCollection<ILine> Lines { get; } = new();

	public ICommand DrawingLineCompletedCommand { get; }
	public ICommand ClearLinesCommand { get; }
	public ICommand AddNewLineCommand { get; }

	public string Logs
	{
		get => logs;
		set => SetProperty(ref logs, value);
	}

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