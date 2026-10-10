using Android.Runtime;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;
using ICameraProvider = CommunityToolkit.Maui.Core.ICameraProvider;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraManagerAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerConnectionAndroidTests : CameraAndroidTestBase
{
	[Fact]
	public void ConstructorThrowsCameraExceptionWhenTheMauiContextHasNoAndroidContext()
	{
		SetupBuilder();

		var exception = Assert.Throws<CameraException>(() => new CameraManager(new TestMauiContext(MauiContext, null), new CameraView(), new StubCameraProvider(), () => { }));

		Assert.Equal("Unable to retrieve Context", exception.Message);
	}

	[Fact]
	public async Task ConstructorAcceptsAnAndroidContextThatIsNotAnActivity()
	{
		SetupBuilder();
		CameraManager? manager = null;

		await InvokeOnMainThreadAsync(() =>
		{
			manager = new CameraManager(new TestMauiContext(MauiContext, Android.App.Application.Context), new CameraView(), new StubCameraProvider(), () => { });
			RegisterCleanup(manager.Dispose);
		});

		Assert.NotNull(manager);
	}

	[Fact]
	public async Task CreatePlatformViewReturnsAFitCenterPreviewViewInTheActivity()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();

		var previewView = session.PreviewView!;

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(PreviewView.ScaleType.FitCenter, previewView.GetScaleType());
			Assert.Same(AppContext, previewView.Context);
		});
	}

	[Fact]
	public async Task CreatePlatformViewEnablesTheOrientationListenerAndDisposeDisablesIt()
	{
		SetupBuilder();
		var registeredBefore = await CountOrientationListenersOfThisAppAsync();

		var session = await CreateSessionAsync();

		await AssertionExtensions.AssertEventuallyAsync(async () => await CountOrientationListenersOfThisAppAsync() == registeredBefore + 1, 10000, 100,
			"CreatePlatformView never registered an orientation listener with the sensor service.");

		await InvokeOnMainThreadAsync(session.Manager.Dispose);

		// Disposing the managed listener alone would leave the Java listener registered, so disable has to remove it.
		await AssertionExtensions.AssertEventuallyAsync(async () => await CountOrientationListenersOfThisAppAsync() == registeredBefore, 10000, 100,
			"Dispose left the orientation listener registered with the sensor service.");
	}

	[Fact]
	public async Task CreatePlatformViewDoesNotOpenAnyCamera()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();

		await SettleUiThreadAsync();

		Assert.False(IsStreaming(session.PreviewView!));
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task CreatePlatformViewOffTheUiThreadThrowsIllegalStateException()
	{
		SetupBuilder();
		var session = await CreateSessionAsync(createPlatformView: false);

		// PreviewView.setScaleType insists on the main thread.
		var exception = Assert.Throws<Java.Lang.IllegalStateException>(() => session.Manager.CreatePlatformView());

		Assert.Contains("main thread", exception.Message);
	}

	[Fact]
	public async Task ConnectCameraBindsPreviewAndImageCaptureAndOpensTheSelectedCamera()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);

			var selected = session.View.SelectedCamera;
			Assert.NotNull(selected);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			await WaitForNativeCameraStateAsync(selected.DeviceId, "OPEN");
			Assert.Equal(1, session.LoadedCount);
			Assert.True(await IsNativelyBoundAsync(session.Native<Preview>("cameraPreview")!));
			Assert.True(await IsNativelyBoundAsync(session.Native<ImageCapture>("imageCapture")!));
			Assert.False(await IsNativelyBoundAsync(session.Native<AndroidX.Camera.Video.VideoCapture>("videoCapture")!));
			// The emulators offer only one quality, so a recording can not show that the recorder asks CameraX for the highest one.
			Assert.Contains("name=HIGHEST", session.Native<AndroidX.Camera.Video.Recorder>("videoRecorder")!.QualitySelector.ToString());
			foreach (var other in session.Provider.AvailableCameras!.Where(camera => camera.DeviceId != selected.DeviceId))
			{
				Assert.Equal("CLOSED", await GetNativeCameraStateAsync(other.DeviceId));
			}
		});
	}

	[Fact]
	public async Task ConnectCameraSelectsTheFirstAvailableCameraWhenNoneIsSelected()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			Assert.Null(session.View.SelectedCamera);
			Assert.Null(session.Provider.AvailableCameras);

			await session.ConnectAsync().WaitAsync(CameraTimeout);

			Assert.NotNull(session.Provider.AvailableCameras);
			Assert.Equal(session.Provider.AvailableCameras[0].DeviceId, session.View.SelectedCamera?.DeviceId);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		});
	}

	[Fact]
	public async Task ConnectCameraKeepsTheCameraThatIsAlreadySelected()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var cameras = await GetRealCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, "Needs two cameras to tell the selected one from the first one; this device has one.");

		await RunWithSessionAsync(async session =>
		{
			await session.Provider.RefreshAvailableCameras(CancellationToken.None);
			var second = session.Provider.AvailableCameras![1];
			session.View.SelectedCamera = second;

			await session.ConnectAsync().WaitAsync(CameraTimeout);

			Assert.Same(second, session.View.SelectedCamera);
			await WaitForNativeCameraStateAsync(second.DeviceId, "OPEN");
			Assert.Equal("CLOSED", await GetNativeCameraStateAsync(session.Provider.AvailableCameras[0].DeviceId));
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ConnectCameraRefreshesTheProviderOnlyWhenItHasNoCameras(bool providerKnowsTheCameras)
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var cameras = await GetRealCamerasAsync();
		var provider = new StubCameraProvider
		{
			AvailableCameras = providerKnowsTheCameras ? cameras : null,
			CamerasAfterRefresh = cameras
		};

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);

			Assert.Equal(providerKnowsTheCameras ? 0 : 1, provider.RefreshCount);
			Assert.Equal(cameras[0].DeviceId, session.View.SelectedCamera?.DeviceId);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		}, provider);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ConnectCameraRaisesErrorOccurredWhenNoCameraIsAvailable(bool providerHasAnEmptyList)
	{
		SetupBuilder();
		var provider = new StubCameraProvider { AvailableCameras = providerHasAnEmptyList ? [] : null, CamerasAfterRefresh = null };
		var session = await CreateSessionAsync(provider);
		using var errors = new CameraErrorRecorder(session.View);

		await session.Manager.ConnectCamera(CancellationToken.None);

		var exception = Assert.IsType<CameraException>(Assert.Single(errors.Errors));
		Assert.Equal("Couldn't connect to camera; no cameras available", exception.Message);
		Assert.Null(session.View.SelectedCamera);
		Assert.Equal(providerHasAnEmptyList ? 0 : 1, provider.RefreshCount);
		Assert.Equal(0, session.LoadedCount);
	}

	[Fact]
	public async Task ConnectCameraWithoutAPlatformViewBindsNothing()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var session = await CreateSessionAsync(createPlatformView: false);

		await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);
		await session.Manager.ConnectCamera(CancellationToken.None).WaitAsync(CameraTimeout);
		await SettleUiThreadAsync();

		Assert.Equal(0, session.LoadedCount);
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task ConnectCameraTwiceBuildsNewUseCasesAndBindsThem()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			var firstPreview = session.Native<Preview>("cameraPreview");
			var firstImageCapture = session.Native<ImageCapture>("imageCapture");
			var firstVideoCapture = session.Native<AndroidX.Camera.Video.VideoCapture>("videoCapture");

			await session.Manager.ConnectCamera(CancellationToken.None).WaitAsync(CameraTimeout);

			var preview = session.Native<Preview>("cameraPreview")!;
			var imageCapture = session.Native<ImageCapture>("imageCapture")!;
			Assert.NotSame(firstPreview, preview);
			Assert.NotSame(firstImageCapture, imageCapture);
			Assert.NotSame(firstVideoCapture, session.Native<AndroidX.Camera.Video.VideoCapture>("videoCapture"));
			Assert.True(await IsNativelyBoundAsync(preview));
			Assert.True(await IsNativelyBoundAsync(imageCapture));
			Assert.Equal(2, session.LoadedCount);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		});
	}

	[Fact]
	public async Task ConnectCameraWithACancelledTokenThrowsOperationCanceledExceptionBeforeAskingTheProviderForCameras()
	{
		SetupBuilder();
		var provider = new StubCameraProvider { CamerasAfterRefresh = await GetRealCamerasAsync() };
		var session = await CreateSessionAsync(provider);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.ConnectCamera(cancellation.Token));

		Assert.Null(session.View.SelectedCamera);
		Assert.Equal(0, session.LoadedCount);
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task ConnectCameraCancelledWhileConnectingEndsQuietly()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		// Reading the real cameras initializes CameraX, so the listener of ConnectCamera is posted to the UI thread at once.
		var provider = new StubCameraProvider { AvailableCameras = await GetRealCamerasAsync() };

		await RunWithSessionAsync(async session =>
		{
			await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);
			using var errors = new CameraErrorRecorder(session.View);
			using var cancellation = new CancellationTokenSource();

			// The CameraX listener is posted to the UI thread, so the cancellation (same message) comes first.
			var connect = session.Manager.ConnectCamera(cancellation.Token);
			cancellation.Cancel();

			await connect.WaitAsync(CameraTimeout);
			Assert.Empty(errors.Errors);

			// The listener still runs, with the cancelled token, and must not let anything escape: an escape fails the test when it ends
			// (see ConnectCameraCancelledWhileConnectingDoesNotLetAnExceptionEscapeFromTheCameraXListener).
			await SettleUiThreadAsync();
		}, provider);
	}

	[Fact]
	public async Task ConnectCameraCancelledWhileConnectingDoesNotLetAnExceptionEscapeFromTheCameraXListener()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var provider = new StubCameraProvider { AvailableCameras = await GetRealCamerasAsync() };

		await RunWithSessionAsync(async session =>
		{
			await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);
			using var cancellation = new CancellationTokenSource();

			var connect = session.Manager.ConnectCamera(cancellation.Token);
			cancellation.Cancel();
			await connect.WaitAsync(CameraTimeout);
			await SettleUiThreadAsync();

			Assert.Empty(UnhandledAsyncVoidExceptions);
		}, provider);
	}

	// The preconditions of the use cases are the capture resolution (CameraViewHandler maps it before the camera connects), the camera
	// executor (CreatePlatformView creates it) and a Context that is a LifecycleOwner (CameraX binds the camera to one). The guards
	// themselves are tested in CameraManagerGuardAndroidTests.
	[Theory]
	[InlineData("resolutionSelector")]
	[InlineData("cameraExecutor")]
	[InlineData("lifecycleOwner")]
	public async Task ConnectCameraWithoutAPreconditionOfTheUseCasesReportsItsExceptionAndLetsNothingEscape(string missing)
	{
		SetupBuilder();
		EnsureCameraPermissions();

		// Reading the real cameras initializes CameraX, so the listener of ConnectCamera runs within a few UI-thread messages.
		var provider = new StubCameraProvider { AvailableCameras = await GetRealCamerasAsync() };
		var session = await CreateSessionAsync(provider, missing is "lifecycleOwner" ? new TestMauiContext(MauiContext, Android.App.Application.Context) : null);
		if (missing is not "resolutionSelector")
		{
			await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);
		}

		if (missing is "cameraExecutor")
		{
			SetPrivateField(session.Manager, "cameraExecutor", null);
		}

		using var errors = new CameraErrorRecorder(session.View);
		using var cancellation = new CancellationTokenSource();
		var connect = session.Manager.ConnectCamera(cancellation.Token);
		try
		{
			await AssertionExtensions.AssertEventually(() => connect.IsCompleted || UnhandledAsyncVoidExceptions.Count > 0, 30000, 50,
				"ConnectCamera neither completed nor did anything escape from its CameraX listener.");
			await SettleUiThreadAsync();

			Assert.Empty(UnhandledAsyncVoidExceptions);
			Assert.True(connect.IsCompleted, "ConnectCamera did not complete although its CameraX listener failed.");
			await connect;
			var exception = Assert.Single(errors.Errors);
			if (missing is "lifecycleOwner")
			{
				Assert.IsType<InvalidCastException>(exception, exactMatch: false);
			}
			else
			{
				Assert.Equal(missing, Assert.IsType<ArgumentNullException>(exception, exactMatch: false).ParamName);
			}

			Assert.Equal(0, session.LoadedCount);
			await AssertEveryCameraIsClosedAsync();
		}
		finally
		{
			// Should ConnectCamera not complete, only the token ends its wait.
			await cancellation.CancelAsync();
		}
	}

	[Fact]
	public async Task DisposeWhileConnectingDoesNotLetAnExceptionEscapeFromTheCameraXListener()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var cameras = await GetRealCamerasAsync();
		var provider = new StubCameraProvider { AvailableCameras = cameras };
		var session = await CreateSessionAsync(provider);
		await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);

		await InvokeOnMainThreadAsync(() =>
		{
			_ = session.Manager.ConnectCamera(CancellationToken.None);
			session.Manager.Dispose();
		});
		await SettleUiThreadAsync();

		Assert.Empty(UnhandledAsyncVoidExceptions);
	}

	// A handler that is disconnected while its camera connects (the page closed at once) releases the manager before CameraX answers. The view
	// is gone, so the connection ends like a cancelled one: before, ConnectCamera reported the ObjectDisposedException of the released manager
	// through ErrorOccurred, which an app shows for a page that the user already left.
	[Fact]
	public async Task DisposeWhileConnectingEndsTheConnectionQuietly()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var cameras = await GetRealCamerasAsync();
		var provider = new StubCameraProvider { AvailableCameras = cameras };
		var session = await CreateSessionAsync(provider);
		await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);
		using var errors = new CameraErrorRecorder(session.View);
		Task? connect = null;

		await InvokeOnMainThreadAsync(() =>
		{
			connect = session.Manager.ConnectCamera(CancellationToken.None);
			session.Manager.Dispose();
		});
		await connect!.WaitAsync(CameraTimeout);
		await SettleUiThreadAsync();

		Assert.Empty(errors.Errors);
		Assert.Equal(0, session.LoadedCount);
	}

	[Fact]
	public async Task DisconnectDoesNotStopTheCamera()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			session.Manager.Disconnect();
			await SettleUiThreadAsync();

			Assert.Equal("OPEN", await GetNativeCameraStateAsync(session.View.SelectedCamera!.DeviceId));
			Assert.True(IsStreaming(session.PreviewView!));
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
		});
	}

	[Fact]
	public async Task StopCameraPreviewUnbindsTheUseCasesAndClosesTheCamera()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			var preview = session.Native<Preview>("cameraPreview")!;
			var imageCapture = session.Native<ImageCapture>("imageCapture")!;

			session.Manager.StopCameraPreview();

			Assert.False(await IsNativelyBoundAsync(preview));
			Assert.False(await IsNativelyBoundAsync(imageCapture));
			await WaitForNativeCameraStateAsync(session.View.SelectedCamera!.DeviceId, "CLOSED");
			await AssertionExtensions.AssertEventually(() => !IsStreaming(session.PreviewView!), 30000, 100, "The preview kept streaming after StopCameraPreview.");
		});
	}

	[Fact]
	public async Task StopCameraPreviewBeforeConnectingDoesNothing()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();

		var exception = await InvokeOnMainThreadAsync(() => Record.Exception(session.Manager.StopCameraPreview));

		Assert.Null(exception);
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task StopCameraPreviewOffTheUiThreadThrowsIllegalStateException()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);

			// ProcessCameraProvider.unbindAll insists on the main thread.
			var exception = await Assert.ThrowsAsync<Java.Lang.IllegalStateException>(() => Task.Run(session.Manager.StopCameraPreview));
			Assert.Contains("main thread", exception.Message);
		});
	}

	[Fact]
	public async Task StartCameraPreviewRestartsAStoppedPreview()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			session.Manager.StopCameraPreview();
			await WaitForNativeCameraStateAsync(session.View.SelectedCamera!.DeviceId, "CLOSED");

			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Equal(2, session.LoadedCount);
			Assert.True(await IsNativelyBoundAsync(session.Native<Preview>("cameraPreview")!));
			Assert.True(await IsNativelyBoundAsync(session.Native<ImageCapture>("imageCapture")!));
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			await WaitForNativeCameraStateAsync(session.View.SelectedCamera!.DeviceId, "OPEN");
		});
	}

	[Fact]
	public async Task StartCameraPreviewWithACancelledTokenThrowsOperationCanceledExceptionAndKeepsTheCameraClosed()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var cameraId = session.View.SelectedCamera!.DeviceId;
			session.Manager.StopCameraPreview();
			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
			var loadedBefore = session.LoadedCount;
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			// The stopped session has to be bound again, which waits for CameraX with the token (CameraView.StartCameraPreview(token)).
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.StartCameraPreview(cancellation.Token));

			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview));
			await AssertEveryCameraIsClosedAsync();

			// Selecting a camera only restarts a running preview, so it does not open the stopped one.
			await session.Manager.UpdateCurrentCamera(session.View.SelectedCamera, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);
			await SettleUiThreadAsync();
			Assert.Equal(loadedBefore, session.LoadedCount);
			await AssertEveryCameraIsClosedAsync();

			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);
			Assert.Equal(loadedBefore + 1, session.LoadedCount);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		});
	}

	[Fact]
	public async Task StartCameraPreviewBindsAgainWhenOnlyTheImageCaptureWasUnbound()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var provider = await GetNativeCameraProviderAsync();
			provider.Unbind(session.ImageCapture);
			Assert.True(provider.IsBound(session.CameraPreview));
			Assert.False(provider.IsBound(session.ImageCapture));
			var loadedBefore = session.LoadedCount;

			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.Equal(loadedBefore + 1, session.LoadedCount);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		});
	}

	[Fact]
	public async Task StartCameraPreviewWhileTheUseCasesAreBoundDoesNotRebindTheCamera()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			var provider = await GetNativeCameraProviderAsync();
			var preview = session.Native<Preview>("cameraPreview")!;

			// A rebind unbinds everything and waits for CameraX before binding again, so the task could not be complete here.
			var start = session.Manager.StartCameraPreview(CancellationToken.None);

			Assert.True(start.IsCompletedSuccessfully);
			Assert.True(provider.IsBound(preview));
			Assert.Equal(2, session.LoadedCount);
		});
	}

	// CameraX runs one focus and metering action per camera and fails the running one as soon as another one starts
	// ("Cancelled by another startFocusAndMetering()"), which is how the test sees the toolkit's request: its own action, started right
	// before StartCameraPreview, is cancelled by the toolkit's action.
	[Fact]
	public async Task StartCameraPreviewStartsAFocusAndMeteringActionOnTheCamera()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var camera = session.Native<ICamera>("camera")!;
			var previewView = session.PreviewView!;
			var point = previewView.MeteringPointFactory.CreatePoint(previewView.Width / 2f, previewView.Height / 2f, 0.1f);
			var probe = new FocusMeteringAction.Builder(point).DisableAutoCancel().Build();
			Skip.IfNot(camera.CameraInfo.IsFocusMeteringSupported(probe),
				"The camera supports no AF, AE or AWB metering region, so CameraX rejects every focus and metering action.");

			// One UI-thread message: CameraX handles both requests on its camera thread in this order, long before a capture result
			// could complete the first one. The bound session is not bound again, so nothing else can end the test's action.
			var probeOutcome = ObserveFailureAsync(camera.CameraControl.StartFocusAndMetering(probe));
			var start = session.Manager.StartCameraPreview(CancellationToken.None);

			Assert.True(start.IsCompletedSuccessfully, "StartCameraPreview bound the camera again although its use cases were bound.");
			var failure = await probeOutcome.WaitAsync(CameraTimeout)
				?? throw new Xunit.Sdk.XunitException("The test's focus and metering action completed: StartCameraPreview started no action of its own.");
			Assert.Equal("Cancelled by another startFocusAndMetering()", failure.Message);
		});
	}

	static Task<Java.Lang.Throwable?> ObserveFailureAsync(Google.Common.Util.Concurrent.IListenableFuture future)
	{
		var outcome = new TaskCompletionSource<Java.Lang.Throwable?>(TaskCreationOptions.RunContinuationsAsynchronously);
		future.AddListener(new Java.Lang.Runnable(() =>
		{
			try
			{
				future.Get();
				outcome.TrySetResult(null);
			}
			catch (Java.Util.Concurrent.ExecutionException ex)
			{
				outcome.TrySetResult(ex.Cause);
			}
			catch (Exception ex)
			{
				outcome.TrySetException(ex);
			}
		}), AndroidX.Core.Content.ContextCompat.GetMainExecutor(AppContext));

		return outcome.Task;
	}

	[Fact]
	public async Task StartCameraPreviewBeforeConnectingDoesNothing()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var withoutPlatformView = await CreateSessionAsync(createPlatformView: false);
		var withPlatformView = await CreateSessionAsync();

		await withoutPlatformView.Manager.StartCameraPreview(CancellationToken.None);
		await withPlatformView.Manager.StartCameraPreview(CancellationToken.None);
		await SettleUiThreadAsync();

		Assert.Equal(0, withoutPlatformView.LoadedCount);
		Assert.Equal(0, withPlatformView.LoadedCount);
		Assert.Null(withPlatformView.View.SelectedCamera);
		await AssertEveryCameraIsClosedAsync();
	}

	[Theory]
	[InlineData("cameraPreview")]
	[InlineData("imageCapture")]
	public async Task StartCameraPreviewDoesNothingWhenAUseCaseIsMissing(string missingUseCase)
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			var loadedBefore = session.LoadedCount;
			var original = GetPrivateField<object>(session.Manager, missingUseCase);
			SetPrivateField(session.Manager, missingUseCase, null);

			try
			{
				await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

				Assert.Equal(loadedBefore, session.LoadedCount);
			}
			finally
			{
				// The manager must find its use cases again to unbind them (and close the camera) when the test ends.
				SetPrivateField(session.Manager, missingUseCase, original);
			}
		});
	}

	[Fact]
	public async Task StartCameraPreviewSelectsTheFirstCameraWhenTheSelectionWasCleared()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			session.View.SelectedCamera = null;

			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Equal(session.Provider.AvailableCameras![0].DeviceId, session.View.SelectedCamera?.DeviceId);
		});
	}

	// StartCameraPreview reports a preview that cannot start through ErrorOccurred instead of throwing (#3299), as on iOS, Mac Catalyst and
	// Windows: an app that starts the preview from a command could not catch the exception.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StartCameraPreviewRaisesErrorOccurredWhenNoCameraIsAvailableAnymore(bool providerHasNoList)
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var provider = new StubCameraProvider { AvailableCameras = await GetRealCamerasAsync() };

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			provider.AvailableCameras = providerHasNoList ? null : [];
			session.View.SelectedCamera = null;

			var exception = await CameraErrorRecorder.AssertReportsAsync<CameraException>(session.View, () => session.Manager.StartCameraPreview(CancellationToken.None));

			Assert.Equal("No camera available on device", exception.Message);
		}, provider);
	}

	[Fact]
	public async Task StartCameraPreviewWithoutACameraSelectorRaisesErrorOccurred()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			session.Manager.StopCameraPreview();
			session.View.SelectedCamera = new CameraInfo("Selector-less camera", "no-selector", CameraPosition.Rear, false, 1, 1, [], null!);

			var exception = await CameraErrorRecorder.AssertReportsAsync<CameraException>(session.View, () => session.Manager.StartCameraPreview(CancellationToken.None));

			Assert.Equal("Unable to retrieve CameraSelector", exception.Message);
		});
	}

	// A camera that CameraX cannot bind (for example an unplugged USB camera) is reported like every other preview that cannot start, with the
	// exception of CameraX.
	[Fact]
	public async Task StartCameraPreviewOfACameraThatCanNotBeBoundRaisesErrorOccurred()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			session.Manager.StopCameraPreview();
			session.View.SelectedCamera = CreateCameraThatCanNotBeBound();
			var loadedBefore = session.LoadedCount;
			using var errors = new CameraErrorRecorder(session.View);

			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.IsType<Java.Lang.IllegalArgumentException>(Assert.Single(errors.Errors), exactMatch: false);
			Assert.Equal(loadedBefore, session.LoadedCount);
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraWithNullDoesNothing()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			var provider = await GetNativeCameraProviderAsync();
			var preview = session.Native<Preview>("cameraPreview")!;

			var update = session.Manager.UpdateCurrentCamera(null, CancellationToken.None);

			Assert.True(update.IsCompletedSuccessfully);
			Assert.True(provider.IsBound(preview));
			Assert.Equal(1, session.LoadedCount);
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraBeforeTheCameraWasStartedDoesNotStartThePreview()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var cameras = await GetRealCamerasAsync();
		var session = await CreateSessionAsync();

		await session.Manager.UpdateCurrentCamera(cameras[0], CancellationToken.None);
		await SettleUiThreadAsync();

		Assert.Equal(0, session.LoadedCount);
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task UpdateCurrentCameraAfterTheCameraWasStoppedDoesNotRestartThePreview()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			session.Manager.StopCameraPreview();
			await WaitForNativeCameraStateAsync(session.View.SelectedCamera!.DeviceId, "CLOSED");

			await session.Manager.UpdateCurrentCamera(session.View.SelectedCamera, CancellationToken.None);
			await SettleUiThreadAsync();

			Assert.Equal(1, session.LoadedCount);
			await AssertEveryCameraIsClosedAsync();
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraSwitchesTheNativeCamera()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var cameras = await GetRealCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, "Needs two cameras to switch between; this device has one.");

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			var first = session.View.SelectedCamera!;
			var other = session.Provider.AvailableCameras!.First(camera => camera.DeviceId != first.DeviceId);
			session.View.SelectedCamera = other;

			await session.Manager.UpdateCurrentCamera(other, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			Assert.Equal(2, session.LoadedCount);
			await WaitForNativeCameraStateAsync(other.DeviceId, "OPEN");
			await WaitForNativeCameraStateAsync(first.DeviceId, "CLOSED");
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraToACameraThatCanNotBeBoundThrowsTheIllegalArgumentExceptionOfCameraX()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var unbindable = CreateCameraThatCanNotBeBound();
			session.View.SelectedCamera = unbindable;
			var loadedBefore = session.LoadedCount;

			// BindToLifecycle finds no camera for the selector (the manager has already unbound the running camera then).
			await Assert.ThrowsAnyAsync<Java.Lang.IllegalArgumentException>(() => session.Manager.UpdateCurrentCamera(unbindable, CancellationToken.None).AsTask().WaitAsync(CameraTimeout));

			Assert.Equal(loadedBefore, session.LoadedCount);
		});
	}

	// The failed switch to a camera that can not be bound (for example an unplugged USB camera) leaves the preview started: when the app
	// selects a valid camera again, the manager opens it.
	[Fact]
	public async Task UpdateCurrentCameraToAValidCameraAfterOneThatCanNotBeBoundOpensTheValidCamera()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var valid = session.View.SelectedCamera!;
			var unbindable = CreateCameraThatCanNotBeBound();
			session.View.SelectedCamera = unbindable;
			await Assert.ThrowsAnyAsync<Java.Lang.IllegalArgumentException>(() => session.Manager.UpdateCurrentCamera(unbindable, CancellationToken.None).AsTask().WaitAsync(CameraTimeout));
			var loadedBefore = session.LoadedCount;

			session.View.SelectedCamera = valid;
			await session.Manager.UpdateCurrentCamera(valid, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			Assert.Equal(loadedBefore + 1, session.LoadedCount);
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			await WaitForNativeCameraStateAsync(valid.DeviceId, "OPEN");
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeBeforeConnectingIsSafeAndRepeatable(bool withPlatformView)
	{
		SetupBuilder();
		var session = await CreateSessionAsync(createPlatformView: withPlatformView);

		var exception = await InvokeOnMainThreadAsync(() => Record.Exception(() =>
		{
			session.Manager.Dispose();
			session.Manager.Dispose();
		}));

		Assert.Null(exception);
		await AssertEveryCameraIsClosedAsync();
	}

	// The page closes while the camera is being bound again (a new extension mode, capture resolution or camera): Dispose runs while
	// RebindCamera waits for the CameraX futures of EnableModes. The rebind has to end without binding the use cases of the disposed
	// manager, which would open the camera again with nothing left to close it.
	[Fact]
	public async Task DisposeWhileARebindWaitsForCameraXEndsTheRebindWithoutOpeningTheCamera()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var cameraId = session.View.SelectedCamera!.DeviceId;

			// One UI-thread message: the CameraX listeners the rebind waits for can only run after Dispose.
			var rebind = session.Manager.SetExtensionMode(AndroidX.Camera.Extensions.ExtensionMode.None, CancellationToken.None);
			Assert.False(rebind.IsCompleted, "SetExtensionMode completed without waiting for CameraX.");
			session.Manager.Dispose();

			var exception = await Record.ExceptionAsync(() => rebind.WaitAsync(CameraTimeout));
			Assert.True(exception is null or ObjectDisposedException or OperationCanceledException, $"The rebind failed with {exception}");
			await SettleUiThreadAsync();

			Assert.Null(GetPrivateField<object>(session.Manager, "camera"));
			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
			await AssertEveryCameraIsClosedAsync();
		});
	}

	[Fact]
	public async Task CameraCommandsAfterDisposeDoNothing()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var cameraId = session.View.SelectedCamera!.DeviceId;
			session.Manager.Dispose();
			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
			var loadedBefore = session.LoadedCount;

			// The manager still counts as initialized and the view still has its selected camera, but every native object the manager
			// would use is gone (SetExtensionMode returns because there is no ProcessCameraProvider any more).
			await session.Manager.StartCameraPreview(CancellationToken.None);
			session.Manager.StopCameraPreview();
			await session.Manager.UpdateCurrentCamera(session.View.SelectedCamera, CancellationToken.None);
			var extension = session.Manager.SetExtensionMode(AndroidX.Camera.Extensions.ExtensionMode.None, CancellationToken.None);
			Assert.True(extension.IsCompletedSuccessfully, "SetExtensionMode of a disposed manager waited for CameraX.");
			await SettleUiThreadAsync();

			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.Null(GetPrivateField<object>(session.Manager, "camera"));
			await AssertEveryCameraIsClosedAsync();
		});
	}

	[Fact]
	public async Task DisposeClosesTheCameraAndReleasesTheNativeObjects()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			var cameraId = session.View.SelectedCamera!.DeviceId;
			var streamState = session.PreviewView!.PreviewStreamState;
			var nativeObjects = new[]
			{
				"camera", "cameraControl", "cameraPreview", "cameraExecutor", "imageCapture", "videoCapture", "videoRecorder",
				"imageCallback", "previewView", "processCameraProvider", "resolutionSelector", "resolutionFilter", "orientationListener"
			}.ToDictionary(name => name, name => GetPrivateField<Java.Lang.Object>(session.Manager, name));
			Assert.All(nativeObjects, entry => Assert.NotNull(entry.Value));

			session.Manager.Dispose();

			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
			await AssertionExtensions.AssertEventually(() => streamState.Value?.ToString() == "IDLE", 30000, 100, "The preview kept streaming after Dispose.");

			// CameraX's ProcessCameraProvider, camera and camera control are shared: every manager that binds to the same LifecycleOwner gets the
			// same managed peers, so they are only released (that they stay usable is asserted by
			// DisposeLeavesTheProcessCameraProviderThatEveryManagerSharesUsable, DisposingOneManagerDoesNotBreakAnotherManager and
			// DisposingOneManagerDoesNotBreakTheZoomAndTorchOfAnotherManager).
			Assert.All(nativeObjects.Where(entry => !IsSharedCameraXObject(entry.Key)), entry => Assert.True(entry.Value!.Handle == IntPtr.Zero, $"{entry.Key} was not disposed."));
			Assert.All(nativeObjects.Keys, name => Assert.Null(GetPrivateField<object>(session.Manager, name)));
		});
	}

	[Fact]
	public async Task DisposeShutsTheCameraExecutorDown()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();

		var (shutdownBefore, shutdownAfter) = await InvokeOnMainThreadAsync(() =>
		{
			var executor = session.Native<Java.Lang.Object>("cameraExecutor")!;

			// A global reference of the test keeps the Java executor reachable after Dispose released the managed peer.
			var reference = JNIEnv.NewGlobalRef(executor.Handle);
			try
			{
				var before = IsExecutorShutdown(reference);
				session.Manager.Dispose();
				return (before, IsExecutorShutdown(reference));
			}
			finally
			{
				JNIEnv.DeleteGlobalRef(reference);
			}
		});

		Assert.False(shutdownBefore);
		Assert.True(shutdownAfter, "Dispose left the camera executor running: its Java thread lives on.");
	}

	// The Recorder still finalizes recordings and releases its encoders on its executor after its VideoCapture is unbound, which the
	// camera executor rejects once Dispose shut it down. Recorder.getExecutor() returns only an executor that the app provided: without
	// one the Recorder runs on the I/O executor of CameraX.
	[Fact]
	public async Task RecorderDoesNotRunOnTheCameraExecutorThatDisposeShutsDown()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);

			var recorder = session.Native<AndroidX.Camera.Video.Recorder>("videoRecorder")!;
			Assert.Null(recorder.Executor);
		});
	}

	[Fact]
	public async Task DisposeLeavesTheProcessCameraProviderThatEveryManagerSharesUsable()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var shared = await GetNativeCameraProviderAsync();

			session.Manager.Dispose();

			Assert.True(shared.Handle != IntPtr.Zero, "Dispose disposed the managed peer of the process-wide ProcessCameraProvider that every CameraManager shares.");
			Assert.NotEmpty(shared.AvailableCameraInfos);
		});
	}

	[Fact]
	public async Task DisposingOneManagerDoesNotBreakTheZoomAndTorchOfAnotherManager()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var first = await CreateSessionAsync();
		var second = await CreateSessionAsync();

		await HostAsync(async () =>
		{
			await first.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitForStreamingAsync(first.PreviewView!);

			// A second CameraView on the same camera binds to the same LifecycleOwner, so both managers share the camera objects.
			await second.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitForStreamingAsync(second.PreviewView!);
			second.Manager.Dispose();

			// CameraViewHandler maps a new ZoomFactor or IsTorchOn of the first CameraView to its manager at any time.
			var zoom = Record.Exception(() => first.Manager.UpdateZoom(1f));
			var torch = Record.Exception(() => first.Manager.UpdateIsTorchOn(false));

			Assert.True(zoom is null, $"UpdateZoom of the first manager failed after the second one was disposed: {zoom}");
			Assert.True(torch is null, $"UpdateIsTorchOn of the first manager failed after the second one was disposed: {torch}");
		}, first.PreviewView!, second.PreviewView!);
	}

	[Fact]
	public async Task DisposingOneManagerDoesNotBreakAnotherManager()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var first = await CreateSessionAsync();
		var second = await CreateSessionAsync();

		await HostAsync(async () =>
		{
			await first.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitForStreamingAsync(first.PreviewView!);

			// A second CameraView, for example on the next page, takes the camera over, so the first one has to start its preview again.
			await second.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitForStreamingAsync(second.PreviewView!);
			second.Manager.Dispose();

			await first.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);
			Assert.True(await IsNativelyBoundAsync(first.ImageCapture));
			Assert.True(await IsNativelyBoundAsync(first.CameraPreview));
			await WaitForStreamingAsync(first.PreviewView!);

			first.Manager.Dispose();
			Assert.Null(GetPrivateField<object>(first.Manager, "orientationListener"));
		}, first.PreviewView!, second.PreviewView!);
	}

	internal static bool IsSharedCameraXObject(string field) => field is "processCameraProvider" or "camera" or "cameraControl";

	static bool IsExecutorShutdown(IntPtr executor)
	{
		var type = JNIEnv.GetObjectClass(executor);
		try
		{
			return JNIEnv.CallBooleanMethod(executor, JNIEnv.GetMethodID(type, "isShutdown", "()Z"));
		}
		finally
		{
			JNIEnv.DeleteLocalRef(type);
		}
	}

	[Fact]
	public async Task ManagerViewAndPreviewViewAreCollectedAfterDispose()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		var (view, manager, previewView) = await CreateConnectAndDisposeAsync();
		await WaitForAllCamerasClosedAsync();

		await AssertCollectedAsync(("CameraView", view), ("CameraManager", manager), ("PreviewView", previewView));
	}

	async Task<(WeakReference View, WeakReference Manager, WeakReference PreviewView)> CreateConnectAndDisposeAsync()
	{
		WeakReference? view = null, manager = null, previewView = null;
		var session = await CreateSessionAsync(track: false);

		await HostAsync(async () =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			view = new WeakReference(session.View);
			manager = new WeakReference(session.Manager);
			previewView = new WeakReference(session.PreviewView);
			session.Manager.Dispose();
			(session.Provider as CameraProvider)?.Dispose();
		}, session.PreviewView!);

		MarkCameraSessionUsed();
		return (view!, manager!, previewView!);
	}
}