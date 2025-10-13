using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockCameraProvider : ICameraProvider
{
	public event EventHandler<IReadOnlyList<CameraInfo>?>? AvailableCamerasChanged;

	public IReadOnlyList<CameraInfo>? AvailableCameras
	{
		get;
		private set
		{
			if (!CameraProvider.AreCameraInfoListsEqual(field, value))
			{
				field = value;
				AvailableCamerasChanged?.Invoke(this, value);
			}
		}
	}

	public ValueTask RefreshAvailableCameras(CancellationToken token)
	{
		AvailableCameras =
		[
			new CameraInfo("Test Camera",
				Guid.NewGuid().ToString(),
				CameraPosition.Front,
				false,
				1.0f,
				5.0f,
				[
					new Size(1920, 1080)
				])
		];

		return ValueTask.CompletedTask;
	}
}