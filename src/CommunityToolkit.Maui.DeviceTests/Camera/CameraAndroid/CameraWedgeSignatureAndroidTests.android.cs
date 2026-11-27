using System.Globalization;
using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraWedgeSignatureAndroid)]
public class CameraWedgeSignatureAndroidTests
{
	static readonly TimeSpan apart = TimeSpan.FromSeconds(5);

	static JavaStackFrame Frame(string className, string methodName) => new(className, methodName, $"{className}.{methodName}(Unknown Source)");

	static JavaThreadStack ThreadStack(long id, string name, params JavaStackFrame[] frames) => new(id, name, "RUNNABLE", frames);

	static JavaThreadStack ThreadInsideClose(long id = 42, string name = "CameraX-core_camera_0") => ThreadStack(id, name,
		Frame("android.os.BinderProxy", "transactNative"),
		Frame("android.os.BinderProxy", "transact"),
		Frame("android.hardware.camera2.ICameraDeviceUser$Stub$Proxy", "disconnect"),
		Frame("android.hardware.camera2.impl.ICameraDeviceUserWrapper", "disconnect"),
		Frame("android.hardware.camera2.impl.CameraDeviceImpl", "close"),
		Frame("androidx.camera.camera2.internal.Camera2CameraImpl", "closeCamera"));

	static JavaThreadStack ThreadWithoutCameraFrames(long id = 42, string name = "CameraX-core_camera_0") => ThreadStack(id, name,
		Frame("java.lang.Object", "wait"),
		Frame("android.os.MessageQueue", "nativePollOnce"));

	[Fact]
	public void ASameThreadInsideCameraDeviceCloseInBothSamplesIsAStuckClose()
	{
		var result = CameraWedgeSignature.FindStuckClose([ThreadInsideClose()], [ThreadInsideClose()], apart);

		Assert.NotNull(result);
	}

	[Fact]
	public void ACloseThatReturnedBeforeTheSecondSampleIsNotAStuckClose()
	{
		var result = CameraWedgeSignature.FindStuckClose([ThreadInsideClose()], [ThreadWithoutCameraFrames()], apart);

		Assert.Null(result);
	}

	[Fact]
	public void TwoDifferentThreadsInCameraDeviceCloseAreNotAStuckClose()
	{
		var result = CameraWedgeSignature.FindStuckClose([ThreadInsideClose(id: 42)], [ThreadInsideClose(id: 43)], apart);

		Assert.Null(result);
	}

	[Fact]
	public void AnotherMethodOfCameraDeviceImplOrOnlyCameraXCloseFramesAreNotAStuckClose()
	{
		var otherMethod = ThreadStack(42, "CameraX-core_camera_0", Frame(CameraWedgeSignature.StuckCloseClass, "configureStreamsChecked"));
		var onlyCameraXClose = ThreadStack(42, "CameraX-core_camera_0", Frame("androidx.camera.camera2.internal.Camera2CameraImpl", "closeCamera"));

		Assert.Null(CameraWedgeSignature.FindStuckClose([otherMethod], [otherMethod], apart));
		Assert.Null(CameraWedgeSignature.FindStuckClose([onlyCameraXClose], [onlyCameraXClose], apart));
	}

	[Fact]
	public void StacksWithoutCameraFramesAreNotAStuckClose()
	{
		var result = CameraWedgeSignature.FindStuckClose([ThreadWithoutCameraFrames()], [ThreadWithoutCameraFrames()], apart);

		Assert.Null(result);
	}

	[Fact]
	public void TheStuckCloseNamesTheThreadItsTopFrameAndTheDelay()
	{
		var result = CameraWedgeSignature.FindStuckClose([ThreadInsideClose()], [ThreadInsideClose()], apart);

		Assert.NotNull(result);
		Assert.Contains("Thread 42 \"CameraX-core_camera_0\"", result, StringComparison.Ordinal);
		Assert.Contains("5 s later", result, StringComparison.Ordinal);
		Assert.Contains("android.os.BinderProxy.transactNative(Unknown Source)", result, StringComparison.Ordinal);
	}

	[Fact]
	public void CameraXWithoutCamerasInBothSamplesWhileCamera2ListsCamerasHasLostThem()
	{
		var result = CameraWedgeSignature.FindLostCameras(0, 0, ["0", "1"], apart);

		Assert.NotNull(result);
		Assert.Contains("Camera2 lists 0, 1", result, StringComparison.Ordinal);
	}

	[Fact]
	public void CameraXThatListsCamerasAgainInTheSecondSampleHasNotLostThem()
	{
		Assert.Null(CameraWedgeSignature.FindLostCameras(0, 2, ["0", "1"], apart));
	}

	[Fact]
	public void ADeviceWithoutCamerasOrAnUnknownCamera2ListHasNotLostThem()
	{
		Assert.Null(CameraWedgeSignature.FindLostCameras(0, 0, [], apart));
		Assert.Null(CameraWedgeSignature.FindLostCameras(0, 0, null, apart));
	}

	[Fact]
	public void CameraXWithCamerasHasNotLostThem()
	{
		Assert.Null(CameraWedgeSignature.FindLostCameras(2, 0, ["0", "1"], apart));
	}

	[Fact]
	public void TheSignaturesMatchAndFormatInvariantlyUnderTurkishCulture()
	{
		var culture = CultureInfo.CurrentCulture;
		var uiCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");

			var stuck = CameraWedgeSignature.FindStuckClose([ThreadInsideClose()], [ThreadInsideClose()], TimeSpan.FromSeconds(5));
			var lost = CameraWedgeSignature.FindLostCameras(0, 0, ["0"], TimeSpan.FromSeconds(5));

			Assert.NotNull(stuck);
			Assert.Contains("Thread 42 ", stuck, StringComparison.Ordinal);
			Assert.Contains("5 s later", stuck, StringComparison.Ordinal);
			Assert.NotNull(lost);
			Assert.Contains("5 s apart", lost, StringComparison.Ordinal);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
		}
	}

	[Fact]
	public void CaptureListsTheMainThreadWithItsJavaFrames()
	{
		var threads = JavaThreadDump.Capture();

		var main = Assert.Single(threads, thread => string.Equals(thread.Name, "main", StringComparison.Ordinal));
		Assert.NotEmpty(main.Frames);
		Assert.All(main.Frames, frame =>
		{
			Assert.NotEmpty(frame.ClassName);
			Assert.NotEmpty(frame.MethodName);
		});
	}
}