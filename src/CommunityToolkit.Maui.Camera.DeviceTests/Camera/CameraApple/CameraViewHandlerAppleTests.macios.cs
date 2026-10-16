using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using Foundation;
using Microsoft.Maui.Handlers;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// The tests use AppleNoConnectCameraViewHandler, which does not connect the camera automatically, so they need neither a camera nor
// the camera permission.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewHandlerAppleTests : CameraHandlerTestBase
{
	static readonly CameraInfo cameraWithoutCaptureDevice = new(
		"Camera without capture device", "fake-handler-no-device", CameraPosition.Front, false, 1f, 4f, [new Size(640, 480)], null!, []);

	[Fact]
	public void HandlerViewTypeUsesUIViewOnApplePlatforms()
	{
		Assert.True(typeof(ViewHandler<ICameraView, UIView>).IsAssignableFrom(typeof(CameraViewHandler)));
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

	// Without custom mappers the handlers take CameraViewHandler's static PropertyMapper and CommandMapper when they are created, so the test
	// swaps them for probes that inherit every default mapping while it creates the handlers: it sees which mappers each handler uses without
	// reading MAUI internals.
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
			AppleMapperCameraViewHandler parameterless, withNullMappers;

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

				parameterless = new AppleMapperCameraViewHandler();
				withNullMappers = new AppleMapperCameraViewHandler(null, null);
			}
			finally
			{
				CameraViewHandler.PropertyMapper = defaultPropertyMapper;
				CameraViewHandler.CommandMapper = defaultCommandMapper;
			}

			var parameterlessView = new CameraView();
			var view = new CameraView { CameraFlashMode = CameraFlashMode.Auto };
			InitializeViewHandler(parameterlessView, parameterless);
			InitializeViewHandler(view, withNullMappers);
			mappedZoomFactors.Clear();

			parameterlessView.ZoomFactor = 2f;
			view.ZoomFactor = 3f;
			parameterless.Invoke("DeviceTestsProbe", 1);
			withNullMappers.Invoke("DeviceTestsProbe", 2);

			List<(CameraViewHandler Handler, float ZoomFactor)> expectedZoomFactors = [(parameterless, 2f), (withNullMappers, 3f)];
			List<(CameraViewHandler Handler, object? Argument)> expectedCommandArguments = [(parameterless, 1), (withNullMappers, 2)];
			Assert.Equal(expectedZoomFactors, mappedZoomFactors);
			Assert.Equal(expectedCommandArguments, commandArguments);
			// The probes inherit the default mappings, so the flash mode of the view still reaches the camera manager.
			Assert.Equal(AVCaptureFlashMode.Auto, new CameraManagerAccessor(withNullMappers.CameraManager).FlashMode);
			view.CameraFlashMode = CameraFlashMode.On;
			Assert.Equal(AVCaptureFlashMode.On, new CameraManagerAccessor(withNullMappers.CameraManager).FlashMode);

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
			var handler = new AppleMapperCameraViewHandler(mapper, commandMapper);
			var view = new CameraView { CameraFlashMode = CameraFlashMode.On };
			InitializeViewHandler(view, handler);
			var access = new CameraManagerAccessor(handler.CameraManager);
			using var device = new FakeCaptureDevice();
			access.CaptureDevice = device;
			access.IsInitialized = true;
			var mappedBeforeTheChange = mappedZoomFactors.Count;

			view.ZoomFactor = 2.5f;
			handler.Invoke("DeviceTestsProbe", 42);

			// The initialized device shows that the replacement ran instead of CameraViewHandler.MapZoomFactor, which would have zoomed it, and the
			// flash mode shows that the mappings that the custom mapper inherits still run.
			Assert.Equal([2.5f], mappedZoomFactors.Skip(mappedBeforeTheChange));
			Assert.Empty(device.Calls);
			Assert.Equal(AVCaptureFlashMode.On, access.FlashMode);
			Assert.Equal([(object?)42], commandArguments);

			access.CaptureDevice = null;
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task HandlerCreatesAPreviewViewBackedByAPhotoSession()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);

			var platformView = handler.PlatformView;
			var previewLayer = Assert.IsType<AVCaptureVideoPreviewLayer>(platformView.Layer, exactMatch: false);
			Assert.Equal(AVLayerVideoGravity.ResizeAspectFill, previewLayer.VideoGravity);
			var session = Assert.IsType<AVCaptureSession>(previewLayer.Session, exactMatch: false);
			Assert.Equal(AVCaptureSession.PresetPhoto, session.SessionPreset);
			Assert.False(session.Running);
			Assert.Same(session, new CameraManagerAccessor(handler.CameraManager).CaptureSession);

			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task HandlerMapsAvailabilityFromTheDefaultVideoCaptureDeviceWhenCreated()
	{
		SetupBuilder();
		var expected = AppleCameraEnvironment.HasVideoCaptureDevice;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			Assert.False(view.IsAvailable);

			CreateHandler<AppleNoConnectCameraViewHandler>(view);

			Assert.Equal(expected, view.IsAvailable);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task ChangingIsAvailableIsOverriddenWithTheStateOfTheDefaultVideoCaptureDevice()
	{
		SetupBuilder();
		var expected = AppleCameraEnvironment.HasVideoCaptureDevice;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			CreateHandler<AppleNoConnectCameraViewHandler>(view);

			((ICameraView)view).IsAvailable = !expected;

			Assert.Equal(expected, view.IsAvailable);
			DisconnectAndRelease(view);
		});
	}

	[Theory]
	[InlineData(CameraFlashMode.On, AVCaptureFlashMode.On)]
	[InlineData(CameraFlashMode.Auto, AVCaptureFlashMode.Auto)]
	[InlineData(CameraFlashMode.Off, AVCaptureFlashMode.Off)]
	public async Task FlashModeChangesReachTheCameraManager(CameraFlashMode flashMode, AVCaptureFlashMode expected)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView { CameraFlashMode = flashMode is CameraFlashMode.Off ? CameraFlashMode.On : CameraFlashMode.Off };
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);

			view.CameraFlashMode = flashMode;

			Assert.Equal(expected, new CameraManagerAccessor(handler.CameraManager).FlashMode);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task PropertyChangesBeforeTheCameraIsConnectedAreAcceptedWithoutTouchingTheSession()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);

			view.IsTorchOn = true;
			view.ZoomFactor = 3f;
			view.ImageCaptureResolution = new Size(800, 600);
			view.SelectedCamera = cameraWithoutCaptureDevice;

			Assert.True(view.IsTorchOn);
			Assert.Equal(3f, view.ZoomFactor);
			Assert.Equal(new Size(800, 600), view.ImageCaptureResolution);
			Assert.Same(cameraWithoutCaptureDevice, view.SelectedCamera);
			Assert.False(access.IsInitialized);
			Assert.Null(access.CaptureDevice);
			Assert.Empty(access.CaptureSession?.Inputs ?? []);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task CameraLoadedCallbackReappliesTheFlashModeAndResetsTheZoomFactor()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			view.CameraFlashMode = CameraFlashMode.Auto;
			view.ZoomFactor = 3f;
			handler.CameraManager.UpdateFlashMode(CameraFlashMode.Off);
			Assert.Equal(AVCaptureFlashMode.Off, access.FlashMode);

			access.OnLoaded();

			Assert.Equal(AVCaptureFlashMode.Auto, access.FlashMode);
			Assert.Equal(1f, view.ZoomFactor);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task DisconnectingTheHandlerDisposesTheCameraManagerAndItsNativeObjects()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var platformView = handler.PlatformView;
			var session = new CameraManagerAccessor(handler.CameraManager).CaptureSession;
			Assert.NotNull(session);

			((IElementHandler)handler).DisconnectHandler();

			var exception = Assert.Throws<InvalidOperationException>(() => handler.CameraManager);
			Assert.Equal("CameraManager cannot be used until the native view has been created", exception.Message);
			Assert.Equal(NativeHandle.Zero, platformView.Handle);
			Assert.Equal(NativeHandle.Zero, session.Handle);
			view.Handler = null;
		});
	}

	[Fact]
	public async Task ReattachingTheViewAfterADisconnectCreatesAFreshPreviewSession()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var first = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var firstPlatformView = first.PlatformView;
			var firstSession = new CameraManagerAccessor(first.CameraManager).CaptureSession;
			Assert.NotNull(firstSession);
			DisconnectAndRelease(view);
			Assert.Equal(NativeHandle.Zero, firstPlatformView.Handle);
			Assert.Equal(NativeHandle.Zero, firstSession.Handle);

			var second = CreateHandler<AppleNoConnectCameraViewHandler>(view);

			Assert.NotSame(first, second);
			var secondSession = new CameraManagerAccessor(second.CameraManager).CaptureSession;
			Assert.NotNull(secondSession);
			Assert.NotSame(firstSession, secondSession);
			Assert.NotEqual(NativeHandle.Zero, secondSession.Handle);
			Assert.NotEqual(NativeHandle.Zero, second.PlatformView.Handle);
			Assert.False(secondSession.Running);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task PropertyChangesAfterTheHandlerWasDisconnectedAreAcceptedWithoutAHandler()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			CreateHandler<AppleNoConnectCameraViewHandler>(view);
			DisconnectAndRelease(view);

			view.IsTorchOn = true;
			view.CameraFlashMode = CameraFlashMode.On;
			view.ZoomFactor = 2f;
			view.ImageCaptureResolution = new Size(800, 600);
			view.SelectedCamera = cameraWithoutCaptureDevice;

			Assert.Null(view.Handler);
			Assert.True(view.IsTorchOn);
			Assert.Equal(CameraFlashMode.On, view.CameraFlashMode);
			Assert.Equal(2f, view.ZoomFactor);
			Assert.Equal(new Size(800, 600), view.ImageCaptureResolution);
			Assert.Same(cameraWithoutCaptureDevice, view.SelectedCamera);
		});
	}

	[Fact]
	public void CameraManagerIsUnavailableBeforeThePlatformViewIsCreated()
	{
		var handler = new AppleNoConnectCameraViewHandler();

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
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var platformView = handler.PlatformView;
			var session = new CameraManagerAccessor(handler.CameraManager).CaptureSession ?? throw new InvalidOperationException("No capture session.");

			handler.Dispose();
			Assert.Equal(NativeHandle.Zero, platformView.Handle);
			Assert.Equal(NativeHandle.Zero, session.Handle);

			Assert.Null(Record.Exception(handler.Dispose));
			Assert.Throws<InvalidOperationException>(() => handler.CameraManager);
		});
	}

	[Fact]
	public async Task CreatePlatformViewWithoutAMauiContextThrowsArgumentNullException()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			var handler = new AppleCreateViewCameraViewHandler();

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
			var handler = CreateHandler<AppleCreateViewCameraViewHandler>(view);
			var manager = handler.CameraManager;
			var session = new CameraManagerAccessor(manager).CaptureSession ?? throw new InvalidOperationException("No capture session.");

			handler.CallDispose(disposing: false);

			Assert.Same(manager, handler.CameraManager);
			Assert.NotEqual(NativeHandle.Zero, session.Handle);
			Assert.NotEqual(NativeHandle.Zero, handler.PlatformView.Handle);

			DisconnectAndRelease(view);
			Assert.Equal(NativeHandle.Zero, session.Handle);
		});
	}

	// CameraViewHandler.Dispose() releases the CameraManager; MAUI still disconnects a handler that the app disposed itself, and
	// DisconnectHandler must not use the released manager (it used to throw InvalidOperationException 'CameraManager cannot be used until
	// the native view has been created').
	[Fact]
	public async Task DisconnectingAHandlerThatWasDisposedBeforeDoesNotThrow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			handler.Dispose();

			((IElementHandler)handler).DisconnectHandler();

			view.Handler = null;
		});
	}

	[Fact]
	public async Task StopCameraPreviewWithoutARunningPreviewDoesNothing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);

			view.StopCameraPreview();
			view.StopCameraPreviewCommand.Execute(null);

			Assert.False(access.IsInitialized);
			Assert.False(access.CaptureSession?.Running);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task StartCameraPreviewWithoutAnyCameraThrowsCameraException()
	{
		Skip.If(AppleCameraEnvironment.HasVideoCaptureDevice,
			$"Needs a device without a camera, such as the iOS simulator: with a camera this would open a capture session and the permission prompt ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);

			try
			{
				var exception = await Assert.ThrowsAsync<CameraException>(() => view.StartCameraPreview(CancellationToken.None));

				Assert.Equal("No camera available on device", exception.Message);
				Assert.False(access.IsInitialized);
			}
			finally
			{
				DisconnectAndRelease(view);
			}
		});
	}

	// A camera that never started has no photo output: CaptureImage throws the camera API's CameraException (as on Windows) instead of an
	// ArgumentNullException for the photo output.
	[Fact]
	public async Task CaptureImageWithoutAStartedCameraThrowsCameraExceptionOnEveryCallAndReleasesTheSemaphore()
	{
		const string cameraNotStartedMessage = "Unable to capture an image because the camera is not initialized. Start the camera preview first.";
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var recorder = new AppleMediaRecorder(view);

			var first = await Assert.ThrowsAsync<CameraException>(() => view.CaptureImage(CancellationToken.None));
			var second = await Assert.ThrowsAsync<CameraException>(() => view.CaptureImage(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Equal(cameraNotStartedMessage, first.Message);
			Assert.Equal(cameraNotStartedMessage, second.Message);
			Assert.Equal([cameraNotStartedMessage, cameraNotStartedMessage], recorder.Failures);
			GC.KeepAlive(recorder);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task StopVideoRecordingWithoutARecordingReturnsTheNullStream()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			CreateHandler<AppleNoConnectCameraViewHandler>(view);

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
		var discovered = await AppleCameraEnvironment.DiscoverCamerasAsync();
		using var view = new CameraView();

		var cameras = await view.GetAvailableCameras(CancellationToken.None);

		Assert.Equal(discovered.Select(camera => camera.DeviceId).Order(), cameras.Select(camera => camera.DeviceId).Order());
		Assert.Same(TestServices.Services.GetRequiredService<ICameraProvider>().AvailableCameras, cameras);
	}

	[Fact]
	public async Task PreviewViewIsLaidOutInTheWindowAndStaysConnectedWhenRemoved()
	{
		await SkipIfTheWindowSceneIsNotActiveAsync();
		SetupBuilder();
		var view = new CameraView { WidthRequest = 160, HeightRequest = 120 };
		AVCaptureVideoPreviewLayer? previewLayer = null;

		await AttachAndRun<AppleNoConnectCameraViewHandler>(view, async handler =>
		{
			await view.OnLoadedAsync();

			Assert.True(view.IsLoadedOnPlatform());
			previewLayer = Assert.IsType<AVCaptureVideoPreviewLayer>(handler.PlatformView.Layer, exactMatch: false);
			Assert.NotNull(previewLayer.Session);
			Assert.Equal(160, handler.PlatformView.Frame.Width);
			Assert.Equal(120, handler.PlatformView.Frame.Height);
			Assert.Null(previewLayer.Connection);
		});

		// UIKit objects belong to the main thread: OnUnloadedAsync and IsLoadedOnPlatform read UIView.Window.
		await InvokeOnMainThreadAsync(async () =>
		{
			await view.OnUnloadedAsync();
			Assert.False(view.IsLoadedOnPlatform());
			Assert.NotNull(previewLayer?.Session);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task HandlerViewAndPlatformViewAreCollectedAfterDisconnect()
	{
		SetupBuilder();
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, managerReference = null!, sessionReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			viewReference = new(view);
			handlerReference = new(handler);
			platformViewReference = new(handler.PlatformView);
			managerReference = new(handler.CameraManager);
			sessionReference = new(new CameraManagerAccessor(handler.CameraManager).CaptureSession);

			((IElementHandler)handler).DisconnectHandler();
			view.Handler = null;
		});

		await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference, managerReference, sessionReference);
	}

	[Fact]
	public async Task TorchChangesReachTheCaptureDevice()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			using var device = new FakeCaptureDevice { TorchIsAvailable = true };
			access.CaptureDevice = device;
			access.IsInitialized = true;

			view.IsTorchOn = true;
			Assert.Equal(["lock", "torch=On", "unlock"], device.Calls);
			Assert.Equal(AVCaptureTorchMode.On, device.TorchMode);

			view.IsTorchOn = false;
			Assert.Equal(["lock", "torch=On", "unlock", "lock", "torch=Off", "unlock"], device.Calls);
			Assert.Equal(AVCaptureTorchMode.Off, device.TorchMode);

			access.CaptureDevice = null;
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task ZoomFactorChangesReachTheCaptureDeviceAndAreClampedToTheSelectedCamera()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			using var device = new FakeCaptureDevice { MinimumZoomFactor = 1, MaximumZoomFactor = 4 };
			view.SelectedCamera = cameraWithoutCaptureDevice;
			access.CaptureDevice = device;
			access.IsInitialized = true;

			view.ZoomFactor = 2.5f;
			Assert.Equal(["lock", "zoom=2.5", "unlock"], device.Calls);
			Assert.Equal(2.5, device.CurrentZoomFactor);

			view.ZoomFactor = 10f;
			Assert.Equal(4f, view.ZoomFactor);
			Assert.Equal(4, device.CurrentZoomFactor);

			access.CaptureDevice = null;
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task ImageCaptureResolutionChangesReachTheCaptureDevice()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			using var device = new FakeCaptureDevice();
			AVCaptureDeviceFormat[] formats = [new FakeCaptureDeviceFormat(640, 480), new FakeCaptureDeviceFormat(1280, 720), new FakeCaptureDeviceFormat(1920, 1080)];
			view.SelectedCamera = new CameraInfo("Fake camera", "fake-handler-fake", CameraPosition.Rear, false, 1f, 4f, [new Size(640, 480)], device, formats);
			access.CaptureDevice = device;
			access.IsInitialized = true;

			view.ImageCaptureResolution = new Size(1280, 720);

			Assert.Equal(["lock", "format=1280x720", "unlock"], device.Calls);

			access.CaptureDevice = null;
			DisconnectAndRelease(view);
		});
	}

	// The photo output reports a pixel format to show that it does not choose the format.
	[Fact]
	public async Task ImageCaptureResolutionThatNoFormatFitsKeepsTheFormatOfTheSessionPreset()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			var presetFormat = new FakeCaptureDeviceFormat(800, 600);
			using var device = new FakeCaptureDevice { CurrentFormat = presetFormat };
			AVCaptureDeviceFormat[] formats = [new FakeCaptureDeviceFormat(640, 480), new FakeCaptureDeviceFormat(1280, 720), new FakeCaptureDeviceFormat(1920, 1080)];
			view.SelectedCamera = new CameraInfo("Fake camera", "fake-handler-preset", CameraPosition.Rear, false, 1f, 4f, [new Size(640, 480)], device, formats);
			var photoOutput = new FakePhotoOutput { SupportedPixelFormats = [NSNumber.FromUInt32(formats[0].FormatDescription.MediaSubType)] };
			access.PhotoOutput = photoOutput;
			access.CaptureDevice = device;
			access.IsInitialized = true;

			view.ImageCaptureResolution = new Size(320, 240);

			Assert.Equal(["lock", "unlock"], device.Calls);
			Assert.Same(presetFormat, device.CurrentFormat);
			Assert.Empty(photoOutput.RequestedFileTypes);

			access.CaptureDevice = null;
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task CameraLoadedCallbackReappliesTheTorchAndResetsTheZoomFactor()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			using var device = new FakeCaptureDevice { TorchIsAvailable = true };
			view.IsTorchOn = true;
			view.ZoomFactor = 3f;
			access.CaptureDevice = device;
			access.IsInitialized = true;
			Assert.Empty(device.Calls);

			access.OnLoaded();

			Assert.Equal(["lock", "torch=On", "unlock", "lock", "zoom=1", "unlock"], device.Calls);
			Assert.True(device.TorchIsActive);
			Assert.Equal(1, device.CurrentZoomFactor);
			Assert.Equal(1f, view.ZoomFactor);

			access.CaptureDevice = null;
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task CaptureImageReturnsThePhotoDataOfTheCapture()
	{
		SetupBuilder();
		byte[] jpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xD9];

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			var output = new FakePhotoOutput
			{
				OnCapture = (fakeOutput, completion) => completion.DidFinishProcessingPhoto(fakeOutput, new FakeCapturePhoto(() => NSData.FromArray(jpegBytes)), null!)
			};
			access.PhotoOutput = output;

			var stream = await view.CaptureImage(CancellationToken.None);

			using var copy = new MemoryStream();
			stream.CopyTo(copy);
			Assert.Equal(jpegBytes, copy.ToArray());
			Assert.Equal(1, output.CaptureCount);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task CaptureImageThrowsCameraExceptionWithTheFailureReasonOfTheCapture()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			var error = new NSError(new NSString("test-domain"), 5, NSDictionary.FromObjectsAndKeys([new NSString("Capture failed")], [NSError.LocalizedDescriptionKey]));
			access.PhotoOutput = new FakePhotoOutput
			{
				OnCapture = (fakeOutput, completion) => completion.DidFinishProcessingPhoto(fakeOutput, null!, error)
			};

			var exception = await Assert.ThrowsAsync<CameraException>(() => view.CaptureImage(CancellationToken.None));

			Assert.Equal("Capture failed", exception.Message);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task CaptureImageHandlesOneCaptureAtATimeAndGivesEachCallerItsOwnPhoto()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			var pendingCompletions = new List<(FakePhotoOutput Output, AVCapturePhotoCaptureDelegate Completion)>();
			var output = new FakePhotoOutput { OnCapture = (fakeOutput, completion) => pendingCompletions.Add((fakeOutput, completion)) };
			access.PhotoOutput = output;

			var first = view.CaptureImage(CancellationToken.None);
			var second = view.CaptureImage(CancellationToken.None);

			Assert.Single(pendingCompletions);
			pendingCompletions[0].Completion.DidFinishProcessingPhoto(output, new FakeCapturePhoto(() => NSData.FromArray([1, 1, 1])), null!);
			var firstStream = await first.WaitAsync(TimeSpan.FromSeconds(5));
			await AssertionExtensions.AssertEventually(() => pendingCompletions.Count is 2, timeout: 5000, message: "The second capture did not start after the first one finished.");
			pendingCompletions[1].Completion.DidFinishProcessingPhoto(output, new FakeCapturePhoto(() => NSData.FromArray([2, 2])), null!);
			var secondStream = await second.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(3, firstStream.Length);
			Assert.Equal(2, secondStream.Length);
			DisconnectAndRelease(view);
		});
	}

	// AttachAndRun looks for the key window of a ForegroundActive scene. A system alert that covers the app (for example a permission
	// prompt that another test app left unanswered on a shared simulator) makes the scene ForegroundInactive.
	async Task SkipIfTheWindowSceneIsNotActiveAsync()
	{
		var activationState = await InvokeOnMainThreadAsync(() =>
		{
			IEnumerable<UIScene> scenes = UIApplication.SharedApplication.ConnectedScenes;
			return scenes.FirstOrDefault()?.ActivationState;
		});

		Skip.IfNot(activationState is UISceneActivationState.ForegroundActive,
			$"The app's window scene is {activationState}, not ForegroundActive, so AttachAndRun cannot find the key window (a system alert covers the app).");
	}

	static void DisconnectAndRelease(CameraView view)
	{
		view.Handler?.DisconnectHandler();
		view.Handler = null;
	}
}