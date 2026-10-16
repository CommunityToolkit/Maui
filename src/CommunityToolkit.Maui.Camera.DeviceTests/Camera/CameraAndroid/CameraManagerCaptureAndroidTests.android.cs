using System.Collections.Concurrent;
using System.Reflection;
using Android.Graphics;
using Android.Hardware;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using AImage = Android.Media.Image;
using ImageReader = Android.Media.ImageReader;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraManagerAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerCaptureAndroidTests : CameraAndroidTestBase
{
	// The events of the CameraView are raised on CameraX's executor thread, hence the concurrent queues.
	sealed class CaptureRecorder
	{
		readonly ConcurrentQueue<Stream> captured = new();
		readonly ConcurrentQueue<string> failures = new();

		public CaptureRecorder(CameraView view)
		{
			view.MediaCaptured += OnMediaCaptured;
			view.MediaCaptureFailed += OnMediaCaptureFailed;
		}

		public IReadOnlyList<Stream> Captured => [.. captured];

		public IReadOnlyList<string> Failures => [.. failures];

		public Task WaitForCaptureAsync(int count = 1) =>
			AssertionExtensions.AssertEventually(() => captured.Count >= count, 40000, 50, $"The camera did not raise MediaCaptured {count} time(s). Failures: {string.Join("; ", failures)}");

		public Task WaitForFailureAsync() =>
			AssertionExtensions.AssertEventually(() => !failures.IsEmpty, 40000, 50, "The camera did not raise MediaCaptureFailed.");

		void OnMediaCaptured(object? sender, MediaCapturedEventArgs e) => captured.Enqueue(e.Media);

		void OnMediaCaptureFailed(object? sender, MediaCaptureFailedEventArgs e) => failures.Enqueue(e.FailureReason);
	}

	// The tests drive the image callback and the resolution filter of CameraManager directly, the callback with images that Android's
	// ImageReader produces, so that every branch of the callback runs.
	static ImageCapture.OnImageCapturedCallback CreateImageCallback(CameraView view)
	{
		var type = typeof(CameraManager).GetNestedType("ImageCallBack", BindingFlags.NonPublic) ?? throw new MissingMemberException("CameraManager.ImageCallBack");
		return (ImageCapture.OnImageCapturedCallback)Activator.CreateInstance(type, view)!;
	}

	static IResolutionFilter CreateResolutionFilter(int width, int height)
	{
		var type = typeof(CameraManager).GetNestedType("ResolutionFilter", BindingFlags.NonPublic) ?? throw new MissingMemberException("CameraManager.ResolutionFilter");
		return (IResolutionFilter)Activator.CreateInstance(type, new Android.Util.Size(width, height))!;
	}

	static byte[] ReadPlane(AImage image)
	{
		var buffer = image.GetPlanes()![0].Buffer!.Duplicate()!;
		var bytes = new byte[buffer.Remaining()];
		buffer.Get(bytes);
		return bytes;
	}

	[Fact]
	public async Task TakePictureRaisesMediaCapturedWithAJpegOfTheNativeCaptureResolution()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithStreamingSessionAsync(async session =>
		{
			var recorder = new CaptureRecorder(session.View);

			await session.Manager.TakePicture(CancellationToken.None);
			await recorder.WaitForCaptureAsync();

			var stream = Assert.IsType<MemoryStream>(Assert.Single(recorder.Captured));
			Assert.Equal(0, stream.Position);
			var jpeg = await ReadAllBytesAsync(stream);
			Assert.True(IsJpeg(jpeg), "The captured stream is not a JPEG.");
			Assert.Equal(session.CaptureResolution, GetJpegSize(jpeg));
			Assert.Empty(recorder.Failures);
		});
	}

	[Fact]
	public async Task TakePictureHonorsTheCaptureResolution()
	{
		SetupBuilder();
		RequireStillCapture();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.SupportedResolutions.Contains(SmallResolution));
		Skip.If(camera is null, "The camera does not support a 320x240 capture size.");

		await RunWithStreamingSessionAsync(async session =>
		{
			var recorder = new CaptureRecorder(session.View);

			await session.Manager.TakePicture(CancellationToken.None);
			await recorder.WaitForCaptureAsync();

			var jpeg = await ReadAllBytesAsync(Assert.Single(recorder.Captured));
			Assert.Equal(SmallSize, GetJpegSize(jpeg));
		}, view: new CameraView { SelectedCamera = camera, ImageCaptureResolution = SmallResolution });
	}

	[Fact]
	public async Task TakePictureTwiceRaisesTwoSeparateStreams()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithStreamingSessionAsync(async session =>
		{
			var recorder = new CaptureRecorder(session.View);

			await session.Manager.TakePicture(CancellationToken.None);
			await recorder.WaitForCaptureAsync();
			await session.Manager.TakePicture(CancellationToken.None);
			await recorder.WaitForCaptureAsync(2);

			var streams = recorder.Captured;
			Assert.NotSame(streams[0], streams[1]);
			Assert.True(IsJpeg(await ReadAllBytesAsync(streams[0])));
			Assert.True(IsJpeg(await ReadAllBytesAsync(streams[1])));
		});
	}

	[Fact]
	public async Task TakePictureWhileTheCameraIsNotBoundRaisesMediaCaptureFailedWithTheNativeError()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var recorder = new CaptureRecorder(session.View);
			session.Manager.StopCameraPreview();
			await WaitForNativeCameraStateAsync(session.View.SelectedCamera!.DeviceId, "CLOSED");

			await session.Manager.TakePicture(CancellationToken.None);
			await recorder.WaitForFailureAsync();

			Assert.Contains("Not bound to a valid Camera", Assert.Single(recorder.Failures));
			Assert.Empty(recorder.Captured);
		});
	}

	// The failure has to be raised at once: CameraView.CaptureImage waits for MediaCaptured or MediaCaptureFailed, and without either
	// it would wait until its token is cancelled instead of throwing CameraException.
	[Fact]
	public async Task TakePictureBeforeTheCameraIsConnectedRaisesMediaCaptureFailed()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();
		var recorder = new CaptureRecorder(session.View);

		await session.Manager.TakePicture(CancellationToken.None);

		Assert.Null(session.Native<ImageCapture>("imageCapture"));
		Assert.Equal("Unable to capture an image because the camera is not initialized.", Assert.Single(recorder.Failures));
		Assert.Empty(recorder.Captured);
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task TakePictureAfterDisposeThrowsArgumentNullException()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();
		await InvokeOnMainThreadAsync(() => session.Manager.Dispose());

		var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await session.Manager.TakePicture(CancellationToken.None));

		Assert.Equal("cameraExecutor", exception.ParamName);
	}

	[Fact]
	public async Task TakePictureBeforeCreatingThePlatformViewThrowsArgumentNullException()
	{
		SetupBuilder();
		var session = await CreateSessionAsync(createPlatformView: false);

		var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await session.Manager.TakePicture(CancellationToken.None));

		Assert.Equal("cameraExecutor", exception.ParamName);
	}

	[Fact]
	public async Task DisposeWhileACaptureIsPendingReportsTheAbortedCaptureAndDoesNotCrashTheApp()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var recorder = new CaptureRecorder(session.View);

			// CameraX queues the request on the main thread; disposing in the same UI-thread message aborts it before it ran,
			// and CameraX reports the abort to the callback of the manager that is being disposed.
			await session.Manager.TakePicture(CancellationToken.None);
			session.Manager.Dispose();

			await recorder.WaitForFailureAsync();
			Assert.Empty(recorder.Captured);
		});
	}

	[Fact]
	public void ImageCallbackWithoutAnImageProxyRaisesMediaCaptureFailed()
	{
		SetupBuilder();
		var view = new CameraView();
		var recorder = new CaptureRecorder(view);
		var callback = CreateImageCallback(view);

		callback.OnCaptureSuccess(null!);

		Assert.Equal("Unable to obtain Image data.", Assert.Single(recorder.Failures));
		Assert.Empty(recorder.Captured);
	}

	[Fact]
	public void ImageCallbackWithAnImageProxyWithoutAnImageRaisesMediaCaptureFailed()
	{
		SetupBuilder();
		var view = new CameraView();
		var recorder = new CaptureRecorder(view);
		var callback = CreateImageCallback(view);

		callback.OnCaptureSuccess(new FakeImageProxy(null));

		Assert.Equal("Unable to obtain Image data.", Assert.Single(recorder.Failures));
		Assert.Empty(recorder.Captured);
	}

	[Fact]
	public void ImageCallbackClosesAnImageProxyThatHasNoImage()
	{
		SetupBuilder();
		var view = new CameraView();
		var callback = CreateImageCallback(view);
		var proxy = new FakeImageProxy(null);

		callback.OnCaptureSuccess(proxy);

		// CameraX stalls further captures until every ImageProxy it handed out is closed.
		Assert.Equal(1, proxy.CloseCount);
	}

	[Fact]
	public async Task ImageCallbackWithAnImageWithoutPlanesRaisesMediaCaptureFailedAndClosesTheImageProxy()
	{
		SetupBuilder();
		if (!OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			throw new SkipException("Needs Android 10 (API 29): an android.media.Image without planes (ImageFormat.PRIVATE) is produced by an ImageReader with GPU usage flags, which older versions do not offer (ImageWriter can not fill a PRIVATE image on the emulators).");
		}

		var view = new CameraView();
		var recorder = new CaptureRecorder(view);
		var callback = CreateImageCallback(view);

		// A PRIVATE image is filled by a GPU producer, hence the hardware canvas.
		using var reader = ImageReader.NewInstance(64, 64, ImageFormatType.Private, 2, HardwareBuffer.UsageGpuColorOutput | HardwareBuffer.UsageGpuSampledImage);
		var available = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		reader.SetOnImageAvailableListener(new ImageAvailableListener(available), new Android.OS.Handler(Android.OS.Looper.MainLooper!));
		var surface = reader.Surface!;
		var canvas = surface.LockHardwareCanvas()!;
		canvas.DrawColor(Android.Graphics.Color.Red);
		surface.UnlockCanvasAndPost(canvas);
		await available.Task.WaitAsync(CameraTimeout);
		using var image = reader.AcquireNextImage()!;
		Assert.Equal(ImageFormatType.Private, (ImageFormatType)image.Format);
		Assert.Empty(image.GetPlanes() ?? []);
		var proxy = new FakeImageProxy(image);

		callback.OnCaptureSuccess(proxy);

		Assert.Equal("Unable to obtain a buffer for the image plane.", Assert.Single(recorder.Failures));
		Assert.Empty(recorder.Captured);
		Assert.Equal(1, proxy.CloseCount);
	}

	sealed class ImageAvailableListener(TaskCompletionSource available) : Java.Lang.Object, ImageReader.IOnImageAvailableListener
	{
		public void OnImageAvailable(ImageReader? imageReader) => available.TrySetResult();
	}

	static AImage.Plane? InvokeGetFirstPlane(AImage.Plane[]? planes)
	{
		// GetFirstPlane is a static local function of OnCaptureSuccess.
		var type = typeof(CameraManager).GetNestedType("ImageCallBack", BindingFlags.NonPublic)!;
		var method = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(method => method.Name.Contains("GetFirstPlane", StringComparison.Ordinal));
		return (AImage.Plane?)method.Invoke(null, [planes]);
	}

	[Fact]
	public void GetFirstPlaneOfNoPlanesIsNull()
	{
		SetupBuilder();

		Assert.Null(InvokeGetFirstPlane(null));
		Assert.Null(InvokeGetFirstPlane([]));
	}

	[Fact]
	public async Task GetFirstPlaneIsTheFirstOfTheImagePlanes()
	{
		SetupBuilder();
		using var source = ImageSource.Create(Android.Graphics.ImageFormatType.Yuv420888);
		using var image = await source.NextAsync(5);
		var planes = image.GetPlanes()!;
		Assert.True(planes.Length > 1, "A YUV image has several planes.");

		var first = InvokeGetFirstPlane(planes);

		Assert.Same(planes[0], first);
		Assert.NotSame(planes[1], first);
	}

	[Fact]
	public async Task ImageCallbackCopiesTheFirstPlaneIntoAMemoryStreamAndClosesTheImageProxy()
	{
		SetupBuilder();
		var view = new CameraView();
		var recorder = new CaptureRecorder(view);
		var callback = CreateImageCallback(view);
		using var source = ImageSource.Create(Android.Graphics.ImageFormatType.Yuv420888);
		using var image = await source.NextAsync(index => (sbyte)(index * 7 + 3));
		var expected = ReadPlane(image);
		Assert.True(expected.Length > 0);
		Assert.Contains(expected, value => value != expected[0]);
		var proxy = new FakeImageProxy(image);

		callback.OnCaptureSuccess(proxy);

		var stream = Assert.IsType<MemoryStream>(Assert.Single(recorder.Captured));
		Assert.Equal(0, stream.Position);
		Assert.Equal(expected, await ReadAllBytesAsync(stream));
		Assert.Empty(recorder.Failures);
		Assert.Equal(1, proxy.CloseCount);
	}

	[Fact]
	public async Task ImageCallbackReportsTheFailureRethrowsAndClosesTheImageProxyWhenASubscriberThrows()
	{
		SetupBuilder();
		var view = new CameraView();
		var recorder = new CaptureRecorder(view);
		var callback = CreateImageCallback(view);
		view.MediaCaptured += ThrowingSubscriber;
		using var source = ImageSource.Create(Android.Graphics.ImageFormatType.Yuv420888);
		using var image = await source.NextAsync(1);
		var proxy = new FakeImageProxy(image);

		var exception = Record.Exception(() => callback.OnCaptureSuccess(proxy));

		Assert.NotNull(exception);
		Assert.Equal(exception.Message, Assert.Single(recorder.Failures));
		Assert.Equal(1, proxy.CloseCount);
		Assert.Single(recorder.Captured);

		static void ThrowingSubscriber(object? sender, MediaCapturedEventArgs e) => throw new InvalidOperationException("The subscriber failed.");
	}

	[Fact]
	public void ImageCallbackOnErrorRaisesMediaCaptureFailedWithTheMessageOfTheException()
	{
		SetupBuilder();
		var view = new CameraView();
		var recorder = new CaptureRecorder(view);
		var callback = CreateImageCallback(view);

		callback.OnError(new ImageCaptureException(ImageCapture.ErrorCaptureFailed, "The sensor failed.", null!));

		Assert.Equal("The sensor failed.", Assert.Single(recorder.Failures));
		Assert.Empty(recorder.Captured);
	}

	[Fact]
	public void ImageCallbackOnErrorWithoutAMessageRaisesMediaCaptureFailedWithTheDefaultMessage()
	{
		SetupBuilder();
		var view = new CameraView();
		var recorder = new CaptureRecorder(view);
		var callback = CreateImageCallback(view);

		callback.OnError(new ImageCaptureException(ImageCapture.ErrorUnknown, null!, null!));

		Assert.Equal("An unknown error occurred.", Assert.Single(recorder.Failures));
	}

	[Fact]
	public void ImageCallbackOnErrorWithoutAnExceptionRaisesMediaCaptureFailedWithTheDefaultMessage()
	{
		SetupBuilder();
		var view = new CameraView();
		var recorder = new CaptureRecorder(view);
		var callback = CreateImageCallback(view);

		callback.OnError(null!);

		Assert.Equal("An unknown error occurred.", Assert.Single(recorder.Failures));
	}

	static List<(int Width, int Height)> Dimensions(IEnumerable<Android.Util.Size> sizes) => sizes.Select(size => (size.Width, size.Height)).ToList();

	static List<Android.Util.Size> Sizes(params (int Width, int Height)[] sizes) => sizes.Select(size => new Android.Util.Size(size.Width, size.Height)).ToList();

	[Fact]
	public void ResolutionFilterKeepsTheSizesThatFitAndSortsThemByAreaDescending()
	{
		var filter = CreateResolutionFilter(1280, 720);
		var supported = Sizes((1920, 1080), (320, 240), (1280, 720), (640, 480));

		var filtered = filter.Filter(supported, 0);

		Assert.Equal(new[] { (1280, 720), (640, 480), (320, 240) }, Dimensions(filtered));
	}

	[Fact]
	public void ResolutionFilterDropsASizeThatIsTooWideAndASizeThatIsTooHigh()
	{
		var filter = CreateResolutionFilter(800, 600);
		var supported = Sizes((1000, 100), (640, 1000), (640, 480));

		var filtered = filter.Filter(supported, 0);

		Assert.Equal(new[] { (640, 480) }, Dimensions(filtered));
	}

	[Fact]
	public void ResolutionFilterKeepsASizeThatEqualsTheTarget()
	{
		var filter = CreateResolutionFilter(640, 480);

		var filtered = filter.Filter(Sizes((640, 480), (641, 480), (640, 481)), 0);

		Assert.Equal(new[] { (640, 480) }, Dimensions(filtered));
	}

	[Fact]
	public void ResolutionFilterKeepsTheOrderOfSizesWithTheSameArea()
	{
		var filter = CreateResolutionFilter(1000, 1000);

		var filtered = filter.Filter(Sizes((400, 300), (300, 400), (100, 100)), 0);

		Assert.Equal(new[] { (400, 300), (300, 400), (100, 100) }, Dimensions(filtered));
	}

	[Fact]
	public void ResolutionFilterReturnsEverySupportedSizeWhenNoneFits()
	{
		var filter = CreateResolutionFilter(320, 240);
		var supported = Sizes((1920, 1080), (1280, 720));

		var filtered = filter.Filter(supported, 0);

		Assert.Same(supported, filtered);
	}

	[Fact]
	public void ResolutionFilterReturnsTheEmptyListThatItWasGiven()
	{
		var filter = CreateResolutionFilter(320, 240);
		var supported = Sizes();

		var filtered = filter.Filter(supported, 0);

		Assert.Same(supported, filtered);
		Assert.Empty(filtered);
	}

	[Fact]
	public void ResolutionFilterReturnsAnEmptyListWhenThereAreNoSupportedSizes()
	{
		var filter = CreateResolutionFilter(320, 240);

		var filtered = filter.Filter(null!, 0);

		Assert.NotNull(filtered);
		Assert.Empty(filtered);
	}

	[Fact]
	public void ResolutionFilterIgnoresTheRotation()
	{
		var filter = CreateResolutionFilter(640, 480);

		Assert.Equal(new[] { (640, 480) }, Dimensions(filter.Filter(Sizes((1280, 960), (640, 480)), 90)));
	}
}