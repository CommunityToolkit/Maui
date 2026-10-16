using System.Reflection;
using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
using CoreMotion;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// Where the tests can run:
//  - iOS simulator: no capture device at all (AVCaptureDevice.GetDefaultDevice(Video) is null), so every camera-bound test skips.
//  - Mac Catalyst on a Mac with a camera: devices are discovered and can be configured (zoom, active format) without permission,
//    but opening a capture input needs the user's consent (a prompt that a headless run cannot answer), so those tests skip
//    unless the permission was already granted.
//  - CI machines without a camera: only the camera-less tests run.
static class AppleCameraEnvironment
{
	// Every process of an iOS simulator has the SIMULATOR_UDID variable; Mac Catalyst has no simulator.
	public static bool IsSimulator => Environment.GetEnvironmentVariable("SIMULATOR_UDID") is not null;

	public static AVAuthorizationStatus CameraAuthorizationStatus => AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);

	public static bool HasVideoCaptureDevice => AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video) is not null;

	public static bool IsCameraAuthorized => CameraAuthorizationStatus is AVAuthorizationStatus.Authorized;

	// CameraManager observes UIDevice.OrientationDidChangeNotification to update the video orientation.
	public static void PostOrientationDidChangeNotification() =>
		NSNotificationCenter.DefaultCenter.PostNotificationName(UIDevice.OrientationDidChangeNotification, null);

	public static string DescribeCameraAccess() =>
		$"camera authorization is {CameraAuthorizationStatus}, video capture device present: {HasVideoCaptureDevice}, simulator: {IsSimulator}";

	static readonly TimeSpan captureSessionStartTimeout = TimeSpan.FromSeconds(5);

	static readonly Lazy<Task<string?>> captureSessionStartProblem = new(ProbeCaptureSessionStartAsync);

	// For the tests that start the manager's real AVCaptureSession on the UI thread. Measured with sessions that an AVCaptureVideoPreviewLayer
	// shows, as CameraManager.CreatePlatformView builds them (an empty session does not start at all): on the iOS 27 simulator StartRunning
	// and StopRunning each take about 9 s, on either thread, so such a test blocks the UI thread for about 27 s; on a Mac with a camera they
	// take milliseconds. On a Mac without a camera, such as a CI virtual machine, they were never tried, and a hang there would block the UI
	// thread and the whole test run until its timeout, so one probe session on a background thread decides, within a bounded time, whether
	// these tests run. A probe that never starts is never touched again.
	public static async Task SkipUnlessCaptureSessionsStartPromptlyAsync()
	{
		Skip.If(IsSimulator, "On the iOS simulator AVCaptureSession.StartRunning and StopRunning of a session shown by a preview layer each take about 9 s (measured on iOS 27), which would block the UI thread for about 27 s per test.");

		var problem = await captureSessionStartProblem.Value;

		Skip.If(problem is not null, problem ?? string.Empty);
	}

	static async Task<string?> ProbeCaptureSessionStartAsync()
	{
		var (session, previewLayer) = await TestDispatcher.Current.DispatchAsync(() =>
		{
			var session = new AVCaptureSession();

			return (session, new AVCaptureVideoPreviewLayer(session));
		});
		var start = Task.Run(session.StartRunning);

		try
		{
			await start.WaitAsync(captureSessionStartTimeout);
		}
		catch (TimeoutException)
		{
			_ = start.ContinueWith(_ => Stop(session, previewLayer), CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

			return $"AVCaptureSession.StartRunning of a probe session shown by a preview layer did not return within {captureSessionStartTimeout.TotalSeconds} s on this machine, so the manager's session cannot be started promptly here ({DescribeCameraAccess()}).";
		}

		_ = Task.Run(() => Stop(session, previewLayer));

		return null;

		static void Stop(AVCaptureSession session, AVCaptureVideoPreviewLayer previewLayer)
		{
			session.StopRunning();
			GC.KeepAlive(previewLayer);
		}
	}

	public static async Task<IReadOnlyList<CameraInfo>> DiscoverCamerasAsync()
	{
		var provider = new CameraProvider();

		try
		{
			await provider.RefreshAvailableCameras(CancellationToken.None);

			return provider.AvailableCameras ?? [];
		}
		finally
		{
			provider.Dispose();
		}
	}
}

