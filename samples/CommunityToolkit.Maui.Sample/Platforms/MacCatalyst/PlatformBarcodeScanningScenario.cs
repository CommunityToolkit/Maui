using System.Windows.Input;
using AVFoundation;
using CoreFoundation;

namespace CommunityToolkit.Maui.Sample;

/// <summary>
/// Apple based implementation
/// </summary>
public partial class PlatformBarcodeScanningScenario
{
	protected override AVCaptureOutput CreateOutput()
	{
		var output = new AVCaptureMetadataOutput();
		
		output.SetDelegate(new BarcodeDetectionDelegate(Command), DispatchQueue.MainQueue);
		output.MetadataObjectTypes =
			AVMetadataObjectType.QRCode | AVMetadataObjectType.EAN13Code;
		
		return output;
	}
}

sealed class BarcodeDetectionDelegate : AVCaptureMetadataOutputObjectsDelegate
{
	readonly ICommand command;

	public BarcodeDetectionDelegate(ICommand command)
	{
		this.command = command;
	}
	
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

				if (this.command.CanExecute(readableObject.StringValue))
				{
					this.command.Execute(readableObject.StringValue);
				}
			}
		}
	}
}