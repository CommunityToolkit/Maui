using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CameraInfoWindowsTests : CameraHandlerTestBase
{
	[Fact]
	public void ConstructorKeepsTheFrameSourceGroupAndTheImageEncodingProperties()
	{
		var small = CreateJpeg(640, 480);
		var large = CreateJpeg(1920, 1080);

		var cameraInfo = new CameraInfo("Name", "device-id", CameraPosition.Front, true, 1f, 5f, [new Size(1920, 1080), new Size(640, 480)], null!, [large, small]);

		Assert.Null(cameraInfo.FrameSourceGroup);
		Assert.Equal([large, small], cameraInfo.ImageEncodingProperties);
		Assert.Equal("Name", cameraInfo.Name);
		Assert.Equal("device-id", cameraInfo.DeviceId);
		Assert.Equal(CameraPosition.Front, cameraInfo.Position);
		Assert.True(cameraInfo.IsFlashSupported);
		Assert.Equal(1f, cameraInfo.MinimumZoomFactor);
		Assert.Equal(5f, cameraInfo.MaximumZoomFactor);
		Assert.Equal([new Size(1920, 1080), new Size(640, 480)], cameraInfo.SupportedResolutions);
	}

	[Fact]
	public void ImageEncodingPropertiesAreACopyOfTheSuppliedProperties()
	{
		var supplied = new List<ImageEncodingProperties> { CreateJpeg(640, 480) };

		var cameraInfo = new CameraInfo("Name", "copy-test", CameraPosition.Rear, false, 1f, 1f, [], null!, supplied);
		supplied.Clear();

		Assert.Single(cameraInfo.ImageEncodingProperties);
		Assert.Equal(640u, cameraInfo.ImageEncodingProperties[0].Width);
		Assert.Equal(480u, cameraInfo.ImageEncodingProperties[0].Height);
	}

	[Fact]
	public async Task DiscoveredCamerasExposeTheirFrameSourceGroupAndImageEncodingProperties()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		IReadOnlyList<CameraInfo> cameras;

		try
		{
			cameras = await WindowsCameraEnvironment.DiscoverCamerasAsync();
		}
		catch (UnauthorizedAccessException exception)
		{
			throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
		}

		Skip.If(cameras.Count is 0, "The camera provider found no camera.");
		foreach (var camera in cameras)
		{
			Assert.NotNull(camera.FrameSourceGroup);
			Assert.Equal(camera.DeviceId, camera.FrameSourceGroup.Id);
			Assert.Equal(camera.Name, camera.FrameSourceGroup.DisplayName);

			// A camera that takes its photos from its video stream (most webcams) lists only VideoEncodingProperties in its photo stream, so it
			// has no photo sizes.
			using var mediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			Skip.IfNot(await mediaCapture.InitializeCameraForCameraView(camera.DeviceId, CancellationToken.None), $"{camera.Name} cannot be opened (another app may be using it).");
			var offeredSizes = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo)
				.OfType<ImageEncodingProperties>()
				.Select(properties => (properties.Width, properties.Height))
				.Distinct()
				.Order()
				.ToList();
			var listedSizes = camera.ImageEncodingProperties.Select(properties => (properties.Width, properties.Height)).ToList();

			Assert.Equal(offeredSizes, listedSizes.Order().ToList());
			Assert.Equal(listedSizes.Count, listedSizes.Distinct().Count());
		}
	}

	static ImageEncodingProperties CreateJpeg(uint width, uint height) => WindowsCameraEnvironment.CreateJpegProperties(width, height);
}