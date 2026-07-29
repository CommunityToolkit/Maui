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

			var data = new byte[bufferSize];
			System.Runtime.InteropServices.Marshal.Copy(baseAddress, data, 0, bufferSize);

			imageBuffer.Unlock(CVPixelBufferLock.ReadOnly);

			var cameraFrame = new CameraFrame(data, width, height);

			scenario.OnFrameReceived(cameraFrame);

			sampleBuffer.Dispose();
		}
	}
}
