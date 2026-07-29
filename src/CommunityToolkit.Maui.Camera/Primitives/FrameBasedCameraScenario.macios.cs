using AVFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;

namespace CommunityToolkit.Maui.Core;

public abstract partial class FrameBasedCameraScenario
{
	AVCaptureVideoDataOutput? videoDataOutput;

	/// <inheritdoc />
	public override void OnAttached()
	{
		base.OnAttached();

		if (videoDataOutput is not null)
		{
			videoDataOutput.Dispose();
		}

		videoDataOutput = new AVCaptureVideoDataOutput();
		videoDataOutput.SetSampleBufferDelegate(new VideoDataOutputDelegate(this), CoreFoundation.DispatchQueue.MainQueue);
	}

	/// <inheritdoc />
	public override void OnDetached()
	{
		base.OnDetached();

		if (videoDataOutput is not null)
		{
			videoDataOutput.Dispose();
			videoDataOutput = null;
		}
	}

	/// <inheritdoc />
	public override AVCaptureOutput Output => videoDataOutput ?? throw new InvalidOperationException("Scenario not attached");

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

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
		{
			videoDataOutput?.Dispose();
			videoDataOutput = null;
		}
	}
}
