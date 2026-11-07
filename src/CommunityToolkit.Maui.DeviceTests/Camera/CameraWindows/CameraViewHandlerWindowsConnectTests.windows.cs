using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture;

namespace CommunityToolkit.Maui.DeviceTests;

// ConnectHandler is tested on the real handler because WindowsNoConnectCameraViewHandler skips connecting the camera. The provider is a
// scripted one without cameras (WindowsCameraProviderOverride), so the connection fails before any MediaCapture exists (no camera and no
// Media Foundation needed), and WindowsRecordingSynchronizationContext receives the exception that an `async void` method could not deliver.
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

	// MapSelectedCamera calls UpdateCurrentCamera, which stops the running preview before it opens the new camera, so when the property setter
	// returns, the preview source is gone and the running capture is closed: had the new camera been opened first, its first await (or its
	// failure) would come before the stop. Opening the unknown camera may have failed by then already, because Media Foundation can refuse an
	// unknown device ID at once (0x80070490, element not found, on Windows 11 26200 with cameras), and the manager then holds no capture; what
	// the restart ends with is ChangingTheSelectedCameraToOneThatCannotBeOpenedRaisesNoUnhandledException.
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
	// machine without a camera.
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

	// An app's CameraViewHandler.PlatformViewFactory creates the platform view in place of CreatePlatformView, which was the only place that
	// created the CameraManager: the first mapper that needed it threw, so the page with the CameraView could not be shown
	[Fact]
	public async Task AHandlerWhosePlatformViewIsAMediaPlayerElementOfThePlatformViewFactoryOfTheAppShowsThePreviewInIt()
	{
		SetupBuilder();
		var provider = new WindowsFakeCameraProvider { AvailableCameras = [] };

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			using var errors = new CameraErrorRecorder(view);
			var mediaElementOfTheApp = new MediaPlayerElement { Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };

			var (handler, exception, unhandled) = CreateHandlerWithThePlatformViewFactoryOfTheApp(view, provider, mediaElementOfTheApp);

			try
			{
				Assert.Null(exception);
				Assert.Empty(unhandled);
				Assert.NotNull(handler);
				Assert.Same(mediaElementOfTheApp, handler.PlatformView);

				// The handler connected the camera through its CameraManager, which found no camera
				Assert.Equal("Couldn't connect to camera; no cameras available", Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
				Assert.Same(mediaElementOfTheApp, new WindowsCameraManagerAccessor(handler.CameraManager).MediaElement);

				// The stretch that the app gave its element, not the Uniform default of the element that the handler creates
				Assert.Equal(Microsoft.UI.Xaml.Media.Stretch.UniformToFill, mediaElementOfTheApp.Stretch);

				// Without a camera no preview starts, so a placeholder stands in for its source: stopping the preview clears it from the element of the app
				WindowsCameraEnvironment.AssignPlaceholderSource(mediaElementOfTheApp);
				view.StopCameraPreview();
				Assert.Null(mediaElementOfTheApp.Source);
			}
			finally
			{
				DisconnectAndRelease(view);
			}
		});
	}

	// Only a MediaPlayerElement can show the preview: with another element of an app's factory the camera works without a preview, and that element
	// stays as the app made it
	[Fact]
	public async Task AHandlerWhosePlatformViewOfThePlatformViewFactoryOfTheAppCannotShowAPreviewStillStartsTheCameraAndLeavesThatViewAlone()
	{
		SetupBuilder();
		var provider = new WindowsFakeCameraProvider { AvailableCameras = [] };

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			using var errors = new CameraErrorRecorder(view);
			var gridOfTheApp = new Microsoft.UI.Xaml.Controls.Grid();

			var (handler, exception, unhandled) = CreateHandlerWithThePlatformViewFactoryOfTheApp(view, provider, gridOfTheApp);

			try
			{
				Assert.Null(exception);
				Assert.Empty(unhandled);
				Assert.NotNull(handler);
				Assert.Same(gridOfTheApp, handler.PlatformView);
				Assert.Empty(gridOfTheApp.Children);
				var mediaElement = new WindowsCameraManagerAccessor(handler.CameraManager).MediaElement;
				Assert.NotNull(mediaElement);
				Assert.Null(mediaElement.Parent);

				// Without a MediaPlayerElement a start returns without trying to open a camera (CameraManager.windows.cs StartPreviewAsync); this
				// one tries, and finds no camera
				await view.StartCameraPreview(CancellationToken.None);

				Assert.Equal(["Couldn't connect to camera; no cameras available", "Couldn't start camera preview; no cameras available"], errors.Errors.Select(error => error.Message));
			}
			finally
			{
				DisconnectAndRelease(view);
			}
		});
	}

	// MapSelectedCamera is `async void`, so nothing may escape the mapper: an exception there reaches no caller and terminates the app. The
	// trigger is the restart of the preview, which cannot open a camera that no device has: PlatformStartCameraPreview reports that through
	// ErrorOccurred.
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

	// MapImageCaptureResolution is `async void`: when SetMediaStreamPropertiesAsync fails, the exception reaches no caller, so the handler must
	// not let it escape, which terminates the app. The trigger without a camera is a manager that looks started with a MediaCapture that was
	// never initialized; that Media Foundation refuses the photo properties there (or VideoDeviceController throws) is expected but unverified.
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

	// MapIsAvailable is `async void`: an exception of its update reaches no caller, so the handler must not let it escape, which terminates the
	// app. The DeviceInformation enumeration of the update cannot be made to fail, so the trigger is a listener of the view that throws when the
	// handler sets IsAvailable to the state of the devices: the view starts out with the opposite state, so the update of the new handler
	// changes it.
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

	// The failure of the `async void` MapIsAvailable reaches no caller, so the view reports it through ErrorOccurred.
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

	static CameraView CreateViewWithTheOppositeAvailability(bool hasCamera)
	{
		var view = new CameraView();
		((ICameraView)view).IsAvailable = !hasCamera;

		return view;
	}

	// Without a camera the MediaCapture is never initialized, and Media Foundation is needed to create it.
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

	// The factory is static: it is set only while the handler is created, so that no other handler gets a view from it. The provider is resolved
	// when the handler is created (CameraViewHandler.cameraProvider), so its override only needs to last that long too.
	(CameraViewHandler? Handler, Exception? Exception, IReadOnlyList<Exception> Unhandled) CreateHandlerWithThePlatformViewFactoryOfTheApp(CameraView view, ICameraProvider provider, Microsoft.UI.Xaml.FrameworkElement platformViewOfTheApp)
	{
		var context = new WindowsRecordingSynchronizationContext();
		var previousFactory = CameraViewHandler.PlatformViewFactory;
		CameraViewHandler? handler = null;
		Exception? exception;

		CameraViewHandler.PlatformViewFactory = _ => platformViewOfTheApp;
		try
		{
			using (new WindowsCameraProviderOverride(provider))
			using (context.Install())
			{
				exception = Record.Exception(() => { handler = CreateHandler<CameraViewHandler>(view); });
			}
		}
		finally
		{
			CameraViewHandler.PlatformViewFactory = previousFactory;
		}

		return (handler, exception, context.Drain());
	}
}