using AndroidX.Camera.Core;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
partial class PlatformCameraScenario : CameraScenario
{
	/// <summary>
	/// 
	/// </summary>
	/// <returns></returns>
	public abstract UseCase UseCase { get; }
}