namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Implementation that provides the ability to discover cameras that are attached to the current device.
/// </summary>
partial class CameraProvider : ICameraProvider
{
	readonly WeakEventManager availableCamerasChangedEventManager = new();

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

	/// <inheritdoc/>
	public partial ValueTask RefreshAvailableCameras(CancellationToken token);

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
}