// Reads and writes the private state of CameraManager (macios) so that branches that a real capture session would reach
// (initialized preview, recording in progress) can be exercised without a camera or the camera permission.
sealed class CameraManagerAccessor(CameraManager manager)
{
	const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
	const BindingFlags staticFlags = BindingFlags.Static | BindingFlags.NonPublic;

	public bool IsInitialized
	{
		get => Get<bool>("isInitialized");
		set => Set("isInitialized", value);
	}

	public AVCaptureSession? CaptureSession
	{
		get => Get<AVCaptureSession?>("captureSession");
		set => Set("captureSession", value);
	}

	public AVCaptureDevice? CaptureDevice
	{
		get => Get<AVCaptureDevice?>("captureDevice");
		set => Set("captureDevice", value);
	}

	public AVCaptureDeviceInput? CaptureInput
	{
		get => Get<AVCaptureDeviceInput?>("captureInput");
		set => Set("captureInput", value);
	}

	public AVCaptureDeviceInput? AudioInput
	{
		get => Get<AVCaptureDeviceInput?>("audioInput");
		set => Set("audioInput", value);
	}

	public AVCapturePhotoOutput? PhotoOutput
	{
		get => Get<AVCapturePhotoOutput?>("photoOutput");
		set => Set("photoOutput", value);
	}

	public AVCaptureMovieFileOutput? VideoOutput
	{
		get => Get<AVCaptureMovieFileOutput?>("videoOutput");
		set => Set("videoOutput", value);
	}

	public AVCaptureFlashMode FlashMode => Get<AVCaptureFlashMode>("flashMode");

	public AVCaptureVideoOrientation VideoOrientation
	{
		get => Get<AVCaptureVideoOrientation>("videoOrientation");
		set => Set("videoOrientation", value);
	}

	public string? VideoRecordingFileName
	{
		get => Get<string?>("videoRecordingFileName");
		set => Set("videoRecordingFileName", value);
	}

	public Stream? VideoRecordingStream
	{
		get => Get<Stream?>("videoRecordingStream");
		set => Set("videoRecordingStream", value);
	}

	public TaskCompletionSource? VideoRecordingFinalizeTcs
	{
		get => Get<TaskCompletionSource?>("videoRecordingFinalizeTcs");
		set => Set("videoRecordingFinalizeTcs", value);
	}

	public object? VideoRecordingDelegate => Get<object?>("videoRecordingDelegate");

	public AVCaptureDeviceRotationCoordinator? RotationCoordinator
	{
		get => Get<AVCaptureDeviceRotationCoordinator?>("rotationCoordinator");
		set => Set("rotationCoordinator", value);
	}

	public CMMotionManager? MotionManager
	{
		get => Get<CMMotionManager?>("motionManager");
		set => Set("motionManager", value);
	}

	public Action OnLoaded => Get<Action>("onLoaded");

	public object? OrientationDidChangeObserver => Get<object?>("orientationDidChangeObserver");

	public UIView? PreviewView
	{
		get => Get<UIView?>("previewView");
		set => Set("previewView", value);
	}

	public void UpdateVideoOrientation() => GetInstanceMethod("UpdateVideoOrientation").Invoke(manager, null);

	public bool TryConfigureAVCaptureConnection(AVCaptureOutput output, out string? errorMessage)
	{
		object?[] arguments = [output, null];
		var result = (bool)GetInstanceMethod("TryConfigureAVCaptureConnection").Invoke(manager, arguments)!;
		errorMessage = (string?)arguments[1];
		return result;
	}

	public static AVCaptureVideoOrientation GetVideoOrientation() =>
		(AVCaptureVideoOrientation)GetStaticMethod("GetVideoOrientation").Invoke(null, null)!;

	public static AVCaptureVideoOrientation GetVideoOrientationFromAccelerometer(double x, double y) =>
		(AVCaptureVideoOrientation)GetStaticMethod("GetVideoOrientationFromAccelerometer").Invoke(null, [x, y])!;

	public static bool MatchesResolution(AVCaptureDeviceFormat format, Size resolution) =>
		(bool)GetStaticMethod("MatchesResolution").Invoke(null, [format, resolution])!;

