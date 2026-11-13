using System.Windows.Input;
using AVFoundation;
using CoreFoundation;

namespace CommunityToolkit.Maui.Sample;

/// <summary>
/// Apple based implementation of <see cref="PlatformBarcodeScanningScenario"/>.
/// </summary>
public partial class PlatformBarcodeScanningScenario
{
	readonly Lazy<AVCaptureMetadataOutput> lazyOutput;

	/// <summary>
	/// Initializes a new instance of the <see cref="PlatformBarcodeScanningScenario"/> class.
	/// </summary>
	public PlatformBarcodeScanningScenario()
	{
		lazyOutput = new Lazy<AVCaptureMetadataOutput>(() =>
		{
			var output = new AVCaptureMetadataOutput();

			output.SetDelegate(new BarcodeDetectionDelegate(() => Command), DispatchQueue.MainQueue);

			return output;
		});
	}

	/// <inheritdoc/>
	public override AVCaptureOutput Output => lazyOutput.Value;

	/// <inheritdoc/>
	public override void OnAttached()
	{
		// Must apply this here once the output has been attached to the capture session.
		lazyOutput.Value.MetadataObjectTypes = lazyOutput.Value.AvailableMetadataObjectTypes;
	}
}

sealed class BarcodeDetectionDelegate(Func<ICommand?> commandProvider) : AVCaptureMetadataOutputObjectsDelegate
{
	public override void DidOutputMetadataObjects(
		AVCaptureMetadataOutput captureOutput,
		AVMetadataObject[] metadataObjects,
		AVCaptureConnection connection)
	{
		foreach (var metadataObject in metadataObjects)
		{
			if (metadataObject is AVMetadataMachineReadableCodeObject readableObject)
			{
				var code = readableObject.StringValue;
					
				Console.WriteLine($"Metadata object {code} at {string.Join(",", readableObject.Corners?? [])}");

				var invokeCommand = commandProvider.Invoke(); 

				if (invokeCommand?.CanExecute(readableObject.StringValue) is true)
				{
					invokeCommand.Execute(readableObject.StringValue);
				}
			}
		}
	}
}