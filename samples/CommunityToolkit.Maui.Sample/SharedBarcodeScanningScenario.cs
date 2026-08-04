using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using ZXing;

namespace CommunityToolkit.Maui.Sample;

/// <summary>
/// A <see cref="FrameBasedCameraScenario"/> implementation that provides barcode scanning using raw frame data.
/// </summary>
public class SharedBarcodeScanningScenario : FrameBasedCameraScenario
{
	readonly BarcodeReaderGeneric barcodeReader = new()
	{
		Options = new ZXing.Common.DecodingOptions
		{
			PossibleFormats = [BarcodeFormat.QR_CODE, BarcodeFormat.CODE_128, BarcodeFormat.EAN_13, BarcodeFormat.EAN_8],
			TryHarder = true
		}
	};

	/// <summary>
	/// Backing BindableProperty for the <see cref="Command"/> property.
	/// </summary>
	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SharedBarcodeScanningScenario));

	/// <summary>
	/// The Command that should be executed when a barcode is detected.
	/// </summary>
	public ICommand? Command
	{
		get => (ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	/// <inheritdoc/>
	public override void OnFrameReceived(CameraFrame frame)
	{
		using (frame)
		{
			// If we needed to convert to a specific format, we could do it here:
			// using var convertedFrame = Convert(frame, CameraFrameFormat.Rgba8888);
			
			RGBLuminanceSource luminanceSource = frame.Format switch
			{
				CameraFrameFormat.Rgba8888 => new RGBLuminanceSource(frame.Data, frame.Width, frame.Height, RGBLuminanceSource.BitmapFormat.RGBA32),
				CameraFrameFormat.Bgra8888 => new RGBLuminanceSource(frame.Data, frame.Width, frame.Height, RGBLuminanceSource.BitmapFormat.BGRA32),
				_ => new RGBLuminanceSource(frame.Data, frame.Width, frame.Height) // Fallback to grayscale/luminance only
			};

			var result = barcodeReader.Decode(luminanceSource);

			if (result is not null)
			{
				if (Command?.CanExecute(result.Text) is true)
				{
					MainThread.BeginInvokeOnMainThread(() => Command.Execute(result.Text));
				}
			}
		}
	}
}