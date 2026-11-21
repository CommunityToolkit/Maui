using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Graphics.Imaging;
using ZXing;
using ZXing.Windows.Compatibility;

namespace CommunityToolkit.Maui.Sample;

/// <summary>
/// Windows based implementation of <see cref="PlatformBarcodeScanningScenario"/>.
/// </summary>
public partial class PlatformBarcodeScanningScenario : IDisposable
{
	readonly BarcodeReader barcodeReader = new()
	{
		Options = new ZXing.Common.DecodingOptions
		{
			PossibleFormats = [BarcodeFormat.QR_CODE, BarcodeFormat.CODE_128, BarcodeFormat.EAN_13, BarcodeFormat.EAN_8],
			TryHarder = true
		}
	};

	MediaFrameReader? frameReader;

	/// <inheritdoc/>
	public override async Task OnAttached(MediaCapture mediaCapture)
	{
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

	void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
	{
		using var frame = sender.TryAcquireLatestFrame();
		var videoFrame = frame?.VideoMediaFrame;
		var softwareBitmap = videoFrame?.SoftwareBitmap;

		if (softwareBitmap is null)
		{
			return;
		}

		if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
		    softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Ignore)
		{
			softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
		}

		var result = barcodeReader.Decode(softwareBitmap);
		if (result != null)
		{
			if (Command?.CanExecute(result.Text) is true)
			{
				MainThread.BeginInvokeOnMainThread(() => Command.Execute(result.Text));
			}
		}
	}

	/// <inheritdoc/>
	public override void OnDetached()
	{
		Dispose();
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (frameReader != null)
		{
			frameReader.FrameArrived -= OnFrameArrived;
			frameReader.Dispose();
			frameReader = null;
		}
	}
}
