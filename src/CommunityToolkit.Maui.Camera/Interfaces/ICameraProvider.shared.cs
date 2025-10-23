using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Interface to retrieve available cameras
/// </summary>
public interface ICameraProvider
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
	/// <param name="token"></param>
	/// <returns></returns>
	[MemberNotNull(nameof(AvailableCameras))]
	ValueTask RefreshAvailableCameras(CancellationToken token);
}