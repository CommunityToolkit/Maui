using System.Diagnostics;
using System.Windows.Input;
using Android.Gms.Tasks;
using Android.Runtime;
using AndroidX.Camera.Core;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Common;

using Scanner = Xamarin.Google.MLKit.Vision.BarCode.BarcodeScanning;

namespace CommunityToolkit.Maui.Sample;

public class BarcodeAnalyzer(Func<ICommand?> commandProvider) : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
	IBarcodeScanner? barcodeScanner;
	readonly Lock resultsLock = new();

	internal void UpdateSymbologies()
	{
		barcodeScanner?.Dispose();
		barcodeScanner = Scanner.GetClient(new BarcodeScannerOptions.Builder()
			.SetBarcodeFormats(Barcode.FormatAllFormats)
			.Build());
	}
	
	public void Analyze(IImageProxy proxy)
	{
		try
		{
			ArgumentNullException.ThrowIfNull(proxy?.Image);
			ArgumentNullException.ThrowIfNull(barcodeScanner);

			using var inputImage = InputImage.FromMediaImage(proxy.Image, 0);
			using var task  = barcodeScanner.Process(inputImage);
			var result = TasksClass.Await(task);

			lock (resultsLock)
			{
				ProcessResults(result);
			}

			result?.Dispose();
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}
		finally
		{
			try
			{
				proxy?.Close();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}
	}
	
	void ProcessResults(Java.Lang.Object? result)
	{
		if (result is not JavaList javaList)
		{
			return;
		}

		if (commandProvider.Invoke() is not Command command)
		{
			return;
		}

		foreach (Barcode barcode in javaList)
		{
			if (barcode is null)
			{
				continue;
			}

			if (string.IsNullOrEmpty(barcode.DisplayValue) && string.IsNullOrEmpty(barcode.RawValue))
			{
				continue;
			}

			if (command.CanExecute(barcode.RawValue) is true)
			{
				command.Execute(barcode.RawValue);
			}
		}
	}
}