using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockCameraProvider : ICameraProvider
{
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; private set; }

	public bool IsInitialized { get; private set; }

	public async ValueTask InitializeAsync(CancellationToken token)
	{
		if (!IsInitialized)
		{
			await RefreshAvailableCameras(token);
			IsInitialized = true;
		}
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