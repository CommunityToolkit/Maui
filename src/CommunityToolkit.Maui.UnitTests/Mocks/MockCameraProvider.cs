using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockCameraProvider : ICameraProvider
{
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; private set; }

	public bool IsInitialized { get; private set; }

	public ValueTask InitializeAsync(CancellationToken token)
	{
		if (IsInitialized)
		{
			return ValueTask.CompletedTask;
		}
		IsInitialized = true;
		return RefreshAvailableCameras(token);
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