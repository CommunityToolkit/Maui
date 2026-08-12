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

		var preferredFormatName = PreferredFormat switch
		{
			CameraFrameFormat.Nv12 => "NV12",
			CameraFrameFormat.Bgra8888 => "BGRA8",
			CameraFrameFormat.Rgba8888 => "RGBA8",
			CameraFrameFormat.Yuv420 => "YUY2",
			_ => null
		};

		var preferredFormat = source.SupportedFormats.FirstOrDefault(f => f.Subtype == preferredFormatName) 
		                      ?? source.SupportedFormats.FirstOrDefault(f => f.Subtype == "NV12") 
		                      ?? source.SupportedFormats.FirstOrDefault();
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
		var size = 4 * width * height;
		var data = Allocate(size);
		softwareBitmap.CopyByteArrayToBuffer(data.AsBuffer(0, size));

		var format = softwareBitmap.BitmapPixelFormat switch
		{
			Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 => CameraFrameFormat.Bgra8888,
			Windows.Graphics.Imaging.BitmapPixelFormat.Rgba8 => CameraFrameFormat.Rgba8888,
			Windows.Graphics.Imaging.BitmapPixelFormat.Nv12 => CameraFrameFormat.Nv12,
			Windows.Graphics.Imaging.BitmapPixelFormat.Yuy2 => CameraFrameFormat.Yuv420, // Not exactly same but close for this enum
			_ => CameraFrameFormat.Unknown
		};

		var cameraFrame = new CameraFrame(data, width, height, format, Free);

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
