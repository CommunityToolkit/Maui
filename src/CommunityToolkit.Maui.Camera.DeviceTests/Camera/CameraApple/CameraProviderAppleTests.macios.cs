using AVFoundation;
using CommunityToolkit.Maui.Core;
using CoreMedia;
using CoreVideo;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraProvider.macios.cs: the capture device types that are searched and the mapping of AVCaptureDevices to CameraInfo.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraProviderAppleTests : CameraHandlerTestBase
{
	[Fact]
	public void CaptureDevicesListEveryBuiltInCameraTypeOfTheCurrentOs()
	{
		Skip.IfNot(UIDevice.CurrentDevice.CheckSystemVersion(15, 4),
			"Needs iOS or Mac Catalyst 15.4 or later: CameraProviderAppleFakeDiscoveryTests.CaptureDevicesListTheBuiltInCameraTypesThatTheOsVersionOffers covers the older versions.");

		AVCaptureDeviceType[] expected =
		[
			AVCaptureDeviceType.BuiltInWideAngleCamera,
			AVCaptureDeviceType.BuiltInTelephotoCamera,
			AVCaptureDeviceType.BuiltInDualCamera,
			AVCaptureDeviceType.BuiltInTrueDepthCamera,
			AVCaptureDeviceType.BuiltInUltraWideCamera,
			AVCaptureDeviceType.BuiltInTripleCamera,
			AVCaptureDeviceType.BuiltInDualWideCamera,
			AVCaptureDeviceType.BuiltInLiDarDepthCamera
		];

		Assert.Equal(expected, GetCaptureDevices());
	}

	[Fact]
	public async Task RefreshWithoutAnyCameraPublishesAnEmptyListOnce()
	{
		Skip.If(AppleCameraEnvironment.HasVideoCaptureDevice, $"Needs a device without a camera, such as the iOS simulator ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		using var provider = new CameraProvider();
		var changes = new AppleCameraListRecorder(provider);
		Assert.Null(provider.AvailableCameras);

		await provider.RefreshAvailableCameras(CancellationToken.None);
		await provider.RefreshAvailableCameras(CancellationToken.None);

		Assert.NotNull(provider.AvailableCameras);
		Assert.Empty(provider.AvailableCameras);
		Assert.Single(changes.Lists);
		Assert.Empty(changes.Lists[0] ?? throw new InvalidOperationException("The event published a null list."));
		GC.KeepAlive(changes);
	}

	[Fact]
	public async Task RefreshPublishesTheDiscoveredCamerasWithTheirDeviceProperties()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, AppleCameraEnvironment.HasVideoCaptureDevice, $"Needs a device with a camera, such as a Mac ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		using var provider = new CameraProvider();
		var changes = new AppleCameraListRecorder(provider);

		await provider.RefreshAvailableCameras(CancellationToken.None);

		var cameras = provider.AvailableCameras ?? throw new InvalidOperationException("The provider published no list.");
		var devices = DiscoverDevices();
		Assert.NotEmpty(cameras);
		Assert.Equal(devices.Select(device => device.UniqueID), cameras.Select(camera => camera.DeviceId));
		Assert.Single(changes.Lists);
		Assert.Same(cameras, changes.Lists[0]);

		foreach (var (camera, device) in cameras.Zip(devices))
		{
			Assert.Equal(device.LocalizedName, camera.Name);
			Assert.Equal(device.HasFlash, camera.IsFlashSupported);
			Assert.Equal((float)device.MinAvailableVideoZoomFactor, camera.MinimumZoomFactor);
			Assert.Equal((float)device.MaxAvailableVideoZoomFactor, camera.MaximumZoomFactor);
			Assert.Same(device, camera.CaptureDevice);
			Assert.Equal(GetExpectedPosition(device.Position), camera.Position);
			Assert.NotEmpty(camera.SupportedResolutions);
			Assert.Equal(camera.SupportedFormats.Count, camera.SupportedResolutions.Count);
		}

		GC.KeepAlive(changes);
	}

	[Fact]
	public async Task RefreshTwiceKeepsTheCamerasAndDoesNotPublishThemAgain()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, AppleCameraEnvironment.HasVideoCaptureDevice, $"Needs a device with a camera, such as a Mac ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		using var provider = new CameraProvider();
		var changes = new AppleCameraListRecorder(provider);

		await provider.RefreshAvailableCameras(CancellationToken.None);
		var first = provider.AvailableCameras;
		await provider.RefreshAvailableCameras(CancellationToken.None);

		Assert.Same(first, provider.AvailableCameras);
		Assert.Single(changes.Lists);
		GC.KeepAlive(changes);
	}

	[Fact]
	public async Task RefreshSkipsFormatsWithTheBiPlanarVideoRangeCodecType()
	{
		var devices = DiscoverDevices();
		var device = devices.FirstOrDefault(candidate => candidate.Formats.Any(IsVideoRangeFormat));
		Skip.If(device is null, $"Needs a camera that offers a 420v (bi-planar video range) format ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();
		var camera = cameras.Single(candidate => candidate.DeviceId == device.UniqueID);

		Assert.DoesNotContain(camera.SupportedFormats, IsVideoRangeFormat);
		Assert.NotEmpty(camera.SupportedFormats);
		Assert.True(device.Formats.Length > camera.SupportedFormats.Count);
	}

	[Fact]
	public async Task RefreshKeepsOnlyTheFirstFormatOfEachResolution()
	{
		var devices = DiscoverDevices();
		var device = devices.FirstOrDefault(candidate =>
		{
			var dimensions = candidate.Formats.Where(format => !IsVideoRangeFormat(format)).Select(GetDimensions).ToList();
			return dimensions.Count != dimensions.Distinct().Count();
		});
		Skip.If(device is null, $"Needs a camera with two non-420v formats of the same resolution, such as the iPhone Continuity Camera ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();
		var camera = cameras.Single(candidate => candidate.DeviceId == device.UniqueID);
		var expectedFormats = new List<AVCaptureDeviceFormat>();

		foreach (var format in device.Formats.Where(format => !IsVideoRangeFormat(format)))
		{
			if (expectedFormats.All(existing => GetDimensions(existing) != GetDimensions(format)))
			{
				expectedFormats.Add(format);
			}
		}

		Assert.Equal(expectedFormats, camera.SupportedFormats);
		Assert.Equal(expectedFormats.Select(GetDimensions), camera.SupportedResolutions.Select(size => ((int)size.Width, (int)size.Height)));
		Assert.Equal(camera.SupportedResolutions.Count, camera.SupportedResolutions.Distinct().Count());
	}

	[Fact]
	public async Task RefreshMapsABackCameraToRear()
	{
		var device = DiscoverDevices().FirstOrDefault(candidate => candidate.Position is AVCaptureDevicePosition.Back);
		Skip.If(device is null, $"Needs a camera on the back of the device, which neither the simulator nor a Mac has ({AppleCameraEnvironment.DescribeCameraAccess()}).");

		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();

		Assert.Equal(CameraPosition.Rear, cameras.Single(camera => camera.DeviceId == device.UniqueID).Position);
	}

	[Fact]
	public async Task RefreshMapsAnUnspecifiedPositionToUnknown()
	{
		var device = DiscoverDevices().FirstOrDefault(candidate => candidate.Position is AVCaptureDevicePosition.Unspecified);
		Skip.If(device is null, $"Needs a camera with an unspecified position, such as an external webcam ({AppleCameraEnvironment.DescribeCameraAccess()}).");

		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();

		Assert.Equal(CameraPosition.Unknown, cameras.Single(camera => camera.DeviceId == device.UniqueID).Position);
	}

	[Fact]
	public async Task RefreshMapsAFrontCameraToFront()
	{
		var device = DiscoverDevices().FirstOrDefault(candidate => candidate.Position is AVCaptureDevicePosition.Front);
		Skip.If(device is null, $"Needs a camera on the front of the device, such as the Mac's built-in camera ({AppleCameraEnvironment.DescribeCameraAccess()}).");

		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();

		Assert.Equal(CameraPosition.Front, cameras.Single(camera => camera.DeviceId == device.UniqueID).Position);
	}

	static AVCaptureDeviceType[] GetCaptureDevices() => CameraProviderAccessor.CaptureDevices;

	// The same discovery as CameraProvider.macios.cs, with the expected device types written out.
	static AVCaptureDevice[] DiscoverDevices()
	{
		var discoverySession = AVCaptureDeviceDiscoverySession.Create(GetCaptureDevices(), AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);

		return discoverySession.Devices;
	}

	static CameraPosition GetExpectedPosition(AVCaptureDevicePosition position) => position switch
	{
		AVCaptureDevicePosition.Front => CameraPosition.Front,
		AVCaptureDevicePosition.Back => CameraPosition.Rear,
		_ => CameraPosition.Unknown
	};

	static bool IsVideoRangeFormat(AVCaptureDeviceFormat format) =>
		(int)format.FormatDescription.VideoCodecType == (int)CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange;

	static (int Width, int Height) GetDimensions(AVCaptureDeviceFormat format)
	{
		var dimensions = ((CMVideoFormatDescription)format.FormatDescription).Dimensions;
		return (dimensions.Width, dimensions.Height);
	}
}