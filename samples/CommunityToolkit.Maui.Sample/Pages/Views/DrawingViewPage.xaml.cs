using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class DrawingViewPage : BasePage<DrawingViewViewModel>
{
	public DrawingViewPage(DrawingViewViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
		DrawingViewControl.DrawAction = (canvas, rect) =>
		{
			canvas.DrawString("Draw & GO", 0, 0, (int)DrawingViewControl.Width, (int)DrawingViewControl.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
		};
	}

	static double GetSide(double value) =>
		double.IsNaN(value) || value <= 1 ? 200 : value;

	void LoadPointsButtonClicked(object? sender, EventArgs? e)
	{
		DrawingViewControl.Lines.Clear();
		foreach (var line in GenerateLines(2))
		{
			DrawingViewControl.Lines.Add(line);
		}
	}

	async void GetCurrentDrawingViewImageClicked(object? sender, EventArgs? e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var stream = await DrawingViewControl.GetImageStream(GestureImage.Width, GestureImage.Height, DrawingViewOutputOption.Lines, cts.Token);

		GestureImage.Source = ImageSource.FromStream(() => stream);
	}

	async void GenerateImageButtonClicked(object? sender, EventArgs? e)
	{
		var lines = GenerateLines(2);

		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await DrawImage(lines, cts.Token);
	}

	async Task DrawImage(IEnumerable<DrawingLine> lines, CancellationToken token)
	{
		var drawingLines = lines.ToList();
		var points = drawingLines.SelectMany(x => x.Points).ToList();
		var stream = await DrawingView.GetImageStream(
			ImageLineOptions.FullCanvas(
				drawingLines.OfType<IDrawingLine>().ToList(),
				new Size(points.Max(x => x.X) - points.Min(x => x.X), points.Max(x => x.Y) - points.Min(x => x.Y)),
				new SolidPaint(Colors.Gray),
				this.DrawingViewControl.Bounds.Size),
			token);

		GestureImage.Source = ImageSource.FromStream(() => stream);
	}

	IEnumerable<DrawingLine> GenerateLines(int count)
	{
		var width = GetSide(GestureImage.Width);
		var height = GetSide(GestureImage.Height);
		for (var i = 0; i < count; i++)
		{
			yield return new DrawingLine
			{
				Points = new(DrawingViewViewModel.GeneratePoints(10, width, height)),
				LineColor = Color.FromRgb(Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255)),
				LineWidth = 10,
				ShouldSmoothPathWhenDrawn = false,
				Granularity = 5
			};
		}
	}

	async void OnDrawingLineCompleted(object? sender, DrawingLineCompletedEventArgs e)
	{
		var width = GetSide(GestureImage.Width);
		var height = GetSide(GestureImage.Height);

		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var stream = await e.LastDrawingLine.GetImageStream(width, height, Colors.Gray.AsPaint(), this.DrawingViewControl.Bounds.Size, cts.Token);

		GestureImage.Source = ImageSource.FromStream(() => stream);
	}
}