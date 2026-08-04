using AVFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;

namespace CommunityToolkit.Maui.Core;

public abstract partial class FrameBasedCameraScenario
{
	readonly Lazy<AVCaptureVideoDataOutput> videoDataOutput;

	/// <summary>
	/// Creates a new instance of <see cref="FrameBasedCameraScenario"/>.
	/// </summary>
	public FrameBasedCameraScenario()
	{
		videoDataOutput = new Lazy<AVCaptureVideoDataOutput>(() =>
		{
			var output = new AVCaptureVideoDataOutput();

			var pixelFormat = PreferredFormat switch
			{
				CameraFrameFormat.Rgba8888 => CVPixelFormatType.CV32RGBA,
				CameraFrameFormat.Bgra8888 => CVPixelFormatType.CV32BGRA,
				CameraFrameFormat.Yuv420BiPlanar => CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange,
				_ => (CVPixelFormatType?)null
			};

			if (pixelFormat.HasValue)
			{
				output.WeakVideoSettings = new NSDictionary(CVPixelBuffer.PixelFormatTypeKey, NSNumber.FromUInt32((uint)pixelFormat.Value));
			}
			
			output.SetSampleBufferDelegate(new VideoDataOutputDelegate(this), CoreFoundation.DispatchQueue.MainQueue);
			
			return output;
		});
	}

	/// <inheritdoc />
	public override AVCaptureOutput Output => videoDataOutput.Value ?? throw new InvalidOperationException("Scenario not attached");

	sealed class VideoDataOutputDelegate(FrameBasedCameraScenario scenario) : AVCaptureVideoDataOutputSampleBufferDelegate
	{
		public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
		{
			using var imageBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
			if (imageBuffer is null)
			{
				return;
			}

			imageBuffer.Lock(CVPixelBufferLock.ReadOnly);

			var width = (int)imageBuffer.Width;
			var height = (int)imageBuffer.Height;
			var baseAddress = imageBuffer.BaseAddress;
			var bytesPerRow = (int)imageBuffer.BytesPerRow;
			var bufferSize = bytesPerRow * height;

			var data = scenario.Allocate(bufferSize);
			System.Runtime.InteropServices.Marshal.Copy(baseAddress, data, 0, bufferSize);

			imageBuffer.Unlock(CVPixelBufferLock.ReadOnly);

			var format = imageBuffer.PixelFormatType switch
			{
				CVPixelFormatType.CV32BGRA => CameraFrameFormat.Bgra8888,
				CVPixelFormatType.CV32RGBA => CameraFrameFormat.Rgba8888,
				CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange => CameraFrameFormat.Yuv420BiPlanar,
				CVPixelFormatType.CV420YpCbCr8BiPlanarFullRange => CameraFrameFormat.Yuv420BiPlanar,
				_ => CameraFrameFormat.Unknown
			};

			var cameraFrame = new CameraFrame(data, width, height, format, scenario.Free);

			scenario.OnFrameReceived(cameraFrame);

			sampleBuffer.Dispose();
		}
	}
}
