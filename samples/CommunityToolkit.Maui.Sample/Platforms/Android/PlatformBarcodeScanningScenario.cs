using AndroidX.Camera.Core;

namespace CommunityToolkit.Maui.Sample;

/// <summary>
/// Android based implementation of <see cref="PlatformBarcodeScanningScenario"/>.
/// </summary>
public partial class PlatformBarcodeScanningScenario
{
	readonly Lazy<UseCase>? lazyUseCase = null;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="PlatformBarcodeScanningScenario"/> class.
	/// </summary>
	public PlatformBarcodeScanningScenario()
	{
		lazyUseCase = new Lazy<UseCase>(() =>
		{
			var imageAnalysis =
				new ImageAnalysis.Builder()
					.SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
					.Build();

			if (imageAnalysis.BackgroundExecutor is null)
			{
				throw new InvalidOperationException("Background executor must be set before use case.");
			}

			imageAnalysis.SetAnalyzer(imageAnalysis.BackgroundExecutor, new BarcodeAnalyzer(() => Command));
		
			return imageAnalysis;
		});
	}
	
	/// <inheritdoc/>
	public override UseCase UseCase => lazyUseCase?.Value!;
}