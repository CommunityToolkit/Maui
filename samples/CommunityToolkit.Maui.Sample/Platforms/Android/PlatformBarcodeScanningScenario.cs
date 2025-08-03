using AndroidX.Camera.Core;

namespace CommunityToolkit.Maui.Sample;

public partial class PlatformBarcodeScanningScenario
{
	readonly Lazy<UseCase> lazyUseCase;
	
	public PlatformBarcodeScanningScenario()
	{
		lazyUseCase = new Lazy<UseCase>(() =>
		{
			var imageAnalysis = ImageAnalysis.Builder
				// enable the following line if RGBA output is needed.
				// .setOutputImageFormat(ImageAnalysis.OUTPUT_IMAGE_FORMAT_RGBA_8888)
				.SetTargetResolution(new Size(1280, 720))
				.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
				.Build();
			// imageAnalysis.SetAnalyzer(executor, ImageAnalysis.Analyzer {
			// 	imageProxy->
			// 		val rotationDegrees = imageProxy.imageInfo.rotationDegrees
			// 		// insert your code here.
			// 		...
			// 	// after done, release the ImageProxy object
			// 	imageProxy.close();
			// });
		
			return imageAnalysis;
		});
	}
	
	public override UseCase UseCase => lazyUseCase.Value;
}