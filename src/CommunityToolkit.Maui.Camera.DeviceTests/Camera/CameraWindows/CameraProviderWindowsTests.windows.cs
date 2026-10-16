using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraProvider.windows.cs: the discovery of video capture devices through DeviceInformation and MediaFrameSourceGroup, and the
// mapping to CameraInfo. The tests that need a camera skip visibly on machines without one, so the camera-less runs of CI only execute
// the empty-list test.
[Category(TestCategory.CameraWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CameraProviderWindowsTests : CameraHandlerTestBase
{
	[Fact]
	public async Task RefreshWithoutAnyCameraPublishesAnEmptyListOnce()
	{
		Skip.If(await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a machine without a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		// The refresh lists the frame source groups (Media Foundation), also without a camera.
		await WindowsCameraEnvironment.SkipUnlessFrameSourceGroupsCanBeListedAsync();
		using var provider = new CameraProvider();
		var changes = new CameraListRecorder(provider);
		Assert.Null(provider.AvailableCameras);

		await provider.RefreshAvailableCameras(CancellationToken.None);
		await provider.RefreshAvailableCameras(CancellationToken.None);

		Assert.NotNull(provider.AvailableCameras);
		Assert.Empty(provider.AvailableCameras);
		Assert.Single(changes.Lists);
		Assert.Empty(changes.Lists[0] ?? throw new InvalidOperationException("The event published a null list."));
		GC.KeepAlive(changes);
	}

	// The token of PlatformRefreshAvailableCameras (CameraProvider.windows.cs, `.AsTask(token)`). RefreshAvailableCameras never passes its
	// caller's token to the platform method (CameraProvider.shared.cs: the refresh is shared between callers, so it runs without a token and
	// each caller's token only ends that caller's wait), so the platform method is called directly. A WinRT operation that completes before
	// AsTask registers the cancellation (or that ignores Cancel) ends the refresh normally, which skips the test visibly.
	[Fact]
	public async Task PlatformRefreshWithACancelledTokenThrowsAndPublishesNothing()
	{
		// If the first operation wins the race, the second one lists the frame source groups (Media Foundation).
		await WindowsCameraEnvironment.SkipUnlessFrameSourceGroupsCanBeListedAsync();
		using var provider = new CameraProvider();
		var changes = new CameraListRecorder(provider);
		var platformRefresh = typeof(CameraProvider).GetMethod("PlatformRefreshAvailableCameras", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMethodException(nameof(CameraProvider), "PlatformRefreshAvailableCameras");
		using var cancellationTokenSource = new CancellationTokenSource();
		await cancellationTokenSource.CancelAsync();

		var exception = await Record.ExceptionAsync(async () => await (ValueTask)platformRefresh.Invoke(provider, [cancellationTokenSource.Token])!);

		Skip.If(exception is null, "The WinRT operations completed before AsTask observed the cancelled token.");
		Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
		Assert.Null(provider.AvailableCameras);
		Assert.Empty(changes.Lists);
		GC.KeepAlive(changes);
	}

	[Fact]
	public async Task RefreshPublishesOneCameraInfoPerVideoCaptureSourceGroup()
	{
		var sourceGroups = await WindowsCameraEnvironment.FindVideoCaptureSourceGroupsAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, sourceGroups.Count > 0, $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		using var provider = new CameraProvider();
		var changes = new CameraListRecorder(provider);

		await RefreshOrSkipAsync(provider);

		var cameras = provider.AvailableCameras ?? throw new InvalidOperationException("The provider published no list.");
		Assert.Equal(sourceGroups.Select(sourceGroup => sourceGroup.Id), cameras.Select(camera => camera.DeviceId));
		Assert.Equal(sourceGroups.Select(sourceGroup => sourceGroup.DisplayName), cameras.Select(camera => camera.Name));
		Assert.Single(changes.Lists);
		Assert.Same(cameras, changes.Lists[0]);
		Assert.All(cameras, camera => Assert.Contains(sourceGroups, sourceGroup => sourceGroup.Id == camera.FrameSourceGroup.Id));
		GC.KeepAlive(changes);
	}

	[Fact]
	public async Task RefreshListsEachResolutionOnceFromTheLargestToTheSmallest()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		using var provider = new CameraProvider();

		await RefreshOrSkipAsync(provider);

		foreach (var camera in provider.AvailableCameras ?? [])
		{
			Assert.Equal(camera.ImageEncodingProperties.Count, camera.SupportedResolutions.Count);
			Assert.Equal(camera.SupportedResolutions.Count, camera.SupportedResolutions.Distinct().Count());
			Assert.Equal(
				camera.ImageEncodingProperties.Select(properties => new Size(properties.Width, properties.Height)),
				camera.SupportedResolutions);
			var areas = camera.SupportedResolutions.Select(resolution => resolution.Width * resolution.Height).ToList();
			Assert.Equal(areas.OrderByDescending(area => area), areas);
		}
	}

	[Fact]
	public async Task RefreshMapsTheEnclosurePanelToTheCameraPosition()
	{
		var devices = await WindowsCameraEnvironment.FindVideoCaptureDevicesAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, devices.Count > 0, $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		using var provider = new CameraProvider();

		await RefreshOrSkipAsync(provider);

		foreach (var camera in provider.AvailableCameras ?? [])
		{
			var device = devices.First(candidate => candidate.Id == camera.DeviceId);
			var expected = device.EnclosureLocation?.Panel switch
			{
				Panel.Front => CameraPosition.Front,
				Panel.Back => CameraPosition.Rear,
				_ => CameraPosition.Unknown
			};

			Assert.Equal(expected, camera.Position);
		}
	}

	[Theory]
	[InlineData(Panel.Front, CameraPosition.Front)]
	[InlineData(Panel.Back, CameraPosition.Rear)]
	public async Task RefreshMapsAFrontOrBackPanelCamera(Panel panel, CameraPosition expected)
	{
		var devices = await WindowsCameraEnvironment.FindVideoCaptureDevicesAsync();
		var device = devices.FirstOrDefault(candidate => candidate.EnclosureLocation?.Panel == panel);
		Skip.If(device is null, $"Needs a camera whose enclosure location is the {panel} panel ({devices.Count} video capture devices found).");
		using var provider = new CameraProvider();

		await RefreshOrSkipAsync(provider);

		Assert.Equal(expected, provider.AvailableCameras?.FirstOrDefault(camera => camera.DeviceId == device.Id)?.Position);
	}

	[Fact]
	public async Task RefreshMapsACameraWithoutEnclosureLocationToUnknown()
	{
		var devices = await WindowsCameraEnvironment.FindVideoCaptureDevicesAsync();
		var device = devices.FirstOrDefault(candidate => candidate.EnclosureLocation is null);
		Skip.If(device is null, $"Needs a camera without an enclosure location, such as most external webcams ({devices.Count} video capture devices found).");
		using var provider = new CameraProvider();

		await RefreshOrSkipAsync(provider);

		Assert.Equal(CameraPosition.Unknown, provider.AvailableCameras?.FirstOrDefault(camera => camera.DeviceId == device.Id)?.Position);
	}

	[Fact]
	public async Task RefreshReportsTheFlashAndZoomSupportOfTheCamera()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		using var provider = new CameraProvider();
		await RefreshOrSkipAsync(provider);

		foreach (var camera in provider.AvailableCameras ?? [])
		{
			using var mediaCapture = new MediaCapture();
			await InitializeOrSkipAsync(mediaCapture, camera);
			var controller = mediaCapture.VideoDeviceController;

			Assert.Equal(controller.FlashControl.Supported, camera.IsFlashSupported);
			Assert.Equal(controller.ZoomControl.Supported ? controller.ZoomControl.Min : 1f, camera.MinimumZoomFactor);
			Assert.Equal(controller.ZoomControl.Supported ? controller.ZoomControl.Max : 1f, camera.MaximumZoomFactor);
		}
	}

	[Fact]
	public async Task RefreshTwiceKeepsTheCamerasAndDoesNotPublishThemAgain()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		using var provider = new CameraProvider();
		var changes = new CameraListRecorder(provider);

		await RefreshOrSkipAsync(provider);
		var first = provider.AvailableCameras;
		await RefreshOrSkipAsync(provider);

		Assert.Same(first, provider.AvailableCameras);
		Assert.Single(changes.Lists);
		GC.KeepAlive(changes);
	}

	// While another capture holds a camera exclusively, the refresh completes and lists exactly the cameras that InitializeCameraForCameraView
	// can open. Windows 11 still opens a camera that another capture holds (idle or streaming, in this app or in another one), so that camera
	// stays listed there. Where Windows refuses the second capture, InitializeCameraForCameraView returns false and the refresh must leave the
	// camera out: it used to read the VideoDeviceController of the MediaCapture that was never initialized, whose getter throws a
	// COMException, so the whole refresh failed. The held camera is listed again once it is free.
	[Fact]
	public async Task RefreshWhileAnotherCaptureHoldsACameraListsExactlyTheCamerasThatCanBeOpened()
	{
		Skip.IfNot(await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		using var discovery = new CameraProvider();
		await RefreshOrSkipAsync(discovery);
		var cameras = discovery.AvailableCameras ?? [];
		Skip.If(cameras.Count is 0, "The camera provider found no camera that can be opened.");
		var heldCamera = cameras[0];

		using (var holder = new MediaCapture())
		{
			await HoldExclusivelyOrSkipAsync(holder, heldCamera);
			var openableCameraIds = new List<string>();
			foreach (var camera in cameras)
			{
				if (await CanBeOpenedAsync(camera))
				{
					openableCameraIds.Add(camera.DeviceId);
				}
			}

			using var provider = new CameraProvider();

			var exception = await Record.ExceptionAsync(() => provider.RefreshAvailableCameras(CancellationToken.None));

			Assert.True(exception is null, $"The refresh threw while another capture held {heldCamera.Name}: {exception}");
			var listedCameras = provider.AvailableCameras ?? throw new InvalidOperationException("The provider published no list.");
			Assert.Equal(openableCameraIds, listedCameras.Select(camera => camera.DeviceId));
		}

		using var providerAfterRelease = new CameraProvider();
		await RefreshOrSkipAsync(providerAfterRelease);

		Assert.Contains(providerAfterRelease.AvailableCameras ?? [], camera => camera.DeviceId == heldCamera.DeviceId);
	}

	static async Task RefreshOrSkipAsync(CameraProvider provider)
	{
		await WindowsCameraEnvironment.SkipUnlessCamerasCanBeOpenedAsync();

		try
		{
			await provider.RefreshAvailableCameras(CancellationToken.None);
		}
		catch (UnauthorizedAccessException exception)
		{
			throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
		}
	}

	static async Task InitializeOrSkipAsync(MediaCapture mediaCapture, CameraInfo camera)
	{
		try
		{
			await CommunityToolkit.Maui.Extensions.CameraViewExtensions.InitializeCameraForCameraView(mediaCapture, camera.DeviceId, CancellationToken.None);
		}
		catch (UnauthorizedAccessException exception)
		{
			throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
		}
	}

	// Opens the camera like an app that uses it, with exclusive control.
	static async Task HoldExclusivelyOrSkipAsync(MediaCapture holder, CameraInfo camera)
	{
		try
		{
			await holder.InitializeAsync(new MediaCaptureInitializationSettings
			{
				VideoDeviceId = camera.DeviceId,
				SharingMode = MediaCaptureSharingMode.ExclusiveControl,
				StreamingCaptureMode = StreamingCaptureMode.Video,
			});
		}
		catch (UnauthorizedAccessException exception)
		{
			throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
		}
		catch (COMException exception)
		{
			throw new SkipException($"{camera.Name} cannot be opened with exclusive control ({exception.Message}).");
		}
	}

	// The check that the refresh makes for each camera: whether InitializeCameraForCameraView opens it now.
	static async Task<bool> CanBeOpenedAsync(CameraInfo camera)
	{
		using var probe = new MediaCapture();

		return await CommunityToolkit.Maui.Extensions.CameraViewExtensions.InitializeCameraForCameraView(probe, camera.DeviceId, CancellationToken.None);
	}

	// CameraProvider raises AvailableCamerasChanged through a WeakEventManager, so the handler's target must stay alive.
	sealed class CameraListRecorder
	{
		public CameraListRecorder(ICameraProvider provider)
		{
			provider.AvailableCamerasChanged += OnAvailableCamerasChanged;
		}

		public List<IReadOnlyList<CameraInfo>?> Lists { get; } = [];

		void OnAvailableCamerasChanged(object? sender, IReadOnlyList<CameraInfo>? cameras) => Lists.Add(cameras);
	}
}