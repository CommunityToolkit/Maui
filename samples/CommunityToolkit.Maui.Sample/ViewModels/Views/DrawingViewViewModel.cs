using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class DrawingViewViewModel : BaseViewModel
{
	readonly IDeviceInfo deviceInfo;
	readonly IDeviceDisplay deviceDisplay;

	[ObservableProperty]
	string logs = string.Empty;

	public DrawingViewViewModel(IDeviceInfo deviceInfo, IDeviceDisplay deviceDisplay)
	{
		this.deviceInfo = deviceInfo;
		this.deviceDisplay = deviceDisplay;

		DrawingLineCompletedCommand = new Command<DrawingLine>(line => Logs = "GestureCompletedCommand executed." + Environment.NewLine + $"Line points count: {line.Points.Count}" + Environment.NewLine + Environment.NewLine + Logs);

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

	public IEnumerable<PointF> GeneratePoints(int count, double viewWidth, double viewHeight)
	{
		int maxWidthInt, maxHeightInt;

		if (deviceInfo.Platform == DevicePlatform.Android || deviceInfo.Platform == DevicePlatform.WinUI)
		{
			maxWidthInt = (int)Math.Round(viewWidth * deviceDisplay.MainDisplayInfo.Density, MidpointRounding.ToZero);
			maxHeightInt = (int)Math.Round(viewHeight * deviceDisplay.MainDisplayInfo.Density, MidpointRounding.ToZero);
		}
		else if (deviceInfo.Platform == DevicePlatform.iOS || deviceInfo.Platform == DevicePlatform.MacCatalyst)
		{
			maxWidthInt = (int)Math.Round(viewWidth, MidpointRounding.ToZero);
			maxHeightInt = (int)Math.Round(viewHeight, MidpointRounding.ToZero);
		}
		else
		{
			throw new NotImplementedException();
		}

		for (var i = 0; i < count; i++)
		{
			yield return new PointF(Random.Shared.Next(1, maxWidthInt), Random.Shared.Next(1, maxHeightInt));
		}
	}
}