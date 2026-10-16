using System.Reflection;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using AndroidX.Camera.Core;
using CommunityToolkit.Maui.Core;
using AndroidCameraManager = Android.Hardware.Camera2.CameraManager;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;
using ICameraProvider = CommunityToolkit.Maui.Core.ICameraProvider;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraProviderAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraProviderAndroidTests : CameraAndroidTestBase
{
	static CameraCharacteristics GetCamera2Characteristics(string cameraId)
	{
		var cameraManager = (AndroidCameraManager)AppContext.GetSystemService(Context.CameraService)!;
		return cameraManager.GetCameraCharacteristics(cameraId)!;
	}

	static int GetCamera2LensFacing(string cameraId) =>
		((Java.Lang.Integer)GetCamera2Characteristics(cameraId).Get(CameraCharacteristics.LensFacing)!).IntValue();

	static List<Size> GetCamera2JpegSizesInCameraProviderOrder(string cameraId)
	{
		var map = (StreamConfigurationMap)GetCamera2Characteristics(cameraId).Get(CameraCharacteristics.ScalerStreamConfigurationMap)!;
		var sizes = new List<Size>();

		if (OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			sizes.AddRange((map.GetHighResolutionOutputSizes((int)ImageFormatType.Jpeg) ?? []).Select(size => new Size(size.Width, size.Height)));
		}

		sizes.AddRange((map.GetOutputSizes((int)ImageFormatType.Jpeg) ?? []).Select(size => new Size(size.Width, size.Height)));
		return sizes;
	}

	static async Task<IReadOnlyList<CameraInfo>> RefreshAsync(CameraProvider provider)
	{
		await provider.RefreshAvailableCameras(CancellationToken.None).WaitAsync(CameraTimeout);
		return provider.AvailableCameras ?? throw new InvalidOperationException("RefreshAvailableCameras left AvailableCameras null.");
	}

	static void SetAvailableCameras(CameraProvider provider, IReadOnlyList<CameraInfo>? cameras) =>
		typeof(CameraProvider).GetProperty(nameof(CameraProvider.AvailableCameras))!.GetSetMethod(true)!.Invoke(provider, [cameras]);

	static CameraInfo CreateInfo(string id) => new("Camera " + id, id, CameraPosition.Rear, false, 1, 1, [], null!);

	[Fact]
	public void AvailableCamerasIsNullUntilTheyAreRefreshed()
	{
		SetupBuilder();
		using var provider = new CameraProvider();

		Assert.Null(provider.AvailableCameras);
	}

	[Fact]
	public async Task RefreshPublishesEveryNativeCameraInTheNativeOrder()
	{
		SetupBuilder();
		using var provider = new CameraProvider();

		var cameras = await RefreshAsync(provider);

		var nativeProvider = await GetNativeCameraProviderAsync();
		var nativeIds = nativeProvider.AvailableCameraInfos.Select(GetNativeCameraId).ToList();
		Assert.NotEmpty(nativeIds);
		Assert.Equal(nativeIds, cameras.Select(camera => camera.DeviceId).ToList());
	}

	[Fact]
	public async Task RefreshMapsTheNativeLensFacingToNameAndPosition()
	{
		SetupBuilder();
		using var provider = new CameraProvider();

		var cameras = await RefreshAsync(provider);

		foreach (var camera in cameras)
		{
			var nativeLensFacing = GetCamera2LensFacing(camera.DeviceId);
			var (expectedName, expectedPosition) = nativeLensFacing switch
			{
				(int)LensFacing.Back => ("Back Camera", CameraPosition.Rear),
				(int)LensFacing.Front => ("Front Camera", CameraPosition.Front),
				_ => ("Unknown Position Camera", CameraPosition.Unknown)
			};

			Assert.Equal(expectedName, camera.Name);
			Assert.Equal(expectedPosition, camera.Position);
		}
	}

	[Fact]
	public async Task RefreshReadsFlashSupportFromTheNativeCamera()
	{
		SetupBuilder();
		using var provider = new CameraProvider();

		var cameras = await RefreshAsync(provider);

		foreach (var camera in cameras)
		{
			var nativeInfo = await GetNativeCameraInfoAsync(camera.DeviceId);
			Assert.Equal(nativeInfo.HasFlashUnit, camera.IsFlashSupported);
		}
	}

	[Fact]
	public async Task RefreshReadsTheZoomRangeFromTheNativeCamera()
	{
		SetupBuilder();
		using var provider = new CameraProvider();

		var cameras = await RefreshAsync(provider);

		foreach (var camera in cameras)
		{
			var zoomState = (IZoomState)(await GetNativeCameraInfoAsync(camera.DeviceId)).ZoomState.Value!;
			Assert.Equal(zoomState.MinZoomRatio, camera.MinimumZoomFactor);
			Assert.Equal(zoomState.MaxZoomRatio, camera.MaximumZoomFactor);
			Assert.True(camera.MinimumZoomFactor <= camera.MaximumZoomFactor);
		}
	}

	[Fact]
	public async Task RefreshReadsTheSupportedResolutionsFromCamera2()
	{
		SetupBuilder();
		using var provider = new CameraProvider();

		var cameras = await RefreshAsync(provider);

		foreach (var camera in cameras)
		{
			var expected = GetCamera2JpegSizesInCameraProviderOrder(camera.DeviceId);
			Assert.NotEmpty(expected);
			Assert.Equal(expected, camera.SupportedResolutions);
		}
	}

	[Fact]
	public async Task RefreshKeepsTheNativeCameraSelectorOfEachCamera()
	{
		SetupBuilder();
		using var provider = new CameraProvider();

		var cameras = await RefreshAsync(provider);

		var nativeCameras = (await GetNativeCameraProviderAsync()).AvailableCameraInfos;
		foreach (var camera in cameras)
		{
			Assert.NotNull(camera.CameraSelector);

			var selected = camera.CameraSelector.Filter(nativeCameras);
			Assert.Equal(new[] { camera.DeviceId }, selected.Select(GetNativeCameraId).ToList());
		}
	}

	[Fact]
	public async Task RefreshRaisesAvailableCamerasChangedWhenTheCamerasAreFirstFound()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		var raised = new List<(object? Sender, IReadOnlyList<CameraInfo>? Cameras)>();
		provider.AvailableCamerasChanged += (sender, cameras) => raised.Add((sender, cameras));

		var cameras = await RefreshAsync(provider);

		var (sender, published) = Assert.Single(raised);
		Assert.Same(provider, sender);
		Assert.Same(cameras, published);
	}

	[Fact]
	public async Task RefreshDoesNotRaiseAvailableCamerasChangedWhenTheCamerasAreUnchanged()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		var first = await RefreshAsync(provider);
		var raised = 0;
		provider.AvailableCamerasChanged += (_, _) => raised++;

		var second = await RefreshAsync(provider);

		Assert.Equal(0, raised);
		Assert.Equal(first, second);
	}

	[Fact]
	public async Task RefreshRaisesAvailableCamerasChangedWhenTheListChanged()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		await RefreshAsync(provider);
		SetAvailableCameras(provider, [CreateInfo("not-a-native-camera")]);
		var raised = new List<IReadOnlyList<CameraInfo>?>();
		provider.AvailableCamerasChanged += (_, cameras) => raised.Add(cameras);

		var refreshed = await RefreshAsync(provider);

		var published = Assert.Single(raised);
		Assert.Same(refreshed, published);
		Assert.DoesNotContain(refreshed, camera => camera.DeviceId == "not-a-native-camera");
	}

	[Fact]
	public async Task RemovedAvailableCamerasChangedHandlerIsNotInvoked()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		var kept = 0;
		var removed = 0;
		EventHandler<IReadOnlyList<CameraInfo>?> removedHandler = (_, _) => removed++;
		provider.AvailableCamerasChanged += (_, _) => kept++;
		provider.AvailableCamerasChanged += removedHandler;
		provider.AvailableCamerasChanged -= removedHandler;

		await RefreshAsync(provider);

		Assert.Equal(1, kept);
		Assert.Equal(0, removed);
	}

	[Fact]
	public async Task RefreshWithACancelledTokenThrowsOperationCanceledException()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.RefreshAvailableCameras(cancellation.Token));

		// The cancelled caller does not get past the semaphore.
		Assert.Null(GetPrivateField<Task>(provider, "refreshAvailableCamerasTask"));

		// The CameraX listener of this request runs after any listener that the provider could have registered on the same future and
		// executor, so no refresh can publish cameras later.
		await GetNativeCameraProviderAsync();
		Assert.Null(provider.AvailableCameras);
	}

	[Fact]
	public async Task RefreshCancelledWhileWaitingForCameraXThrowsButTheNativeQueryStillCompletes()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		using var cancellation = new CancellationTokenSource();

		// Both the request and the cancellation happen in one UI thread message, so the CameraX listener (posted to the UI
		// thread) can not have run when the token is cancelled.
		Task? refresh = null;
		await InvokeOnMainThreadAsync(() =>
		{
			refresh = provider.RefreshAvailableCameras(cancellation.Token);
			cancellation.Cancel();
		});
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh!);

		await AssertionExtensions.AssertEventually(() => provider.AvailableCameras is not null, 30000, 50, "The CameraX query stopped when the token was cancelled.");
	}

	[Fact]
	public async Task ConcurrentRefreshesShareOnePlatformRefresh()
	{
		SetupBuilder();
		using var provider = new CameraProvider();

		await InvokeOnMainThreadAsync(async () =>
		{
			// On the UI thread the CameraX listener of the first refresh can not run before the second call.
			var first = provider.RefreshAvailableCameras(CancellationToken.None);
			var taskOfFirst = GetPrivateField<Task>(provider, "refreshAvailableCamerasTask");
			var second = provider.RefreshAvailableCameras(CancellationToken.None);
			var taskOfSecond = GetPrivateField<Task>(provider, "refreshAvailableCamerasTask");

			Assert.NotNull(taskOfFirst);
			Assert.Same(taskOfFirst, taskOfSecond);
			await Task.WhenAll(first, second).WaitAsync(CameraTimeout);
			Assert.NotNull(provider.AvailableCameras);
		});
	}

	[Fact]
	public async Task RefreshAfterTheLastOneCompletedStartsANewPlatformRefresh()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		var native = await RefreshAsync(provider);
		var nativeIds = native.Select(camera => camera.DeviceId).ToList();

		// A platform refresh that really runs publishes what CameraX reports again, while reusing the completed one would leave the list as it
		// is. Comparing Task instances can not tell them apart: a refresh that completes synchronously is Task.CompletedTask.
		SetAvailableCameras(provider, [CreateInfo("not-a-native-camera")]);

		var refreshed = await RefreshAsync(provider);

		Assert.Equal(nativeIds, refreshed.Select(camera => camera.DeviceId).ToList());
	}

	[Fact]
	public async Task RefreshAfterDisposeThrowsObjectDisposedException()
	{
		SetupBuilder();
		var provider = new CameraProvider();
		provider.Dispose();

		await Assert.ThrowsAsync<ObjectDisposedException>(() => provider.RefreshAvailableCameras(CancellationToken.None));
	}

	[Fact]
	public void DisposeCanBeCalledTwice()
	{
		SetupBuilder();
		var provider = new CameraProvider();

		provider.Dispose();
		var exception = Record.Exception(provider.Dispose);

		Assert.Null(exception);
	}

	[Fact]
	public void TheCameraBuilderExtensionRegistersOneSharedCameraProvider()
	{
		SetupBuilder();

		var first = ApplicationServices.GetRequiredService<ICameraProvider>();
		var second = ApplicationServices.GetRequiredService<ICameraProvider>();

		Assert.IsType<CameraProvider>(first);
		Assert.Same(first, second);
	}

	static List<Size>? GetCamera2HighResolutionSizes(string cameraId)
	{
		var map = (StreamConfigurationMap)GetCamera2Characteristics(cameraId).Get(CameraCharacteristics.ScalerStreamConfigurationMap)!;
		return map.GetHighResolutionOutputSizes((int)ImageFormatType.Jpeg)?.Select(size => new Size(size.Width, size.Height)).ToList();
	}

	[Fact]
	public async Task RefreshListsTheHighResolutionSizesBeforeTheNormalSizesOfACameraThatSupportsThem()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		var cameras = await RefreshAsync(provider);
		var camera = cameras.FirstOrDefault(camera => GetCamera2HighResolutionSizes(camera.DeviceId) is { Count: > 0 });
		Skip.If(camera is null, "No camera of this device supports high resolution capture, so the high resolution sizes are not listed here.");

		var highResolutions = GetCamera2HighResolutionSizes(camera.DeviceId)!;

		Assert.Equal(highResolutions, camera.SupportedResolutions.Take(highResolutions.Count));
		Assert.True(camera.SupportedResolutions.Count > highResolutions.Count, "The normal sizes follow the high resolution sizes.");
	}

	[Fact]
	public async Task RefreshListsOnlyTheNormalSizesOfACameraWithoutHighResolutionSupport()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		var cameras = await RefreshAsync(provider);
		var camera = cameras.FirstOrDefault(camera => GetCamera2HighResolutionSizes(camera.DeviceId) is null or { Count: 0 });
		Skip.If(camera is null, "Every camera of this device supports high resolution capture, so the arm without high resolution sizes is not taken here.");

		Assert.Equal(GetCamera2JpegSizesInCameraProviderOrder(camera.DeviceId), camera.SupportedResolutions);
	}

	[Fact]
	public async Task SecondRefreshIsNotCancelledWhenOnlyTheFirstCallerCancels()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		using var cancellation = new CancellationTokenSource();
		Task first = null!, second = null!;

		// All of this happens in one UI-thread message, so CameraX can not have answered yet when the first caller cancels.
		await InvokeOnMainThreadAsync(() =>
		{
			first = provider.RefreshAvailableCameras(cancellation.Token);
			second = provider.RefreshAvailableCameras(CancellationToken.None);
			cancellation.Cancel();
		});

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
		await second.WaitAsync(CameraTimeout);
		Assert.NotNull(provider.AvailableCameras);
	}

	[Fact]
	public async Task ProviderIsCollectedAfterItRefreshedTheCamerasAndWasDisposed()
	{
		SetupBuilder();

		var provider = await RefreshAndDisposeAsync();

		await AssertCollectedAsync(("CameraProvider", provider));
	}

	// The provider only lives in this method, so that no local of the test keeps it alive.
	static async Task<WeakReference> RefreshAndDisposeAsync()
	{
		var provider = new CameraProvider();
		provider.AvailableCamerasChanged += (_, _) => { };
		await RefreshAsync(provider);
		var reference = new WeakReference(provider);
		provider.Dispose();
		return reference;
	}

	[Fact]
	public async Task RefreshMapsAnExternalCameraToTheUnknownPosition()
	{
		SetupBuilder();
		using var provider = new CameraProvider();
		var cameras = await RefreshAsync(provider);
		var external = cameras.Where(camera => GetCamera2LensFacing(camera.DeviceId) == (int)LensFacing.External).ToList();
		Skip.If(external.Count is 0, "Needs an external (USB) camera (LENS_FACING_EXTERNAL): the emulators of the pool only have a front and a back camera.");

		Assert.All(external, camera =>
		{
			Assert.Equal("Unknown Position Camera", camera.Name);
			Assert.Equal(CameraPosition.Unknown, camera.Position);
		});
	}

	[Fact]
	public async Task RefreshSkipsHighResolutionSizesBelowAndroid6()
	{
		SetupBuilder();
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(23), "Needs Android 5 (API 21 or 22): StreamConfigurationMap.GetHighResolutionOutputSizes is only skipped there, and the pool (like CI) runs API 28 and 36.");
		using var provider = new CameraProvider();

		var cameras = await RefreshAsync(provider);

		foreach (var camera in cameras)
		{
			var map = (StreamConfigurationMap)GetCamera2Characteristics(camera.DeviceId).Get(CameraCharacteristics.ScalerStreamConfigurationMap)!;
			Assert.Equal((map.GetOutputSizes((int)ImageFormatType.Jpeg) ?? []).Select(size => new Size(size.Width, size.Height)), camera.SupportedResolutions);
		}
	}

	[Fact]
	public async Task RefreshPublishesAnEmptyListWhenTheDeviceHasNoCamera()
	{
		SetupBuilder();
		var cameraManager = (AndroidCameraManager)AppContext.GetSystemService(Context.CameraService)!;
		Skip.If(cameraManager.GetCameraIdList().Length > 0, "Needs a device without cameras (an emulator started with -camera-back none -camera-front none): every emulator of the pool (and of CI) has cameras.");
		using var provider = new CameraProvider();

		var cameras = await RefreshAsync(provider);

		Assert.Empty(cameras);
	}

	// The same branch on a device with cameras: CameraX can be limited to some of the device's cameras
	// (CameraXConfig.Builder.SetAvailableCamerasLimiter). Limited to none, it lists no camera at all, as on a device without cameras.
	[Fact]
	public async Task RefreshPublishesAnEmptyListWhenCameraXListsNoCamera()
	{
		SetupBuilder();
		await LimitCameraXToNoCameraAsync();
		using var provider = new CameraProvider();
		var raised = new List<IReadOnlyList<CameraInfo>?>();
		provider.AvailableCamerasChanged += (_, cameras) => raised.Add(cameras);

		var cameras = await RefreshAsync(provider);

		Assert.Empty((await GetNativeCameraProviderAsync()).AvailableCameraInfos);
		Assert.Empty(cameras);
		Assert.Same(cameras, Assert.Single(raised));
	}

	// CameraX fails to initialize when the camera service lists fewer cameras than the device declares ("Device reporting less cameras
	// than anticipated. On real devices: Retrying initialization might resolve temporary camera errors"), as on an emulator whose camera
	// HAL lost its cameras. CameraProvider reads the result of the initialization in a CameraX listener on the UI thread: the exception
	// escaped from that listener and terminated the app, and the refresh never completed.
	[Fact]
	public async Task RefreshThrowsTheErrorOfCameraXWhenCameraXCanNotInitialize()
	{
		SetupBuilder();
		Skip.IfNot(AppContext.PackageManager?.HasSystemFeature(PackageManager.FeatureCamera) is true,
			"Needs a device that declares a back camera (android.hardware.camera): CameraX only fails to initialize when it misses a camera that the device declares.");
		await MakeCameraXFailToInitializeAsync();
		using var provider = new CameraProvider();
		var raised = 0;
		provider.AvailableCamerasChanged += (_, _) => raised++;

		var exception = await Assert.ThrowsAsync<Java.Util.Concurrent.ExecutionException>(
			() => provider.RefreshAvailableCameras(CancellationToken.None).WaitAsync(CameraTimeout));

		var initializationException = Assert.IsType<InitializationException>(exception.Cause);
		Assert.IsType<CameraUnavailableException>(initializationException.Cause);
		Assert.Null(provider.AvailableCameras);
		Assert.Equal(0, raised);
	}

	Task LimitCameraXToNoCameraAsync() => ReconfigureCameraXAsync(new CameraSelector.Builder().AddCameraFilter(new NoCameraFilter()).Build());

	// CameraX must find the back camera that the device declares, but its camera limiter selects no camera. Without retries it fails at
	// once, instead of after retrying for 6 seconds.
	Task MakeCameraXFailToInitializeAsync() => ReconfigureCameraXAsync(
		new CameraSelector.Builder().RequireLensFacing(CameraSelector.LensFacingBack).AddCameraFilter(new NoCameraFilter()).Build(),
		RetryPolicy.Never);

	// Shutting down and then configuring is how CameraX's own tests reconfigure it (ProcessCameraProvider.ShutdownAsync, then
	// ConfigureInstance). The cleanup shuts CameraX down once more, so that the next test starts it with its default configuration again.
	async Task ReconfigureCameraXAsync(CameraSelector availableCamerasLimiter, IRetryPolicy? initializationRetryPolicy = null)
	{
		var native = await GetNativeCameraProviderAsync();
		RegisterCleanup(() => native.ShutdownAsync());

		await InvokeOnMainThreadAsync(() => WaitForFutureAsync(native.ShutdownAsync()));
		var configuration = CameraXConfig.Builder.FromConfig(AndroidX.Camera.Camera2.Camera2Config.DefaultConfig()).SetAvailableCamerasLimiter(availableCamerasLimiter);
		if (initializationRetryPolicy is not null)
		{
			configuration = configuration.SetCameraProviderInitRetryPolicy(initializationRetryPolicy);
		}

		AndroidX.Camera.Lifecycle.ProcessCameraProvider.ConfigureInstance(configuration.Build());
	}

	static Task WaitForFutureAsync(Google.Common.Util.Concurrent.IListenableFuture future)
	{
		var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		future.AddListener(new Java.Lang.Runnable(() =>
		{
			try
			{
				future.Get();
				done.TrySetResult();
			}
			catch (Exception ex)
			{
				done.TrySetException(ex);
			}
		}), AndroidX.Core.Content.ContextCompat.GetMainExecutor(AppContext));

		return done.Task.WaitAsync(CameraTimeout);
	}

	sealed class NoCameraFilter : Java.Lang.Object, ICameraFilter
	{
		public IList<ICameraInfo> Filter(IList<ICameraInfo> cameraInfos) => [];
	}
}