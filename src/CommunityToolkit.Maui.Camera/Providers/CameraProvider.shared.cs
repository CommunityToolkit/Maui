namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Implementation that provides the ability to discover cameras that are attached to the current device.
/// </summary>
sealed partial class CameraProvider : ICameraProvider
{
	readonly WeakEventManager availableCamerasChangedEventManager = new();
	readonly SemaphoreSlim refreshAvailableCamerasSemaphore = new(1, 1);

	Task? refreshAvailableCamerasTask;

	public event EventHandler<IReadOnlyList<CameraInfo>?> AvailableCamerasChanged
	{
		add => availableCamerasChangedEventManager.AddEventHandler(value);
		remove => availableCamerasChangedEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc/>
	public IReadOnlyList<CameraInfo>? AvailableCameras
	{
		get;
		private set
		{
			if (!AreCameraInfoListsEqual(field, value))
			{
				field = value;
				availableCamerasChangedEventManager.HandleEvent(this, value, nameof(AvailableCamerasChanged));
			}
		}
	}

	public void Dispose()
	{
		refreshAvailableCamerasSemaphore.Dispose();
	}

	/// <inheritdoc/>
	public async Task RefreshAvailableCameras(CancellationToken token)
	{
		Task refreshTask;

		await refreshAvailableCamerasSemaphore.WaitAsync(token);

		try
		{
			if (refreshAvailableCamerasTask is null || refreshAvailableCamerasTask.IsCompleted)
			{
				// Every caller that arrives while this refresh runs shares it, so the token of the first caller must not cancel it:
				// each caller's token only cancels that caller's wait below
				refreshAvailableCamerasTask = PlatformRefreshAvailableCameras(CancellationToken.None).AsTask();
			}

			refreshTask = refreshAvailableCamerasTask;
		}
		finally
		{
			refreshAvailableCamerasSemaphore.Release();
		}

		await refreshTask.WaitAsync(token);
	}

	internal static bool AreCameraInfoListsEqual(in IReadOnlyList<CameraInfo>? cameraInfoList1, in IReadOnlyList<CameraInfo>? cameraInfoList2)
	{
		if (cameraInfoList1 is null && cameraInfoList2 is null)
		{
			return true;
		}

		if (cameraInfoList1 is null || cameraInfoList2 is null)
		{
			return false;
		}

		var cameraInfosInList1ButNotInList2 = cameraInfoList1.Except(cameraInfoList2).ToList();
		var cameraInfosInList2ButNotInList1 = cameraInfoList2.Except(cameraInfoList1).ToList();

		return cameraInfosInList1ButNotInList2.Count is 0 && cameraInfosInList2ButNotInList1.Count is 0;
	}

	private partial ValueTask PlatformRefreshAvailableCameras(CancellationToken token);
}