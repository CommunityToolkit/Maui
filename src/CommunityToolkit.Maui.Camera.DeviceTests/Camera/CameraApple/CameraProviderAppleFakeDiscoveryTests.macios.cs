using AVFoundation;
using CommunityToolkit.Maui.Core;
using CoreMedia;
using CoreVideo;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraProvider.macios.cs against a scripted discovery session (AppleDiscoveryStub, FakeDeviceDiscoverySession and FakeCaptureDevice):
// every device layout (front, back and unspecified positions, an undefined position, 420v and duplicate formats, no device) runs on any
// machine. CameraProviderAppleTests checks the same code against the real cameras of the machine, which the simulator does not have.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraProviderAppleFakeDiscoveryTests : CameraHandlerTestBase
{
	const CMVideoCodecType videoRange420 = (CMVideoCodecType)CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange;
	const CMVideoCodecType fullRange420 = (CMVideoCodecType)CVPixelFormatType.CV420YpCbCr8BiPlanarFullRange;

	[Fact]
	public async Task RefreshAsksTheDiscoverySessionForTheBuiltInVideoDevicesAtAnyPosition()
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			using var stub = new AppleDiscoveryStub();
			using var provider = new CameraProvider();

			await provider.RefreshAvailableCameras(CancellationToken.None);

			var request = Assert.Single(stub.Requests);
			Assert.Equal(CameraProviderAccessor.CaptureDevices.Select(deviceType => deviceType.GetConstant()!.ToString()), request.DeviceTypes);
			Assert.Equal(AppleMediaTypes.Video, request.MediaType);
			Assert.Equal(AVCaptureDevicePosition.Unspecified, request.Position);
		});
	}

	[Fact]
	public async Task RefreshPublishesEveryDeviceAsACameraInDiscoveryOrder()
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			var frontFormats = new AVCaptureDeviceFormat[] { new FakeCaptureDeviceFormat(1920, 1080), new FakeCaptureDeviceFormat(640, 480) };
			using var front = new FakeCaptureDevice
			{
				Name = "Front Camera",
				Identifier = "fake-front",
				DevicePosition = AVCaptureDevicePosition.Front,
				FlashIsAvailable = true,
				MinimumZoomFactor = 1,
				MaximumZoomFactor = 6,
				DeviceFormats = frontFormats
			};
			var backFormats = new AVCaptureDeviceFormat[] { new FakeCaptureDeviceFormat(4032, 3024) };
			using var back = new FakeCaptureDevice
			{
				Name = "Back Camera",
				Identifier = "fake-back",
				DevicePosition = AVCaptureDevicePosition.Back,
				FlashIsAvailable = false,
				MinimumZoomFactor = 0.5,
				MaximumZoomFactor = 15,
				DeviceFormats = backFormats
			};
			using var stub = new AppleDiscoveryStub(front, back);
			using var provider = new CameraProvider();
			var recorder = new AppleCameraListRecorder(provider);

			await provider.RefreshAvailableCameras(CancellationToken.None);

			var cameras = provider.AvailableCameras ?? throw new InvalidOperationException("The provider published no list.");
			Assert.Same(cameras, Assert.Single(recorder.Lists));
			Assert.Collection(cameras,
				camera =>
				{
					Assert.Equal("Front Camera", camera.Name);
					Assert.Equal("fake-front", camera.DeviceId);
					Assert.Equal(CameraPosition.Front, camera.Position);
					Assert.True(camera.IsFlashSupported);
					Assert.Equal(1f, camera.MinimumZoomFactor);
					Assert.Equal(6f, camera.MaximumZoomFactor);
					Assert.Same(front, camera.CaptureDevice);
					Assert.Equal(frontFormats, camera.SupportedFormats);
					Assert.Equal([new Size(1920, 1080), new Size(640, 480)], camera.SupportedResolutions);
				},
				camera =>
				{
					Assert.Equal("Back Camera", camera.Name);
					Assert.Equal("fake-back", camera.DeviceId);
					Assert.Equal(CameraPosition.Rear, camera.Position);
					Assert.False(camera.IsFlashSupported);
					Assert.Equal(0.5f, camera.MinimumZoomFactor);
					Assert.Equal(15f, camera.MaximumZoomFactor);
					Assert.Same(back, camera.CaptureDevice);
					Assert.Equal(backFormats, camera.SupportedFormats);
					Assert.Equal([new Size(4032, 3024)], camera.SupportedResolutions);
				});
			GC.KeepAlive(recorder);
		});
	}

	[Theory]
	[InlineData(AVCaptureDevicePosition.Front, CameraPosition.Front)]
	[InlineData(AVCaptureDevicePosition.Back, CameraPosition.Rear)]
	[InlineData(AVCaptureDevicePosition.Unspecified, CameraPosition.Unknown)]
	public async Task RefreshMapsTheDevicePositionToTheCameraPosition(AVCaptureDevicePosition devicePosition, CameraPosition expected)
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			using var device = new FakeCaptureDevice { DevicePosition = devicePosition };
			using var stub = new AppleDiscoveryStub(device);
			using var provider = new CameraProvider();

			await provider.RefreshAvailableCameras(CancellationToken.None);

			Assert.Equal(expected, Assert.Single(provider.AvailableCameras ?? []).Position);
		});
	}

	[Fact]
	public async Task RefreshWithAnUnsupportedDevicePositionThrowsAndTheProviderRefreshesAgainAfterwards()
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			using var unsupported = new FakeCaptureDevice { DevicePosition = (AVCaptureDevicePosition)42 };
			using var supported = new FakeCaptureDevice { DevicePosition = AVCaptureDevicePosition.Front };
			using var stub = new AppleDiscoveryStub(supported, unsupported);
			using var provider = new CameraProvider();
			var recorder = new AppleCameraListRecorder(provider);

			var exception = await Assert.ThrowsAsync<NotSupportedException>(() => provider.RefreshAvailableCameras(CancellationToken.None));

			Assert.Equal("42 is not yet supported", exception.Message);
			Assert.Null(provider.AvailableCameras);
			Assert.Empty(recorder.Lists);

			// The failed refresh released its lock: the next one discovers again.
			stub.Session = new FakeDeviceDiscoverySession(supported);
			await provider.RefreshAvailableCameras(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(2, stub.Requests.Count);
			Assert.Same(supported, Assert.Single(provider.AvailableCameras ?? []).CaptureDevice);
			Assert.Single(recorder.Lists);
			GC.KeepAlive(recorder);
		});
	}

	[Fact]
	public async Task RefreshSkipsFormatsWithTheBiPlanarVideoRangeCodecType()
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			var videoRange = new FakeCaptureDeviceFormat(1920, 1080, videoRange420);
			var fullRange = new FakeCaptureDeviceFormat(1920, 1080, fullRange420);
			var jpeg = new FakeCaptureDeviceFormat(1280, 720, CMVideoCodecType.JPEG);
			var largerVideoRange = new FakeCaptureDeviceFormat(3840, 2160, videoRange420);
			using var device = new FakeCaptureDevice { DeviceFormats = [videoRange, fullRange, jpeg, largerVideoRange] };
			using var stub = new AppleDiscoveryStub(device);
			using var provider = new CameraProvider();

			await provider.RefreshAvailableCameras(CancellationToken.None);

			// Only the 420v (video range) formats are skipped, not the full range ones of the same resolution.
			var camera = Assert.Single(provider.AvailableCameras ?? []);
			Assert.Equal(new AVCaptureDeviceFormat[] { fullRange, jpeg }, camera.SupportedFormats);
			Assert.Equal([new Size(1920, 1080), new Size(1280, 720)], camera.SupportedResolutions);
		});
	}

	[Fact]
	public async Task RefreshKeepsOnlyTheFirstFormatOfEachResolution()
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			var first = new FakeCaptureDeviceFormat(1280, 720, CMVideoCodecType.JPEG);
			var sameResolution = new FakeCaptureDeviceFormat(1280, 720, fullRange420);
			var otherResolution = new FakeCaptureDeviceFormat(720, 1280, CMVideoCodecType.JPEG);
			var sameResolutionAgain = new FakeCaptureDeviceFormat(1280, 720, CMVideoCodecType.H264);
			using var device = new FakeCaptureDevice { DeviceFormats = [first, sameResolution, otherResolution, sameResolutionAgain] };
			using var stub = new AppleDiscoveryStub(device);
			using var provider = new CameraProvider();

			await provider.RefreshAvailableCameras(CancellationToken.None);

			// Width and height both count: 720x1280 is another resolution than 1280x720.
			var camera = Assert.Single(provider.AvailableCameras ?? []);
			Assert.Equal(new AVCaptureDeviceFormat[] { first, otherResolution }, camera.SupportedFormats);
			Assert.Equal([new Size(1280, 720), new Size(720, 1280)], camera.SupportedResolutions);
		});
	}

	[Fact]
	public async Task RefreshOfADeviceWithoutUsableFormatsKeepsAnEmptyFormatAndResolutionList()
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			using var withoutFormats = new FakeCaptureDevice { Identifier = "fake-no-formats", DeviceFormats = [] };
			using var onlyVideoRange = new FakeCaptureDevice { Identifier = "fake-420v-only", DeviceFormats = [new FakeCaptureDeviceFormat(1920, 1080, videoRange420)] };
			using var stub = new AppleDiscoveryStub(withoutFormats, onlyVideoRange);
			using var provider = new CameraProvider();

			await provider.RefreshAvailableCameras(CancellationToken.None);

			var cameras = provider.AvailableCameras ?? throw new InvalidOperationException("The provider published no list.");
			Assert.Equal(2, cameras.Count);
			Assert.All(cameras, camera =>
			{
				Assert.Empty(camera.SupportedFormats);
				Assert.Empty(camera.SupportedResolutions);
			});
		});
	}

	[Fact]
	public async Task RefreshWithoutAnyDevicePublishesAnEmptyListOnlyOnce()
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			using var stub = new AppleDiscoveryStub();
			using var provider = new CameraProvider();
			var recorder = new AppleCameraListRecorder(provider);
			Assert.Null(provider.AvailableCameras);

			await provider.RefreshAvailableCameras(CancellationToken.None);
			var first = provider.AvailableCameras;
			await provider.RefreshAvailableCameras(CancellationToken.None);

			Assert.NotNull(first);
			Assert.Empty(first);
			Assert.Same(first, provider.AvailableCameras);
			Assert.Empty(Assert.Single(recorder.Lists) ?? throw new InvalidOperationException("The event published a null list."));
			Assert.Equal(2, stub.Requests.Count);
			GC.KeepAlive(recorder);
		});
	}

	[Fact]
	public async Task RefreshPublishesAgainOnlyWhenTheSetOfDevicesChanges()
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			using var first = new FakeCaptureDevice { Identifier = "fake-first" };
			using var second = new FakeCaptureDevice { Identifier = "fake-second" };
			using var stub = new AppleDiscoveryStub(first, second);
			using var provider = new CameraProvider();
			var recorder = new AppleCameraListRecorder(provider);

			await provider.RefreshAvailableCameras(CancellationToken.None);
			var published = provider.AvailableCameras;

			// The same devices in another order: the same set of cameras, so the published list stays.
			stub.Session = new FakeDeviceDiscoverySession(second, first);
			await provider.RefreshAvailableCameras(CancellationToken.None);
			Assert.Same(published, provider.AvailableCameras);
			Assert.Single(recorder.Lists);

			// One camera gone: a new list is published.
			stub.Session = new FakeDeviceDiscoverySession(second);
			await provider.RefreshAvailableCameras(CancellationToken.None);

			Assert.Equal(2, recorder.Lists.Count);
			Assert.Equal(["fake-second"], provider.AvailableCameras?.Select(camera => camera.DeviceId));
			Assert.Same(provider.AvailableCameras, recorder.Lists[1]);
			GC.KeepAlive(recorder);
		});
	}

	[Fact]
	public async Task RefreshWithACancelledTokenThrowsBeforeDiscoveringCameras()
	{
		await InvokeOnMainThreadAsync(async () =>
		{
			using var device = new FakeCaptureDevice();
			using var stub = new AppleDiscoveryStub(device);
			using var provider = new CameraProvider();
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.RefreshAvailableCameras(cancellationTokenSource.Token));

			Assert.Empty(stub.Requests);
			Assert.Null(provider.AvailableCameras);
		});
	}

	[Theory]
	[InlineData(10, 3, 3)]
	[InlineData(11, 0, 3)]
	[InlineData(11, 1, 4)]
	[InlineData(12, 4, 4)]
	[InlineData(13, 0, 7)]
	[InlineData(15, 3, 7)]
	[InlineData(15, 4, 8)]
	[InlineData(27, 0, 8)]
	public async Task CaptureDevicesListTheBuiltInCameraTypesThatTheOsVersionOffers(int major, int minor, int expectedCount)
	{
		AVCaptureDeviceType[] allTypes =
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

		var captureDevices = await InvokeOnMainThreadAsync(() =>
		{
			// The type initializer caches the device types of the real OS: it must not run inside the override.
			CameraProviderAccessor.EnsureTypeInitialized();
			using var version = new AppleSystemVersionOverride(new Version(major, minor));

			return CameraProviderAccessor.InitializeCaptureDevices();
		});

		Assert.Equal(allTypes.Take(expectedCount), captureDevices);
	}
}