	public static AVCapturePhotoCaptureDelegate CreatePhotoCaptureDelegateWrapper()
	{
		var wrapperType = typeof(CameraManager).GetNestedType("AVCapturePhotoCaptureDelegateWrapper", BindingFlags.NonPublic)
			?? throw new MissingMemberException(nameof(CameraManager), "AVCapturePhotoCaptureDelegateWrapper");

		return (AVCapturePhotoCaptureDelegate)Activator.CreateInstance(wrapperType, nonPublic: true)!;
	}

	public static Task<(AVCapturePhotoOutput? Output, AVCapturePhoto? Photo, NSError? Error)> GetPhotoCaptureResultAsync(AVCapturePhotoCaptureDelegate wrapper)
	{
		var task = (Task)wrapper.GetType().GetProperty("Task", BindingFlags.Public | BindingFlags.Instance)!.GetValue(wrapper)!;

		return Await();

		async Task<(AVCapturePhotoOutput? Output, AVCapturePhoto? Photo, NSError? Error)> Await()
		{
			await task;

			var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
			var resultType = result.GetType();

			return ((AVCapturePhotoOutput?)resultType.GetProperty("Output")!.GetValue(result),
				(AVCapturePhoto?)resultType.GetProperty("Photo")!.GetValue(result),
				(NSError?)resultType.GetProperty("Error")!.GetValue(result));
		}
	}

	static MethodInfo GetInstanceMethod(string name) =>
		typeof(CameraManager).GetMethod(name, instanceFlags) ?? throw new MissingMethodException(nameof(CameraManager), name);

	static MethodInfo GetStaticMethod(string name) =>
		typeof(CameraManager).GetMethod(name, staticFlags) ?? throw new MissingMethodException(nameof(CameraManager), name);

	static FieldInfo GetField(string name) =>
		typeof(CameraManager).GetField(name, instanceFlags) ?? throw new MissingFieldException(nameof(CameraManager), name);

	T Get<T>(string name) => (T)GetField(name).GetValue(manager)!;

	void Set(string name, object? value) => GetField(name).SetValue(manager, value);
}

static class CameraProviderAccessor
{
	public static AVCaptureDeviceType[] CaptureDevices
	{
		get
		{
			var captureDevicesField = typeof(CameraProvider).GetField("captureDevices", BindingFlags.Static | BindingFlags.NonPublic)
				?? throw new MissingFieldException(nameof(CameraProvider), "captureDevices");

			return (AVCaptureDeviceType[])captureDevicesField.GetValue(null)!;
		}
	}

	public static AVCaptureDeviceType[] InitializeCaptureDevices()
	{
		var method = typeof(CameraProvider).GetMethod("InitializeCaptureDevices", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new MissingMethodException(nameof(CameraProvider), "InitializeCaptureDevices");

		return (AVCaptureDeviceType[])method.Invoke(null, null)!;
	}

	// Runs the type initializer now, so that it caches CaptureDevices for the real OS version and never inside an OS version override.
	public static void EnsureTypeInitialized() =>
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(CameraProvider).TypeHandle);
}

static class AppleFakeCameras
{
	public static CameraInfo Create(FakeCaptureDevice device, CameraPosition position = CameraPosition.Rear, params AVCaptureDeviceFormat[] formats) =>
		new(device.Name, device.Identifier, position, device.FlashIsAvailable, (float)device.MinimumZoomFactor, (float)device.MaximumZoomFactor,
			[.. formats.Select(format => format is FakeCaptureDeviceFormat fake ? new Size(fake.Width, fake.Height) : Size.Zero)], device, formats);

	// PlatformStartCameraPreview fails on this camera with "No Camera found".
	public static CameraInfo WithoutCaptureDevice(string deviceId = "fake-no-device", CameraPosition position = CameraPosition.Rear) =>
		new("Camera without capture device", deviceId, position, false, 1f, 4f, [new Size(640, 480)], null!, []);
}

// CameraProvider raises AvailableCamerasChanged through a WeakEventManager, so the recorder must stay referenced by the test.
sealed class AppleCameraListRecorder
{
	public AppleCameraListRecorder(ICameraProvider provider)
	{
		provider.AvailableCamerasChanged += OnAvailableCamerasChanged;
	}

	public List<IReadOnlyList<CameraInfo>?> Lists { get; } = [];

