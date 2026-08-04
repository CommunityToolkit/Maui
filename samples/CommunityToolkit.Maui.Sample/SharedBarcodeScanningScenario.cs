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
	/// Initializes a new instance of the <see cref="SharedBarcodeScanningScenario"/> class.
	/// </summary>
	public SharedBarcodeScanningScenario()
	{
		// Request BGRA8888 as it is commonly supported and easy to work with in ZXing
		PreferredFormat = CameraFrameFormat.Bgra8888;
	}

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

	// Example to highlight how you can convert from one format to another. This method should only be executed
	// for Android where we don't currently support the BGRA8888 format.
	/// <inheritdoc/>
	public override CameraFrame Convert(CameraFrame frame, CameraFrameFormat format)
	{
		if (frame.Format == CameraFrameFormat.Rgba8888 && format == CameraFrameFormat.Bgra8888)
		{
			var data = Allocate(frame.Data.Length);
			Array.Copy(frame.Data, data, frame.Data.Length);

			// RGBA to BGRA: Swap R and B channels
			for (var i = 0; i < frame.Data.Length; i += 4)
			{
				(data[i], data[i + 2]) = (data[i + 2], data[i]);
			}

			return CreateFrame(data, frame.Width, frame.Height, CameraFrameFormat.Bgra8888, Free);
		}

		return base.Convert(frame, format);
	}

	/// <inheritdoc/>
	public override void OnFrameReceived(CameraFrame frame)
	{
		using var convertedFrame = Convert(frame, CameraFrameFormat.Bgra8888);
		using (frame)
		{
			RGBLuminanceSource luminanceSource = convertedFrame.Format switch
			{
				CameraFrameFormat.Rgba8888 => new RGBLuminanceSource(convertedFrame.Data, convertedFrame.Width, convertedFrame.Height, RGBLuminanceSource.BitmapFormat.RGBA32),
				CameraFrameFormat.Bgra8888 => new RGBLuminanceSource(convertedFrame.Data, convertedFrame.Width, convertedFrame.Height, RGBLuminanceSource.BitmapFormat.BGRA32),
				_ => new RGBLuminanceSource(convertedFrame.Data, convertedFrame.Width, convertedFrame.Height) // Fallback to grayscale/luminance only
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