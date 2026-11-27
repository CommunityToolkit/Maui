using AndroidX.Camera.Core;
using AndroidX.Lifecycle;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraClosedCheckAndroid)]
[Collection(TestCollections.PermanentAndroidGrants)]
public class CameraClosedCheckAndroidTests : CameraAndroidTestBase
{
	// Reproduces CI run 44721184121: a deferred camera switch bound camera 1 while camera 0 was closing and the dispose unbound it a few
	// milliseconds later, all before the main thread applied the OPENING that CameraX posted for camera 1.
	[Fact]
	public async Task EveryCameraIsClosedAfterCameraXDroppedTheClosedPublicationOfACameraThatWasBoundAndUnboundInOneMessage()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		MarkCameraSessionUsed();
		await WaitForAllCamerasClosedAsync();
		LastDroppedClosedPublication = null;

		var provider = await GetNativeCameraProviderAsync();
		RegisterCleanup(() => provider.UnbindAll());
		var backCameraId = FindCameraId(provider, CameraSelector.DefaultBackCamera!);
		var frontCameraId = FindCameraId(provider, CameraSelector.DefaultFrontCamera!);
		Skip.If(backCameraId is null || frontCameraId is null || backCameraId == frontCameraId, "The test needs a back and a front camera.");

		var lifecycleOwner = (ILifecycleOwner)AppContext;
		using var backCapture = new ImageCapture.Builder().Build()!;
		await InvokeOnMainThreadAsync(() => provider.BindToLifecycle(lifecycleOwner, CameraSelector.DefaultBackCamera!, backCapture));
		await WaitForNativeCameraStateAsync(backCameraId!, "OPEN");

		try
		{
			using var frontCapture = new ImageCapture.Builder().Build()!;
			await InvokeOnMainThreadAsync(() =>
			{
				provider.UnbindAll();
				provider.BindToLifecycle(lifecycleOwner, CameraSelector.DefaultFrontCamera!, frontCapture);
				provider.UnbindAll();
				Thread.Sleep(1000);
			});
			await Task.Delay(1000);

			// PENDING_OPEN when the front camera's open was attempted before the back camera counted as closing, OPENING otherwise.
			var frontCameraXState = await GetNativeCameraStateAsync(frontCameraId!);
			Assert.True(frontCameraXState is "OPENING" or "PENDING_OPEN",
				$"Precondition: CameraX still reports a camera that is bound and unbound in one message as OPENING or PENDING_OPEN (it reports {frontCameraXState}). It does not any more, so CameraX no longer drops the CLOSED publication and the camera-service based closed check can be replaced by CameraX's state again, for example after a CameraX 1.6 bump.");

			await WaitForAllCamerasClosedAsync();
			Assert.NotNull(LastDroppedClosedPublication);
			Assert.Matches($"{frontCameraId}=(OPENING|PENDING_OPEN)", LastDroppedClosedPublication);
		}
		finally
		{
			await ReopenAndCloseCameraAsync(provider, lifecycleOwner, frontCameraId!);
		}
	}

	// A camera that opens and closes for real makes CameraX publish CLOSED again, so later tests do not read the stale OPENING.
	static async Task ReopenAndCloseCameraAsync(AndroidX.Camera.Lifecycle.ProcessCameraProvider provider, ILifecycleOwner lifecycleOwner, string cameraId)
	{
		using var capture = new ImageCapture.Builder().Build()!;
		await InvokeOnMainThreadAsync(() => provider.BindToLifecycle(lifecycleOwner, CameraSelector.DefaultFrontCamera!, capture));
		await WaitForNativeCameraStateAsync(cameraId, "OPEN");
		await InvokeOnMainThreadAsync(() => provider.UnbindAll());
		await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
	}

	static string? FindCameraId(AndroidX.Camera.Lifecycle.ProcessCameraProvider provider, CameraSelector selector)
	{
		var matching = selector.Filter(provider.AvailableCameraInfos)!;
		return matching.Count == 0 ? null : GetNativeCameraId(matching[0]);
	}
}