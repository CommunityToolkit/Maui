using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.Storage.Streams;

namespace CommunityToolkit.Maui.DeviceTests;

static class WindowsCameraEnvironment
{
	public static async Task<IReadOnlyList<DeviceInformation>> FindVideoCaptureDevicesAsync()
	{
		var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

		return [.. devices];
	}

	public static async Task<bool> HasVideoCaptureDeviceAsync() => (await FindVideoCaptureDevicesAsync()).Count > 0;

	// A packaged app may open a camera only once a person allowed it: the first time it tries, Windows shows a consent prompt ("Let ... access
	// your camera?") and MediaCapture.InitializeAsync waits for the answer, which a run that nobody watches never gives (the toolkit's camera
	// discovery opens every camera). Such a run skips the tests that open a real camera. An unpackaged app follows the "Let desktop apps
	// access your camera" setting instead, and a machine without a camera opens none.
	public static async Task SkipUnlessCamerasCanBeOpenedAsync()
	{
		if (AppInfo.PackagingModel is not AppPackagingModel.Packaged || !await HasVideoCaptureDeviceAsync())
		{
			return;
		}

		var access = AppCapability.Create("webcam").CheckAccess();
		Skip.If(access is not AppCapabilityAccessStatus.Allowed, $"Camera access of this packaged app is {access}: Windows asks a person the first time the app opens a camera (or Settings > Privacy & security > Camera), which nobody does in a headless run.");
	}

