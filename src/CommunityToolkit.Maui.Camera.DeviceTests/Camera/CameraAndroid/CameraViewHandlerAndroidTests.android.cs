using Android.Content.PM;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// CameraViewHandler on Android: the real handler hosts a CameraView in the runner's window, creates the CameraX PreviewView,
/// connects the camera, maps the view's properties and releases everything when it disconnects. Every assertion reads the
/// native CameraX state (camera state, torch, zoom, flash mode, negotiated resolution, bound use cases) or the PreviewView.
/// </summary>
[Category(TestCategory.CameraViewHandlerAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewHandlerAndroidTests : CameraAndroidTestBase
{
	static Task WaitUntilAsync(Func<bool> condition, string message) => AssertionExtensions.AssertEventually(condition, 30000, 50, message);

	// ---- creating the handler ---------------------------------------------------------------------------------------------

	[Fact]
	public async Task HandlerCreatesAFitCenterPreviewViewAndOpensTheFirstNativeCamera()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			var nativeCameras = (await GetNativeCameraProviderAsync()).AvailableCameraInfos;
			var firstId = GetNativeCameraId(nativeCameras[0]);

			Assert.Same(host.PreviewView, host.Handler.PlatformView);
			Assert.Equal(PreviewView.ScaleType.FitCenter, host.PreviewView.GetScaleType());
			Assert.Equal(firstId, host.View.SelectedCamera?.DeviceId);
			Assert.Equal("OPEN", await GetNativeCameraStateAsync(firstId));
			foreach (var other in nativeCameras.Skip(1))
			{
				Assert.Equal("CLOSED", GetNativeCameraState(other));
			}

			Assert.True(await IsNativelyBoundAsync(host.Native<Preview>("cameraPreview")!));
			Assert.True(await IsNativelyBoundAsync(host.Native<ImageCapture>("imageCapture")!));
			Assert.False(await IsNativelyBoundAsync(host.Native<AndroidX.Camera.Video.VideoCapture>("videoCapture")!));
		});
	}

	[Fact]
	public async Task HandlerMapsTheAvailabilityOfTheCameraFeature()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(host =>
		{
			Assert.True(AppContext.PackageManager!.HasSystemFeature(PackageManager.FeatureCamera));
			Assert.True(host.View.IsAvailable);
			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task HandlerMapsTheAvailabilityFromThePackageManagerOfItsOwnContext()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var context = new ConfigurableContext(AppContext);
		var view = new CameraView();
		CameraViewHandler handler = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			handler = CreateHandler<CameraViewHandler>(view, new TestMauiContext(MauiContext, context));
			RegisterHandlerCleanup(handler);
		});
		Assert.True(view.IsAvailable);

		// The handler also connects the camera; the disconnect at the end of the test must not interrupt that.
		await WaitUntilTheHandlerConnectedTheCameraAsync(handler);

		await InvokeOnMainThreadAsync(() =>
		{
			context.HidePackageManager = true;
			handler.UpdateValue(nameof(ICameraView.IsAvailable));
		});
		Assert.False(view.IsAvailable);

		await InvokeOnMainThreadAsync(() =>
		{
			context.HidePackageManager = false;
			handler.UpdateValue(nameof(ICameraView.IsAvailable));
		});
		Assert.True(view.IsAvailable);
	}

	[Fact]
	public async Task HandlerTakesItsCameraProviderFromThePlatformApplication()
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		var provider = new StubCameraProvider { AvailableCameras = null, CamerasAfterRefresh = cameras };
		UseCameraProvider(provider);

		await RunWithCameraViewAsync(host =>
		{
			Assert.Equal(1, provider.RefreshCount);
			Assert.Same(cameras[0], host.View.SelectedCamera);
			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task HandlerDoesNotRefreshTheCamerasOfAProviderThatAlreadyKnowsThem()
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		var provider = new StubCameraProvider { AvailableCameras = cameras };
		UseCameraProvider(provider);

		await RunWithCameraViewAsync(host =>
		{
			Assert.Equal(0, provider.RefreshCount);
			Assert.Same(cameras[0], host.View.SelectedCamera);
			return Task.CompletedTask;
		});
	}

	[Fact]
	public void HandlerThrowsCameraExceptionWhenThereIsNoPlatformApplication()
	{
		SetupBuilder();
		var previous = IPlatformApplication.Current;
		IPlatformApplication.Current = null;

		try
		{
			var exception = Assert.Throws<CameraException>(() => new CameraViewHandler());

			Assert.Equal("CameraProvider not found", exception.Message);
		}
		finally
		{
			IPlatformApplication.Current = previous;
		}
	}

	[Fact]
	public void HandlerThrowsInvalidOperationExceptionWhenNoCameraProviderIsRegistered()
	{
		SetupBuilder();
		var previous = IPlatformApplication.Current ?? throw new InvalidOperationException("The runner has no IPlatformApplication.");
		IPlatformApplication.Current = new StubPlatformApplication(new ServiceCollection().BuildServiceProvider(), previous.Application);

		try
		{
			Assert.Throws<InvalidOperationException>(() => new CameraViewHandler());
		}
		finally
		{
			IPlatformApplication.Current = previous;
		}
	}

	[Fact]
	public void CameraManagerOfAHandlerWithoutAPlatformViewThrowsInvalidOperationException()
	{
		SetupBuilder();
		var handler = new CameraViewHandler();

		var exception = Assert.Throws<InvalidOperationException>(() => handler.CameraManager);

		Assert.Equal("CameraManager cannot be used until the native view has been created", exception.Message);
	}

	[Fact]
	public void SettingTheVirtualViewWithoutAMauiContextThrowsArgumentNullException()
	{
		SetupBuilder();
		var handler = new CameraViewHandler();

		var exception = Assert.Throws<ArgumentNullException>(() => handler.SetVirtualView(new CameraView()));

		Assert.Equal("MauiContext", exception.ParamName);
	}

	[Fact]
	public async Task HandlerWithoutMappersUsesTheDefaultMappers()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var view = new CameraView();
		await InvokeOnMainThreadAsync(() =>
		{
			var handler = new CameraViewHandler(null, null);
			handler.SetMauiContext(MauiContext);
			handler.SetVirtualView(view);
		});

		await RunWithCameraViewAsync(host =>
		{
			host.View.CameraFlashMode = CameraFlashMode.On;

			Assert.Equal(ImageCapture.FlashModeOn, host.Native<ImageCapture>("imageCapture")!.FlashMode);
			return Task.CompletedTask;
		}, view);
	}

	[Fact]
	public async Task HandlerUsesTheMappersItWasGiven()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var mappedFlashModes = new List<CameraFlashMode>();
		var mapper = new PropertyMapper<ICameraView, CameraViewHandler>(CameraViewHandler.PropertyMapper)
		{
			[nameof(ICameraView.CameraFlashMode)] = (_, view) => mappedFlashModes.Add(view.CameraFlashMode)
		};
		var view = new CameraView();
		await InvokeOnMainThreadAsync(() =>
		{
			var handler = new CameraViewHandler(mapper, CameraViewHandler.CommandMapper);
			handler.SetMauiContext(MauiContext);
			handler.SetVirtualView(view);
		});

		await RunWithCameraViewAsync(host =>
		{
			mappedFlashModes.Clear();

			host.View.CameraFlashMode = CameraFlashMode.On;

			// The custom mapping replaced the default one: it saw the change and the native flash mode did not follow.
			Assert.Equal(new[] { CameraFlashMode.On }, mappedFlashModes);
			Assert.Equal(ImageCapture.FlashModeOff, host.Native<ImageCapture>("imageCapture")!.FlashMode);
			return Task.CompletedTask;
		}, view);
	}

	// ---- mapping the properties of the view --------------------------------------------------------------------------------

	[Fact]
	public async Task FlashModeOfTheViewReachesTheNativeImageCapture()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(host =>
		{
			var imageCapture = host.Native<ImageCapture>("imageCapture")!;
			Assert.Equal(ImageCapture.FlashModeOff, imageCapture.FlashMode);

			host.View.CameraFlashMode = CameraFlashMode.On;
			Assert.Equal(ImageCapture.FlashModeOn, imageCapture.FlashMode);

			host.View.CameraFlashMode = CameraFlashMode.Auto;
			Assert.Equal(ImageCapture.FlashModeAuto, imageCapture.FlashMode);

			host.View.CameraFlashMode = CameraFlashMode.Off;
			Assert.Equal(ImageCapture.FlashModeOff, imageCapture.FlashMode);
			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task IsTorchOnOfTheViewReachesTheNativeCamera()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.IsFlashSupported);
		Skip.If(camera is null, "No camera of this device has a flash unit, so the torch can not be switched on.");

		await RunWithCameraViewAsync(async host =>
		{
			host.View.IsTorchOn = true;
			await WaitForNativeTorchAsync(camera.DeviceId, true);

			host.View.IsTorchOn = false;
			await WaitForNativeTorchAsync(camera.DeviceId, false);
		}, new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task ZoomFactorOfTheViewReachesTheNativeCamera()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.MaximumZoomFactor > 1f);
		Skip.If(camera is null, "No camera of this device can zoom in.");
		var ratio = Math.Min(2f, camera.MaximumZoomFactor);

		await RunWithCameraViewAsync(async host =>
		{
			host.View.ZoomFactor = ratio;
			await WaitForNativeZoomAsync(camera.DeviceId, ratio);

			host.View.ZoomFactor = 1f;
			await WaitForNativeZoomAsync(camera.DeviceId, 1f);
		}, new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task ZoomFactorAboveTheMaximumOfTheCameraIsCoercedAndReachesTheNativeCameraAsTheMaximum()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.MaximumZoomFactor > 1f);
		Skip.If(camera is null, "No camera of this device can zoom in.");

		await RunWithCameraViewAsync(async host =>
		{
			host.View.ZoomFactor = camera.MaximumZoomFactor + 1000;

			Assert.Equal(camera.MaximumZoomFactor, host.View.ZoomFactor);
			await WaitForNativeZoomAsync(camera.DeviceId, camera.MaximumZoomFactor);
		}, new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task ZoomFactorBelowTheMinimumOfTheCameraIsCoercedAndReachesTheNativeCameraAsTheMinimum()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.MaximumZoomFactor > camera.MinimumZoomFactor);
		Skip.If(camera is null, "No camera of this device can zoom.");
		var ratio = Math.Min(camera.MinimumZoomFactor + 1f, camera.MaximumZoomFactor);

		await RunWithCameraViewAsync(async host =>
		{
			// Zoom in first, so that the native camera has to follow the coerced value back to the minimum.
			host.View.ZoomFactor = ratio;
			await WaitForNativeZoomAsync(camera.DeviceId, ratio);

			host.View.ZoomFactor = camera.MinimumZoomFactor / 2f;

			Assert.Equal(camera.MinimumZoomFactor, host.View.ZoomFactor);
			await WaitForNativeZoomAsync(camera.DeviceId, camera.MinimumZoomFactor, message: $"The native zoom ratio of camera {camera.DeviceId} did not return to the minimum {camera.MinimumZoomFactor}.");
		}, new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task ZoomFactorOfAViewWithoutASelectedCameraIsNotCoercedAndStillReachesTheNativeCamera()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.MaximumZoomFactor > 1f);
		Skip.If(camera is null, "No camera of this device can zoom in.");
		var ratio = Math.Min(2f, camera.MaximumZoomFactor);

		await RunWithCameraViewAsync(async host =>
		{
			// Clearing the selection keeps the running camera (UpdateCurrentCamera ignores null), but the view has no camera left to
			// coerce the zoom against.
			host.View.SelectedCamera = null;

			host.View.ZoomFactor = ratio;
			Assert.Equal(ratio, host.View.ZoomFactor);
			await WaitForNativeZoomAsync(camera.DeviceId, ratio);

			host.View.ZoomFactor = camera.MaximumZoomFactor + 1000;
			Assert.Equal(camera.MaximumZoomFactor + 1000, host.View.ZoomFactor);
		}, new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task ImageCaptureResolutionOfTheViewReachesTheNativeUseCases()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.SupportedResolutions.Contains(SmallResolution));
		Skip.If(camera is null, "The camera does not support a 320x240 capture size.");

		await RunWithCameraViewAsync(async host =>
		{
			host.View.ImageCaptureResolution = SmallResolution;

			// The mapping is async void: the use cases are rebuilt and bound again once CameraX answers.
			await WaitUntilAsync(() => GetNativeResolution(host.Native<ImageCapture>("imageCapture")?.ResolutionInfo) == SmallSize, "The image capture never got the 320x240 resolution.");
			await WaitUntilAsync(() => GetNativeResolution(host.Native<Preview>("cameraPreview")?.ResolutionInfo) == SmallSize, "The preview never got the 320x240 resolution.");
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);
		}, new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task SelectedCameraOfTheViewSwitchesTheNativeCamera()
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, "Needs two cameras to switch between; this device has one.");

		await RunWithCameraViewAsync(async host =>
		{
			var first = host.View.SelectedCamera!;
			var other = cameras.First(camera => camera.DeviceId != first.DeviceId);

			host.View.SelectedCamera = other;

			await WaitForNativeCameraStateAsync(other.DeviceId, "OPEN");
			await WaitForNativeCameraStateAsync(first.DeviceId, "CLOSED");
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);
			Assert.True(await IsNativelyBoundAsync(host.Native<ImageCapture>("imageCapture")!));
		});
	}

	// The app selects another camera while it records: the switch waits for the recording, which keeps its camera and runs on (CameraX would
	// finalize it if its use cases were bound to another camera), and once the recording is stopped the view shows the selected camera.
	[Fact]
	public async Task SelectedCameraOfTheViewSwitchesTheNativeCameraOnceTheRecordingStops()
	{
		SetupBuilder();
		RequireVideoRecording();
		var cameras = await GetRealCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, "Needs two cameras to switch between; this device has one.");
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithCameraViewAsync(async host =>
		{
			var first = host.View.SelectedCamera!;
			var other = cameras.First(camera => camera.DeviceId != first.DeviceId);
			await WaitUntilTheHandlerConnectedTheCameraAsync(host.Handler);
			await host.View.StartVideoRecording(stream).WaitAsync(CameraTimeout);
			await WaitUntilAsync(() => ListRecordingFiles().Except(before).SingleOrDefault() is string path && new FileInfo(path).Length > 0, "The recording never wrote any data into the cache directory.");
			var finalized = host.Native<TaskCompletionSource>("videoRecordingFinalizeTcs")!;

			host.View.SelectedCamera = other;

			// CameraX finalizes a recording whose VideoCapture was unbound within about half a second.
			var window = System.Diagnostics.Stopwatch.StartNew();
			await WaitUntilAsync(() => finalized.Task.IsCompleted || window.ElapsedMilliseconds >= 2000, "The observation window did not end.");
			Assert.False(finalized.Task.IsCompleted, "CameraX finalized the recording after the camera switch.");
			Assert.Equal(first.DeviceId, GetBoundCameraId(host.Native<ICamera>("camera")!));
			Assert.Equal("CLOSED", await GetNativeCameraStateAsync(other.DeviceId));

			Assert.Same(stream, await host.View.StopVideoRecording().WaitAsync(CameraTimeout));

			Assert.True(ReadVideoInfo(stream.ToArray()).HasVideo, "Android does not find a video track in the recording.");
			await WaitForNativeCameraStateAsync(other.DeviceId, "OPEN");
			await WaitForNativeCameraStateAsync(first.DeviceId, "CLOSED");
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);
			Assert.True(await IsNativelyBoundAsync(host.Native<ImageCapture>("imageCapture")!));
		}, includeRecordAudio: true);
	}

	// The app clears SelectedCamera while it records and then selects the camera of the recording again, as the same CameraInfo or as another
	// instance of it (for example a Picker whose items are replaced). That is no camera switch: the recording runs on, and the preview shows
	// the camera once the recording is stopped. The manager used to take the change for StopCameraPreview (SelectedCamera equalled the camera
	// it last started), unbind the use cases and so make CameraX end the recording without telling the app.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SelectingTheCameraOfTheRecordingAgainAfterNullKeepsTheRecordingRunning(bool withAnotherInstanceOfTheCamera)
	{
		SetupBuilder();
		RequireVideoRecording();
		var cameras = await GetRealCamerasAsync();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithCameraViewAsync(async host =>
		{
			var first = host.View.SelectedCamera!;
			var sameCamera = withAnotherInstanceOfTheCamera ? cameras.Single(camera => camera.DeviceId == first.DeviceId) : first;
			Assert.Equal(withAnotherInstanceOfTheCamera, !ReferenceEquals(first, sameCamera));
			await WaitUntilTheHandlerConnectedTheCameraAsync(host.Handler);
			await host.View.StartVideoRecording(stream).WaitAsync(CameraTimeout);
			await WaitUntilAsync(() => ListRecordingFiles().Except(before).SingleOrDefault() is string path && new FileInfo(path).Length > 0, "The recording never wrote any data into the cache directory.");
			var finalized = host.Native<TaskCompletionSource>("videoRecordingFinalizeTcs")!;

			host.View.SelectedCamera = null;
			host.View.SelectedCamera = sameCamera;

			// CameraX finalizes a recording whose VideoCapture was unbound within about half a second.
			var window = System.Diagnostics.Stopwatch.StartNew();
			await WaitUntilAsync(() => finalized.Task.IsCompleted || window.ElapsedMilliseconds >= 2000, "The observation window did not end.");
			Assert.False(finalized.Task.IsCompleted, "CameraX finalized the recording after SelectedCamera came back to the camera of the recording.");
			Assert.Equal(first.DeviceId, GetBoundCameraId(host.Native<ICamera>("camera")!));

			Assert.Same(stream, await host.View.StopVideoRecording().WaitAsync(CameraTimeout));

			// The camera of the recording is bound again for the photo session, so CameraX does not publish its OPEN state again
			Assert.True(ReadVideoInfo(stream.ToArray()).HasVideo, "Android does not find a video track in the recording.");
			Assert.Equal(first.DeviceId, GetBoundCameraId(host.Native<ICamera>("camera")!));
			Assert.True(await IsNativelyBoundAsync(host.Native<ImageCapture>("imageCapture")!), "The ImageCapture of the camera is not bound.");
			Assert.False(await IsNativelyBoundAsync(host.Native<AndroidX.Camera.Video.VideoCapture>("videoCapture")!), "The VideoCapture of the stopped recording is still bound.");
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);
		}, includeRecordAudio: true);
	}

	// ---- Init: what the handler applies again whenever the camera (re)loads --------------------------------------------------

	[Fact]
	public async Task LoadingTheCameraAgainAppliesTheFlashModeToTheRebuiltImageCaptureAndResetsTheZoomFactor()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.SupportedResolutions.Contains(SmallResolution) && camera.MaximumZoomFactor > 1f);
		Skip.If(camera is null, "Needs a camera that can zoom in and captures at 320x240.");

		await RunWithCameraViewAsync(async host =>
		{
			var imageCapture = host.Native<ImageCapture>("imageCapture")!;
			host.View.CameraFlashMode = CameraFlashMode.On;
			host.View.ZoomFactor = Math.Min(2f, camera.MaximumZoomFactor);
			await WaitForNativeZoomAsync(camera.DeviceId, host.View.ZoomFactor);

			// A new resolution rebuilds the use cases (the new ImageCapture starts with CameraX's default flash mode) and restarts
			// the preview, which loads the camera again.
			host.View.ImageCaptureResolution = SmallResolution;

			await WaitUntilAsync(() => !ReferenceEquals(imageCapture, host.Native<ImageCapture>("imageCapture")) && host.Native<ImageCapture>("imageCapture")?.ResolutionInfo is not null, "The image capture was not rebuilt.");
			await WaitUntilAsync(() => host.Native<ImageCapture>("imageCapture")!.FlashMode == ImageCapture.FlashModeOn, "The flash mode was not applied to the rebuilt image capture.");
			await WaitUntilAsync(() => host.View.ZoomFactor == 1f, "The zoom factor was not reset.");
			await WaitForNativeZoomAsync(camera.DeviceId, 1f);
		}, new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task LoadingTheCameraAgainSwitchesTheTorchOnAgainWhenTheViewWantsItOn()
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		var camera = cameras.FirstOrDefault(camera => camera.IsFlashSupported);
		Skip.If(camera is null, "No camera of this device has a flash unit, so the torch can not be switched on.");
		var other = cameras.FirstOrDefault(other => other.DeviceId != camera.DeviceId);
		Skip.If(other is null, "Needs a second camera to switch to and back from.");

		await RunWithCameraViewAsync(async host =>
		{
			host.View.IsTorchOn = true;
			await WaitForNativeTorchAsync(camera.DeviceId, true);

			host.View.SelectedCamera = other;
			await WaitForNativeCameraStateAsync(other.DeviceId, "OPEN");
			await WaitForNativeCameraStateAsync(camera.DeviceId, "CLOSED");
			Assert.True(host.View.IsTorchOn);

			// The torch of the first camera went out with its session; loading it again maps IsTorchOn once more.
			host.View.SelectedCamera = camera;
			await WaitForNativeCameraStateAsync(camera.DeviceId, "OPEN");
			await WaitForNativeTorchAsync(camera.DeviceId, true);
		}, new CameraView { SelectedCamera = camera });
	}

	// ---- properties that were set while the view had no handler ------------------------------------------------------------------

	[Fact]
	public async Task FlashModeAndTorchSetBeforeTheViewIsHostedAreAppliedWhenTheCameraLoads()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.IsFlashSupported);
		Skip.If(camera is null, "No camera of this device has a flash unit, so the torch can not be switched on.");
		var view = new CameraView { SelectedCamera = camera, CameraFlashMode = CameraFlashMode.On, IsTorchOn = true };

		await RunWithCameraViewAsync(async host =>
		{
			// The mappings ran while the handler had no camera and did nothing; the handler applies them again when the camera loads.
			Assert.Equal(ImageCapture.FlashModeOn, host.Native<ImageCapture>("imageCapture")!.FlashMode);
			await WaitForNativeTorchAsync(camera.DeviceId, true);
		}, view);
	}

	[Fact]
	public async Task ImageCaptureResolutionSetBeforeTheViewIsHostedIsUsedWhenTheCameraConnects()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.SupportedResolutions.Contains(SmallResolution));
		Skip.If(camera is null, "The camera does not support a 320x240 capture size.");
		var view = new CameraView { SelectedCamera = camera, ImageCaptureResolution = SmallResolution };

		await RunWithCameraViewAsync(host =>
		{
			// The first use cases the handler builds already have the resolution of the view.
			Assert.Equal(SmallSize, GetNativeResolution(host.Native<ImageCapture>("imageCapture")!.ResolutionInfo));
			Assert.Equal(SmallSize, GetNativeResolution(host.Native<Preview>("cameraPreview")!.ResolutionInfo));
			return Task.CompletedTask;
		}, view);
	}

	[Fact]
	public async Task ViewCanBeHostedAgainAfterItsHandlerWasDisconnected()
	{
		SetupBuilder();
		var view = new CameraView();
		CameraViewHandler? firstHandler = null;
		string? firstCameraId = null;

		await RunWithCameraViewAsync(host =>
		{
			firstHandler = host.Handler;
			firstCameraId = host.SelectedCameraId;
			((IElementHandler)host.Handler).DisconnectHandler();
			return Task.CompletedTask;
		}, view, disconnectWhenDone: false);
		await WaitForAllCamerasClosedAsync();

		await RunWithCameraViewAsync(async host =>
		{
			Assert.NotSame(firstHandler, host.Handler);
			Assert.Equal(firstCameraId, host.SelectedCameraId);
			Assert.Equal("OPEN", await GetNativeCameraStateAsync(host.SelectedCameraId));
			Assert.True(await IsNativelyBoundAsync(host.Native<Preview>("cameraPreview")!));
			Assert.True(await IsNativelyBoundAsync(host.Native<ImageCapture>("imageCapture")!));
		}, view);
	}

	// ---- disconnecting and disposing -------------------------------------------------------------------------------------------

	[Fact]
	public async Task DisconnectHandlerClosesTheCameraAndReleasesTheManager()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			var cameraId = host.SelectedCameraId;
			var streamState = host.PreviewView.PreviewStreamState;

			((IElementHandler)host.Handler).DisconnectHandler();

			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
			await WaitUntilAsync(() => streamState.Value?.ToString() == "IDLE", "The preview kept streaming after the handler disconnected.");
			var exception = Assert.Throws<InvalidOperationException>(() => host.Handler.CameraManager);
			Assert.Equal("CameraManager cannot be used until the native view has been created", exception.Message);
			Assert.Null(host.View.Handler);
		}, disconnectWhenDone: false);
	}

	[Fact]
	public async Task DisconnectingTheHandlerTwiceIsSafe()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(host =>
		{
			((IElementHandler)host.Handler).DisconnectHandler();

			var exception = Record.Exception(() => ((IElementHandler)host.Handler).DisconnectHandler());

			Assert.Null(exception);
			return Task.CompletedTask;
		}, disconnectWhenDone: false);
	}

	[Fact]
	public async Task ViewMethodsThrowInvalidOperationExceptionOnceTheHandlerIsDisconnected()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			((IElementHandler)host.Handler).DisconnectHandler();

			var start = await Assert.ThrowsAsync<InvalidOperationException>(() => host.View.StartCameraPreview(CancellationToken.None));
			var capture = await Assert.ThrowsAsync<InvalidOperationException>(() => host.View.CaptureImage(CancellationToken.None));
			var extension = await Assert.ThrowsAsync<InvalidOperationException>(() => host.View.SetExtensionMode(0));
			var stopRecording = await Assert.ThrowsAsync<InvalidOperationException>(() => host.View.StopVideoRecording());
			var stop = Assert.Throws<InvalidOperationException>(host.View.StopCameraPreview);

			Assert.All(new[] { start, capture, extension, stopRecording, stop }, exception => Assert.Equal("Unable to retrieve Handler", exception.Message));
		}, disconnectWhenDone: false);
	}

	[Fact]
	public async Task DisposeClosesTheCameraAndReleasesTheManager()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			var cameraId = host.SelectedCameraId;
			var manager = host.Manager;
			var nativeObjects = new[] { "camera", "cameraControl", "cameraPreview", "imageCapture", "videoCapture", "processCameraProvider", "orientationListener" }
				.ToDictionary(name => name, name => host.Native<Java.Lang.Object>(name));
			Assert.All(nativeObjects, entry => Assert.NotNull(entry.Value));

			host.Handler.Dispose();

			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");

			// The manager releases every native object and disposes the ones it created itself; CameraX's shared ProcessCameraProvider,
			// camera and camera control are only released (see CameraManagerConnectionAndroidTests.DisposeClosesTheCameraAndReleasesTheNativeObjects).
			Assert.All(nativeObjects.Where(entry => !CameraManagerConnectionAndroidTests.IsSharedCameraXObject(entry.Key)), entry => Assert.True(entry.Value!.Handle == IntPtr.Zero, $"{entry.Key} was not disposed."));
			Assert.All(nativeObjects.Keys, name => Assert.Null(GetPrivateField<object>(manager, name)));
			var exception = Assert.Throws<InvalidOperationException>(() => host.Handler.CameraManager);
			Assert.Equal("CameraManager cannot be used until the native view has been created", exception.Message);
		}, disconnectWhenDone: false);
	}

	[Fact]
	public async Task DisposingTheHandlerTwiceIsSafe()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(host =>
		{
			host.Handler.Dispose();

			var exception = Record.Exception(host.Handler.Dispose);

			Assert.Null(exception);
			return Task.CompletedTask;
		}, disconnectWhenDone: false);
	}

	[Fact]
	public async Task DisconnectHandlerAfterDisposeDoesNotThrow()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(host =>
		{
			host.Handler.Dispose();

			var exception = Record.Exception(() => ((IElementHandler)host.Handler).DisconnectHandler());

			Assert.Null(exception);
			return Task.CompletedTask;
		}, disconnectWhenDone: false);
	}

	[Fact]
	public async Task HandlerConnectingWithoutAnyCameraDoesNotLetTheCameraExceptionEscape()
	{
		SetupBuilder();
		UseCameraProvider(new StubCameraProvider { AvailableCameras = [] });

		await RunWithCameraViewAsync(async host =>
		{
			// ConnectHandler is async void: an exception it does not handle can only reach the UI thread, which ends the app.
			await SettleUiThreadAsync();

			Assert.Empty(UnhandledAsyncVoidExceptions);
			Assert.Null(host.View.SelectedCamera);
			await AssertEveryCameraIsClosedAsync();
		}, waitForStreaming: false);
	}

	[Fact]
	public async Task SelectingACameraThatCanNotBeBoundDoesNotCrashAndAValidCameraCanBeSelectedAgain()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			var valid = host.View.SelectedCamera!;

			// MapSelectedCamera (async void) tries to bind the use cases to the selected camera, which CameraX can not do. The CameraX
			// futures it waits for completed when the camera was connected, so their listeners run within a few UI-thread messages.
			host.View.SelectedCamera = CreateCameraThatCanNotBeBound();
			for (var i = 0; i < 10 && UnhandledAsyncVoidExceptions.Count is 0; i++)
			{
				await SettleUiThreadAsync();
			}

			Assert.Empty(UnhandledAsyncVoidExceptions);

			// The app selects a camera that exists again: the view shows its preview.
			host.View.SelectedCamera = valid;
			await WaitForNativeCameraStateAsync(valid.DeviceId, "OPEN");
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);
			Assert.True(await IsNativelyBoundAsync(host.Native<ImageCapture>("imageCapture")!));
		});
	}

	// The camera switch of the async void MapSelectedCamera fails (CameraX cannot bind the selected camera): no caller can catch that, so
	// the view reports it through ErrorOccurred (the handler used to only trace it, so the app never learned why the preview stopped).
	[Fact]
	public async Task SelectingACameraThatCanNotBeBoundRaisesErrorOccurred()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			using var errors = new CameraErrorRecorder(host.View);

			host.View.SelectedCamera = CreateCameraThatCanNotBeBound();
			for (var i = 0; i < 10 && errors.Errors.Count is 0; i++)
			{
				await SettleUiThreadAsync();
			}

			Assert.IsType<Java.Lang.IllegalArgumentException>(Assert.Single(errors.Errors), exactMatch: false);
		});
	}

	// The page closes right after the user switched the camera (or the app changed the capture resolution): the async void mapper
	// is still binding the camera again when the handler disconnects and disposes the manager.
	[Theory]
	[InlineData(nameof(CameraView.SelectedCamera))]
	[InlineData(nameof(CameraView.ImageCaptureResolution))]
	public async Task DisconnectingTheHandlerWhileAPropertyChangeBindsTheCameraDoesNotLetAnExceptionEscape(string property)
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		if (property is nameof(CameraView.SelectedCamera))
		{
			DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, "Needs two cameras to switch between; this device has one.");
		}

		await RunWithCameraViewAsync(async host =>
		{
			var selected = host.View.SelectedCamera!;

			// One UI-thread message: the mapper starts the rebind, which waits for the CameraX futures of EnableModes, and then the
			// handler disconnects.
			if (property is nameof(CameraView.SelectedCamera))
			{
				host.View.SelectedCamera = cameras.First(camera => camera.DeviceId != selected.DeviceId);
			}
			else
			{
				host.View.ImageCaptureResolution = SmallResolution;
			}

			((IElementHandler)host.Handler).DisconnectHandler();

			// The CameraX futures completed when the camera was connected, so their listeners run within a few UI-thread messages.
			for (var i = 0; i < 10 && UnhandledAsyncVoidExceptions.Count is 0; i++)
			{
				await SettleUiThreadAsync();
			}

			Assert.Empty(UnhandledAsyncVoidExceptions);
			await WaitForAllCamerasClosedAsync();
		}, disconnectWhenDone: false);
	}

	// What the disconnect makes fail in the async void mapper (the manager is released while it binds the camera again) is not an error to
	// report: the view is gone, so ErrorOccurred stays silent.
	[Theory]
	[InlineData(nameof(CameraView.SelectedCamera))]
	[InlineData(nameof(CameraView.ImageCaptureResolution))]
	public async Task DisconnectingTheHandlerWhileAPropertyChangeBindsTheCameraRaisesNoErrorOccurred(string property)
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		if (property is nameof(CameraView.SelectedCamera))
		{
			DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, "Needs two cameras to switch between; this device has one.");
		}

		await RunWithCameraViewAsync(async host =>
		{
			var selected = host.View.SelectedCamera!;
			using var errors = new CameraErrorRecorder(host.View);

			if (property is nameof(CameraView.SelectedCamera))
			{
				host.View.SelectedCamera = cameras.First(camera => camera.DeviceId != selected.DeviceId);
			}
			else
			{
				host.View.ImageCaptureResolution = SmallResolution;
			}

			((IElementHandler)host.Handler).DisconnectHandler();

			for (var i = 0; i < 10; i++)
			{
				await SettleUiThreadAsync();
			}

			await WaitForAllCamerasClosedAsync();
			Assert.Empty(errors.Errors);
		}, disconnectWhenDone: false);
	}

	sealed class HandlerThatDisposesWithoutDisposing : CameraViewHandler
	{
		public void DisposeWithoutDisposing() => Dispose(false);
	}

	[Fact]
	public async Task DisposeWithoutDisposingKeepsTheManagerAndTheCameraAndDisposeReleasesThem()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var view = new CameraView();
		var handler = new HandlerThatDisposesWithoutDisposing();
		await InvokeOnMainThreadAsync(() =>
		{
			handler.SetMauiContext(MauiContext);
			handler.SetVirtualView(view);
		});

		await RunWithCameraViewAsync(async host =>
		{
			var manager = host.Manager;

			handler.DisposeWithoutDisposing();

			Assert.Same(manager, handler.CameraManager);
			Assert.Equal("OPEN", await GetNativeCameraStateAsync(host.SelectedCameraId));

			handler.Dispose();

			await WaitForNativeCameraStateAsync(host.SelectedCameraId, "CLOSED");
			Assert.Throws<InvalidOperationException>(() => handler.CameraManager);
		}, view, disconnectWhenDone: false);
	}

	// ---- leaks -----------------------------------------------------------------------------------------------------------------

	[Fact]
	public async Task ViewHandlerAndPreviewViewAreCollectedAfterTheHandlerWasDisconnected()
	{
		SetupBuilder();

		var (view, handler, previewView) = await CreateHostAndDisconnectAsync();
		await WaitForAllCamerasClosedAsync();

		await AssertCollectedAsync(("CameraView", view), ("CameraViewHandler", handler), ("PreviewView", previewView));
	}

	// Everything the test creates lives in this method, so that no local of the test keeps it alive.
	async Task<(WeakReference View, WeakReference Handler, WeakReference PreviewView)> CreateHostAndDisconnectAsync()
	{
		WeakReference? view = null, handler = null, previewView = null;

		await RunWithCameraViewAsync(host =>
		{
			view = new WeakReference(host.View);
			handler = new WeakReference(host.Handler);
			previewView = new WeakReference(host.PreviewView);
			((IElementHandler)host.Handler).DisconnectHandler();
			return Task.CompletedTask;
		}, disconnectWhenDone: false);

		return (view!, handler!, previewView!);
	}
}