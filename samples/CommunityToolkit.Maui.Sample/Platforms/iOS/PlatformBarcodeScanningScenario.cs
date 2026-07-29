using System.Windows.Input;
using AVFoundation;
using CoreFoundation;

namespace CommunityToolkit.Maui.Sample;

/// <summary>
/// Apple based implementation
/// </summary>
public partial class PlatformBarcodeScanningScenario
{
	readonly Lazy<AVCaptureMetadataOutput> lazyOutput;

	public PlatformBarcodeScanningScenario()
	{
		lazyOutput = new Lazy<AVCaptureMetadataOutput>(() =>
		{
			var output = new AVCaptureMetadataOutput();

			output.SetDelegate(new BarcodeDetectionDelegate(() => Command), DispatchQueue.MainQueue);

			return output;
		});
	}

	public override AVCaptureOutput Output => lazyOutput.Value;

	public override void OnAttached()
	{
		// Must apply this here once the output has been attached to the capture session.
		lazyOutput.Value.MetadataObjectTypes = lazyOutput.Value.AvailableMetadataObjectTypes;
	}
}

sealed class BarcodeDetectionDelegate(Func<ICommand?> command) : AVCaptureMetadataOutputObjectsDelegate
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

				var invokeCommand = command.Invoke(); 

				if (invokeCommand?.CanExecute(readableObject.StringValue) is true)
				{
					invokeCommand.Execute(readableObject.StringValue);
				}
			}
		}
	}
}