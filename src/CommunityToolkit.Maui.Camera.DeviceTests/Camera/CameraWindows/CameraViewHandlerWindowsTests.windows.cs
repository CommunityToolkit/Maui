using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraViewHandler and CameraView on Windows, driven through WindowsNoConnectCameraViewHandler: it behaves like the real handler
// except that it does not connect the camera automatically (see its comment), so the tests need no camera. The WINDOWS branch of
// CameraViewHandler.shared.cs (MapIsAvailable) and the MediaPlayerElement platform view are covered here.
[Category(TestCategory.CameraWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewHandlerWindowsTests : CameraHandlerTestBase
{
	static readonly CameraInfo cameraWithoutFrameSourceGroup = new(
		"Camera without frame source group", "fake-handler-no-group", CameraPosition.Front, false, 1f, 4f, [new Size(640, 480)], null!, []);

	[Fact]
	public void HandlerViewTypeUsesFrameworkElementOnWindows()
	{
		Assert.True(typeof(ViewHandler<ICameraView, Microsoft.UI.Xaml.FrameworkElement>).IsAssignableFrom(typeof(CameraViewHandler)));
		Assert.True(typeof(IPlatformViewHandler).IsAssignableFrom(typeof(CameraViewHandler)));
	}

	[Fact]
	public void CameraViewDoesNotExposeTheAndroidOnlyExtensionMode()
	{
		Assert.Null(typeof(CameraView).GetMethod("SetExtensionMode"));
	}

	[Fact]
	public void PropertyMapperMapsEveryCameraViewProperty()
	{
		var keys = CameraViewHandler.PropertyMapper.GetKeys().ToList();

		Assert.Contains(nameof(ICameraView.CameraFlashMode), keys);
		Assert.Contains(nameof(ICameraView.IsTorchOn), keys);
		Assert.Contains(nameof(ICameraView.IsAvailable), keys);
		Assert.Contains(nameof(ICameraView.ZoomFactor), keys);
		Assert.Contains(nameof(ICameraView.ImageCaptureResolution), keys);
		Assert.Contains(nameof(ICameraView.SelectedCamera), keys);
	}

	// Both constructors without custom mappers take CameraViewHandler's static PropertyMapper and CommandMapper when the handler is created.
	// The test swaps them for probes that inherit every default mapping while it creates the handlers, so it sees which mappers each handler
	// uses without reading MAUI internals.
	[Fact]
	public async Task HandlerCreatedWithoutCustomMappersUsesTheMappersOfCameraViewHandler()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			List<(CameraViewHandler Handler, float ZoomFactor)> mappedZoomFactors = [];
			List<(CameraViewHandler Handler, object? Argument)> commandArguments = [];
			var defaultPropertyMapper = CameraViewHandler.PropertyMapper;
			var defaultCommandMapper = CameraViewHandler.CommandMapper;
			WindowsMapperCameraViewHandler parameterless, withNullMappers;

			try
			{
				CameraViewHandler.PropertyMapper = new PropertyMapper<ICameraView, CameraViewHandler>(defaultPropertyMapper)
				{
					[nameof(ICameraView.ZoomFactor)] = (handler, cameraView) => mappedZoomFactors.Add((handler, cameraView.ZoomFactor))
				};
				CameraViewHandler.CommandMapper = new CommandMapper<ICameraView, CameraViewHandler>(defaultCommandMapper)
				{
					["DeviceTestsProbe"] = (handler, _, argument) => commandArguments.Add((handler, argument))
				};

				parameterless = new WindowsMapperCameraViewHandler();
				withNullMappers = new WindowsMapperCameraViewHandler(null, null);
			}
			finally
			{
				CameraViewHandler.PropertyMapper = defaultPropertyMapper;
				CameraViewHandler.CommandMapper = defaultCommandMapper;
			}

			var parameterlessView = new CameraView();
			var view = new CameraView();
			InitializeViewHandler(parameterlessView, parameterless);
			InitializeViewHandler(view, withNullMappers);
			mappedZoomFactors.Clear();

			parameterlessView.ZoomFactor = 2f;
			view.ZoomFactor = 3f;
			parameterless.Invoke("DeviceTestsProbe", 1);
			withNullMappers.Invoke("DeviceTestsProbe", 2);

			// Each handler kept the mappers that were current when it was created.
			List<(CameraViewHandler Handler, float ZoomFactor)> expectedZoomFactors = [(parameterless, 2f), (withNullMappers, 3f)];
			List<(CameraViewHandler Handler, object? Argument)> expectedCommandArguments = [(parameterless, 1), (withNullMappers, 2)];
			Assert.Equal(expectedZoomFactors, mappedZoomFactors);
			Assert.Equal(expectedCommandArguments, commandArguments);
			Assert.IsType<MediaPlayerElement>(withNullMappers.PlatformView);

			DisconnectAndRelease(parameterlessView);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task HandlerCreatedWithCustomMappersUsesThemInsteadOfTheMappersOfCameraViewHandler()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			List<float> mappedZoomFactors = [];
			List<object?> commandArguments = [];
			var mapper = new PropertyMapper<ICameraView, CameraViewHandler>(CameraViewHandler.PropertyMapper)
			{
				[nameof(ICameraView.ZoomFactor)] = (_, cameraView) => mappedZoomFactors.Add(cameraView.ZoomFactor)
			};
			var commandMapper = new CommandMapper<ICameraView, CameraViewHandler>(CameraViewHandler.CommandMapper)
			{
				["DeviceTestsProbe"] = (_, _, argument) => commandArguments.Add(argument)
			};
			var handler = new WindowsMapperCameraViewHandler(mapper, commandMapper);
			var view = new CameraView();
			InitializeViewHandler(view, handler);
			var mappedBeforeTheChange = mappedZoomFactors.Count;

			view.ZoomFactor = 2.5f;
			handler.Invoke("DeviceTestsProbe", 42);

			Assert.Equal([2.5f], mappedZoomFactors.Skip(mappedBeforeTheChange));
			Assert.Equal([(object?)42], commandArguments);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task HandlerCreatesAMediaPlayerElementPlatformView()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);

			var platformView = Assert.IsType<MediaPlayerElement>(handler.PlatformView);
			Assert.Same(platformView, new WindowsCameraManagerAccessor(handler.CameraManager).MediaElement);
			Assert.Null(platformView.Source);

			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task ChangingIsAvailableIsOverriddenWithTheStateOfTheVideoCaptureDevices()
	{
		SetupBuilder();
		var expected = await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync();
		var view = new CameraView();
		await InvokeOnMainThreadAsync(() => CreateHandler<WindowsNoConnectCameraViewHandler>(view));

		// IsAvailable is mapped: a change makes the handler recompute it asynchronously from the devices.
		await InvokeOnMainThreadAsync(() => ((ICameraView)view).IsAvailable = !expected);

		await AssertHelpers.AssertEventually(
			async () => await InvokeOnMainThreadAsync(() => view.IsAvailable) == expected,
			timeout: 5000,
			message: $"IsAvailable did not become {expected}.");
		await InvokeOnMainThreadAsync(() => DisconnectAndRelease(view));
	}

	[Fact]
	public async Task HandlerMapsAvailabilityWhenACameraExists()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		SetupBuilder();
		var view = new CameraView();
		Assert.False(view.IsAvailable);

		await InvokeOnMainThreadAsync(() => CreateHandler<WindowsNoConnectCameraViewHandler>(view));

		await AssertHelpers.AssertEventually(
			async () => await InvokeOnMainThreadAsync(() => view.IsAvailable),
			timeout: 5000,
			message: "IsAvailable did not become true although a camera exists.");
		await InvokeOnMainThreadAsync(() => DisconnectAndRelease(view));
	}

	[Fact]
	public async Task HandlerMapsAvailabilityWhenNoCameraExists()
	{
		Skip.If(await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a machine without a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		SetupBuilder();
		var view = new CameraView();
		await InvokeOnMainThreadAsync(() =>
		{
			((ICameraView)view).IsAvailable = true;
			CreateHandler<WindowsNoConnectCameraViewHandler>(view);
		});

		await AssertHelpers.AssertEventually(
			async () => !await InvokeOnMainThreadAsync(() => view.IsAvailable),
			timeout: 5000,
			message: "IsAvailable did not become false although no camera exists.");
		await InvokeOnMainThreadAsync(() => DisconnectAndRelease(view));
	}

	[Fact]
	public async Task PropertyChangesBeforeTheCameraIsConnectedAreAcceptedWithoutTouchingTheCapture()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);

			view.CameraFlashMode = CameraFlashMode.Auto;
			view.IsTorchOn = true;
			view.ZoomFactor = 3f;
			view.ImageCaptureResolution = new Size(800, 600);
			view.SelectedCamera = cameraWithoutFrameSourceGroup;

			Assert.Equal(CameraFlashMode.Auto, view.CameraFlashMode);
			Assert.True(view.IsTorchOn);
			Assert.Equal(3f, view.ZoomFactor);
			Assert.Equal(new Size(800, 600), view.ImageCaptureResolution);
			Assert.Same(cameraWithoutFrameSourceGroup, view.SelectedCamera);
			Assert.False(access.IsInitialized);
			Assert.Null(access.MediaCapture);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task CameraLoadedCallbackResetsTheZoomFactor()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			view.ZoomFactor = 3f;
			Assert.Equal(3f, view.ZoomFactor);

			new WindowsCameraManagerAccessor(handler.CameraManager).OnLoaded();

			Assert.Equal(1f, view.ZoomFactor);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task DisconnectingTheHandlerDisposesTheCameraManagerAndClearsThePreview()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var mediaElement = Assert.IsType<MediaPlayerElement>(handler.PlatformView);
			WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);

			((IElementHandler)handler).DisconnectHandler();

			var exception = Assert.Throws<InvalidOperationException>(() => handler.CameraManager);
			Assert.Equal("CameraManager cannot be used until the native view has been created", exception.Message);
			Assert.Null(mediaElement.Source);
			view.Handler = null;
		});
	}

	[Fact]
	public void CameraManagerIsUnavailableBeforeThePlatformViewIsCreated()
	{
		var handler = new WindowsNoConnectCameraViewHandler();

		var exception = Assert.Throws<InvalidOperationException>(() => handler.CameraManager);

		Assert.Equal("CameraManager cannot be used until the native view has been created", exception.Message);
	}

	[Fact]
	public async Task DisposingTheHandlerTwiceIsHarmless()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var mediaElement = Assert.IsType<MediaPlayerElement>(handler.PlatformView);
			WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);

			handler.Dispose();
			Assert.Null(mediaElement.Source);

			Assert.Null(Record.Exception(handler.Dispose));
			Assert.Throws<InvalidOperationException>(() => handler.CameraManager);
		});
	}

	[Fact]
	public async Task CreatePlatformViewWithoutAMauiContextThrowsArgumentNullException()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			var handler = new WindowsCreateViewCameraViewHandler();

			var exception = Assert.Throws<ArgumentNullException>(() => handler.CallCreatePlatformView());

			Assert.Equal("MauiContext", exception.ParamName);
			Assert.Throws<InvalidOperationException>(() => handler.CameraManager);
		});
	}

	[Fact]
	public async Task DisposeWithoutDisposingManagedResourcesKeepsTheCameraManager()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsCreateViewCameraViewHandler>(view);
			var manager = handler.CameraManager;
			var mediaElement = Assert.IsType<MediaPlayerElement>(handler.PlatformView);
			WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);

			// Dispose(false) is the finalizer path: managed objects such as the camera manager are not touched.
			handler.CallDispose(disposing: false);

			Assert.Same(manager, handler.CameraManager);
			Assert.NotNull(mediaElement.Source);

			DisconnectAndRelease(view);
			Assert.Null(mediaElement.Source);
		});
	}

	[Fact]
	public async Task StopCameraPreviewWithoutARunningPreviewDoesNothing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);

			view.StopCameraPreview();
			view.StopCameraPreviewCommand.Execute(null);

			Assert.False(access.IsInitialized);
			Assert.Null(access.MediaCapture);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task StartCameraPreviewWithoutAnyCameraRaisesErrorOccurred()
	{
		Skip.If(await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(),
			$"Needs a machine without a camera: with a camera this would open it ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			using var errors = new CameraErrorRecorder(view);
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);

			await view.StartCameraPreview(CancellationToken.None);

			var exception = Assert.IsType<CameraException>(Assert.Single(errors.Errors));
			Assert.Equal("Couldn't start camera preview; no cameras available", exception.Message);
			Assert.False(access.IsInitialized);
			Assert.Null(access.MediaCapture);
			DisconnectAndRelease(view);
		});
	}

	// Whatever a capture without a MediaCapture ends with (see CaptureImageWithoutAMediaCaptureThrowsCameraException), it must release the
	// semaphore that serializes captures.
	[Fact]
	public async Task CaptureImageWithoutAMediaCaptureReleasesTheSemaphoreWhenItEnds()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var semaphore = (SemaphoreSlim)typeof(CameraView).GetField("captureImageSemaphoreSlim", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

			for (var attempt = 0; attempt < 2; attempt++)
			{
				using var cancellationTokenSource = new CancellationTokenSource();
				var capture = view.CaptureImage(cancellationTokenSource.Token);
				await cancellationTokenSource.CancelAsync();

				await Record.ExceptionAsync(() => capture);

				Assert.Equal(1, semaphore.CurrentCount);
			}

			DisconnectAndRelease(view);
		});
	}

	// Without a MediaCapture (a camera that never started, or StopCameraPreview) CaptureImage must fail with CameraException instead of waiting
	// for the media events forever with its caller's token, with the reason that iOS and Mac Catalyst report for a camera that never started
	// (CameraViewHandlerAppleTests.CaptureImageWithoutAStartedCameraThrowsCameraExceptionOnEveryCallAndReleasesTheSemaphore).
	[Fact]
	public async Task CaptureImageWithoutAMediaCaptureThrowsCameraException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var recorder = new WindowsMediaRecorder(view);

			try
			{
				var exception = await Assert.ThrowsAsync<CameraException>(() => view.CaptureImage(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));

				Assert.Equal("Unable to capture an image because the camera is not initialized. Start the camera preview first.", exception.Message);
				Assert.Equal([exception.Message], recorder.Failures);
			}
			finally
			{
				GC.KeepAlive(recorder);
				DisconnectAndRelease(view);
			}
		});
	}

	// The capture itself fails (CapturePhotoToStreamAsync on a MediaCapture that was never initialized, without a camera): CaptureImage throws
	// the documented CameraException with the reason that MediaCaptureFailed reported, as on Android, iOS and Mac Catalyst. Windows used to
	// rethrow the platform exception (a COMException) and leave that CameraException unobserved.
	[Fact]
	public async Task CaptureImageWhenTheCaptureFailsThrowsCameraExceptionWithTheReason()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);
			access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			var recorder = new WindowsMediaRecorder(view);

			try
			{
				var exception = await Assert.ThrowsAsync<CameraException>(() => view.CaptureImage(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));

				Assert.Equal([exception.Message], recorder.Failures);
				Assert.Empty(recorder.Captured);
			}
			finally
			{
				GC.KeepAlive(recorder);
				DisconnectAndRelease(view);
			}
		});
	}

	[Fact]
	public async Task StopVideoRecordingWithoutARecordingReturnsTheNullStream()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			CreateHandler<WindowsNoConnectCameraViewHandler>(view);

			var stream = await view.StopVideoRecording(CancellationToken.None);
			var typedStream = await view.StopVideoRecording<Stream>(CancellationToken.None);

			Assert.Same(Stream.Null, stream);
			Assert.Same(Stream.Null, typedStream);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task GetAvailableCamerasReturnsTheCamerasOfTheRunnersProvider()
	{
		// Both providers list the frame source groups (Media Foundation), also without a camera.
		await WindowsCameraEnvironment.SkipUnlessFrameSourceGroupsCanBeListedAsync();
		IReadOnlyList<CameraInfo> discovered;

		try
		{
			discovered = await WindowsCameraEnvironment.DiscoverCamerasAsync();
		}
		catch (UnauthorizedAccessException exception)
		{
			throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
		}

		using var view = new CameraView();

		var cameras = await view.GetAvailableCameras(CancellationToken.None);

		Assert.Equal(discovered.Select(camera => camera.DeviceId).Order(), cameras.Select(camera => camera.DeviceId).Order());
		Assert.Same(TestServices.Services.GetRequiredService<ICameraProvider>().AvailableCameras, cameras);
	}

	[Fact]
	public async Task PreviewViewIsLoadedInTheWindowAndStaysConnectedWhenRemoved()
	{
		SetupBuilder();
		var view = new CameraView { WidthRequest = 160, HeightRequest = 120 };

		await AttachAndRun<WindowsNoConnectCameraViewHandler>(view, async handler =>
		{
			await view.OnLoadedAsync();

			Assert.True(view.IsLoadedOnPlatform());
			var mediaElement = Assert.IsType<MediaPlayerElement>(handler.PlatformView);
			Assert.True(mediaElement.IsLoaded);
		});

		// WinUI objects may only be used on their own thread (RPC_E_WRONG_THREAD otherwise): OnUnloadedAsync and IsLoadedOnPlatform read
		// FrameworkElement.IsLoaded.
		await InvokeOnMainThreadAsync(async () =>
		{
			await view.OnUnloadedAsync();
			Assert.False(view.IsLoadedOnPlatform());
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task HandlerViewAndPlatformViewAreCollectedAfterDisconnect()
	{
		SetupBuilder();
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, managerReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			viewReference = new(view);
			handlerReference = new(handler);
			platformViewReference = new(handler.PlatformView);
			managerReference = new(handler.CameraManager);

			((IElementHandler)handler).DisconnectHandler();
			view.Handler = null;
		});

		await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference, managerReference);
	}

	static void DisconnectAndRelease(CameraView view)
	{
		view.Handler?.DisconnectHandler();
		view.Handler = null;
	}
}