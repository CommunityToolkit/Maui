using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Interface to retrieve available cameras
/// </summary>
public interface ICameraProvider
{
	/// <summary>
	/// Cameras available on device
	/// </summary>
	/// <remarks>
	/// List is initialized using <see cref="InitializeAsync"/>, and can be refreshed using <see cref="RefreshAvailableCameras(CancellationToken)"/>
	/// </remarks>
	IReadOnlyList<CameraInfo>? AvailableCameras { get; }

	/// <summary>
	/// Refreshes <see cref="AvailableCameras"/> with the cameras available on device
	/// </summary>
	/// <remarks>
	/// Only call this method when the available cameras on device has been updated, as the list is automatically initialized with <see cref="InitializeAsync"/>
	/// </remarks>
	/// <param name="token"></param>
	/// <returns></returns>
	[MemberNotNull(nameof(AvailableCameras))]
	ValueTask RefreshAvailableCameras(CancellationToken token);

	/// <summary>
	/// The <see cref="Task"/> to initialize the available cameras list. This task is automatically started when the <see cref="ICameraProvider"/> is created.
	/// </summary>
	Task InitializeAsync { get; }

}