using AVFoundation;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

// The IOS/MACCATALYST blocks of CameraInfo.shared.cs: the constructor parameters and the internal CaptureDevice and
// SupportedFormats properties that CameraManager.macios.cs uses.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraInfoAppleTests : CameraHandlerTestBase
{
	[Fact]
	public void ConstructorWithoutADeviceKeepsNullCaptureDeviceAndNoFormats()
	{
		var cameraInfo = new CameraInfo("Name", "device-id", CameraPosition.Rear, true, 1f, 5f, [new Size(640, 480), new Size(1280, 720)], null!, []);

		Assert.Null(cameraInfo.CaptureDevice);
		Assert.Empty(cameraInfo.SupportedFormats);
		Assert.Equal("Name", cameraInfo.Name);
		Assert.Equal("device-id", cameraInfo.DeviceId);
		Assert.Equal(CameraPosition.Rear, cameraInfo.Position);
		Assert.True(cameraInfo.IsFlashSupported);
		Assert.Equal(1f, cameraInfo.MinimumZoomFactor);
		Assert.Equal(5f, cameraInfo.MaximumZoomFactor);
		Assert.Equal([new Size(640, 480), new Size(1280, 720)], cameraInfo.SupportedResolutions);
	}

	[Fact]
	public void ConstructorKeepsTheCaptureDeviceAndExposesTheSuppliedFormatsInOrder()
	{
		using var device = new FakeCaptureDevice();
		AVCaptureDeviceFormat[] formats = [new FakeCaptureDeviceFormat(1920, 1080), new FakeCaptureDeviceFormat(640, 480), new FakeCaptureDeviceFormat(1280, 720)];

		var cameraInfo = new CameraInfo("Name", "device-id", CameraPosition.Front, false, 1f, 1f, [], device, formats);

		Assert.Same(device, cameraInfo.CaptureDevice);
		Assert.Equal(formats, cameraInfo.SupportedFormats);
	}

	[Fact]
	public void SupportedFormatsAreACopyOfTheSuppliedFakeFormats()
	{
		using var device = new FakeCaptureDevice();
		var suppliedFormats = new List<AVCaptureDeviceFormat> { new FakeCaptureDeviceFormat(1920, 1080), new FakeCaptureDeviceFormat(640, 480) };
		var expected = suppliedFormats.ToList();

		var cameraInfo = new CameraInfo("Name", "copy-test-fake", CameraPosition.Front, false, 1f, 1f, [], device, suppliedFormats);
		suppliedFormats.Clear();

		Assert.Equal(expected, cameraInfo.SupportedFormats);
	}

	[Fact]
	public async Task DiscoveredCamerasExposeTheirCaptureDeviceAndFormats()
	{
		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, cameras.Count > 0, $"Needs a camera to provide an AVCaptureDevice ({AppleCameraEnvironment.DescribeCameraAccess()}).");

		foreach (var camera in cameras)
		{
			var device = Assert.IsType<AVCaptureDevice>(camera.CaptureDevice, exactMatch: false);
			Assert.Equal(device.UniqueID, camera.DeviceId);
			Assert.NotEmpty(camera.SupportedFormats);
			Assert.All(camera.SupportedFormats, format => Assert.Contains(format, device.Formats));
		}
	}

	[Fact]
	public async Task SupportedFormatsAreACopyOfTheSuppliedFormats()
	{
		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, cameras.Count > 0, $"Needs a camera to provide AVCaptureDeviceFormats ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		var device = cameras[0].CaptureDevice;
		var formats = cameras[0].SupportedFormats;
		var suppliedFormats = new List<AVCaptureDeviceFormat>(formats);

		var cameraInfo = new CameraInfo("Name", "copy-test", CameraPosition.Front, false, 1f, 1f, [], device!, suppliedFormats);
		suppliedFormats.Clear();

		Assert.Same(device, cameraInfo.CaptureDevice);
		Assert.Equal(formats.Count, cameraInfo.SupportedFormats.Count);
		Assert.Equal(formats, cameraInfo.SupportedFormats);
	}
}