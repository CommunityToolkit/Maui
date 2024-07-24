using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockCameraProvider : ICameraProvider
{
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; private set; }

	public ValueTask RefreshAvailableCameras(CancellationToken token)
	{
		AvailableCameras = new[]
		{
			new CameraInfo("Test Camera",
				Guid.NewGuid().ToString(),
				CameraPosition.Front,
				false,
				1.0f,
				5.0f,
				new[]
				{
					new Size(1920, 1080)
				})
		};

		return ValueTask.CompletedTask;
	}
}