using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Sample;

/// <summary>
/// A <see cref="FrameBasedCameraScenario"/> implementation that provides barcode scanning using raw frame data.
/// </summary>
public class SharedBarcodeScanningScenario : FrameBasedCameraScenario
{
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
		// This is where common barcode scanning logic would go.
		// For example, using ZXing.Net to process the raw byte array in frame.Data.
		// To avoid adding a heavy dependency to the core library for this sample,
		// we'll just demonstrate the frame receiving capability.
		System.Diagnostics.Trace.WriteLine($"Frame received: {frame.Width}x{frame.Height}");
	}
}