	void OnAvailableCamerasChanged(object? sender, IReadOnlyList<CameraInfo>? cameras) => Lists.Add(cameras);
}

sealed class AppleCameraManagerScope : IDisposable
{
	public AppleCameraManagerScope(IMauiContext mauiContext, ICameraProvider? provider = null)
	{
		View = new CameraView();
		Provider = provider ?? new AppleFakeCameraProvider();
		Manager = new CameraManager(mauiContext, View, Provider, () => LoadedCount++);
		Access = new CameraManagerAccessor(Manager);
	}

	public CameraView View { get; }

	public ICameraProvider Provider { get; }

	public CameraManager Manager { get; }

	public CameraManagerAccessor Access { get; }

	public int LoadedCount { get; private set; }

	public UIView? PlatformView { get; set; }

	public void Dispose()
	{
		Manager.Dispose();
		View.Dispose();
	}
}

sealed class AppleFakeCameraProvider : ICameraProvider
{
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; set; }

	public IReadOnlyList<CameraInfo> CamerasAfterRefresh { get; set; } = [];

	public int RefreshCount { get; private set; }

	public event EventHandler<IReadOnlyList<CameraInfo>?>? AvailableCamerasChanged;

	public Task RefreshAvailableCameras(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		RefreshCount++;
		AvailableCameras = CamerasAfterRefresh;
		AvailableCamerasChanged?.Invoke(this, AvailableCameras);

		return Task.CompletedTask;
	}

	public void Dispose()
	{
	}
}

// The real CameraViewHandler connects the camera from an `async void` ConnectHandler, which no test can await, and with a camera it opens
// a capture session and shows the system permission prompt, which a headless run cannot answer. Skipping the automatic connection lets the
// tests start and stop the preview explicitly.
sealed class AppleNoConnectCameraViewHandler : CameraViewHandler
{
	protected override void ConnectHandler(UIView platformView)
	{
	}
}

// Calls the protected members of CameraViewHandler that the framework never calls in an unusual state: CreatePlatformView without a
// MauiContext and Dispose(false). Like AppleNoConnectCameraViewHandler it does not connect the camera.
sealed class AppleCreateViewCameraViewHandler : CameraViewHandler
{
	public UIView CallCreatePlatformView() => CreatePlatformView();

	public void CallDispose(bool disposing) => Dispose(disposing);

	protected override void ConnectHandler(UIView platformView)
	{
	}
}

// Like AppleNoConnectCameraViewHandler it does not connect the camera.
sealed class AppleMapperCameraViewHandler : CameraViewHandler
{
	public AppleMapperCameraViewHandler()
	{
	}

	public AppleMapperCameraViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper) : base(mapper, commandMapper)
	{
	}

	protected override void ConnectHandler(UIView platformView)
	{
	}
}

// AvailableCameras stays null after a refresh, to reach the "still no camera after refreshing" checks.
sealed class AppleNeverPublishingCameraProvider : ICameraProvider
{
	public IReadOnlyList<CameraInfo>? AvailableCameras => null;

	public int RefreshCount { get; private set; }

	public event EventHandler<IReadOnlyList<CameraInfo>?>? AvailableCamerasChanged
	{
		add { }
		remove { }
	}

	public Task RefreshAvailableCameras(CancellationToken token)
	{
		RefreshCount++;
		return Task.CompletedTask;
	}

	public void Dispose()
	{
	}
}

// CameraViewHandler and CameraView resolve ICameraProvider from IPlatformApplication.Current.Services (the runner's app), so a test connects
// the real handler to a scripted provider by overriding that lookup. Keep it short: the runner reads IPlatformApplication.Current too.
sealed class AppleCameraProviderOverride : IDisposable
{
	readonly IPlatformApplication? previous = IPlatformApplication.Current;

	AppleCameraProviderOverride(ICameraProvider? provider, bool keepPlatformApplication)
	{
		IPlatformApplication.Current = !keepPlatformApplication || previous is null
			? null
			: new PlatformApplicationDecorator(previous, new ServiceProviderDecorator(previous.Services, provider));
	}

	public static AppleCameraProviderOverride With(ICameraProvider provider) => new(provider, keepPlatformApplication: true);

	public static AppleCameraProviderOverride WithoutCameraProvider() => new(null, keepPlatformApplication: true);

