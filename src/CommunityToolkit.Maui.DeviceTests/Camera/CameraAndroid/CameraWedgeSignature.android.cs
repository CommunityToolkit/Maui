using System.Globalization;
using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests;

static class CameraWedgeSignature
{
	internal static readonly TimeSpan ConfirmationDelay = TimeSpan.FromSeconds(5);

	internal const string StuckCloseClass = "android.hardware.camera2.impl.CameraDeviceImpl";
	internal const string StuckCloseMethod = "close";

	// Healthy closes took at most 1.5 s in 12,600 closes on API 32 and 33, so a thread in the same close five seconds later is stuck.
	internal static string? FindStuckClose(IReadOnlyList<JavaThreadStack> first, IReadOnlyList<JavaThreadStack> second, TimeSpan apart)
	{
		var insideCloseInFirstSample = first.Where(IsInsideCameraDeviceClose).Select(thread => thread.ThreadId).ToHashSet();
		var stuck = second.FirstOrDefault(thread => insideCloseInFirstSample.Contains(thread.ThreadId) && IsInsideCameraDeviceClose(thread));
		if (stuck is null)
		{
			return null;
		}

		var topFrame = stuck.Frames.Count > 0 ? stuck.Frames[0].Text : "no frame";
		return string.Create(CultureInfo.InvariantCulture,
			$"Thread {stuck.ThreadId} \"{stuck.Name}\" is still inside CameraDeviceImpl.close {apart.TotalSeconds:0} s later (top frame {topFrame}): a CameraDevice.close() that never returns, the camera hang of the Android 12 and 13 emulators.");
	}

	internal static string? FindLostCameras(int cameraXCamerasFirst, int cameraXCamerasSecond, IReadOnlyList<string>? platformCameraIds, TimeSpan apart)
	{
		if (cameraXCamerasFirst != 0 || cameraXCamerasSecond != 0 || platformCameraIds is null || platformCameraIds.Count == 0)
		{
			return null;
		}

		return string.Create(CultureInfo.InvariantCulture,
			$"CameraX lists no camera in two samples {apart.TotalSeconds:0} s apart while Camera2 lists {string.Join(", ", platformCameraIds)}: CameraX lost its cameras for this process, as CameraX 1.5.3 does when its camera-presence update fails after a cameraserver restart (logcat: \"CameraPresencePrvdr: A core module failed to update\").");
	}

	static bool IsInsideCameraDeviceClose(JavaThreadStack thread) =>
		thread.Frames.Any(frame => string.Equals(frame.ClassName, StuckCloseClass, StringComparison.Ordinal) && string.Equals(frame.MethodName, StuckCloseMethod, StringComparison.Ordinal));
}
