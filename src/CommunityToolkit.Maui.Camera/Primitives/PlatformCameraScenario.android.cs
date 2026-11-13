using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using Java.Util.Concurrent;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Android based implementation of <see cref="PlatformCameraScenario"/>.
/// </summary>
public abstract partial class PlatformCameraScenario : CameraScenario
{
	/// <summary>
	/// Called when the scenario is attached to the camera.
	/// </summary>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	public virtual Task OnAttached(IExecutorService? cameraExecutor, ResolutionSelector? resolutionSelector) => Task.CompletedTask;
	
	/// <summary>
	/// Gets the <see cref="UseCase"/> for this scenario.
	/// </summary>
	public abstract UseCase UseCase { get; }
}
