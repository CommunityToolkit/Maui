using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// The real CameraViewHandler (not AppleNoConnectCameraViewHandler) connects the camera from its `async void` ConnectHandler as soon as it gets a
// platform view, and its property mappers for SelectedCamera and ImageCaptureResolution are `async void` too. In an app an exception there
// reaches no caller and terminates it, so AppleRecordingSynchronizationContext receives what they could not deliver and the tests check that
// nothing escapes. The camera provider is scripted (AppleCameraProviderOverride), so the handler never opens a real camera or the permission prompt.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewHandlerAppleConnectTests : CameraHandlerTestBase
{
	static readonly CameraInfo cameraWithoutCaptureDevice = AppleFakeCameras.WithoutCaptureDevice("fake-connect-no-device");

	[Fact]
	public async Task ConnectingTheHandlerWithoutAnyCameraLeavesTheSessionIdle()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [] };

		await ConnectAsync(provider, (view, handler, _, _) =>
		{
			var access = new CameraManagerAccessor(handler.CameraManager);

			Assert.Equal(1, provider.RefreshCount);
			Assert.Null(view.SelectedCamera);
			Assert.False(access.IsInitialized);
			Assert.False(access.CaptureSession?.Running);
			Assert.Empty(access.CaptureSession?.Inputs ?? []);
		});
	}

	[Fact]
	public async Task ConnectingTheHandlerRefreshesTheProviderAndSelectsItsFirstCamera()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [cameraWithoutCaptureDevice] };

		await ConnectAsync(provider, (view, handler, _, _) =>
		{
			var access = new CameraManagerAccessor(handler.CameraManager);

			Assert.Equal(1, provider.RefreshCount);
			Assert.Same(cameraWithoutCaptureDevice, view.SelectedCamera);
			Assert.False(access.IsInitialized);
		});
	}

	[Fact]
	public async Task ConnectingTheHandlerDoesNotRefreshAProviderThatKnowsItsCameras()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = [cameraWithoutCaptureDevice] };

		await ConnectAsync(provider, (view, _, _, _) =>
		{
			Assert.Equal(0, provider.RefreshCount);
			Assert.Same(cameraWithoutCaptureDevice, view.SelectedCamera);
		});
	}

	[Fact]
	public async Task ConnectingTheHandlerKeepsTheCameraThatTheViewAlreadySelected()
	{
		var selected = AppleFakeCameras.WithoutCaptureDevice("fake-connect-selected", CameraPosition.Front);
		var provider = new AppleFakeCameraProvider { AvailableCameras = [cameraWithoutCaptureDevice] };

		await ConnectAsync(provider, (view, _, _, _) =>
		{
			Assert.Same(selected, view.SelectedCamera);
			Assert.Equal(0, provider.RefreshCount);
		}, selected);
	}

	[Fact]
	public async Task ChangingTheSelectedCameraToOneThatCannotStartStopsTheSessionAndKeepsThePreviewStarted()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			var session = new FakeCaptureSession { IsRunning = true };
			access.CaptureSession = session;
			access.IsInitialized = true;
			var context = new AppleRecordingSynchronizationContext();

			using (context.Install())
			{
				view.SelectedCamera = cameraWithoutCaptureDevice;
			}

			context.Drain();

			// The manager stays initialized so that the next SelectedCamera starts the preview
			// (SelectingACameraAfterOneThatCannotStartStartsThePreviewWithIt).
			Assert.Equal(["stop", "begin"], session.Calls.Take(2));
			Assert.True(access.IsInitialized);
			Assert.False(session.IsRunning);
			DisconnectAndRelease(view);
		});
	}

	// A camera switch that fails (here on a camera without a capture device, as for one that another app uses) used to leave the manager
	// stopped, and the handler then ignored every later SelectedCamera: the preview stayed black until the app called StartCameraPreview.
	[Fact]
	public async Task SelectingACameraAfterOneThatCannotStartStartsThePreviewWithIt()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var device = new FakeCaptureDevice { Identifier = "fake-connect-valid" };
			var validCamera = AppleFakeCameras.Create(device, CameraPosition.Rear, new FakeCaptureDeviceFormat(640, 480));
			using var inputs = new AppleDeviceInputStub();
			using var coordinators = new AppleRotationCoordinatorStub();
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			var session = new FakeCaptureSession { IsRunning = true };
			access.CaptureSession = session;
			access.IsInitialized = true;
			var context = new AppleRecordingSynchronizationContext();

			using (context.Install())
			{
				view.SelectedCamera = cameraWithoutCaptureDevice;
				view.SelectedCamera = validCamera;
			}

			var unhandled = context.Drain();

			Assert.Empty(unhandled);
			Assert.Same(device, Assert.Single(inputs.Devices));
			Assert.Same(device, access.CaptureDevice);
			Assert.Equal(inputs.CreatedInputs[0], Assert.Single(session.Inputs).Handle.Handle);
			Assert.True(session.IsRunning);
			Assert.True(access.IsInitialized);
			access.CaptureDevice = null;
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task CreatingTheHandlerWithoutAPlatformApplicationThrowsCameraException()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			CameraException exception;

			using (AppleCameraProviderOverride.WithoutPlatformApplication())
			{
				exception = Assert.Throws<CameraException>(() => new CameraViewHandler());
			}

			Assert.Equal("CameraProvider not found", exception.Message);
		});
	}

	[Fact]
	public async Task GetAvailableCamerasWithoutAPlatformApplicationThrowsCameraException()
	{
		using var view = new CameraView();
		CameraException exception;

		using (AppleCameraProviderOverride.WithoutPlatformApplication())
		{
			exception = await Assert.ThrowsAsync<CameraException>(() => view.GetAvailableCameras(CancellationToken.None).AsTask());
		}

		Assert.Equal("Unable to retrieve CameraProvider", exception.Message);
	}

	// An app that never called UseMauiCommunityToolkitCamera: its services know no ICameraProvider, and GetRequiredService throws (the
	// handler's own CameraException is only for a missing platform application).
	[Fact]
	public async Task CreatingTheHandlerWithoutARegisteredCameraProviderThrowsInvalidOperationException()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			InvalidOperationException exception;

			using (AppleCameraProviderOverride.WithoutCameraProvider())
			{
				exception = Assert.Throws<InvalidOperationException>(() => new CameraViewHandler());
			}

			Assert.Contains(typeof(ICameraProvider).FullName!, exception.Message, StringComparison.Ordinal);
		});
	}

	[Fact]
	public async Task GetAvailableCamerasWithoutARegisteredCameraProviderThrowsInvalidOperationException()
	{
		using var view = new CameraView();
		InvalidOperationException exception;

		using (AppleCameraProviderOverride.WithoutCameraProvider())
		{
			exception = await Assert.ThrowsAsync<InvalidOperationException>(() => view.GetAvailableCameras(CancellationToken.None).AsTask());
		}

		Assert.Contains(typeof(ICameraProvider).FullName!, exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task GetAvailableCamerasRefreshesTheProviderOnlyWhenItKnowsNoCameras()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [cameraWithoutCaptureDevice] };
		using var view = new CameraView();
		IReadOnlyList<CameraInfo> first, second;

		using (AppleCameraProviderOverride.With(provider))
		{
			first = await view.GetAvailableCameras(CancellationToken.None);
			second = await view.GetAvailableCameras(CancellationToken.None);
		}

		Assert.Equal(1, provider.RefreshCount);
		Assert.Same(provider.AvailableCameras, first);
		Assert.Same(first, second);
		Assert.Same(cameraWithoutCaptureDevice, Assert.Single(first));
	}

	[Fact]
	public async Task GetAvailableCamerasThrowsCameraExceptionWhenTheProviderStillKnowsNoCameraAfterRefreshing()
	{
		var provider = new AppleNeverPublishingCameraProvider();
		using var view = new CameraView();
		CameraException exception;

		using (AppleCameraProviderOverride.With(provider))
		{
			exception = await Assert.ThrowsAsync<CameraException>(() => view.GetAvailableCameras(CancellationToken.None).AsTask());
		}

		Assert.Equal("No camera available on device", exception.Message);
		Assert.Equal(1, provider.RefreshCount);
	}

	// ConnectHandler is `async void`: a CameraException of ConnectCamera (no camera, a camera without capture device, an input that cannot be
	// created) reaches no caller, and before ConnectCamera reported it through ErrorOccurred, it terminated the app on every device without a
	// camera, such as the iOS simulator (SIGABRT, "Unhandled managed exception: No camera available on device
	// (CommunityToolkit.Maui.Core.CameraException)").
	[Fact]
	public async Task ConnectingTheHandlerWithoutAnyCameraRaisesNoUnhandledException()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [] };

		await ConnectAsync(provider, (_, _, unhandled, _) =>
		{
			Assert.Empty(unhandled);
		});
	}

	[Fact]
	public async Task ConnectingTheHandlerWithoutAnyCameraRaisesErrorOccurred()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [] };

		await ConnectAsync(provider, (_, _, _, errors) =>
		{
			var exception = Assert.IsType<CameraException>(Assert.Single(errors));
			Assert.Equal("Couldn't connect to camera; no cameras available", exception.Message);
		});
	}

	[Fact]
	public async Task ConnectingTheHandlerToACameraThatCannotStartRaisesErrorOccurred()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = [cameraWithoutCaptureDevice] };

		await ConnectAsync(provider, (_, _, unhandled, errors) =>
		{
			Assert.Empty(unhandled);
			var exception = Assert.IsType<CameraException>(Assert.Single(errors));
			Assert.Equal("No Camera found", exception.Message);
		});
	}

	// MapSelectedCamera is `async void`: when the camera switch fails (a camera whose input cannot be created, for example because another app
	// uses it), UpdateCurrentCamera's exception reaches no caller, and before the handler caught it, it terminated the app (the recording
	// context received CameraException('No Camera found')).
	[Fact]
	public async Task ChangingTheSelectedCameraToOneThatCannotStartRaisesNoUnhandledException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			access.CaptureSession = new FakeCaptureSession { IsRunning = true };
			access.IsInitialized = true;
			var context = new AppleRecordingSynchronizationContext();

			using (context.Install())
			{
				view.SelectedCamera = cameraWithoutCaptureDevice;
			}

			var unhandled = context.Drain();
			DisconnectAndRelease(view);

			Assert.Empty(unhandled);
		});
	}

	// The CameraException of a camera switch that fails reaches no caller of the `async void` MapSelectedCamera. The handler used to only trace
	// it, so the app never learned why the preview stopped.
	[Fact]
	public async Task ChangingTheSelectedCameraToOneThatCannotStartRaisesErrorOccurred()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			using var errors = new CameraErrorRecorder(view);
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			access.CaptureSession = new FakeCaptureSession { IsRunning = true };
			access.IsInitialized = true;
			var context = new AppleRecordingSynchronizationContext();

			using (context.Install())
			{
				view.SelectedCamera = cameraWithoutCaptureDevice;
			}

			context.Drain();
			DisconnectAndRelease(view);

			var exception = Assert.IsType<CameraException>(Assert.Single(errors.Errors));
			Assert.Equal("No Camera found", exception.Message);
		});
	}

	// Modeled with FakeCaptureDevice in its strict mode, which raises like Apple documents for -[AVCaptureDevice setActiveFormat:] (a real
	// reproduction needs two cameras). After StopCameraPreview and a camera change (UpdateCurrentCamera does nothing while stopped) the
	// SelectedCamera and the capture device of the last started camera are different devices: applying the formats of the one to the other
	// raises NSInvalidArgumentException, which the `async void` MapImageCaptureResolution must not let escape, and must not leave the device
	// locked.
	[Fact]
	public async Task ChangingTheResolutionAfterSelectingAnotherCameraWhileStoppedRaisesNoUnhandledException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var stoppedCameraDevice = new FakeCaptureDevice { Identifier = "fake-stopped", RejectsFormatsOfOtherDevices = true };
			stoppedCameraDevice.DeviceFormats = [new FakeCaptureDeviceFormat(1280, 720)];
			using var otherDevice = new FakeCaptureDevice { Identifier = "fake-other" };
			var otherFormat = new FakeCaptureDeviceFormat(640, 480);
			otherDevice.DeviceFormats = [otherFormat];
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			// What a started and then stopped preview of the first camera leaves behind.
			access.CaptureDevice = stoppedCameraDevice;
			access.IsInitialized = false;
			view.SelectedCamera = AppleFakeCameras.Create(otherDevice, CameraPosition.Front, otherFormat);
			var context = new AppleRecordingSynchronizationContext();

			using (context.Install())
			{
				view.ImageCaptureResolution = new Size(640, 480);
			}

			var unhandled = context.Drain();
			access.CaptureDevice = null;
			DisconnectAndRelease(view);

			Assert.Empty(unhandled);
			Assert.False(stoppedCameraDevice.IsLocked);
		});
	}

	// The device rejects the format that the resolution selects (FakeCaptureDevice's strict mode, after Apple's documentation of
	// -[AVCaptureDevice setActiveFormat:]: NSInvalidArgumentException for a format that is not one of the device's), so UpdateCaptureResolution
	// raises: the `async void` MapImageCaptureResolution must not let that escape, it would terminate the app.
	[Fact]
	public async Task ChangingTheResolutionToAFormatThatTheDeviceRejectsRaisesNoUnhandledException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var device = new FakeCaptureDevice { Identifier = "fake-rejecting", RejectsFormatsOfOtherDevices = true };
			device.DeviceFormats = [new FakeCaptureDeviceFormat(1280, 720)];
			var rejectedFormat = new FakeCaptureDeviceFormat(640, 480);
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			view.SelectedCamera = AppleFakeCameras.Create(device, CameraPosition.Rear, rejectedFormat);
			// What a started preview of that camera leaves behind.
			access.CaptureDevice = device;
			access.IsInitialized = true;
			var context = new AppleRecordingSynchronizationContext();

			using (context.Install())
			{
				view.ImageCaptureResolution = new Size(640, 480);
			}

			var unhandled = context.Drain();
			access.CaptureDevice = null;
			access.IsInitialized = false;
			DisconnectAndRelease(view);

			Assert.Contains("format-rejected", device.Calls);
			Assert.Empty(unhandled);
			Assert.False(device.IsLocked);
		});
	}

	// The device rejects the format as in ChangingTheResolutionToAFormatThatTheDeviceRejectsRaisesNoUnhandledException.
	[Fact]
	public async Task ChangingTheResolutionToAFormatThatTheDeviceRejectsRaisesErrorOccurred()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var device = new FakeCaptureDevice { Identifier = "fake-rejecting-reported", RejectsFormatsOfOtherDevices = true };
			device.DeviceFormats = [new FakeCaptureDeviceFormat(1280, 720)];
			var rejectedFormat = new FakeCaptureDeviceFormat(640, 480);
			var view = new CameraView();
			using var errors = new CameraErrorRecorder(view);
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			view.SelectedCamera = AppleFakeCameras.Create(device, CameraPosition.Rear, rejectedFormat);
			// What a started preview of that camera leaves behind.
			access.CaptureDevice = device;
			access.IsInitialized = true;
			var context = new AppleRecordingSynchronizationContext();

			using (context.Install())
			{
				view.ImageCaptureResolution = new Size(640, 480);
			}

			context.Drain();
			access.CaptureDevice = null;
			access.IsInitialized = false;
			DisconnectAndRelease(view);

			var exception = Assert.IsType<ObjCException>(Assert.Single(errors.Errors));
			Assert.Equal("NSInvalidArgumentException", exception.Name);
		});
	}

	// Disconnecting the handler at the end disposes the manager, whose session stop raises if a failed start left its configuration block open.
	async Task ConnectAsync(ICameraProvider provider, Action<CameraView, CameraViewHandler, IReadOnlyList<Exception>, IReadOnlyList<Exception>> test, CameraInfo? selectedCamera = null)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView { SelectedCamera = selectedCamera };
			using var errors = new CameraErrorRecorder(view);
			var context = new AppleRecordingSynchronizationContext();
			CameraViewHandler handler;

			using (AppleCameraProviderOverride.With(provider))
			using (context.Install())
			{
				handler = CreateHandler<CameraViewHandler>(view);
			}

			var unhandled = context.Drain();

			try
			{
				test(view, handler, unhandled, errors.Errors);
			}
			finally
			{
				DisconnectAndRelease(view);
			}
		});
	}

	static void DisconnectAndRelease(CameraView view)
	{
		view.Handler?.DisconnectHandler();
		view.Handler = null;
		view.Dispose();
	}
}