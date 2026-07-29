using AndroidX.Camera.Core;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Android based implementation of <see cref="PlatformCameraScenario"/>.
/// </summary>
partial class PlatformCameraScenario : CameraScenario
{
	/// <summary>
	/// Gets the <see cref="UseCase"/> for this scenario.
	/// </summary>
	public abstract UseCase UseCase { get; }
}
