using System.Reflection;
using Android.Runtime;
using Android.Views;
using AndroidX.Camera.Core;
using AndroidX.Camera.Extensions;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;
using Runnable = Java.Lang.Runnable;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// CameraManager on Android: the controls of a running camera (flash mode, torch, zoom, capture resolution, device orientation and
/// the CameraX extension mode). Every assertion reads what CameraX reports natively (use case state, camera state, torch and zoom
/// state, negotiated resolutions, bound use cases).
/// </summary>
[Category(TestCategory.CameraManagerAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerControlAndroidTests : CameraAndroidTestBase
{
	static async Task<CameraInfo> GetCameraAsync(Func<CameraInfo, bool> predicate, string skipReason)
	{
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(predicate);
		Skip.If(camera is null, skipReason);
		return camera;
	}

	static async Task<bool> IsNativeExtensionAvailableAsync(CameraInfo camera, int extensionMode)
	{
		var context = AppContext;
		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var nativeProvider = (await GetNativeCameraProviderAsync()).JavaCast<AndroidX.Camera.Core.ICameraProvider>();
		var future = ExtensionsManager.GetInstanceAsync(context, nativeProvider) ?? throw new InvalidOperationException("CameraX has no extensions manager.");

		future.AddListener(new Runnable(() =>
		{
			try
			{
				var extensionsManager = (ExtensionsManager)future.Get()!;
				completion.TrySetResult(extensionsManager.IsExtensionAvailable(camera.CameraSelector!, extensionMode));
			}
			catch (Exception ex)
			{
				completion.TrySetException(ex);
			}
		}), ContextCompat.GetMainExecutor(context));

		return await completion.Task.WaitAsync(CameraTimeout);
	}

	/// <summary>
	/// The SessionProcessor of the camera that CameraX bound: a camera bound through an extension-enabled CameraSelector carries the
	/// vendor extension's SessionProcessor in its extended config, a camera bound through a plain selector has none.
	/// </summary>
	static AndroidX.Camera.Core.Impl.ISessionProcessor? GetSessionProcessorOfTheBoundCamera(ManagerSession session) =>
		(session.Native<ICamera>("camera") ?? throw new InvalidOperationException("The manager has no bound camera.")).ExtendedConfig.GetSessionProcessor(null);

	// ---- flash mode ---------------------------------------------------------------------------------------------------

	[Fact]
	public async Task UpdateFlashModeBeforeConnectingDoesNotChangeTheFlashModeOfTheCameraThatConnectsLater()
	{
		SetupBuilder();
		EnsureCameraPermissions();

		await RunWithSessionAsync(async session =>
		{
			session.Manager.UpdateFlashMode(CameraFlashMode.On);
			Assert.Null(session.Native<ImageCapture>("imageCapture"));

			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);

			Assert.Equal(ImageCapture.FlashModeOff, session.ImageCapture.FlashMode);
		});
	}

	[Fact]
	public async Task UpdateFlashModeSetsTheFlashModeOfTheNativeImageCapture()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(session =>
		{
			var imageCapture = session.ImageCapture;
			Assert.Equal(ImageCapture.FlashModeOff, imageCapture.FlashMode);

			foreach (var (mode, expected) in new[]
			{
				(CameraFlashMode.On, ImageCapture.FlashModeOn),
				(CameraFlashMode.Auto, ImageCapture.FlashModeAuto),
				(CameraFlashMode.Off, ImageCapture.FlashModeOff),
				(CameraFlashMode.On, ImageCapture.FlashModeOn),
				(CameraFlashMode.Off, ImageCapture.FlashModeOff)
			})
			{
				session.Manager.UpdateFlashMode(mode);

				Assert.True(expected == imageCapture.FlashMode, $"{mode} should set the native flash mode {expected}, but it is {imageCapture.FlashMode}.");
			}

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateFlashModeWithAnUndefinedModeThrowsNotSupportedExceptionAndKeepsTheFlashMode()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(session =>
		{
			session.Manager.UpdateFlashMode(CameraFlashMode.On);

			var exception = Assert.Throws<NotSupportedException>(() => session.Manager.UpdateFlashMode((CameraFlashMode)42));

			Assert.Equal("Flash mode 42 not supported.", exception.Message);
			Assert.Equal(ImageCapture.FlashModeOn, session.ImageCapture.FlashMode);
			return Task.CompletedTask;
		});
	}

	// ---- torch --------------------------------------------------------------------------------------------------------

	[Fact]
	public async Task UpdateIsTorchOnBeforeConnectingDoesNothing()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();

		var exception = await InvokeOnMainThreadAsync(() => Record.Exception(() => session.Manager.UpdateIsTorchOn(true)));

		Assert.Null(exception);
		Assert.Null(session.Native<ICameraControl>("cameraControl"));
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task UpdateIsTorchOnSwitchesTheNativeTorchOnAndOff()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.IsFlashSupported, "No camera of this device has a flash unit, so the torch can not be switched on.");

		await RunWithStreamingSessionAsync(async session =>
		{
			Assert.False(IsNativeTorchOn(await GetNativeCameraInfoAsync(camera.DeviceId)));

			session.Manager.UpdateIsTorchOn(true);
			await WaitForNativeTorchAsync(camera.DeviceId, true);

			session.Manager.UpdateIsTorchOn(false);
			await WaitForNativeTorchAsync(camera.DeviceId, false);
		}, view: new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task UpdateIsTorchOnWithACameraWithoutAFlashUnitLeavesTheTorchOffAndDoesNotThrow()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => !camera.IsFlashSupported, "Every camera of this device reports a flash unit (the cameras of the emulators do), so the request that CameraX rejects can not be made.");

		await RunWithStreamingSessionAsync(async session =>
		{
			var nativeInfo = await GetNativeCameraInfoAsync(camera.DeviceId);
			Assert.False(nativeInfo.HasFlashUnit);

			// CameraX answers the request with a failed future ("No flash unit") that the toolkit does not observe: nothing may
			// throw and nothing may change.
			var exception = Record.Exception(() => session.Manager.UpdateIsTorchOn(true));
			await SettleUiThreadAsync();

			Assert.Null(exception);
			Assert.False(IsNativeTorchOn(await GetNativeCameraInfoAsync(camera.DeviceId)));
		}, view: new CameraView { SelectedCamera = camera });
	}

	// ---- zoom ---------------------------------------------------------------------------------------------------------

	[Fact]
	public async Task UpdateZoomBeforeConnectingDoesNothing()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();

		var exception = await InvokeOnMainThreadAsync(() => Record.Exception(() => session.Manager.UpdateZoom(2)));

		Assert.Null(exception);
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task UpdateZoomSetsTheNativeZoomRatio()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.MaximumZoomFactor > 1f, "No camera of this device can zoom in.");
		var ratio = Math.Min(2f, camera.MaximumZoomFactor);

		await RunWithStreamingSessionAsync(async session =>
		{
			// A camera starts at a zoom ratio of 1.
			await WaitForNativeZoomAsync(camera.DeviceId, 1f);

			session.Manager.UpdateZoom(ratio);
			await WaitForNativeZoomAsync(camera.DeviceId, ratio);

			session.Manager.UpdateZoom(1f);
			await WaitForNativeZoomAsync(camera.DeviceId, 1f);
		}, view: new CameraView { SelectedCamera = camera });
	}

	[Theory]
	[InlineData("AboveTheMaximum")]
	[InlineData("BelowTheMinimum")]
	[InlineData("Zero")]
	[InlineData("Negative")]
	[InlineData("PositiveInfinity")]
	public async Task UpdateZoomOutsideTheRangeOfTheCameraIsRejectedByCameraXAndDoesNotThrow(string kind)
	{
		SetupBuilder();
		var camera = await GetCameraAsync(_ => true, "The device has no camera.");
		var invalidRatio = kind switch
		{
			"AboveTheMaximum" => camera.MaximumZoomFactor + 1000,
			"BelowTheMinimum" => camera.MinimumZoomFactor / 1000,
			"Zero" => 0f,
			"Negative" => -2f,
			"PositiveInfinity" => float.PositiveInfinity,
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
		};

		await RunWithStreamingSessionAsync(async session =>
		{
			var before = GetNativeZoomRatio(await GetNativeCameraInfoAsync(camera.DeviceId));

			var exception = Record.Exception(() => session.Manager.UpdateZoom(invalidRatio));
			await SettleUiThreadAsync();

			Assert.Null(exception);
			Assert.Equal(before, GetNativeZoomRatio(await GetNativeCameraInfoAsync(camera.DeviceId)));
		}, view: new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task UpdatesAfterDisposeDoNothing()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(session =>
		{
			session.Manager.Dispose();

			Assert.Null(Record.Exception(() => session.Manager.UpdateFlashMode(CameraFlashMode.On)));
			Assert.Null(Record.Exception(() => session.Manager.UpdateIsTorchOn(true)));
			Assert.Null(Record.Exception(() => session.Manager.UpdateZoom(2)));
			return Task.CompletedTask;
		});
	}

	// ---- capture resolution -------------------------------------------------------------------------------------------

	[Fact]
	public async Task UpdateCaptureResolutionAppliesTheLargestSizeThatFitsToTheNativeUseCases()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.SupportedResolutions.Contains(SmallResolution), "The camera does not support a 320x240 capture size.");

		await RunWithStreamingSessionAsync(async session =>
		{
			var (defaultWidth, defaultHeight) = session.CaptureResolution;
			Assert.True(defaultWidth * defaultHeight > SmallSize.Width * SmallSize.Height, $"The default capture resolution {defaultWidth}x{defaultHeight} should be larger than {SmallSize.Width}x{SmallSize.Height}.");
			var loadedBefore = session.LoadedCount;

			await session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			Assert.Equal(SmallSize, session.CaptureResolution);
			Assert.Equal(SmallSize, session.PreviewResolution);
			Assert.Equal(loadedBefore + 1, session.LoadedCount);
			// A higher resolution wins over the capture rate (the emulators list no high-resolution sizes, so the sizes cannot show it).
			Assert.Equal(AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector.PreferHigherResolutionOverCaptureRate,
				session.Native<AndroidX.Camera.Core.ResolutionSelector.ResolutionSelector>("resolutionSelector")!.AllowedResolutionMode);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		}, view: new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithTheSameSizeDoesNotRebuildTheUseCases()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.SupportedResolutions.Contains(SmallResolution), "The camera does not support a 320x240 capture size.");

		await RunWithStreamingSessionAsync(async session =>
		{
			var preview = session.CameraPreview;
			var imageCapture = session.ImageCapture;
			var loadedBefore = session.LoadedCount;

			await session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			Assert.Same(preview, session.CameraPreview);
			Assert.Same(imageCapture, session.ImageCapture);
			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.True(await IsNativelyBoundAsync(preview));
			Assert.True(await IsNativelyBoundAsync(imageCapture));
		}, view: new CameraView { SelectedCamera = camera, ImageCaptureResolution = SmallResolution });
	}

	[Theory]
	[InlineData(640, 240)]
	[InlineData(320, 480)]
	[InlineData(640, 480)]
	public async Task UpdateCaptureResolutionWithAnotherWidthOrHeightRebuildsTheUseCases(int width, int height)
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.SupportedResolutions.Contains(SmallResolution), "The camera does not support a 320x240 capture size.");
		Skip.IfNot(camera.SupportedResolutions.Any(size => size.Width <= width && size.Height <= height), $"The camera has no capture size that fits {width}x{height}.");

		await RunWithStreamingSessionAsync(async session =>
		{
			var preview = session.CameraPreview;
			var imageCapture = session.ImageCapture;
			var filter = GetPrivateField<object>(session.Manager, "resolutionFilter")!;
			var loadedBefore = session.LoadedCount;

			await session.Manager.UpdateCaptureResolution(new Size(width, height), CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			Assert.NotSame(preview, session.CameraPreview);
			Assert.NotSame(imageCapture, session.ImageCapture);
			Assert.Same(filter, GetPrivateField<object>(session.Manager, "resolutionFilter"));
			var target = (Android.Util.Size)filter.GetType().GetProperty("TargetSize")!.GetValue(filter)!;
			Assert.Equal((width, height), (target.Width, target.Height));
			Assert.Equal(loadedBefore + 1, session.LoadedCount);
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));

			var (captureWidth, captureHeight) = session.CaptureResolution;
			Assert.True(captureWidth <= width && captureHeight <= height, $"The capture resolution {captureWidth}x{captureHeight} must fit {width}x{height}.");
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		}, view: new CameraView { SelectedCamera = camera, ImageCaptureResolution = SmallResolution });
	}

	// The app changes the capture resolution twice in one UI-thread message: the second change replaces (and disposes) the use cases that
	// the first one built before CameraX answered the first one. Both changes complete, and the use cases of the last resolution are bound.
	[Fact]
	public async Task UpdateCaptureResolutionTwiceInOneUiThreadMessageBindsTheUseCasesOfTheLastResolution()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.SupportedResolutions.Contains(SmallResolution), "The camera does not support a 320x240 capture size.");

		await RunWithStreamingSessionAsync(async session =>
		{
			var first = session.Manager.UpdateCaptureResolution(new Size(640, 480), CancellationToken.None).AsTask();
			var second = session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask();

			await first.WaitAsync(CameraTimeout);
			await second.WaitAsync(CameraTimeout);

			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.Equal(SmallSize, session.CaptureResolution);
			Assert.Equal(SmallSize, session.PreviewResolution);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		}, view: new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task UpdateCaptureResolutionBeforeConnectingBuildsNothingAndIsUsedByTheNextConnect()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var camera = await GetCameraAsync(camera => camera.SupportedResolutions.Contains(SmallResolution), "The camera does not support a 320x240 capture size.");
		var view = new CameraView { SelectedCamera = camera, ImageCaptureResolution = SmallResolution };

		await RunWithSessionAsync(async session =>
		{
			await session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);
			await SettleUiThreadAsync();

			Assert.Null(session.Native<Preview>("cameraPreview"));
			Assert.Null(session.Native<ImageCapture>("imageCapture"));
			Assert.Equal(0, session.LoadedCount);
			await AssertEveryCameraIsClosedAsync();

			// ConnectAsync maps the view's resolution (the same size) again, which changes nothing, then connects.
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);

			Assert.Equal(SmallSize, session.CaptureResolution);
			Assert.Equal(SmallSize, session.PreviewResolution);
		}, view: view);
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithASizeOfZeroRemovesTheRestrictionAgain()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.SupportedResolutions.Contains(SmallResolution), "The camera does not support a 320x240 capture size.");

		await RunWithStreamingSessionAsync(async session =>
		{
			var unrestricted = session.CaptureResolution;
			await session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);
			Assert.Equal(SmallSize, session.CaptureResolution);
			var loadedBefore = session.LoadedCount;

			await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			Assert.Equal(unrestricted, session.CaptureResolution);
			Assert.Equal(loadedBefore + 1, session.LoadedCount);

			// The same size again is ignored.
			var imageCapture = session.ImageCapture;
			await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);
			Assert.Same(imageCapture, session.ImageCapture);
			Assert.Equal(loadedBefore + 1, session.LoadedCount);
		}, view: new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithAFractionalSizeTruncatesToWholePixels()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.SupportedResolutions.Contains(SmallResolution), "The camera does not support a 320x240 capture size.");
		var smaller = camera.SupportedResolutions.Where(size => size.Width <= 319 && size.Height <= 239).OrderByDescending(size => size.Width * size.Height).ToList();
		Skip.If(smaller.Count is 0, "The camera has no capture size below 320x240.");

		await RunWithStreamingSessionAsync(async session =>
		{
			// 319.9 x 239.9 truncates to 319 x 239, which 320x240 no longer fits; rounding would still select 320x240.
			await session.Manager.UpdateCaptureResolution(new Size(319.9, 239.9), CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			Assert.Equal(((int)smaller[0].Width, (int)smaller[0].Height), session.CaptureResolution);
		}, view: new CameraView { SelectedCamera = camera, ImageCaptureResolution = SmallResolution });
	}

	[Fact]
	public async Task UpdateCaptureResolutionAfterStopCameraPreviewDoesNotReopenTheCamera()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var cameraId = session.View.SelectedCamera!.DeviceId;
			session.Manager.StopCameraPreview();
			await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
			var loadedBefore = session.LoadedCount;

			await session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);
			await SettleUiThreadAsync();

			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview));
			await AssertEveryCameraIsClosedAsync();
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionWhileThePreviewIsStoppedIsAppliedWhenThePreviewStarts()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.SupportedResolutions.Contains(SmallResolution), "The camera does not support a 320x240 capture size.");

		await RunWithStreamingSessionAsync(async session =>
		{
			var (defaultWidth, defaultHeight) = session.CaptureResolution;
			Assert.True(defaultWidth * defaultHeight > SmallSize.Width * SmallSize.Height, $"The default capture resolution {defaultWidth}x{defaultHeight} should be larger than {SmallSize.Width}x{SmallSize.Height}.");
			session.Manager.StopCameraPreview();
			await WaitForNativeCameraStateAsync(camera.DeviceId, "CLOSED");
			await session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

			// The use cases are not rebuilt while the preview is stopped; the ResolutionFilter they hold is the one the manager changed.
			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Equal(SmallSize, session.CaptureResolution);
			Assert.Equal(SmallSize, session.PreviewResolution);
		}, view: new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithACancelledTokenThrowsAndTheNextPreviewStartUsesTheNewResolution()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(camera => camera.SupportedResolutions.Contains(SmallResolution), "The camera does not support a 320x240 capture size.");

		await RunWithStreamingSessionAsync(async session =>
		{
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			// The use cases are rebuilt for the new size first; binding them waits for CameraX with the token.
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.UpdateCaptureResolution(SmallResolution, cancellation.Token).AsTask());

			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.Equal(SmallSize, session.CaptureResolution);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
		}, view: new CameraView { SelectedCamera = camera });
	}

	// ---- orientation --------------------------------------------------------------------------------------------------

	// Android's OrientationEventListener reports the device rotation clockwise in degrees; CameraX wants the opposite
	// Surface rotation (the mapping of the CameraX documentation).
	[Theory]
	[InlineData(0, (int)SurfaceOrientation.Rotation0)]
	[InlineData(44, (int)SurfaceOrientation.Rotation0)]
	[InlineData(45, (int)SurfaceOrientation.Rotation270)]
	[InlineData(90, (int)SurfaceOrientation.Rotation270)]
	[InlineData(134, (int)SurfaceOrientation.Rotation270)]
	[InlineData(135, (int)SurfaceOrientation.Rotation180)]
	[InlineData(180, (int)SurfaceOrientation.Rotation180)]
	[InlineData(224, (int)SurfaceOrientation.Rotation180)]
	[InlineData(225, (int)SurfaceOrientation.Rotation90)]
	[InlineData(270, (int)SurfaceOrientation.Rotation90)]
	[InlineData(314, (int)SurfaceOrientation.Rotation90)]
	[InlineData(315, (int)SurfaceOrientation.Rotation0)]
	[InlineData(359, (int)SurfaceOrientation.Rotation0)]
	public async Task OrientationChangeSetsTheTargetRotationOfEveryNativeUseCase(int degrees, int expectedRotation)
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(session =>
		{
			var listener = GetPrivateField<OrientationEventListener>(session.Manager, "orientationListener")!;
			var imageCapture = session.ImageCapture;
			var preview = session.CameraPreview;
			var videoCapture = session.Native<AndroidX.Camera.Video.VideoCapture>("videoCapture")!;

			// Start from a rotation that differs from the expected one, so that the assertions can fail.
			var other = (expectedRotation + 1) % 4;
			imageCapture.TargetRotation = other;
			preview.TargetRotation = other;
			videoCapture.TargetRotation = other;

			// Everything happens in this one UI-thread message, so that a sensor event of the emulator can not interleave.
			listener.OnOrientationChanged(degrees);

			Assert.Equal(expectedRotation, imageCapture.TargetRotation);
			Assert.Equal(expectedRotation, preview.TargetRotation);
			Assert.Equal(expectedRotation, videoCapture.TargetRotation);
			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UnknownOrientationDoesNotChangeTheTargetRotation()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(session =>
		{
			var listener = GetPrivateField<OrientationEventListener>(session.Manager, "orientationListener")!;
			var imageCapture = session.ImageCapture;
			var preview = session.CameraPreview;
			var videoCapture = session.Native<AndroidX.Camera.Video.VideoCapture>("videoCapture")!;
			imageCapture.TargetRotation = (int)SurfaceOrientation.Rotation90;
			preview.TargetRotation = (int)SurfaceOrientation.Rotation90;
			videoCapture.TargetRotation = (int)SurfaceOrientation.Rotation90;

			listener.OnOrientationChanged(OrientationEventListener.OrientationUnknown);

			Assert.Equal((int)SurfaceOrientation.Rotation90, imageCapture.TargetRotation);
			Assert.Equal((int)SurfaceOrientation.Rotation90, preview.TargetRotation);
			Assert.Equal((int)SurfaceOrientation.Rotation90, videoCapture.TargetRotation);
			return Task.CompletedTask;
		});
	}

	[Theory]
	[InlineData(-2)]
	[InlineData(360)]
	[InlineData(720)]
	[InlineData(int.MaxValue)]
	public void SurfaceRotationOfAnOutOfRangeAngleIsRotation0(int degrees)
	{
		Assert.Equal((int)SurfaceOrientation.Rotation0, InvokeGetSurfaceRotation(degrees));
	}

	[Theory]
	[InlineData(0, (int)SurfaceOrientation.Rotation0)]
	[InlineData(44, (int)SurfaceOrientation.Rotation0)]
	[InlineData(45, (int)SurfaceOrientation.Rotation270)]
	[InlineData(134, (int)SurfaceOrientation.Rotation270)]
	[InlineData(135, (int)SurfaceOrientation.Rotation180)]
	[InlineData(224, (int)SurfaceOrientation.Rotation180)]
	[InlineData(225, (int)SurfaceOrientation.Rotation90)]
	[InlineData(314, (int)SurfaceOrientation.Rotation90)]
	[InlineData(315, (int)SurfaceOrientation.Rotation0)]
	[InlineData(359, (int)SurfaceOrientation.Rotation0)]
	public void SurfaceRotationFollowsTheCameraXOrientationRanges(int degrees, int expectedRotation)
	{
		Assert.Equal(expectedRotation, InvokeGetSurfaceRotation(degrees));
	}

	static int InvokeGetSurfaceRotation(int degrees) =>
		(int)typeof(CameraManager).GetMethod("GetSurfaceRotation", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [degrees])!;

	[Fact]
	public async Task OrientationChangesBeforeConnectingAndAfterDisposeDoNotThrow()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();
		var onOrientationChanged = typeof(CameraManager).GetMethod("OnOrientationChanged", BindingFlags.NonPublic | BindingFlags.Instance)!;

		// No use case exists yet: every null-conditional assignment is skipped.
		var beforeConnect = await InvokeOnMainThreadAsync(() => Record.Exception(() => onOrientationChanged.Invoke(session.Manager, [90])));
		Assert.Null(beforeConnect);

		await InvokeOnMainThreadAsync(() => session.Manager.Dispose());

		var afterDispose = await InvokeOnMainThreadAsync(() => Record.Exception(() => onOrientationChanged.Invoke(session.Manager, [90])));
		Assert.Null(afterDispose);
	}

	// ---- extension mode -----------------------------------------------------------------------------------------------

	[Fact]
	public async Task SetExtensionModeBeforeConnectingDoesNotOpenAnyCamera()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();

		await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None);
		await SettleUiThreadAsync();

		Assert.Null(session.Native<ICamera>("camera"));
		Assert.Null(session.Native<Preview>("cameraPreview"));
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task SetExtensionModeWithASelectedCameraBeforeConnectingDoesNotOpenIt()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var camera = (await GetRealCamerasAsync())[0];

		// The view has a camera and the ResolutionSelector exists, but the manager has no ProcessCameraProvider before ConnectCamera.
		var session = await CreateSessionAsync(view: new CameraView { SelectedCamera = camera });
		await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);

		var extension = session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None);
		Assert.True(extension.IsCompletedSuccessfully, "SetExtensionMode waited for CameraX although the manager is not connected.");
		await SettleUiThreadAsync();

		Assert.Null(session.Native<ICamera>("camera"));
		Assert.Null(session.Native<Preview>("cameraPreview"));
		Assert.Equal(0, session.LoadedCount);
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task SetExtensionModeWithoutASelectedCameraDoesNotRebind()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			session.View.SelectedCamera = null;
			await UnbindAllNativeAsync();

			await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.False(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture));
		});
	}

	[Fact]
	public async Task SetExtensionModeWithoutAPreviewDoesNotRebind()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var imageCapture = session.ImageCapture;
			SetPrivateField(session.Manager, "cameraPreview", null);
			await UnbindAllNativeAsync();

			await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.False(await IsNativelyBoundAsync(imageCapture));
		});
	}

	[Fact]
	public async Task SetExtensionModeWithoutAnImageCaptureDoesNotBindTheSession()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var preview = session.CameraPreview;
			var imageCapture = session.ImageCapture;
			SetPrivateField(session.Manager, "imageCapture", null);
			try
			{
				await UnbindAllNativeAsync();

				await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None).WaitAsync(CameraTimeout);

				Assert.False(await IsNativelyBoundAsync(preview));
				Assert.False(await IsNativelyBoundAsync(imageCapture));
			}
			finally
			{
				SetPrivateField(session.Manager, "imageCapture", imageCapture);
			}
		});
	}

	[Fact]
	public async Task SetExtensionModeBindsThePhotoSessionAgain()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var cameraId = session.View.SelectedCamera!.DeviceId;
			await UnbindAllNativeAsync();
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture));

			await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.False(await IsNativelyBoundAsync(session.Native<AndroidX.Camera.Video.VideoCapture>("videoCapture")!));
			Assert.Equal(cameraId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			await WaitForStreamingAsync(session.PreviewView!);
		});
	}

	[Fact]
	public async Task SetExtensionModeWithTheNoneExtensionKeepsTheSelectedCameraOpen()
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, "Needs two cameras to tell the selected one from the first one; this device has one.");
		var second = cameras[1];
		Skip.IfNot(await IsNativeExtensionAvailableAsync(second, ExtensionMode.None), "CameraX does not report the None extension as available for the second camera.");

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None).WaitAsync(CameraTimeout);

			// The extension-enabled selector that CameraX returns for ExtensionMode.None selects the same camera, without any extension.
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.Equal(second.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			Assert.Null(GetSessionProcessorOfTheBoundCamera(session));
			Assert.Equal("CLOSED", await GetNativeCameraStateAsync(cameras[0].DeviceId));
			await WaitForStreamingAsync(session.PreviewView!);
		}, view: new CameraView { SelectedCamera = second });
	}

	[Fact]
	public async Task SetExtensionModeWithAnExtensionThatIsNotAvailableBindsTheCameraWithThePlainSelector()
	{
		SetupBuilder();
		var camera = await GetCameraAsync(_ => true, "The device has no camera.");
		Skip.If(await IsNativeExtensionAvailableAsync(camera, ExtensionMode.Bokeh), "This device offers the bokeh extension: the plain-selector path of EnableModes can not be reached with it.");

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.SetExtensionMode(ExtensionMode.Bokeh, CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.Equal(camera.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			Assert.Null(GetSessionProcessorOfTheBoundCamera(session));
			await WaitForStreamingAsync(session.PreviewView!);
		});
	}

	[Theory]
	[InlineData(ExtensionMode.Bokeh)]
	[InlineData(ExtensionMode.Hdr)]
	[InlineData(ExtensionMode.Night)]
	[InlineData(ExtensionMode.FaceRetouch)]
	[InlineData(ExtensionMode.Auto)]
	public async Task SetExtensionModeWithAnAvailableVendorExtensionBindsTheExtensionEnabledCamera(int extensionMode)
	{
		SetupBuilder();
		var camera = await GetCameraAsync(_ => true, "The device has no camera.");
		Skip.IfNot(await IsNativeExtensionAvailableAsync(camera, extensionMode),
			$"Needs a device whose camera offers CameraX vendor extension {extensionMode}: the extension-enabled selector is only used then (emulators have no vendor extensions).");

		await RunWithStreamingSessionAsync(async session =>
		{
			await session.Manager.SetExtensionMode(extensionMode, CancellationToken.None).WaitAsync(CameraTimeout);

			// Only a camera bound through the extension-enabled selector carries the vendor extension's SessionProcessor.
			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.Equal(camera.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
			Assert.NotNull(GetSessionProcessorOfTheBoundCamera(session));
			await WaitForStreamingAsync(session.PreviewView!);

			// Without the extension the camera is bound through the plain selector again.
			await session.Manager.SetExtensionMode(ExtensionMode.None, CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Null(GetSessionProcessorOfTheBoundCamera(session));
			Assert.Equal(camera.DeviceId, GetBoundCameraId(session.Native<ICamera>("camera")!));
		});
	}

	[Fact]
	public async Task SetExtensionModeWithACancelledTokenThrowsOperationCanceledExceptionAndKeepsTheSessionBound()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			// EnableModes waits for CameraX with the cancelled token before the session is unbound.
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.SetExtensionMode(ExtensionMode.None, cancellation.Token));

			Assert.True(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.True(await IsNativelyBoundAsync(session.ImageCapture));
			await WaitForStreamingAsync(session.PreviewView!);
		});
	}
}