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
	/// Gets a value indicating whether the camera provider has been successfully initialized.
	/// </summary>
	bool IsInitialized { get; }

	/// <summary>
	/// Refreshes <see cref="AvailableCameras"/> with the cameras available on device.
	/// </summary>
	/// <param name="token"></param>
	/// <returns></returns>
	[MemberNotNull(nameof(AvailableCameras))]
	Task RefreshAvailableCameras(CancellationToken token);

	/// <summary>
	/// Initialize the camera provider by refreshing the <see cref="AvailableCameras"/>. 
	/// </summary>
	/// <remarks>
	/// If the provider is already initialized, the <see cref="AvailableCameras"/> will not be refreshed again until <see cref="RefreshAvailableCameras(CancellationToken)"/> is called,
	/// and this method will return a <see cref="ValueTask.CompletedTask"/>.
	/// </remarks>
	ValueTask InitializeAsync(CancellationToken token);

}