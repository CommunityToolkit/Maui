using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class DrawingViewPage : BasePage
{
	static readonly Random random = new();

	public DrawingViewPage()
	{
		InitializeComponent();
		DrawingViewControl.DrawingLineCompletedCommand = new Command<Line>(line =>
		{
			Logs.Text += "GestureCompletedCommand executed" + Environment.NewLine;
			var stream = Line.GetImageStream(line.Points.ToList(), new Size(GestureImage.Width, GestureImage.Height), 10, Colors.White, Colors.Black);
			GestureImage.Source = ImageSource.FromStream(() => stream);
		});

		BindingContext = this;
	}

	void LoadPointsButtonClicked(object sender, EventArgs e)
	{
		DrawingViewControl.Lines.Clear();
		foreach (var line in GenerateLines(2))
		{
			DrawingViewControl.Lines.Add(line);
		}
	}

	void DisplayHiddenLabelButtonClicked(object sender, EventArgs e) =>
		HiddenPanel.IsVisible = !HiddenPanel.IsVisible;

	void GetCurrentDrawingViewImageClicked(object sender, EventArgs e)
	{
		var stream = DrawingViewControl.GetImageStream(GestureImage.Width, GestureImage.Height);
		GestureImage.Source = ImageSource.FromStream(() => stream);
	}

	void GetImageClicked(object sender, EventArgs e)
	{
		var lines = GenerateLines(2);
		DrawImage(lines.ToList());
	}

	ObservableCollection<Line> GenerateLines(int count)
	{
		var lines = new ObservableCollection<Line>();
		for (var i = 0; i < count; i++)
		{
			lines.Add(new Line()
			{
				Points = GeneratePoints(10),
				LineColor = Color.FromRgb(random.Next(255), random.Next(255), random.Next(255)),
				LineWidth = 10,
				EnableSmoothedPath = false,
				Granularity = 5
			});
		}

		return lines;
	}

	ObservableCollection<Point> GeneratePoints(int count)
	{
		var points = new ObservableCollection<Point>();
		for (var i = 0; i < count; i++)
		{
			points.Add(new Point(random.Next(1, (int)DrawingViewControl.Width), random.Next(1, (int)DrawingViewControl.Height)));
		}

		return points;
	}

	void DrawImage(List<Line> lines)
	{
		var stream = DrawingView.GetImageStream(lines, new Size(GestureImage.Width, GestureImage.Height), Colors.Gray);
		GestureImage.Source = ImageSource.FromStream(() => stream);
	}

	void AddNewLine(object sender, EventArgs e) =>
		DrawingViewControl.Lines.Add(new Line()
		{
			Points = GeneratePoints(10),
			LineColor = Color.FromRgb(random.Next(255), random.Next(255), random.Next(255)),
			LineWidth = 10,
			EnableSmoothedPath = true,
			Granularity = 5
		});

	void ClearLines_Clicked(object sender, EventArgs e) => DrawingViewControl.Lines.Clear();
}