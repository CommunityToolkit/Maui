using Windows.Media.Capture;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Windows based implementation of <see cref="PlatformCameraScenario"/>.
/// </summary>
partial class PlatformCameraScenario : CameraScenario
{
	/// <summary>
	/// Called when the scenario is attached to the camera.
	/// </summary>
	/// <param name="mediaCapture">The <see cref="MediaCapture"/> instance.</param>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	public abstract Task OnAttached(MediaCapture mediaCapture);
}
