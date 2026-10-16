using System.Reflection;
using AndroidX.Camera.Core;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

// No public call reaches most of these guards, because every caller checks the same preconditions first, so the tests call the private
// helpers of CameraManager through reflection.
[Category(TestCategory.CameraManagerAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerGuardAndroidTests : CameraAndroidTestBase
{
	static object? InvokePrivate(CameraManager manager, string method, params object[] arguments)
	{
		var info = typeof(CameraManager).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new MissingMethodException(nameof(CameraManager), method);
		try
		{
			return info.Invoke(manager, arguments);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is not null)
		{
			throw ex.InnerException;
		}
	}

	static Task InvokePrivateAsync(CameraManager manager, string method) =>
		(Task)InvokePrivate(manager, method, CancellationToken.None)!;

	[Fact]
	public async Task SessionIsNotBoundBeforeTheManagerIsConnected()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();

		var (photo, video) = await InvokeOnMainThreadAsync(() => ((bool)InvokePrivate(session.Manager, "IsPhotoSessionBound")!, (bool)InvokePrivate(session.Manager, "IsVideoSessionBound")!));

		Assert.False(photo);
		Assert.False(video);
	}

	[Theory]
	[InlineData("cameraPreview", "IsPhotoSessionBound")]
	[InlineData("cameraPreview", "IsVideoSessionBound")]
	[InlineData("imageCapture", "IsPhotoSessionBound")]
	[InlineData("imageCapture", "IsVideoSessionBound")]
	[InlineData("videoCapture", "IsVideoSessionBound")]
	public async Task SessionIsNotBoundWithoutThePreviewOrItsUseCase(string missingField, string check)
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			if (check is "IsVideoSessionBound")
			{
				// Bind the video session natively, so that only the missing object can make the check false.
				await InvokePrivateAsync(session.Manager, "BindVideoSessionAsync").WaitAsync(CameraTimeout);
				Assert.True(await IsNativelyBoundAsync(session.VideoCapture));
			}

			Assert.True((bool)InvokePrivate(session.Manager, check)!);
			var original = GetPrivateField<object>(session.Manager, missingField);
			SetPrivateField(session.Manager, missingField, null);
			try
			{
				Assert.False((bool)InvokePrivate(session.Manager, check)!);
			}
			finally
			{
				SetPrivateField(session.Manager, missingField, original);
			}
		});
	}

	[Theory]
	[InlineData("BindPhotoSessionAsync", "processCameraProvider")]
	[InlineData("BindPhotoSessionAsync", "SelectedCamera")]
	[InlineData("BindPhotoSessionAsync", "cameraPreview")]
	[InlineData("BindVideoSessionAsync", "processCameraProvider")]
	[InlineData("BindVideoSessionAsync", "cameraPreview")]
	[InlineData("BindVideoSessionAsync", "SelectedCamera")]
	public async Task BindingASessionDoesNothingWithoutAPrecondition(string bind, string missing)
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			var camera = session.Native<ICamera>("camera");
			var selectedCamera = session.View.SelectedCamera;
			var original = missing is "SelectedCamera" ? null : GetPrivateField<object>(session.Manager, missing);
			await UnbindAllNativeAsync();

			if (missing is "SelectedCamera")
			{
				session.View.SelectedCamera = null;
			}
			else
			{
				SetPrivateField(session.Manager, missing, null);
			}

			try
			{
				await InvokePrivateAsync(session.Manager, bind).WaitAsync(CameraTimeout);
			}
			finally
			{
				if (missing is "SelectedCamera")
				{
					session.View.SelectedCamera = selectedCamera;
				}
				else
				{
					SetPrivateField(session.Manager, missing, original);
				}
			}

			Assert.False(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture));
			Assert.Same(camera, session.Native<ICamera>("camera"));
		});
	}

	// ConnectCamera runs RebuildCameraPreview and BindCamera in its CameraX listener and reports their exceptions through ErrorOccurred
	// (asserted by CameraManagerConnectionAndroidTests.ConnectCameraWithoutAPreconditionOfTheUseCasesReportsItsExceptionAndLetsNothingEscape);
	// their guards are called directly here.

	[Fact]
	public async Task RebuildCameraPreviewWithoutAnExecutorThrowsArgumentNullException()
	{
		SetupBuilder();

		// Without CreatePlatformView there is no executor; UpdateCaptureResolution creates the ResolutionSelector.
		var session = await CreateSessionAsync(createPlatformView: false);
		await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<ArgumentNullException>(() => InvokePrivate(session.Manager, "RebuildCameraPreview")));

		Assert.Equal("cameraExecutor", exception.ParamName);
		Assert.Null(session.Native<Preview>("cameraPreview"));
	}

	[Fact]
	public async Task RebuildCameraPreviewWithoutAResolutionSelectorThrowsArgumentNullException()
	{
		SetupBuilder();

		// Without UpdateCaptureResolution there is no ResolutionSelector; CreatePlatformView creates the executor.
		var session = await CreateSessionAsync();

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<ArgumentNullException>(() => InvokePrivate(session.Manager, "RebuildCameraPreview")));

		Assert.Equal("resolutionSelector", exception.ParamName);
		Assert.Null(session.Native<Preview>("cameraPreview"));
	}

	[Fact]
	public async Task BindCameraWithAContextThatIsNotALifecycleOwnerThrowsInvalidCastException()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var camera = (await GetRealCamerasAsync())[0];

		// CameraX binds the camera to a LifecycleOwner and the manager casts its Context to one: an application Context is none.
		var session = await CreateSessionAsync(mauiContext: new TestMauiContext(MauiContext, Android.App.Application.Context));

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = await GetNativeCameraProviderAsync();

			Assert.Throws<InvalidCastException>(() => InvokePrivate(session.Manager, "BindCamera", provider, camera.CameraSelector!, Array.Empty<UseCase>()));
		});

		Assert.Null(session.Native<ICamera>("camera"));
		await AssertEveryCameraIsClosedAsync();
	}

	[Fact]
	public async Task RebuildImageCaptureWithoutAResolutionSelectorThrowsArgumentNullException()
	{
		SetupBuilder();

		var session = await CreateSessionAsync();

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<ArgumentNullException>(() => InvokePrivate(session.Manager, "RebuildImageCapture")));

		Assert.Equal("resolutionSelector", exception.ParamName);
		Assert.Null(session.Native<ImageCapture>("imageCapture"));
	}

	[Fact]
	public async Task TakePictureWithoutAnImageCallbackThrowsArgumentNullException()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();
		var callback = GetPrivateField<object>(session.Manager, "imageCallback");
		SetPrivateField(session.Manager, "imageCallback", null);

		try
		{
			// The executor exists (CreatePlatformView ran), so the second guard is the one that throws.
			var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await session.Manager.TakePicture(CancellationToken.None));

			Assert.Equal("imageCallback", exception.ParamName);
		}
		finally
		{
			SetPrivateField(session.Manager, "imageCallback", callback);
		}
	}
}