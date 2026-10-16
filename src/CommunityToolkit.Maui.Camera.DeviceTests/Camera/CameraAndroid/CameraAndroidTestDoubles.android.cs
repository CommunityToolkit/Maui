using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using AndroidX.Camera.Core;
using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Core;
using AImage = Android.Media.Image;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;
using ICameraProvider = CommunityToolkit.Maui.Core.ICameraProvider;

namespace CommunityToolkit.Maui.DeviceTests;

sealed class StubCameraProvider : ICameraProvider
{
	public IReadOnlyList<CameraInfo>? AvailableCameras { get; set; }

	public IReadOnlyList<CameraInfo>? CamerasAfterRefresh { get; set; }

	public int RefreshCount { get; private set; }

	public event EventHandler<IReadOnlyList<CameraInfo>?>? AvailableCamerasChanged;

	public Task RefreshAvailableCameras(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		RefreshCount++;

		if (CamerasAfterRefresh is not null)
		{
			AvailableCameras = CamerasAfterRefresh;
			AvailableCamerasChanged?.Invoke(this, AvailableCameras);
		}

		return Task.CompletedTask;
	}

	public void Dispose()
	{
	}
}

// Implements ILifecycleOwner like the runner's activity, so that CameraX can still bind to it.
sealed class ConfigurableContext : ContextWrapper, ILifecycleOwner
{
	readonly ILifecycleOwner lifecycleOwner;

	public ConfigurableContext(Context baseContext) : base(baseContext)
	{
		lifecycleOwner = (ILifecycleOwner)baseContext;
	}

	public HashSet<string> DeniedPermissions { get; } = [];

	public bool HidePackageManager { get; set; }

	public Java.IO.File? CacheDirectoryOverride { get; set; }

	// Lets code that keeps getApplicationContext() of the Context it was given (CameraX's PendingRecording does, through
	// ContextUtil.getApplicationContext) still see the denied permissions.
	public bool IsItsOwnApplicationContext { get; set; }

	public Lifecycle Lifecycle => lifecycleOwner.Lifecycle;

	public override Context? ApplicationContext => IsItsOwnApplicationContext ? this : base.ApplicationContext;

	public override PackageManager? PackageManager => HidePackageManager ? null : base.PackageManager;

	public override Java.IO.File? CacheDir => CacheDirectoryOverride ?? base.CacheDir;

	public override Permission CheckPermission(string? permission, int pid, int uid) =>
		permission is not null && DeniedPermissions.Contains(permission) ? Permission.Denied : base.CheckPermission(permission, pid, uid);

	public override Permission CheckSelfPermission(string? permission) =>
		permission is not null && DeniedPermissions.Contains(permission) ? Permission.Denied : base.CheckSelfPermission(permission);
}

sealed class TestMauiContext(IMauiContext inner, Context? context) : IMauiContext
{
	public IServiceProvider Services => inner.Services;

	public IMauiHandlersFactory Handlers => inner.Handlers;

	public Context? Context => context;
}

sealed class StubPlatformApplication(IServiceProvider services, IApplication application) : IPlatformApplication
{
	public IServiceProvider Services => services;

	public IApplication Application => application;
}

sealed class FakeImageProxy(AImage? image) : Java.Lang.Object, IImageProxy
{
	public int CloseCount { get; private set; }

	public Android.Graphics.Rect CropRect { get; set; } = new();

	public int Format => (int)ImageFormatType.Jpeg;

	public int Height => image?.Height ?? 0;

	public AImage? Image => image;

	public IImageInfo ImageInfo => null!;

	public int Width => image?.Width ?? 0;

	public void Close() => CloseCount++;

	public IImageProxyPlaneProxy[] GetPlanes() => [];
}

// android.media.Image can not be subclassed, so real images are produced through an ImageReader/ImageWriter pair.
sealed class ImageSource : IDisposable
{
	readonly ImageReader reader;
	readonly ImageWriter writer;
	readonly TaskCompletionSource available = new(TaskCreationOptions.RunContinuationsAsynchronously);

	ImageSource(ImageFormatType format, int size)
	{
		reader = ImageReader.NewInstance(size, size, format, 2)!;
		reader.SetOnImageAvailableListener(new Listener(available), new Android.OS.Handler(Android.OS.Looper.MainLooper!));
		writer = ImageWriter.NewInstance(reader.Surface!, 2)!;
	}

	public static ImageSource Create(ImageFormatType format, int size = 8) => new(format, size);

	public Task<AImage> NextAsync(sbyte fill) => NextAsync(_ => fill);

	public async Task<AImage> NextAsync(Func<int, sbyte> pattern)
	{
		var input = writer.DequeueInputImage()!;
		var planes = input.GetPlanes();
		if (planes is { Length: > 0 })
		{
			var buffer = planes[0].Buffer!;
			for (var i = 0; i < buffer.Capacity(); i++)
			{
				buffer.Put(i, pattern(i));
			}
		}

		writer.QueueInputImage(input);
		await available.Task.WaitAsync(TimeSpan.FromSeconds(10));
		return reader.AcquireNextImage()!;
	}

	public void Dispose()
	{
		writer.Close();
		reader.Close();
	}

	sealed class Listener(TaskCompletionSource available) : Java.Lang.Object, ImageReader.IOnImageAvailableListener
	{
		public void OnImageAvailable(ImageReader? imageReader) => available.TrySetResult();
	}
}

sealed class LiveDataRecorder : Java.Lang.Object, IObserver
{
	readonly List<string> values = [];
	readonly LiveData liveData;

	public LiveDataRecorder(LiveData liveData)
	{
		this.liveData = liveData;
		liveData.ObserveForever(this);
	}

	public IReadOnlyList<string> Values
	{
		get
		{
			lock (values)
			{
				return [.. values];
			}
		}
	}

	public void OnChanged(Java.Lang.Object? value)
	{
		var text = value is CameraState state ? state.GetType()?.ToString() ?? "UNKNOWN" : value?.ToString() ?? "null";
		lock (values)
		{
			values.Add(text);
		}
	}

	public void Clear()
	{
		lock (values)
		{
			values.Clear();
		}
	}

	public void Stop() => liveData.RemoveObserver(this);
}

sealed class NonSeekableStream(System.IO.Stream inner) : System.IO.Stream
{
	public System.IO.Stream Inner => inner;

	public override bool CanRead => inner.CanRead;

	public override bool CanSeek => false;

	public override bool CanWrite => inner.CanWrite;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public override void Flush() => inner.Flush();

	public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

	public override long Seek(long offset, System.IO.SeekOrigin origin) => throw new NotSupportedException();

	public override void SetLength(long value) => throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
}