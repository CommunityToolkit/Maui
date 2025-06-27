namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Implementation that provides the ability to discover cameras that are attached to the current device.
/// </summary>
partial class CameraProvider : ICameraProvider
{
	readonly Lock initializationLock = new();
	Task? initializationTask;

	internal partial ValueTask PlatformRefreshAvailableCameras(CancellationToken token);

	/// <inheritdoc/>
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; private set; }

	/// <inheritdoc/>
	public bool IsInitialized => initializationTask?.IsCompletedSuccessfully is true;

	bool IsNotRefreshing => initializationTask is null || initializationTask.IsCompleted;

	/// <inheritdoc/>	
	public ValueTask InitializeAsync(CancellationToken token)
	{
		lock (initializationLock)
		{
			if (IsInitialized)
			{
				return ValueTask.CompletedTask;
			}

			if (IsNotRefreshing)
			{
				initializationTask = PlatformRefreshAvailableCameras(token).AsTask();
			}

			return new ValueTask(initializationTask!);
		}
	}

	/// <inheritdoc/>
	public ValueTask RefreshAvailableCameras(CancellationToken token)
	{
		lock (initializationLock)
		{
			if (IsNotRefreshing)
			{
				initializationTask = PlatformRefreshAvailableCameras(token).AsTask();
			}

			return new ValueTask(initializationTask!);
		}
	}

}
