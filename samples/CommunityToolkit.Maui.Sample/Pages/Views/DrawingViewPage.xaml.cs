using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class DrawingViewPage : BasePage<DrawingViewViewModel>
{
	public DrawingViewPage(IDeviceInfo deviceInfo, DrawingViewViewModel viewModel) : base(deviceInfo, viewModel)
	{
		InitializeComponent();
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
		DrawImage(lines);
	}

	void DrawImage(in IEnumerable<ILine> lines)
	{
		var stream = DrawingView.GetImageStream(lines, new Size(GestureImage.Width, GestureImage.Height), Colors.Gray);
		GestureImage.Source = ImageSource.FromStream(() => stream);
	}

	IEnumerable<ILine> GenerateLines(int count)
	{
		var width = double.IsNaN(DrawingViewControl.Width) ? 200 : DrawingViewControl.Width;
		var height = double.IsNaN(DrawingViewControl.Height) ? 200 : DrawingViewControl.Height;
		for (var i = 0; i < count; i++)
		{
			yield return new Line()
			{
				Points = new(BindingContext.GeneratePoints(10, width, height)),
				LineColor = Color.FromRgb(Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255)),
				LineWidth = 10,
				EnableSmoothedPath = false,
				Granularity = 5
			};
		}
	}

	void OnDrawingLineCompleted(object sender, DrawingLineCompletedEventArgs e)
	{
		var stream = e.Line.GetImageStream(GestureImage.Width, GestureImage.Height, Colors.Gray);
		GestureImage.Source = ImageSource.FromStream(() => stream);
	}
}