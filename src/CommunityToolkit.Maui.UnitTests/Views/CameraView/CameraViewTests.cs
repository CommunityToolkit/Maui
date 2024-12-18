using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class CameraViewTests : BaseHandlerTest
{
	readonly CameraView cameraView = new();
	readonly MockCameraProvider mockCameraProvider;

	public CameraViewTests()
	{
		Assert.IsType<ICameraView>(cameraView, exactMatch: false);
		mockCameraProvider = (MockCameraProvider)ServiceProvider.GetRequiredService<ICameraProvider>();
	}

	[Fact]
	public void VerifyDefaults()
	{
		Assert.Equal(CameraViewDefaults.IsAvailable, cameraView.IsAvailable);
		Assert.Equal(CameraViewDefaults.IsTorchOn, cameraView.IsTorchOn);
		Assert.Equal(CameraViewDefaults.IsCameraBusy, cameraView.IsCameraBusy);
		Assert.Equal(CameraViewDefaults.ZoomFactor, cameraView.ZoomFactor);
		Assert.Equal(CameraViewDefaults.ImageCaptureResolution, cameraView.ImageCaptureResolution);
		Assert.Equal(CameraViewDefaults.CameraFlashMode, cameraView.CameraFlashMode);
		Assert.Null(mockCameraProvider.AvailableCameras);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ZoomFactor_WithSelectedCamera_IsCoercedToRange()
	{
		if (mockCameraProvider.AvailableCameras is null)
		{
			await mockCameraProvider.RefreshAvailableCameras(CancellationToken.None);

			if (mockCameraProvider.AvailableCameras is null)
			{
				throw new InvalidOperationException();
			}
		}

		cameraView.SelectedCamera = mockCameraProvider.AvailableCameras[0];
		cameraView.ZoomFactor = 0.5f; // Below minimum
		Assert.Equal(1.0f, cameraView.ZoomFactor);

		cameraView.ZoomFactor = 6.0f; // Above maximum
		Assert.Equal(5.0f, cameraView.ZoomFactor);
	}

	[Fact]
	public void OnMediaCaptured_RaisesMediaCapturedEvent()
	{
		bool eventRaised = false;
		cameraView.MediaCaptured += (sender, args) => eventRaised = true;

		var imageData = new MemoryStream();
		cameraView.OnMediaCaptured(imageData);

		Assert.True(eventRaised);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task OnMediaCapturedFailed_RaisesMediaCaptureFailedEvent()
	{
		const string failureMessage = "Proof that this test passes";

		bool wasEventRaised = false;
		var mediaCaptureFailedTcs = new TaskCompletionSource<MediaCaptureFailedEventArgs>();
		cameraView.MediaCaptureFailed += HandleMediaCaptureFailed;

		cameraView.OnMediaCapturedFailed(failureMessage);

		var mediaCaptureFailedEventArgs = await mediaCaptureFailedTcs.Task;

		Assert.True(wasEventRaised);
		Assert.Equal(failureMessage, mediaCaptureFailedEventArgs.FailureReason);

		void HandleMediaCaptureFailed(object? sender, MediaCaptureFailedEventArgs e)
		{
			cameraView.MediaCaptureFailed -= HandleMediaCaptureFailed;
			wasEventRaised = true;
			mediaCaptureFailedTcs.SetResult(e);
		}
	}
}