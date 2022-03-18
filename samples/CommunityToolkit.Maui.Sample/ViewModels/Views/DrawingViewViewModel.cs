using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public class DrawingViewViewModel : BaseViewModel
{
	ObservableCollection<ILine> lines;
	string logs;

	public DrawingViewViewModel()
	{
		lines = new();
		logs = string.Empty;
		DrawingLineCompletedCommand = new Command<ILine>(line =>
		{
			Logs += $"GestureCompletedCommand executed. Line points count: {line.Points.Count}" + Environment.NewLine;
		});
		ClearLinesCommand = new Command(Lines.Clear);
		AddNewLineCommand = new Command(() => Lines.Add(new Line()
		{
			Points = GeneratePoints(10),
			LineColor = Color.FromRgb(Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255)),
			LineWidth = 10,
			EnableSmoothedPath = true,
			Granularity = 5
		}));
	}

	public ObservableCollection<ILine> Lines
	{
		get => lines;
		set => SetProperty(ref lines, value);
	}

	public ICommand DrawingLineCompletedCommand { get; set; }
	public ICommand ClearLinesCommand { get; set; }
	public ICommand AddNewLineCommand { get; set; }

	public string Logs
	{
		get => logs;
		set => SetProperty(ref logs, value);
	}

	public ObservableCollection<Point> GeneratePoints(int count)
	{
		var points = new ObservableCollection<Point>();
		for (var i = 0; i < count; i++)
		{
			points.Add(new Point(Random.Shared.Next(1, 200), Random.Shared.Next(1, 200)));
		}

		return points;
	}
}