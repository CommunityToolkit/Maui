namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Base class for platform-specific camera scenarios.
/// </summary>
public abstract partial class PlatformCameraScenario : CameraScenario
{
	/// <summary>
	/// Called when the scenario is detached from the camera.
	/// </summary>
	public virtual void OnDetached()
	{
	}
}
