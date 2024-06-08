namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Implementation that provides the ability to discover cameras that are attached to the current device.
/// </summary>
partial class CameraProvider : ICameraProvider
{
	/// <summary>
	/// Gets the cameras that are connected to the current device.
	/// </summary>
	/// <remarks>
	/// If <see langword="null"/>, use <see cref="RefreshAvailableCameras"/> to initialize list.
	/// </remarks>
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; private set; }

	/// <summary>
	/// Refreshes the <see cref="AvailableCameras"/> that are connected to the current device.
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public partial ValueTask RefreshAvailableCameras(CancellationToken token);
}