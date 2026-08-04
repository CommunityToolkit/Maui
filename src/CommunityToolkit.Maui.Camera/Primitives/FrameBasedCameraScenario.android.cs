using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using Java.Util.Concurrent;

namespace CommunityToolkit.Maui.Core;

public abstract partial class FrameBasedCameraScenario
{
	ImageAnalysis? imageAnalysis;
	
	public override Task OnAttached(IExecutorService? cameraExecutor, ResolutionSelector? resolutionSelector)
	{
		ArgumentNullException.ThrowIfNull(cameraExecutor);
		ArgumentNullException.ThrowIfNull(resolutionSelector);

		OnDetached();

		imageAnalysis = new ImageAnalysis.Builder()
			.SetResolutionSelector(resolutionSelector)
			.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
			.Build();

		imageAnalysis.SetAnalyzer(cameraExecutor, new FrameAnalyzer(this));
		
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public override void OnDetached()
	{
		base.OnDetached();

		if (imageAnalysis is not null)
		{
			imageAnalysis.Dispose();
			imageAnalysis = null;
		}
	}

	/// <inheritdoc />
	public override UseCase UseCase => imageAnalysis ?? throw new InvalidOperationException("Scenario not attached");
	
	sealed class FrameAnalyzer(FrameBasedCameraScenario scenario) : Java.Lang.Object, ImageAnalysis.IAnalyzer
	{
		public void Analyze(IImageProxy image)
		{
			var planes = image.GetPlanes();
			if (planes.Length == 0)
			{
				image.Close();
				return;
			}

			var buffer = planes[0].Buffer;
			if (buffer is null)
			{
				image.Close();
				return;
			}

			var data = scenario.Allocate(buffer.Remaining());
			buffer.Get(data, 0, buffer.Remaining());

			var format = image.Format switch
			{
				(int)Android.Graphics.ImageFormatType.Yuv420888 => CameraFrameFormat.Yuv420,
				_ => CameraFrameFormat.Unknown
			};

			var cameraFrame = new CameraFrame(data, image.Width, image.Height, format, scenario.Free);
			
			scenario.OnFrameReceived(cameraFrame);

			image.Close();
		}
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
		{
			imageAnalysis?.Dispose();
			imageAnalysis = null;
		}
	}
}
