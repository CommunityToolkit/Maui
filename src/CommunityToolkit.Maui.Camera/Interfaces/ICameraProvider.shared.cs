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
	/// Refreshes the <see cref="AvailableCameras"/> on device, regardless of the current initialization state.
	/// </summary>
	/// <remarks>
	/// This ensures the list is up to date if available cameras have changed after initialization.
	/// </remarks>
	/// <param name="token"></param>
	/// <returns></returns>
	[MemberNotNull(nameof(AvailableCameras))]
	Task RefreshAvailableCameras(CancellationToken token);

	/// <summary>
	/// Initialize the camera provider by refreshing the <see cref="AvailableCameras"/>. 
	/// This performs a one-time discovery of available cameras. Subsequent calls are no-ops unless initialization failed previously.
	/// </summary>
	/// <remarks>
	/// To force a refresh after the provider is initialized, call <see cref="RefreshAvailableCameras(CancellationToken)"/>.
	/// </remarks>
	ValueTask InitializeAsync(CancellationToken token);

}