	public static AppleCameraProviderOverride WithoutPlatformApplication() => new(null, keepPlatformApplication: false);

	public void Dispose() => IPlatformApplication.Current = previous;

	sealed class PlatformApplicationDecorator(IPlatformApplication inner, IServiceProvider services) : IPlatformApplication
	{
		public IServiceProvider Services => services;

		public IApplication Application => inner.Application;
	}

	sealed class ServiceProviderDecorator(IServiceProvider inner, ICameraProvider? provider) : IServiceProvider
	{
		public object? GetService(Type serviceType) => serviceType == typeof(ICameraProvider) ? provider : inner.GetService(serviceType);
	}
}

// An `async void` method (CameraViewHandler.ConnectHandler, MapSelectedCamera, MapImageCaptureResolution) delivers an exception that
// escapes it to the SynchronizationContext that was current when it started; in an app that is the UI context, which rethrows it and
// terminates the app. With this context installed, a test sees the exception as a posted callback instead.
sealed class AppleRecordingSynchronizationContext : SynchronizationContext
{
	readonly List<(SendOrPostCallback Callback, object? State)> posted = [];

	public override void Post(SendOrPostCallback d, object? state)
	{
		lock (posted)
		{
			posted.Add((d, state));
		}
	}

	public override void Send(SendOrPostCallback d, object? state) => d(state);

	public override SynchronizationContext CreateCopy() => this;

	public IDisposable Install()
	{
		var previous = Current;
		SetSynchronizationContext(this);

		return new Restore(previous);
	}

	public IReadOnlyList<Exception> Drain()
	{
		var exceptions = new List<Exception>();
		using var installed = Install();

		while (true)
		{
			(SendOrPostCallback Callback, object? State)[] batch;
			lock (posted)
			{
				batch = [.. posted];
				posted.Clear();
			}

			if (batch.Length is 0)
			{
				return exceptions;
			}

			foreach (var (callback, state) in batch)
			{
				try
				{
					callback(state);
				}
				catch (Exception exception)
				{
					exceptions.Add(exception);
				}
			}
		}
	}

	sealed class Restore(SynchronizationContext? previous) : IDisposable
	{
		public void Dispose() => SetSynchronizationContext(previous);
	}
}

// PlatformStopVideoRecording only rewinds seekable streams.
sealed class NonSeekableWriteStream : Stream
{
	readonly MemoryStream inner = new();

	public override bool CanRead => false;

	public override bool CanSeek => false;

	public override bool CanWrite => true;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public byte[] WrittenBytes => inner.ToArray();

	public override void Flush() => inner.Flush();

	public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			inner.Dispose();
		}

		base.Dispose(disposing);
	}
}

// Lets a test dispose the camera manager while PlatformStopVideoRecording copies the movie: the manager's Dispose disposes this stream,
// so the write then throws ObjectDisposedException.
sealed class StreamThatRunsAnActionOnTheFirstWrite(Action action) : MemoryStream
{
	public bool ActionRan { get; private set; }

	public override void Write(byte[] buffer, int offset, int count)
	{
		RunActionOnce();
		base.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		RunActionOnce();
		base.Write(buffer);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		RunActionOnce();
		return base.WriteAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		RunActionOnce();
		return base.WriteAsync(buffer, cancellationToken);
	}

	void RunActionOnce()
	{
		if (!ActionRan)
		{
			ActionRan = true;
			action();
		}
	}
}

// The CameraView raises its media events through a WeakEventManager, so the subscriber must stay referenced by the test.
sealed class AppleMediaRecorder
{
	public AppleMediaRecorder(CameraView view)
	{
		view.MediaCaptured += OnMediaCaptured;
		view.MediaCaptureFailed += OnMediaCaptureFailed;
	}

	public List<Stream> Captured { get; } = [];

	public List<string> Failures { get; } = [];

	public TaskCompletionSource<Stream> FirstCapture { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

	void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
	{
		Captured.Add(e.Media);
		FirstCapture.TrySetResult(e.Media);
	}

	void OnMediaCaptureFailed(object? sender, MediaCaptureFailedEventArgs e)
	{
		Failures.Add(e.FailureReason);
		FirstCapture.TrySetException(new CameraException(e.FailureReason));
	}
}