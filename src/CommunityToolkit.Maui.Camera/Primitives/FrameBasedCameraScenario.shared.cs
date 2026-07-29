namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A <see cref="PlatformCameraScenario"/> that receives raw frames from the camera.
/// </summary>
public abstract partial class FrameBasedCameraScenario : PlatformCameraScenario
{
	/// <summary>
	/// Called when a new frame is received from the camera.
	/// </summary>
	/// <param name="frame">The camera frame.</param>
	public virtual void OnFrameReceived(CameraFrame frame)
	{
	}
}
