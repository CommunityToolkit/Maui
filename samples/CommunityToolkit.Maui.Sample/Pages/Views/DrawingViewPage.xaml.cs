using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class DrawingViewPage : BasePage<DrawingViewViewModel>
{
	readonly DrawingViewViewModel viewModel;

	public DrawingViewPage(DrawingViewViewModel viewModel) : base(viewModel)
	{
		this.viewModel = viewModel;
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
		DrawImage(lines.ToList());
	}

	ObservableCollection<ILine> GenerateLines(int count)
	{
		var lines = new ObservableCollection<ILine>();
		for (var i = 0; i < count; i++)
		{
			lines.Add(new Line()
			{
				Points = viewModel.GeneratePoints(10),
				LineColor = Color.FromRgb(Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255)),
				LineWidth = 10,
				EnableSmoothedPath = false,
				Granularity = 5
			});
		}

		return lines;
	}

	void DrawImage(IEnumerable<ILine> lines)
	{
		var stream = DrawingView.GetImageStream(lines, new Size(GestureImage.Width, GestureImage.Height), Colors.Gray);
		GestureImage.Source = ImageSource.FromStream(() => stream);
	}
}