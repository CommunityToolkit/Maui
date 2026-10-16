using System.Reflection;
using System.Runtime.CompilerServices;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using AView = Android.Views.View;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;
using ICameraProvider = CommunityToolkit.Maui.Core.ICameraProvider;
using Runnable = Java.Lang.Runnable;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract class CameraAndroidTestBase : CameraHandlerTestBase
{
	internal const string CameraPermission = "android.permission.CAMERA";
	internal const string RecordAudioPermission = "android.permission.RECORD_AUDIO";

	// Only an upper bound: reached when a test fails or the machine is overloaded.
	internal static readonly TimeSpan CameraTimeout = TimeSpan.FromSeconds(45);

	readonly List<Action> cleanups = [];
	readonly AsyncVoidExceptionGuard asyncVoidGuard = new();
	SynchronizationContext? mainThreadSynchronizationContext;
	bool asyncVoidGuardInstalled;
	bool usedCameraSession;

	protected static Context AppContext => MauiProgramDefaults.DefaultContext;

	protected void SetupBuilder(Action<MauiAppBuilder>? configureBuilder = null, [CallerMemberName] string? testName = null)
	{
		// The runner only reports results when the whole run ends, so a crash of the app (the camera HAL, the media codec or
		// the toolkit) loses them all. This marker in the device log tells which test was running.
		Android.Util.Log.Info("CameraDeviceTests", $"START {GetType().Name}.{testName}");

		base.SetupBuilder(configureBuilder);
		InstallAsyncVoidGuard();
	}

	// Without the guard, an exception that escapes from async void code (CameraViewHandler.ConnectHandler, the CameraX listener callbacks
	// of CameraManager) would crash the whole test run.
	void InstallAsyncVoidGuard()
	{
		if (asyncVoidGuardInstalled)
		{
			return;
		}

		asyncVoidGuardInstalled = true;
		TestDispatcher.Current.Dispatch(() =>
		{
			mainThreadSynchronizationContext = SynchronizationContext.Current;
			asyncVoidGuard.Inner = mainThreadSynchronizationContext;
			SynchronizationContext.SetSynchronizationContext(asyncVoidGuard);
		});
	}

	protected IReadOnlyList<Exception> UnhandledAsyncVoidExceptions => asyncVoidGuard.Snapshot();

	protected static bool IsPermissionGranted(string permission) =>
		AppContext.CheckSelfPermission(permission) == Permission.Granted;

	// Revoking a runtime permission kills the app process, so tests never revoke: the permissions stay granted until XHarness
	// uninstalls the app. Tests that need a denied permission use a Context whose permission checks are denied instead.
	protected static void EnsureCameraPermissions(bool includeRecordAudio = false)
	{
		string[] permissions = includeRecordAudio ? [CameraPermission, RecordAudioPermission] : [CameraPermission];

		foreach (var permission in permissions.Where(permission => !IsPermissionGranted(permission)))
		{
			TryGrantPermission(permission);
		}

		Skip.IfNot(permissions.All(IsPermissionGranted),
			$"The app does not have {string.Join(" and ", permissions)} and the instrumentation could not grant it (start the tests through eng/devices/android.ps1, or grant it in the app settings).");
	}

	static void TryGrantPermission(string permission)
	{
		try
		{
			_ = RunShellCommand($"pm grant {AppContext.PackageName} {permission}");
		}
		catch (Exception ex)
		{
			Android.Util.Log.Warn("CameraAndroidTests", $"Could not grant {permission}: {ex.Message}");
		}
	}

	// The instrumentation's UiAutomation has the identity of the adb shell; AndroidX's GrantPermissionRule grants permissions the same way.
	static string RunShellCommand(string command)
	{
		var uiAutomation = MauiTestInstrumentation.Current?.UiAutomation
			?? throw new InvalidOperationException("The app was not started by an instrumentation, so it has no UiAutomation to run shell commands.");

		using var descriptor = uiAutomation.ExecuteShellCommand(command);
		using var stream = new Android.Runtime.InputStreamInvoker(new Android.OS.ParcelFileDescriptor.AutoCloseInputStream(descriptor));
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	// The accelerometer connections that OrientationEventListener.enable registers (and disable removes) appear in dumpsys sensorservice
	// as "android.view.OrientationEventListener | WakeLockRefCount 0 | uid 10216 | ...".
	protected static async Task<int> CountOrientationListenersOfThisAppAsync()
	{
		string dump;
		try
		{
			dump = await Task.Run(() => RunShellCommand("dumpsys sensorservice"));
		}
		catch (InvalidOperationException ex)
		{
			throw new SkipException(ex.Message);
		}

		Skip.IfNot(dump.Contains("Active sensors", StringComparison.Ordinal), "The sensor service dump of this device has no list of active sensors.");
		var uid = Android.OS.Process.MyUid();
		return dump.Split('\n').Count(line => line.Contains("android.view.OrientationEventListener |", StringComparison.Ordinal) && line.Contains($"| uid {uid} |", StringComparison.Ordinal));
	}

	// The managed peer of the process-wide CameraX provider is shared by every CameraManager of the process: never dispose it.
	protected static async Task<ProcessCameraProvider> GetNativeCameraProviderAsync()
	{
		var context = AppContext;
		var completion = new TaskCompletionSource<ProcessCameraProvider>(TaskCreationOptions.RunContinuationsAsynchronously);
		var future = ProcessCameraProvider.GetInstance(context);
		future.AddListener(new Runnable(() =>
		{
			try
			{
				completion.TrySetResult((ProcessCameraProvider)future.Get()!);
			}
			catch (Exception ex)
			{
				completion.TrySetException(ex);
			}
		}), ContextCompat.GetMainExecutor(context));

		return await completion.Task.WaitAsync(CameraTimeout);
	}

	protected static string GetNativeCameraId(ICameraInfo nativeInfo) =>
		AndroidX.Camera.Camera2.InterOp.Camera2CameraInfo.From(nativeInfo)?.CameraId ?? throw new InvalidOperationException("The native camera has no id.");

	protected static async Task<ICameraInfo> GetNativeCameraInfoAsync(string cameraId)
	{
		var provider = await GetNativeCameraProviderAsync();
		return provider.AvailableCameraInfos.FirstOrDefault(info => GetNativeCameraId(info) == cameraId)
			?? throw new InvalidOperationException($"CameraX does not know a camera with id {cameraId}.");
	}

	// CameraState.GetType() is CameraX's getType(), not Object.GetType(): PENDING_OPEN, OPENING, OPEN, CLOSING or CLOSED.
	protected static string GetNativeCameraState(ICameraInfo nativeInfo) =>
		(nativeInfo.CameraState.Value as CameraState)?.GetType()?.ToString() ?? "UNKNOWN";

	protected static async Task<string> GetNativeCameraStateAsync(string cameraId) =>
		GetNativeCameraState(await GetNativeCameraInfoAsync(cameraId));

	// JavaCast, as CameraProvider reads it: in a trimmed (Release) app the value is a plain Java.Lang.Object, which a cast to IZoomState rejects.
	protected static IZoomState GetNativeZoomState(ICameraInfo nativeInfo) =>
		nativeInfo.ZoomState.Value?.JavaCast<IZoomState>() ?? throw new InvalidOperationException("The native camera has no zoom state.");

	protected static float GetNativeZoomRatio(ICameraInfo nativeInfo) => GetNativeZoomState(nativeInfo).ZoomRatio;

	protected static bool IsNativeTorchOn(ICameraInfo nativeInfo) =>
		(nativeInfo.TorchState.Value as Java.Lang.Integer)?.IntValue() == 1;

	protected static async Task WaitForNativeCameraStateAsync(string cameraId, string expectedState, int timeoutMilliseconds = 30000)
	{
		var last = "never read";
		try
		{
			await AssertionExtensions.AssertEventuallyAsync(async () =>
			{
				last = await GetNativeCameraStateAsync(cameraId);
				return last == expectedState;
			}, timeoutMilliseconds, 100, $"Camera {cameraId} never reached the native state {expectedState}.");
		}
		catch (Exception ex) when (ex is not Xunit.Sdk.XunitException || ex.Message.Contains("never reached", StringComparison.Ordinal))
		{
			var all = string.Join(", ", (await GetNativeCameraProviderAsync()).AvailableCameraInfos.Select(info => $"{GetNativeCameraId(info)}={GetNativeCameraState(info)}"));
			throw new Xunit.Sdk.XunitException($"Camera {cameraId} never reached the native state {expectedState} (last state {last}; every camera: {all}).");
		}
	}

	protected static async Task WaitForStreamingAsync(PreviewView previewView, int timeoutMilliseconds = 60000)
	{
		try
		{
			await AssertionExtensions.AssertEventually(() => IsStreaming(previewView), timeoutMilliseconds, 100, "The preview never started streaming.");
		}
		catch (Xunit.Sdk.XunitException ex) when (ex.Message.Contains("never started streaming", StringComparison.Ordinal))
		{
			var all = string.Join(", ", (await GetNativeCameraProviderAsync()).AvailableCameraInfos.Select(info => $"{GetNativeCameraId(info)}={GetNativeCameraState(info)}"));
			throw new Xunit.Sdk.XunitException($"The preview never started streaming (stream state {previewView.PreviewStreamState.Value}; every camera: {all}).");
		}
	}

	protected static bool IsStreaming(PreviewView previewView) =>
		previewView.PreviewStreamState.Value?.ToString() == "STREAMING";

	protected static async Task WaitForAllCamerasClosedAsync(int timeoutMilliseconds = 30000)
	{
		await AssertionExtensions.AssertEventuallyAsync(async () =>
		{
			var provider = await GetNativeCameraProviderAsync();
			return provider.AvailableCameraInfos.All(info => GetNativeCameraState(info) is "CLOSED" or "UNKNOWN");
		}, timeoutMilliseconds, 100, "A camera is still open.");
	}

	protected static async Task<bool> IsNativelyBoundAsync(UseCase useCase) =>
		(await GetNativeCameraProviderAsync()).IsBound(useCase);

	// Streaming proves that the bound camera is open, so this deliberately does not wait for CameraX's public camera state
	// (ICameraInfo.CameraState) to become OPEN. When the toolkit rebinds a camera that is already open (UnbindAll followed by
	// BindToLifecycle in one UI-thread message: a new capture resolution, an extension mode, a recording that starts or stops, a
	// restarted preview), CameraX's camera thread goes CLOSING, REOPENING and OPENED before the UI thread dispatched the states it
	// posted, and CameraStateMachine drops the final OPEN because LiveData still holds the OPEN of before (logcat:
	// "Transitioning camera internal state: REOPENING --> OPENED" without a following "Publishing new public camera state ... OPEN").
	// The public state then stays OPENING for good while the camera streams. A camera that was closed before (never rebound while open)
	// does publish OPEN: tests that open a camera for the first time assert it with WaitForNativeCameraStateAsync.
	protected static Task WaitUntilCameraIsStreamingAsync(CameraView view, PreviewView previewView)
	{
		ArgumentNullException.ThrowIfNull(view.SelectedCamera);
		return WaitForStreamingAsync(previewView);
	}

	protected static Task WaitForNativeTorchAsync(string cameraId, bool on, int timeoutMilliseconds = 30000, string? message = null) =>
		AssertionExtensions.AssertEventuallyAsync(async () => IsNativeTorchOn(await GetNativeCameraInfoAsync(cameraId)) == on, timeoutMilliseconds, 50,
			message ?? $"The torch of camera {cameraId} never became {(on ? "on" : "off")}.");

	protected static Task WaitForNativeZoomAsync(string cameraId, float ratio, int timeoutMilliseconds = 30000, string? message = null) =>
		AssertionExtensions.AssertEventuallyAsync(async () => Math.Abs(GetNativeZoomRatio(await GetNativeCameraInfoAsync(cameraId)) - ratio) < 0.01f, timeoutMilliseconds, 50,
			message ?? $"The zoom ratio of camera {cameraId} never became {ratio}.");

	// Simulates another app component that uses CameraX. Must run on the UI thread.
	protected static async Task UnbindAllNativeAsync() => (await GetNativeCameraProviderAsync()).UnbindAll();

	// A 4:3 capture size that is smaller than what the cameras of the emulators capture by default (the largest 4:3 size: 640x480 on
	// the Android 9 emulator, 2048x1536 and more on newer ones), so that restricting the capture resolution to it visibly changes the
	// resolution CameraX negotiates.
	protected static Size SmallResolution => new(320, 240);

	protected static (int Width, int Height) SmallSize => (320, 240);

	protected static string GetBoundCameraId(ICamera camera) => GetNativeCameraId(camera.CameraInfo);

	protected static (int Width, int Height)? GetNativeResolution(ResolutionInfo? info) =>
		info?.Resolution is Android.Util.Size size ? (size.Width, size.Height) : null;

	protected Task SettleUiThreadAsync() =>
		InvokeOnMainThreadAsync(() => { }).ContinueWith(_ => InvokeOnMainThreadAsync(() => { }), TaskScheduler.Default).Unwrap();

	// Waiting only for STREAMING could return before the rebind took effect, while the PreviewView still reports the stream of the
	// old session. Must be called on the UI thread: LiveData observers are added there.
	protected static async Task RunAndWaitForANewPreviewStreamAsync(PreviewView previewView, Func<Task> action)
	{
		var states = new LiveDataRecorder(previewView.PreviewStreamState);
		try
		{
			await action();
			await AssertionExtensions.AssertEventually(() =>
			{
				var values = states.Values.ToList();
				var idle = values.IndexOf("IDLE");
				return idle >= 0 && values.Skip(idle + 1).Contains("STREAMING");
			}, 30000, 50, $"The preview did not restart its stream (stream states: {string.Join(", ", states.Values)}).");
		}
		finally
		{
			states.Stop();
		}
	}

	// The selector matches no camera of the device, like an external USB camera that is not plugged in or a CameraInfo that outlived its camera.
	protected static CameraInfo CreateCameraThatCanNotBeBound() =>
		new("External Camera", "unplugged-camera", CameraPosition.Unknown, false, 1, 1, [],
			new CameraSelector.Builder().RequireLensFacing(CameraSelector.LensFacingExternal)!.Build());

	internal static async Task<Stream> TakePictureAsync(ManagerSession session)
	{
		var outcome = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
		EventHandler<MediaCapturedEventArgs> onCaptured = (_, e) => outcome.TrySetResult(e.Media);
		EventHandler<MediaCaptureFailedEventArgs> onFailed = (_, e) => outcome.TrySetException(new CameraException(e.FailureReason));
		session.View.MediaCaptured += onCaptured;
		session.View.MediaCaptureFailed += onFailed;
		try
		{
			await session.Manager.TakePicture(CancellationToken.None);
			return await outcome.Task.WaitAsync(CameraTimeout);
		}
		finally
		{
			session.View.MediaCaptured -= onCaptured;
			session.View.MediaCaptureFailed -= onFailed;
		}
	}

	protected static void RequireStillCapture() =>
		Skip.If(IsAndroid9OrOlderEmulator,
			"The camera HAL of the Android 9 (and older) emulator images crashes with SIGSEGV (camera.ranchu JpegCompressor/createThumbnail) on every still capture, so this device can not produce a JPEG.");

	protected static void RequireVideoRecording() =>
		Skip.If(IsAndroid9OrOlderEmulator,
			"The software H.264 encoder of the Android 9 (and older) emulator images aborts (media.codec: Codec2BufferUtils.cpp:214 'Check failed: (src.width() & 1) == 0') as soon as a recording receives frames and takes the app down with it, so this device can not record a video.");

	static bool IsAndroid9OrOlderEmulator => !OperatingSystem.IsAndroidVersionAtLeast(29) && Android.OS.Build.Hardware is "ranchu" or "goldfish";

	protected static (int Width, int Height) GetJpegSize(byte[] jpeg)
	{
		var options = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
		Android.Graphics.BitmapFactory.DecodeByteArray(jpeg, 0, jpeg.Length, options);
		return (options.OutWidth, options.OutHeight);
	}

	protected static async Task<byte[]> ReadAllBytesAsync(Stream stream)
	{
		using var copy = new MemoryStream();
		await stream.CopyToAsync(copy);
		return copy.ToArray();
	}

	protected static bool IsJpeg(byte[] data) =>
		data.Length > 4 && data[0] == 0xFF && data[1] == 0xD8 && data[^2] == 0xFF && data[^1] == 0xD9;

	protected static IReadOnlyList<string> ListRecordingFiles() =>
		AppContext.CacheDir?.ListFiles()?.Where(file => file.Name.EndsWith(".mp4", StringComparison.Ordinal)).Select(file => file.AbsolutePath).Order().ToList() ?? [];

	protected static (bool HasVideo, long DurationMilliseconds, int Width, int Height) ReadVideoInfo(byte[] mp4)
	{
		var path = System.IO.Path.Combine(AppContext.CacheDir!.AbsolutePath, $"probe-{Guid.NewGuid():N}.probe");
		File.WriteAllBytes(path, mp4);

		try
		{
			using var retriever = new Android.Media.MediaMetadataRetriever();
			try
			{
				retriever.SetDataSource(path);
				_ = long.TryParse(retriever.ExtractMetadata(Android.Media.MetadataKey.Duration), out var duration);
				_ = int.TryParse(retriever.ExtractMetadata(Android.Media.MetadataKey.VideoWidth), out var width);
				_ = int.TryParse(retriever.ExtractMetadata(Android.Media.MetadataKey.VideoHeight), out var height);
				return (retriever.ExtractMetadata(Android.Media.MetadataKey.HasVideo) == "yes", duration, width, height);
			}
			finally
			{
				retriever.Release();
			}
		}
		finally
		{
			File.Delete(path);
		}
	}

	// The Java peers of a view are released by CameraX and the Android runtime a little later than the managed objects, so this retries for up
	// to 20 seconds. Unlike AssertionExtensions.WaitForGC it lists the survivors by type name: the toolkit disposes the Java peers of its views,
	// and ToString on a disposed peer throws.
	protected static async Task AssertCollectedAsync(params (string Name, WeakReference Reference)[] references)
	{
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
		while (references.Any(entry => entry.Reference.IsAlive) && DateTime.UtcNow < deadline)
		{
			await AssertionExtensions.Collect();
			await Task.Delay(100);
		}

		var survivors = references.Where(entry => entry.Reference.IsAlive).Select(entry => $"{entry.Name} ({entry.Reference.Target?.GetType().FullName})").ToList();
		Assert.True(survivors.Count is 0, $"Expected every reference to be collected, but these are still alive after 20 seconds of garbage collections: {string.Join(", ", survivors)}.");
	}

	// White box on purpose: the tests assert the native objects that CameraManager holds in its private fields, never its flags.
	protected static T? GetPrivateField<T>(object target, string name) =>
		(T?)GetFieldInfo(target, name).GetValue(target);

	protected static void SetPrivateField(object target, string name, object? value) =>
		GetFieldInfo(target, name).SetValue(target, value);

	static FieldInfo GetFieldInfo(object target, string name) =>
		target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(target.GetType().FullName, name);

	// Unlike AttachAndRun, this can host several views at once, and AttachAndRun can not be nested.
	protected Task HostAsync(Func<Task> body, params AView[] views) =>
		InvokeOnMainThreadAsync(async () =>
		{
			var activity = (Activity)AppContext;
			var root = activity.FindViewById<FrameLayout>(Android.Resource.Id.Content)!;
			var container = new FrameLayout(activity)
			{
				LayoutParameters = new FrameLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent)
			};

			foreach (var view in views)
			{
				container.AddView(view, new FrameLayout.LayoutParams(320, 240) { Gravity = Android.Views.GravityFlags.Center });
			}

			root.AddView(container);
			try
			{
				await AssertionExtensions.AssertEventually(() => views[0].IsLaidOut && views[0].Width > 0 && views[0].Height > 0, 5000, 20, "The hosted view was not laid out.");
				await body();
			}
			finally
			{
				root.RemoveView(container);
				container.RemoveAllViews();
			}
		});

	internal sealed class ManagerSession
	{
		public required CameraView View { get; init; }

		public required CameraManager Manager { get; init; }

		public required ICameraProvider Provider { get; init; }

		public PreviewView? PreviewView { get; set; }

		public int LoadedCount { get; set; }

		public T? Native<T>(string field) where T : class => GetPrivateField<T>(Manager, field);

		// Not cached: the manager replaces its use cases whenever it rebuilds them.
		public ImageCapture ImageCapture => Native<ImageCapture>("imageCapture") ?? throw new InvalidOperationException("The manager has no ImageCapture.");

		public Preview CameraPreview => Native<Preview>("cameraPreview") ?? throw new InvalidOperationException("The manager has no Preview.");

		public AndroidX.Camera.Video.VideoCapture VideoCapture => Native<AndroidX.Camera.Video.VideoCapture>("videoCapture") ?? throw new InvalidOperationException("The manager has no VideoCapture.");

		public (int Width, int Height) CaptureResolution => GetNativeResolution(ImageCapture.ResolutionInfo) ?? throw new InvalidOperationException("The ImageCapture is not bound.");

		public (int Width, int Height) PreviewResolution => GetNativeResolution(CameraPreview.ResolutionInfo) ?? throw new InvalidOperationException("The Preview is not bound.");

		// Like CameraViewHandler, this maps the capture resolution first: it creates the ResolutionSelector that the use cases need.
		public async Task ConnectAsync(CancellationToken token = default)
		{
			await Manager.UpdateCaptureResolution(View.ImageCaptureResolution, token);
			await Manager.ConnectCamera(token);
		}
	}

	internal async Task<ManagerSession> CreateSessionAsync(ICameraProvider? provider = null, IMauiContext? mauiContext = null, bool createPlatformView = true, CameraView? view = null, bool track = true)
	{
		usedCameraSession = true;
		provider ??= new CameraProvider();
		view ??= new CameraView();
		var session = (ManagerSession?)null;

		await InvokeOnMainThreadAsync(() =>
		{
			var manager = new CameraManager(mauiContext ?? MauiContext, view, provider, () => session!.LoadedCount++);
			session = new ManagerSession { View = view, Manager = manager, Provider = provider };
			if (track)
			{
				cleanups.Add(() =>
				{
					manager.Dispose();
					if (provider is CameraProvider ownProvider)
					{
						ownProvider.Dispose();
					}
				});
			}

			if (createPlatformView)
			{
				session.PreviewView = manager.CreatePlatformView();
			}
		});

		return session!;
	}

	internal async Task RunWithSessionAsync(Func<ManagerSession, Task> body, ICameraProvider? provider = null, IMauiContext? mauiContext = null, CameraView? view = null)
	{
		var session = await CreateSessionAsync(provider, mauiContext, true, view);
		await HostAsync(() => body(session), session.PreviewView!);
	}

	internal sealed class HostedView
	{
		public required CameraView View { get; init; }

		public required CameraViewHandler Handler { get; init; }

		public required PreviewView PreviewView { get; init; }

		public CameraManager Manager => Handler.CameraManager;

		public T? Native<T>(string field) where T : class => GetPrivateField<T>(Manager, field);

		public string SelectedCameraId => (View.SelectedCamera ?? throw new InvalidOperationException("No camera is selected.")).DeviceId;
	}

	internal async Task RunWithCameraViewAsync(Func<HostedView, Task> body, CameraView? view = null, bool includeRecordAudio = false, bool waitForStreaming = true, bool disconnectWhenDone = true)
	{
		EnsureCameraPermissions(includeRecordAudio);
		view ??= new CameraView();

		await AttachAndRun(view, async handler =>
		{
			var cameraViewHandler = (CameraViewHandler)handler;
			if (disconnectWhenDone)
			{
				RegisterHandlerCleanup(cameraViewHandler);
			}
			else
			{
				MarkCameraSessionUsed();
			}

			var previewView = (PreviewView)handler.PlatformView!;

			if (waitForStreaming)
			{
				await WaitUntilCameraIsStreamingAsync(view, previewView);
			}

			await body(new HostedView { View = view, Handler = cameraViewHandler, PreviewView = previewView });
		});
	}

	// The handler connects the camera in its async void ConnectHandler, which returns no task to await. A handler that is disconnected
	// before the connection has finished ends it without a camera (like a cancelled one), so a test that is not about that waits for the
	// connection first. It takes longest for the first camera of the process (CameraX and its extensions start).
	protected static Task WaitUntilTheHandlerConnectedTheCameraAsync(CameraViewHandler handler) =>
		AssertionExtensions.AssertEventually(() => GetPrivateField<bool>(handler.CameraManager, "isInitialized"), 30000, 50,
			"The handler never finished connecting the camera.");

	// CameraView and CameraViewHandler resolve their ICameraProvider from IPlatformApplication.Current.
	protected void UseCameraProvider(ICameraProvider provider)
	{
		var previous = IPlatformApplication.Current;
		var services = new ServiceCollection().AddSingleton(provider).BuildServiceProvider();
		IPlatformApplication.Current = new StubPlatformApplication(services, previous?.Application ?? throw new InvalidOperationException("The runner has no IPlatformApplication."));
		RegisterCleanup(() => IPlatformApplication.Current = previous);
	}

	internal async Task RunWithStreamingSessionAsync(Func<ManagerSession, Task> body, ICameraProvider? provider = null, CameraView? view = null, bool includeRecordAudio = false)
	{
		EnsureCameraPermissions(includeRecordAudio);

		await RunWithSessionAsync(async session =>
		{
			await session.ConnectAsync().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView!);
			await body(session);
		}, provider, null, view);
	}

	protected static async Task<IReadOnlyList<CameraInfo>> GetRealCamerasAsync()
	{
		using var provider = new CameraProvider();
		await provider.RefreshAvailableCameras(CancellationToken.None).WaitAsync(CameraTimeout);
		return provider.AvailableCameras!;
	}

	protected static async Task AssertEveryCameraIsClosedAsync()
	{
		var provider = await GetNativeCameraProviderAsync();
		foreach (var info in provider.AvailableCameraInfos)
		{
			Assert.Equal("CLOSED", GetNativeCameraState(info));
		}
	}

	protected void MarkCameraSessionUsed() => usedCameraSession = true;

	protected void RegisterCleanup(Action cleanup) => cleanups.Add(cleanup);

	protected void RegisterHandlerCleanup(IElementHandler handler)
	{
		usedCameraSession = true;
		cleanups.Add(() => handler.DisconnectHandler());
	}

	// CameraX keeps its state (ProcessCameraProvider, bound use cases, open cameras) for the whole process, so every camera session that a
	// test opened is closed again here.
	protected override async ValueTask DisposeAsyncCore()
	{
		var failures = new List<Exception>();
		try
		{
			if (cleanups.Count > 0)
			{
				await InvokeOnMainThreadAsync(() =>
				{
					for (var i = cleanups.Count - 1; i >= 0; i--)
					{
						try
						{
							cleanups[i]();
						}
						catch (Exception ex)
						{
							failures.Add(ex);
						}
					}
				});
			}

			if (usedCameraSession)
			{
				try
				{
					await WaitForAllCamerasClosedAsync();
				}
				catch (Exception ex)
				{
					failures.Add(ex);
				}
			}
		}
		finally
		{
			try
			{
				if (asyncVoidGuardInstalled)
				{
					await InvokeOnMainThreadAsync(() => SynchronizationContext.SetSynchronizationContext(mainThreadSynchronizationContext));
				}
			}
			finally
			{
				await base.DisposeAsyncCore();
			}
		}

		failures.AddRange(asyncVoidGuard.Snapshot());
		if (failures.Count > 0)
		{
			throw new AggregateException("The test left failures behind (cleanup, or exceptions that escaped from async void code).", failures);
		}
	}

	sealed class AsyncVoidExceptionGuard : SynchronizationContext
	{
		readonly List<Exception> exceptions = [];

		public SynchronizationContext? Inner { get; set; }

		public IReadOnlyList<Exception> Snapshot()
		{
			lock (exceptions)
			{
				return [.. exceptions];
			}
		}

		public override void Post(SendOrPostCallback d, object? state)
		{
			void Run(object? callbackState)
			{
				try
				{
					d(callbackState);
				}
				catch (Exception ex)
				{
					lock (exceptions)
					{
						exceptions.Add(ex);
					}
				}
			}

			if (Inner is null)
			{
				ThreadPool.QueueUserWorkItem(Run, state);
			}
			else
			{
				Inner.Post(Run, state);
			}
		}

		public override void Send(SendOrPostCallback d, object? state) => d(state);

		public override SynchronizationContext CreateCopy() => this;
	}
}