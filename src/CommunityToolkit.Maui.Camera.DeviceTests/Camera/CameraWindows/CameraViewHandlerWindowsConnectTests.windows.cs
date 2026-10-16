using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture;

namespace CommunityToolkit.Maui.DeviceTests;

// The `async void` members of CameraViewHandler on Windows: ConnectHandler (the real handler, not WindowsNoConnectCameraViewHandler, which
// connects the camera as soon as it gets a platform view), MapSelectedCamera and MapImageCaptureResolution, and the provider lookup of the
// handler. The provider is a scripted one without cameras (WindowsCameraProviderOverride), so the connection fails before any MediaCapture
// exists (no camera and no Media Foundation needed), and WindowsRecordingSynchronizationContext receives the exception that the async void
// method could not deliver. The same shared code is run on iOS and Mac Catalyst by CameraViewHandlerAppleConnectTests.
[Category(TestCategory.CameraWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewHandlerWindowsConnectTests : CameraHandlerTestBase
{
	// How long the async void mappers may take to give up on a camera that Media Foundation cannot open.
	static readonly TimeSpan asyncVoidTimeout = TimeSpan.FromSeconds(30);

	[Fact]
	public async Task CreatingTheHandlerWithoutAPlatformApplicationThrowsCameraException()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			CameraException exception;

			using (WindowsCameraProviderOverride.WithoutPlatformApplication())
			{
				exception = Assert.Throws<CameraException>(() => new CameraViewHandler());
			}

			Assert.Equal("CameraProvider not found", exception.Message);
		});
	}

	// An app that never called UseMauiCommunityToolkitCamera: GetRequiredService throws for the missing ICameraProvider.
	[Fact]
	public async Task CreatingTheHandlerWithoutARegisteredCameraProviderThrowsInvalidOperationException()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			InvalidOperationException exception;

			using (WindowsCameraProviderOverride.WithoutCameraProvider())
			{
				exception = Assert.Throws<InvalidOperationException>(() => new CameraViewHandler());
			}

			Assert.Contains(typeof(ICameraProvider).FullName!, exception.Message, StringComparison.Ordinal);
		});
	}

	// MapSelectedCamera without a camera (Media Foundation is needed for the stand-in MediaCapture): the manager looks started (a preview
	// source, an uninitialized MediaCapture, isInitialized) and the view selects a camera that no device has. UpdateCurrentCamera stops the
	// running preview before it opens the new camera, so when the property setter returns, the preview source is gone and the running capture
	// is closed: had the new camera been opened first, its first await (or its failure) would come before the stop. Opening the unknown camera
	// may have failed by then already, because Media Foundation can refuse an unknown device ID at once (0x80070490, element not found, on
	// Windows 11 26200 with cameras), and the manager then holds no capture; what the restart ends with is
	// ChangingTheSelectedCameraToOneThatCannotBeOpenedRaisesNoUnhandledException.
	[Fact]
	public async Task ChangingTheSelectedCameraWhileThePreviewRunsStopsItBeforeOpeningTheNewCamera()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);

			try
			{
				var (mediaElement, runningCapture) = SeedARunningPreview(access);
				var context = new WindowsRecordingSynchronizationContext();

				using (context.Install())
				{
					view.SelectedCamera = new CameraInfo("Camera that no device has", "fake-connect-unknown-device", CameraPosition.Rear, false, 1f, 1f, [], null!, []);
				}

				Assert.Null(mediaElement.Source);
				Assert.NotSame(runningCapture, access.MediaCapture);
				Assert.True(await WindowsCameraEnvironment.IsClosedAsync(runningCapture), "The running capture was not closed before the new camera was opened.");
			}
			finally
			{
				DisconnectAndRelease(view);
			}
		});
	}
	[Fact]
	public async Task ConnectingTheHandlerWithoutAnyCameraLeavesThePreviewIdle()
	{
		var provider = new WindowsFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [] };

		await ConnectAsync(provider, (view, handler, _, _) =>
		{
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);

			Assert.Equal(1, provider.RefreshCount);
			Assert.Null(view.SelectedCamera);
			Assert.False(access.IsInitialized);
			Assert.Null(access.MediaCapture);
			Assert.Null(access.MediaElement?.Source);
		});
	}

	// ConnectHandler is `async void`, so an exception of ConnectCamera reaches no caller: nothing may escape, which terminates the app on a
	// machine without a camera (the same shared code runs on iOS and Mac Catalyst in CameraViewHandlerAppleConnectTests).
	[Fact]
	public async Task ConnectingTheHandlerWithoutAnyCameraRaisesNoUnhandledException()
	{
		var provider = new WindowsFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [] };

		await ConnectAsync(provider, (_, _, unhandled, _) => Assert.Empty(unhandled));
	}

	// The camera permission that was removed (upstream issue #3292) leaves the provider without cameras: the view reports that through
	// ErrorOccurred, the only way an app learns that the camera did not connect.
	[Fact]
	public async Task ConnectingTheHandlerWithoutAnyCameraRaisesErrorOccurred()
	{
		var provider = new WindowsFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [] };

		await ConnectAsync(provider, (_, _, _, errors) =>
		{
			var exception = Assert.IsType<CameraException>(Assert.Single(errors));
			Assert.Equal("Couldn't connect to camera; no cameras available", exception.Message);
		});
	}

	// MapSelectedCamera is `async void` (the same shared code runs on iOS and Mac Catalyst in
	// CameraViewHandlerAppleConnectTests.ChangingTheSelectedCameraToOneThatCannotStartRaisesNoUnhandledException). The Windows trigger: the
	// restart of the preview cannot open a camera that no device has, which PlatformStartCameraPreview reports through ErrorOccurred. Nothing
	// may escape the mapper: an exception there reaches no caller and terminates the app.
	[Fact]
	public async Task ChangingTheSelectedCameraToOneThatCannotBeOpenedRaisesNoUnhandledException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);
			IReadOnlyList<Exception> unhandled;

			try
			{
				SeedARunningPreview(access);
				var cameraThatNoDeviceHas = new CameraInfo("Camera that no device has", "fake-connect-unknown-device", CameraPosition.Rear, false, 1f, 1f,
					[new Size(640, 480)], null!, [WindowsCameraEnvironment.CreateJpegProperties(640, 480)]);
				var context = new WindowsRecordingSynchronizationContext();

				using (context.Install())
				{
					view.SelectedCamera = cameraThatNoDeviceHas;
				}

				unhandled = await context.DrainUntilAsyncVoidMethodsEndAsync(asyncVoidTimeout);
			}
			finally
			{
				DisconnectAndRelease(view);
			}

			Assert.Empty(unhandled);
		});
	}

	// MapImageCaptureResolution is `async void` (the same shared code runs on iOS and Mac Catalyst): when
	// SetMediaStreamPropertiesAsync fails, the exception reaches no caller, so the handler must not let it escape, which terminates the app. The
	// trigger without a camera is a manager that looks started with a MediaCapture that was never initialized; that Media Foundation refuses the
	// photo properties there (or VideoDeviceController throws) is expected but unverified.
	[Fact]
	public async Task ChangingTheResolutionWhenTheCaptureCannotApplyItRaisesNoUnhandledException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);
			IReadOnlyList<Exception> unhandled;

			try
			{
				// Selected while the manager is not initialized, so that the change does not restart the preview.
				view.SelectedCamera = new CameraInfo("Camera with one photo size", "fake-connect-photo-size", CameraPosition.Rear, false, 1f, 1f,
					[new Size(640, 480)], null!, [WindowsCameraEnvironment.CreateJpegProperties(640, 480)]);
				SeedARunningPreview(access);
				var context = new WindowsRecordingSynchronizationContext();

				using (context.Install())
				{
					view.ImageCaptureResolution = new Size(640, 480);
				}

				unhandled = await context.DrainUntilAsyncVoidMethodsEndAsync(asyncVoidTimeout);
			}
			finally
			{
				DisconnectAndRelease(view);
			}

			Assert.Empty(unhandled);
		});
	}

	// MapIsAvailable is `async void`: it sets IsAvailable from the video capture devices that DeviceInformation finds,
	// and an exception of that update reaches no caller, so the handler must not let it escape, which terminates the app. The device
	// enumeration cannot be made to fail, so the trigger is a listener of the view that throws when the handler sets IsAvailable to the state
	// of the devices: the view starts out with the opposite state, so the update of the new handler changes it.
	//
	// The view gets that state before it has a handler. Set later, the change would run MapIsAvailable from inside the test's own SetValue, and
	// where the enumeration completes synchronously (the Windows Server CI runner, which has no camera) the handler's write is nested in that
	// SetValue: MAUI queues a nested set of a property that is being set and applies it when the outer SetValue ends, so the listener would
	// throw out of the test's SetValue instead of out of the handler's update.
	[Fact]
	public async Task AnAvailabilityUpdateThatFailsRaisesNoUnhandledException()
	{
		SetupBuilder();
		var hasCamera = await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = CreateViewWithTheOppositeAvailability(hasCamera);
			var failedUpdates = 0;
			var context = new WindowsRecordingSynchronizationContext();
			IReadOnlyList<Exception> unhandled;

			view.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(CameraView.IsAvailable) && view.IsAvailable == hasCamera)
				{
					failedUpdates++;
					throw new InvalidOperationException("The availability update failed.");
				}
			};

			try
			{
				using (context.Install())
				{
					CreateHandler<WindowsNoConnectCameraViewHandler>(view);
				}

				unhandled = await context.DrainUntilAsyncVoidMethodsEndAsync(asyncVoidTimeout);
			}
			finally
			{
				DisconnectAndRelease(view);
			}

			Assert.Empty(unhandled);
			// The update of the new handler, once: the listener's exception ends that set before the mapper of IsAvailable could run again.
			Assert.Equal(1, failedUpdates);
		});
	}

	// The failure of the `async void` MapIsAvailable reaches no caller (see AnAvailabilityUpdateThatFailsRaisesNoUnhandledException): the view
	// reports it through ErrorOccurred.
	[Fact]
	public async Task AnAvailabilityUpdateThatFailsRaisesErrorOccurred()
	{
		SetupBuilder();
		var hasCamera = await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = CreateViewWithTheOppositeAvailability(hasCamera);
			using var errors = new CameraErrorRecorder(view);
			var failedUpdates = 0;
			var context = new WindowsRecordingSynchronizationContext();

			view.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == nameof(CameraView.IsAvailable) && view.IsAvailable == hasCamera)
				{
					failedUpdates++;
					throw new InvalidOperationException("The availability update failed.");
				}
			};

			try
			{
				using (context.Install())
				{
					CreateHandler<WindowsNoConnectCameraViewHandler>(view);
				}

				await context.DrainUntilAsyncVoidMethodsEndAsync(asyncVoidTimeout);
			}
			finally
			{
				DisconnectAndRelease(view);
			}

			// The update of the new handler, once: the listener's exception ends that set before the mapper of IsAvailable could run again.
			Assert.Equal(1, failedUpdates);
			Assert.Equal(failedUpdates, errors.Errors.Count);
			Assert.All(errors.Errors, error => Assert.Equal("The availability update failed.", Assert.IsType<InvalidOperationException>(error).Message));
		});
	}

	// A view without a handler whose IsAvailable is the opposite of the state of the devices, so that the availability update of the handler
	// it gets changes IsAvailable (no mapper runs for a view without a handler).
	static CameraView CreateViewWithTheOppositeAvailability(bool hasCamera)
	{
		var view = new CameraView();
		((ICameraView)view).IsAvailable = !hasCamera;

		return view;
	}

	// What a started preview leaves in the manager, without a camera: a preview source on the platform view, a MediaCapture (never
	// initialized; Media Foundation is needed to create it) and isInitialized.
	static (MediaPlayerElement MediaElement, MediaCapture Capture) SeedARunningPreview(WindowsCameraManagerAccessor access)
	{
		var mediaElement = access.MediaElement ?? throw new InvalidOperationException("The handler created no platform view.");
		var capture = WindowsCameraEnvironment.CreateMediaCapture();
		WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);
		access.MediaCapture = capture;
		access.IsInitialized = true;

		return (mediaElement, capture);
	}

	static void DisconnectAndRelease(CameraView view)
	{
		view.Handler?.DisconnectHandler();
		view.Handler = null;
		view.Dispose();
	}

	// Creates the real handler for a new CameraView with the given provider and the recording SynchronizationContext installed, and runs the
	// test with what the async void methods could not deliver and with what the view reported through ErrorOccurred.
	async Task ConnectAsync(ICameraProvider provider, Action<CameraView, CameraViewHandler, IReadOnlyList<Exception>, IReadOnlyList<Exception>> test)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			using var errors = new CameraErrorRecorder(view);
			var context = new WindowsRecordingSynchronizationContext();
			CameraViewHandler handler;

			using (new WindowsCameraProviderOverride(provider))
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
}