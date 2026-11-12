// Qualifying 'Android.App.Application' inside this namespace would resolve against the
// 'CommunityToolkit.Maui.Android' namespace the MediaElement source generator emits, so it
// needs 'global::'. The bare 'Application' name would also be ambiguous with
// Microsoft.Maui.Controls.Application. The analyzer callback updates the UI, so it runs on
// the app's main executor (as CameraManager.android.cs does for its camera callbacks).
using AndroidX.Camera.Core;
using AndroidX.Core.Content;

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

			imageAnalysis.SetAnalyzer(ContextCompat.GetMainExecutor(global::Android.App.Application.Context)!, new BarcodeAnalyzer(() => Command));
		
			return imageAnalysis;
		});
	}
	
	/// <inheritdoc/>
	public override UseCase UseCase => lazyUseCase?.Value!;
}