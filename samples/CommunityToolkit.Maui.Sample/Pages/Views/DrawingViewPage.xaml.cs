using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class DrawingViewPage : BasePage<DrawingViewViewModel>
{
	public DrawingViewPage(IDeviceInfo deviceInfo, DrawingViewViewModel viewModel) : base(deviceInfo, viewModel)
	{
		InitializeComponent();
		DrawingViewControl.DrawAction = (canvas, rect) =>
		{
			canvas.DrawString("Draw & GO", 0, 0, (int)DrawingViewControl.Width, (int)DrawingViewControl.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
		};
	}

	void LoadPointsButtonClicked(object sender, EventArgs e)
	{
		DrawingViewControl.Lines.Clear();
		foreach (var line in GenerateLines(2))
		{
			DrawingViewControl.Lines.Add(line);
		}
	}

	async void GetCurrentDrawingViewImageClicked(object sender, EventArgs e)
	{
		var stream = await DrawingViewControl.GetImageStream(GestureImage.Width, GestureImage.Height);
		GestureImage.Source = ImageSource.FromStream(() => stream);
	}

	async void GenerateImageButtonClicked(object sender, EventArgs e)
	{
		var lines = GenerateLines(2);
		await DrawImage(lines);
	}

	async Task DrawImage(IEnumerable<DrawingLine> lines)
	{
		var stream = await DrawingView.GetImageStream(lines, new Size(GestureImage.Width, GestureImage.Height), Colors.Gray);
		GestureImage.Source = ImageSource.FromStream(() => stream);
	}

	IEnumerable<DrawingLine> GenerateLines(int count)
	{
		var width = double.IsNaN(DrawingViewControl.Width) ? 200 : DrawingViewControl.Width;
		var height = double.IsNaN(DrawingViewControl.Height) ? 200 : DrawingViewControl.Height;
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

	async void OnDrawingLineCompleted(object sender, DrawingLineCompletedEventArgs e)
	{
		var stream = await e.LastDrawingLine.GetImageStream(GestureImage.Width, GestureImage.Height, Colors.Gray);
		GestureImage.Source = ImageSource.FromStream(() => stream);
	}
}