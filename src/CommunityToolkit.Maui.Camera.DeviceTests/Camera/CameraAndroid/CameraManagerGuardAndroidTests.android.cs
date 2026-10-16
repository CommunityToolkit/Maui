using System.Reflection;
using AndroidX.Camera.Core;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// CameraManager on Android: the defensive guards of its private helpers that no public call reaches, because every caller checks
/// the same preconditions first (white box: the helpers are called through reflection, on a real manager and the real camera).
/// The public paths through the same helpers are tested in the other CameraManager test classes.
/// </summary>
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

	// ---- IsPhotoSessionBound / IsVideoSessionBound ------------------------------------------------------------------------------

	[Fact]
	public async Task SessionIsNotBoundBeforeTheManagerIsConnected()
	{
		SetupBuilder();
		var session = await CreateSessionAsync();

		// No ProcessCameraProvider yet.
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

	// ---- BindPhotoSessionAsync / BindVideoSessionAsync ------------------------------------------------------------------------

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

			// Nothing was bound and the manager kept its camera.
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview));
			Assert.False(await IsNativelyBoundAsync(session.ImageCapture));
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture));
			Assert.Same(camera, session.Native<ICamera>("camera"));
		});
	}

	// ---- RebuildCameraPreview / BindCamera --------------------------------------------------------------------------------------
	// ConnectCamera runs them in its CameraX listener and fails with their exceptions (asserted by
	// CameraManagerConnectionAndroidTests.ConnectCameraWithoutAPreconditionOfTheUseCasesFailsWithItsExceptionAndLetsNothingEscape); their
	// guards are called directly here.

	[Fact]
	public async Task RebuildCameraPreviewWithoutAnExecutorThrowsArgumentNullException()
	{
		SetupBuilder();

		// CreatePlatformView, which creates the executor, was never called; UpdateCaptureResolution created the ResolutionSelector.
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

		// UpdateCaptureResolution, which creates the ResolutionSelector, was never called; CreatePlatformView created the executor.
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

	// ---- RebuildImageCapture / PlatformTakePicture ----------------------------------------------------------------------------

	[Fact]
	public async Task RebuildImageCaptureWithoutAResolutionSelectorThrowsArgumentNullException()
	{
		SetupBuilder();

		// UpdateCaptureResolution was never called, so there is no ResolutionSelector.
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