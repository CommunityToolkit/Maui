using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public sealed class MockCameraProvider : ICameraProvider, IDisposable
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

	public void Dispose()
	{

	}

	public Task RefreshAvailableCameras(CancellationToken token)
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

		return Task.CompletedTask;
	}
}