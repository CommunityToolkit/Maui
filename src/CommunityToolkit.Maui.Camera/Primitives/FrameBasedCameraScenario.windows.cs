using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace CommunityToolkit.Maui.Core;

public abstract partial class FrameBasedCameraScenario
{
	MediaFrameReader? frameReader;

	/// <inheritdoc />
	public override async Task OnAttached(MediaCapture mediaCapture)
	{
		await base.OnAttached(mediaCapture);

		var source = mediaCapture.FrameSources.Values.FirstOrDefault(s => s.Info.SourceKind == MediaFrameSourceKind.Color);
		if (source is null)
		{
			return;
		}

		var preferredFormat = source.SupportedFormats.FirstOrDefault(f => f.Subtype == "NV12") ?? source.SupportedFormats.FirstOrDefault();
		if (preferredFormat is null)
		{
			return;
		}

		frameReader = await mediaCapture.CreateFrameReaderAsync(source, preferredFormat.Subtype);
		frameReader.FrameArrived += OnFrameArrived;
		await frameReader.StartAsync();
	}

	/// <inheritdoc />
	public override void OnDetached()
	{
		base.OnDetached();

		if (frameReader != null)
		{
			frameReader.FrameArrived -= OnFrameArrived;
			frameReader.Dispose();
			frameReader = null;
		}
	}

	void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
	{
		using var frame = sender.TryAcquireLatestFrame();
		var videoFrame = frame?.VideoMediaFrame;
		var softwareBitmap = videoFrame?.SoftwareBitmap;

		if (softwareBitmap is null)
		{
			return;
		}

		// Windows often gives us BGRA8, which is easy to convert to byte array.
		// For simplicity in this common implementation, we'll try to get it to a standard format if needed.
		// However, the current CameraFrame just takes a byte[].

		// To get raw bytes from SoftwareBitmap:
		var width = softwareBitmap.PixelWidth;
		var height = softwareBitmap.PixelHeight;
		var data = new byte[4 * width * height];
		softwareBitmap.CopyByteArrayToBuffer(data.AsBuffer());

		var cameraFrame = new CameraFrame(data, width, height);

		OnFrameReceived(cameraFrame);
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
		{
			if (frameReader != null)
			{
				frameReader.FrameArrived -= OnFrameArrived;
				frameReader.Dispose();
				frameReader = null;
			}
		}
	}
}
