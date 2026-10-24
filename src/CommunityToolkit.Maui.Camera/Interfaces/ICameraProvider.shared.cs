using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Interface to retrieve available cameras
/// </summary>
public interface ICameraProvider : IDisposable
{
	/// <summary>
	/// Event fires when the contents <see cref="AvailableCameras"/> has changed
	/// </summary>
	event EventHandler<IReadOnlyList<CameraInfo>?> AvailableCamerasChanged;

	/// <summary>
	/// Cameras available on device
	/// </summary>
	/// <remarks>
	/// List is initialized using <see cref="RefreshAvailableCameras(CancellationToken)"/>
	/// </remarks>
	IReadOnlyList<CameraInfo>? AvailableCameras { get; }

	/// <summary>
	/// Assigns <see cref="AvailableCameras"/> with the cameras available on device
	/// </summary>
	/// <remarks>
	/// On Android, when CameraX cannot initialize (for example when the camera service lists fewer cameras than the device declares), the
	/// returned <see cref="Task"/> fails with the exception in which CameraX reports it (a <c>Java.Util.Concurrent.ExecutionException</c>
	/// whose cause is an <c>AndroidX.Camera.Core.InitializationException</c>), and <see cref="AvailableCameras"/> keeps its value.
	/// </remarks>
	/// <param name="token"></param>
	/// <returns></returns>
	[MemberNotNull(nameof(AvailableCameras))]
	Task RefreshAvailableCameras(CancellationToken token);
}