using System.Diagnostics;
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

		var builder = new ImageAnalysis.Builder()
			.SetResolutionSelector(resolutionSelector)
			.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest);

		if (PreferredFormat == CameraFrameFormat.Yuv420)
		{
			builder.SetOutputImageFormat(ImageAnalysis.OutputImageFormatYuv420888);
		}
		else if (PreferredFormat == CameraFrameFormat.Rgba8888)
		{
			builder.SetOutputImageFormat(ImageAnalysis.OutputImageFormatRgba8888);
		}
		else
		{
			Trace.TraceWarning("Unsupported camera frame format {0}, falling back to YUV", PreferredFormat);
			builder.SetOutputImageFormat(ImageAnalysis.OutputImageFormatYuv420888);
		}

		imageAnalysis = builder.Build();

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

			CameraFrame cameraFrame;

			if (image.Format == (int)Android.Graphics.ImageFormatType.Yuv420888 && planes.Length >= 3)
			{
				var yBuffer = planes[0].Buffer;
				var uBuffer = planes[1].Buffer;
				var vBuffer = planes[2].Buffer;

				if (yBuffer is null || uBuffer is null || vBuffer is null)
				{
					image.Close();
					return;
				}

				var ySize = yBuffer.Remaining();
				var uSize = uBuffer.Remaining();
				var vSize = vBuffer.Remaining();
				var totalSize = ySize + uSize + vSize;

				var data = scenario.Allocate(totalSize);
				
				yBuffer.Get(data, 0, ySize);
				uBuffer.Get(data, ySize, uSize);
				vBuffer.Get(data, ySize + uSize, vSize);

				cameraFrame = new CameraFrame(data, image.Width, image.Height, CameraFrameFormat.Yuv420, scenario.Free);
			}
			else
			{
				var buffer = planes[0].Buffer;
				if (buffer is null)
				{
					image.Close();
					return;
				}

				var data = scenario.Allocate(buffer.Remaining());
				buffer.Get(data, 0, buffer.Remaining());

				// TODO: Unsure of this format mapping
				var format = image.Format switch
				{
					(int)Android.Graphics.ImageFormatType.Yuv420888 => CameraFrameFormat.Yuv420,
					(int)Android.Graphics.Format.Rgba8888 => CameraFrameFormat.Rgba8888,
					_ => CameraFrameFormat.Unknown
				};

				cameraFrame = new CameraFrame(data, image.Width, image.Height, format, scenario.Free);
			}

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
