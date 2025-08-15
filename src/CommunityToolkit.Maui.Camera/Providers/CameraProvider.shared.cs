namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Implementation that provides the ability to discover cameras that are attached to the current device.
/// </summary>
partial class CameraProvider : ICameraProvider
{
	readonly Lock refreshLock = new();
	Task? refreshTask;

	internal partial ValueTask PlatformRefreshAvailableCameras(CancellationToken token);

	/// <inheritdoc/>
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; private set; }

	/// <inheritdoc/>
	public bool IsInitialized => refreshTask?.IsCompletedSuccessfully is true;

	ValueTask GetRefreshTask(CancellationToken token)
	{
		if (refreshTask is null || refreshTask.IsCompleted)
		{
			refreshTask = PlatformRefreshAvailableCameras(token).AsTask();
		}

		return new ValueTask(refreshTask);
	}

	/// <inheritdoc/>	
	public ValueTask InitializeAsync(CancellationToken token)
	{
		lock (refreshLock)
		{
			if (IsInitialized)
			{
				return ValueTask.CompletedTask;
			}

			return GetRefreshTask(token);
		}
	}

	/// <inheritdoc/>
	public ValueTask RefreshAvailableCameras(CancellationToken token)
	{
		lock (refreshLock)
		{
			return GetRefreshTask(token);
		}
	}

}
