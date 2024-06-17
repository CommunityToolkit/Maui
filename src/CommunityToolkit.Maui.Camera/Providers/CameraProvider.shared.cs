namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Implementation that provides the ability to discover cameras that are attached to the current device.
/// </summary>
partial class CameraProvider : ICameraProvider
{
	/// <inheritdoc/>
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; private set; }

	/// <inheritdoc/>
	public partial ValueTask RefreshAvailableCameras(CancellationToken token);
}