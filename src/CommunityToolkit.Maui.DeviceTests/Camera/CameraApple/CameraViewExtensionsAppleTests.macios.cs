using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CoreMedia;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewExtensionsAppleTests : CameraHandlerTestBase
{
	[Theory]
	[InlineData(CameraFlashMode.Off, AVCaptureFlashMode.Off)]
	[InlineData(CameraFlashMode.On, AVCaptureFlashMode.On)]
	[InlineData(CameraFlashMode.Auto, AVCaptureFlashMode.Auto)]
	public void ToPlatformMapsEveryFlashMode(CameraFlashMode flashMode, AVCaptureFlashMode expected)
	{
		Assert.Equal(expected, flashMode.ToPlatform());
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(3)]
	[InlineData(42)]
	public void ToPlatformThrowsNotSupportedExceptionForAnUndefinedFlashMode(int undefinedFlashMode)
	{
		var exception = Assert.Throws<NotSupportedException>(() => ((CameraFlashMode)undefinedFlashMode).ToPlatform());

		Assert.Equal($"{undefinedFlashMode} is not yet supported", exception.Message);
	}

	[Fact]
	public void UpdateAvailabilityReflectsTheDefaultVideoCaptureDevice()
	{
		using var view = new CameraView();
		var expected = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video) is not null;

		// Start from the opposite value so that a call that does not update the property fails the test.
		((ICameraView)view).IsAvailable = !expected;
		view.UpdateAvailability();

		Assert.Equal(expected, view.IsAvailable);
	}

	[Fact]
	public void UpdateAvailabilityIsFalseWithoutAVideoCaptureDevice()
	{
		Skip.If(AppleCameraEnvironment.HasVideoCaptureDevice, $"Needs a device without a camera, such as the iOS simulator ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		using var view = new CameraView();
		((ICameraView)view).IsAvailable = true;

		view.UpdateAvailability();

		Assert.False(view.IsAvailable);
	}

	[Fact]
	public void UpdateAvailabilityIsTrueWithAVideoCaptureDevice()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, AppleCameraEnvironment.HasVideoCaptureDevice, $"Needs a device with a camera, such as a Mac ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		using var view = new CameraView();
		Assert.False(view.IsAvailable);

		view.UpdateAvailability();

		Assert.True(view.IsAvailable);
	}

	[Theory]
	[InlineData(640, 480, 307200)]
	[InlineData(1280, 720, 921600)]
	[InlineData(1920, 1080, 2073600)]
	[InlineData(3840, 2160, 8294400)]
	[InlineData(1, 1000, 1000)]
	[InlineData(1000, 1, 1000)]
	[InlineData(0, 1080, 0)]
	public void ResolutionAreaIsWidthTimesHeightOfTheFormatDimensions(int width, int height, int expectedArea)
	{
		AVCaptureDeviceFormat format = new FakeCaptureDeviceFormat(width, height);

		Assert.Equal(expectedArea, format.ResolutionArea);
	}

	[Fact]
	public async Task ResolutionAreaIsWidthTimesHeightOfTheFormat()
	{
		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, cameras.Count > 0, $"Needs a camera to provide AVCaptureDeviceFormats ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		var formats = cameras.SelectMany(camera => camera.SupportedFormats).ToList();

		foreach (var format in formats)
		{
			var dimensions = ((CMVideoFormatDescription)format.FormatDescription).Dimensions;
			Assert.Equal(dimensions.Width * dimensions.Height, format.ResolutionArea);
		}

		// Known values, independent of the formula under test: VGA and 720p.
		Assert.Contains(formats, format => IsResolution(format, 640, 480) && format.ResolutionArea is 307200);
		Assert.Contains(formats, format => IsResolution(format, 1280, 720) && format.ResolutionArea is 921600);
	}

	static bool IsResolution(AVCaptureDeviceFormat format, int width, int height)
	{
		var dimensions = ((CMVideoFormatDescription)format.FormatDescription).Dimensions;
		return dimensions.Width == width && dimensions.Height == height;
	}
}