	// Uses the toolkit's own CameraProvider (CameraProvider.windows.cs), so tests use exactly the cameras the toolkit sees.
	public static async Task<IReadOnlyList<CameraInfo>> DiscoverCamerasAsync()
	{
		await SkipUnlessCamerasCanBeOpenedAsync();
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

	public static async Task<IReadOnlyList<MediaFrameSourceGroup>> FindVideoCaptureSourceGroupsAsync()
	{
		var devices = await FindVideoCaptureDevicesAsync();
		var sourceGroups = await MediaFrameSourceGroup.FindAllAsync();

		return [.. sourceGroups.Where(sourceGroup => devices.Any(device => device.Id == sourceGroup.Id))];
	}

	public static async Task<string> DescribeCameraAccessAsync() =>
		$"video capture devices: {(await FindVideoCaptureDevicesAsync()).Count}, packaging: {AppInfo.PackagingModel}";

	// Media Foundation is an optional feature of Windows Server images (CI runners): without it the WinRT media classes (MediaCapture,
	// MediaSource, MediaFrameSourceGroup, ImageEncodingProperties) cannot be activated ("class not registered" or a missing DLL). The tests
	// that need such an object skip visibly instead of failing on those machines. The guards below wrap only the tests' own calls, never a
	// call of the toolkit: a toolkit call that fails after a guard passed still fails its test.
	public static MediaCapture CreateMediaCapture()
	{
		try
		{
			return new MediaCapture();
		}
		catch (Exception exception) when (IsMediaFoundationUnavailable(exception))
		{
			throw MediaFoundationUnavailable("MediaCapture cannot be created", exception);
		}
	}

	// The tests that refresh a real CameraProvider make the same call as CameraProvider.windows.cs (listing the frame source groups) first, so
	// that a machine without Media Foundation skips them visibly; the provider's own call then runs unguarded.
	public static async Task SkipUnlessFrameSourceGroupsCanBeListedAsync()
	{
		try
		{
			await MediaFrameSourceGroup.FindAllAsync();
		}
		catch (Exception exception) when (IsMediaFoundationUnavailable(exception))
		{
			throw MediaFoundationUnavailable("The frame source groups cannot be listed", exception);
		}
	}

	// StopPreviewAsync is the probe: it fails with RO_E_CLOSED (ObjectDisposedException) once the capture is closed, and with
	// MF_E_NOT_INITIALIZED (0xC00D36B6) on an open capture that was never initialized, and it opens no camera. The properties of the capture
	// are no witness: CameraStreamState, for one, still answers after Close (Windows 11 26200).
	public static async Task<bool> IsClosedAsync(MediaCapture mediaCapture)
	{
		try
		{
			await mediaCapture.StopPreviewAsync();
			return false;
		}
		catch (ObjectDisposedException)
		{
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static ImageEncodingProperties CreateJpegProperties(uint width, uint height)
	{
		ImageEncodingProperties properties;

		try
		{
			properties = ImageEncodingProperties.CreateJpeg();
		}
		catch (Exception exception) when (IsMediaFoundationUnavailable(exception))
		{
			throw MediaFoundationUnavailable("ImageEncodingProperties cannot be created", exception);
		}

		properties.Width = width;
		properties.Height = height;

		return properties;
	}

	public static void AssignPlaceholderSource(MediaPlayerElement mediaElement)
	{
		try
		{
			mediaElement.Source = MediaSource.CreateFromStream(new InMemoryRandomAccessStream(), "video/mp4");
		}
		catch (Exception exception) when (IsMediaFoundationUnavailable(exception))
		{
			throw MediaFoundationUnavailable("A MediaSource cannot be assigned", exception);
		}
	}

	static bool IsMediaFoundationUnavailable(Exception exception) =>
		exception is COMException or TypeLoadException or FileNotFoundException or InvalidOperationException or PlatformNotSupportedException;

	static SkipException MediaFoundationUnavailable(string what, Exception exception) =>
		new($"{what} on this machine ({exception.GetType().Name}: {exception.Message}); Media Foundation is probably not installed.");
}

// Reads and writes the private state of CameraManager (windows) so that guards that a started MediaCapture would pass can be
// exercised without a camera.
sealed class WindowsCameraManagerAccessor(CameraManager manager)
{
	const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

	public bool IsInitialized
	{
		get => Get<bool>("isInitialized");
		set => Set("isInitialized", value);
	}

	public MediaPlayerElement? MediaElement
	{
		get => Get<MediaPlayerElement?>("mediaElement");
		set => Set("mediaElement", value);
	}

	public LowLagMediaRecording? MediaRecording => Get<LowLagMediaRecording?>("mediaRecording");

	public MediaCapture? MediaCapture
	{
		get => Get<MediaCapture?>("mediaCapture");
		set => Set("mediaCapture", value);
	}

	public Stream? VideoCaptureStream
	{
		get => Get<Stream?>("videoCaptureStream");
		set => Set("videoCaptureStream", value);
	}

	public Action OnLoaded => Get<Action>("onLoaded");

	static FieldInfo GetField(string name) =>
		typeof(CameraManager).GetField(name, instanceFlags) ?? throw new MissingFieldException(nameof(CameraManager), name);

	T Get<T>(string name) => (T)GetField(name).GetValue(manager)!;

	void Set(string name, object? value) => GetField(name).SetValue(manager, value);
}

sealed class WindowsCameraManagerScope : IDisposable
{
	public WindowsCameraManagerScope(IMauiContext mauiContext, ICameraProvider? provider = null)
	{
		View = new CameraView();
		Provider = provider ?? new WindowsFakeCameraProvider();
		Manager = new CameraManager(mauiContext, View, Provider, () => LoadedCount++);
		Access = new WindowsCameraManagerAccessor(Manager);
	}

	public CameraView View { get; }

	public ICameraProvider Provider { get; }

	public CameraManager Manager { get; }

	public WindowsCameraManagerAccessor Access { get; }

	public int LoadedCount { get; private set; }

	public void Dispose()
	{
		Manager.Dispose();
		View.Dispose();
	}
}

sealed class WindowsFakeCameraProvider : ICameraProvider
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

// The real CameraViewHandler connects the camera from an `async void` ConnectHandler, which no test can await, and with a camera it opens the
// device (and may show the Windows camera consent prompt). Skipping that connection lets the tests start and stop the preview explicitly.
sealed class WindowsNoConnectCameraViewHandler : CameraViewHandler
{
	protected override void ConnectHandler(Microsoft.UI.Xaml.FrameworkElement platformView)
	{
	}
}

// Calls the protected members of CameraViewHandler that the framework never calls in an unusual state: CreatePlatformView without a
// MauiContext and Dispose(false). Like WindowsNoConnectCameraViewHandler it does not connect the camera.
sealed class WindowsCreateViewCameraViewHandler : CameraViewHandler
{
	public Microsoft.UI.Xaml.FrameworkElement CallCreatePlatformView() => CreatePlatformView();

	public void CallDispose(bool disposing) => Dispose(disposing);

	protected override void ConnectHandler(Microsoft.UI.Xaml.FrameworkElement platformView)
	{
	}
}

// Like WindowsNoConnectCameraViewHandler it does not connect the camera.
sealed class WindowsMapperCameraViewHandler : CameraViewHandler
{
	public WindowsMapperCameraViewHandler()
	{
	}

	public WindowsMapperCameraViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper) : base(mapper, commandMapper)
	{
	}

	protected override void ConnectHandler(Microsoft.UI.Xaml.FrameworkElement platformView)
	{
	}
}

// CameraViewHandler and CameraView resolve ICameraProvider from IPlatformApplication.Current.Services (the runner's app). This override
// makes them resolve the given provider until it is disposed, so a test can connect the real handler to a scripted provider. Keep it short:
// the runner reads IPlatformApplication.Current too.
sealed class WindowsCameraProviderOverride : IDisposable
{
	readonly IPlatformApplication? previous = IPlatformApplication.Current;

	public WindowsCameraProviderOverride(ICameraProvider provider) : this(provider, keepPlatformApplication: true)
	{
	}

	WindowsCameraProviderOverride(ICameraProvider? provider, bool keepPlatformApplication)
	{
		IPlatformApplication.Current = !keepPlatformApplication || previous is null
			? null
			: new PlatformApplicationDecorator(previous, new ServiceProviderDecorator(previous.Services, provider));
	}

	public static WindowsCameraProviderOverride WithoutCameraProvider() => new(null, keepPlatformApplication: true);

	public static WindowsCameraProviderOverride WithoutPlatformApplication() => new(null, keepPlatformApplication: false);

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

// An exception that escapes an `async void` method (such as CameraViewHandler.ConnectHandler) is delivered to the SynchronizationContext that
// was current when the method started, which in an app rethrows it and terminates the app. With this context installed, a test sees it as a
// posted callback instead.
sealed class WindowsRecordingSynchronizationContext : SynchronizationContext
{
	readonly List<(SendOrPostCallback Callback, object? State)> posted = [];
	TaskCompletionSource postedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
	int runningAsyncVoidMethods;

	public override void Post(SendOrPostCallback d, object? state)
	{
		TaskCompletionSource signal;
		lock (posted)
		{
			posted.Add((d, state));
			signal = postedSignal;
		}

		signal.TrySetResult();
	}

	// AsyncVoidMethodBuilder reports to the context that is current when an async void method starts, and again when the method ends
	// (after posting the exception that escaped it, if any).
	public override void OperationStarted() => Interlocked.Increment(ref runningAsyncVoidMethods);

	public override void OperationCompleted() => Interlocked.Decrement(ref runningAsyncVoidMethods);

	// Like Drain, for async void methods that await WinRT operations: the continuations after such an await are posted here when the
	// operation completes, on a thread pool thread.
	public async Task<IReadOnlyList<Exception>> DrainUntilAsyncVoidMethodsEndAsync(TimeSpan timeout)
	{
		var exceptions = new List<Exception>();
		var elapsed = System.Diagnostics.Stopwatch.StartNew();

		while (true)
		{
			exceptions.AddRange(Drain());

			if (Volatile.Read(ref runningAsyncVoidMethods) <= 0)
			{
				exceptions.AddRange(Drain());

				return exceptions;
			}

			Task nextPost;
			lock (posted)
			{
				if (posted.Count > 0)
				{
					continue;
				}

				if (postedSignal.Task.IsCompleted)
				{
					postedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
				}

				nextPost = postedSignal.Task;
			}

			var remaining = timeout - elapsed.Elapsed;

			try
			{
				await nextPost.WaitAsync(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
			}
			catch (TimeoutException)
			{
				throw new TimeoutException($"{Volatile.Read(ref runningAsyncVoidMethods)} async void method(s) did not end within {timeout}.");
			}
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

// The CameraView raises its media events through a WeakEventManager, so the subscriber must stay referenced by the test.
sealed class WindowsMediaRecorder
{
	public WindowsMediaRecorder(CameraView view)
	{
		view.MediaCaptured += OnMediaCaptured;
		view.MediaCaptureFailed += OnMediaCaptureFailed;
	}

	public List<Stream> Captured { get; } = [];

	public List<string> Failures { get; } = [];

	public TaskCompletionSource<Stream> FirstCapture { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

	void OnMediaCaptured(object? sender, CommunityToolkit.Maui.Core.MediaCapturedEventArgs e)
	{
		Captured.Add(e.Media);
		FirstCapture.TrySetResult(e.Media);
	}

	void OnMediaCaptureFailed(object? sender, CommunityToolkit.Maui.Core.MediaCaptureFailedEventArgs e)
	{
		Failures.Add(e.FailureReason);
		FirstCapture.TrySetException(new CameraException(e.FailureReason));
	}
}