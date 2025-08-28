namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Implementation that provides the ability to discover cameras that are attached to the current device.
/// </summary>
partial class CameraProvider : ICameraProvider, IDisposable
{
	readonly SemaphoreSlim refreshAvailableCamerasSemaphore = new(1, 1);
	Task? refreshAvailableCamerasTask;

	~CameraProvider()
	{
		Dispose(false);
	}

	/// <inheritdoc/>
	public bool IsInitialized => refreshAvailableCamerasTask?.IsCompletedSuccessfully is true;

	/// <inheritdoc/>
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; private set; }

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>	
	public async ValueTask InitializeAsync(CancellationToken token)
	{
		if (!IsInitialized)
		{
			await RefreshAvailableCameras(token);
		}
	}

	/// <inheritdoc/>
	public async Task RefreshAvailableCameras(CancellationToken token)
	{
		await refreshAvailableCamerasSemaphore.WaitAsync(token);
		
		try
		{
			if (refreshAvailableCamerasTask is null || refreshAvailableCamerasTask.IsCompleted)
			{
				refreshAvailableCamerasTask = PlatformRefreshAvailableCameras(token).AsTask();
			}

			await refreshAvailableCamerasTask;
		}
		finally
		{
			refreshAvailableCamerasSemaphore.Release();
		}
	}

	void Dispose(bool disposing)
	{
		if (disposing)
		{
			refreshAvailableCamerasSemaphore.Dispose();

			refreshAvailableCamerasTask?.Dispose();
			refreshAvailableCamerasTask = null;
		}
	}
	
	private partial ValueTask PlatformRefreshAvailableCameras(CancellationToken token);
}