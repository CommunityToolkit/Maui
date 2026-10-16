using AVFoundation;
using CommunityToolkit.Maui.Core;
using CoreMedia;
using Foundation;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraManager.macios.cs members that configure a real AVCaptureDevice: zoom, active format (capture resolution) and torch.
// Configuring a device does not need the camera permission (opening a capture input does), so these run on any Mac with a camera.
// They skip on the iOS simulator and on machines without a camera. The device state is restored after every test.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerAppleDeviceTests : CameraHandlerTestBase
{
	[Fact]
	public async Task UpdateZoomAppliesAFactorInsideTheAvailableRange()
	{
		await RunWithInitializedManagerAsync((scope, device) =>
		{
			Skip.If((float)device.MaxAvailableVideoZoomFactor < 2f, "The camera's maximum zoom factor is below 2.");

			scope.Manager.UpdateZoom(2f);

			Assert.Equal(2f, (float)device.VideoZoomFactor, precision: 3);
		});
	}

	[Fact]
	public async Task UpdateZoomAcceptsTheMinimumAndMaximumAvailableFactors()
	{
		await RunWithInitializedManagerAsync((scope, device) =>
		{
			var minimum = (float)device.MinAvailableVideoZoomFactor;
			var maximum = (float)device.MaxAvailableVideoZoomFactor;
			Skip.If(maximum <= minimum, "The camera has no zoom range.");

			scope.Manager.UpdateZoom(maximum);
			Assert.Equal(maximum, (float)device.VideoZoomFactor, precision: 3);

			scope.Manager.UpdateZoom(minimum);
			Assert.Equal(minimum, (float)device.VideoZoomFactor, precision: 3);
		});
	}

	[Fact]
	public async Task UpdateZoomIgnoresFactorsOutsideTheAvailableRange()
	{
		await RunWithInitializedManagerAsync((scope, device) =>
		{
			var minimum = (float)device.MinAvailableVideoZoomFactor;
			var maximum = (float)device.MaxAvailableVideoZoomFactor;
			Skip.If(maximum < 2f, "The camera's maximum zoom factor is below 2.");
			scope.Manager.UpdateZoom(2f);
			var zoomBefore = (float)device.VideoZoomFactor;

			scope.Manager.UpdateZoom(minimum - 0.5f);
			Assert.Equal(zoomBefore, (float)device.VideoZoomFactor, precision: 3);

			scope.Manager.UpdateZoom(maximum + 1f);
			Assert.Equal(zoomBefore, (float)device.VideoZoomFactor, precision: 3);
		});
	}

	[Fact]
	public async Task UpdateZoomIsIgnoredUntilTheManagerIsInitialized()
	{
		await RunWithInitializedManagerAsync((scope, device) =>
		{
			Skip.If((float)device.MaxAvailableVideoZoomFactor < 2f, "The camera's maximum zoom factor is below 2.");
			var zoomBefore = (float)device.VideoZoomFactor;
			scope.Access.IsInitialized = false;

			scope.Manager.UpdateZoom(2f);

			Assert.Equal(zoomBefore, (float)device.VideoZoomFactor, precision: 3);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionSelectsTheLargestFormatThatFitsTheRequest()
	{
		await RunWithInitializedManagerAsync(async (scope, device) =>
		{
			var camera = scope.View.SelectedCamera ?? throw new InvalidOperationException("No camera selected.");
			var formatsByArea = camera.SupportedFormats.OrderBy(GetArea).ToList();
			Skip.If(formatsByArea.Count < 3, "The camera offers fewer than three formats.");
			var requested = formatsByArea[formatsByArea.Count / 2];
			var requestedDimensions = GetDimensions(requested);
			SetActiveFormat(device, formatsByArea[^1]);

			await scope.Manager.UpdateCaptureResolution(new Size(requestedDimensions.Width, requestedDimensions.Height), CancellationToken.None);

			Assert.Equal(requestedDimensions, GetDimensions(device.ActiveFormat));
			Assert.Contains(device.ActiveFormat, camera.SupportedFormats);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionAcceptsARequestLargerThanEveryFormat()
	{
		await RunWithInitializedManagerAsync(async (scope, device) =>
		{
			var camera = scope.View.SelectedCamera ?? throw new InvalidOperationException("No camera selected.");
			var formatsByArea = camera.SupportedFormats.OrderBy(GetArea).ToList();
			Skip.If(formatsByArea.Count < 2, "The camera offers fewer than two formats.");
			SetActiveFormat(device, formatsByArea[0]);

			await scope.Manager.UpdateCaptureResolution(new Size(100000, 100000), CancellationToken.None);

			Assert.Equal(GetDimensions(formatsByArea[^1]), GetDimensions(device.ActiveFormat));
		});
	}

	// No format fits the request (also the default ImageCaptureResolution, Size.Zero): the camera keeps its active format, which a real start gets
	// from the session preset, with or without a photo output.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdateCaptureResolutionKeepsTheActiveFormatWhenNoFormatFitsTheRequest(bool withPhotoOutput)
	{
		await RunWithInitializedManagerAsync(async (scope, device) =>
		{
			var camera = scope.View.SelectedCamera ?? throw new InvalidOperationException("No camera selected.");
			var formatsByArea = camera.SupportedFormats.OrderBy(GetArea).ToList();
			Skip.If(formatsByArea.Count < 2, "The camera offers fewer than two formats.");
			using var photoOutput = new AVCapturePhotoOutput();
			scope.Access.PhotoOutput = withPhotoOutput ? photoOutput : null;
			SetActiveFormat(device, formatsByArea[0]);
			var activeFormat = device.ActiveFormat;

			try
			{
				await scope.Manager.UpdateCaptureResolution(new Size(1, 1), CancellationToken.None);

				Assert.Equal(activeFormat.Handle, device.ActiveFormat.Handle);
			}
			finally
			{
				// The output belongs to the test, not to the manager that the scope disposes.
				scope.Access.PhotoOutput = null;
			}
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithoutACaptureDeviceKeepsTheActiveFormat()
	{
		await RunWithInitializedManagerAsync(async (scope, device) =>
		{
			var camera = scope.View.SelectedCamera ?? throw new InvalidOperationException("No camera selected.");
			var formatsByArea = camera.SupportedFormats.OrderBy(GetArea).ToList();
			Skip.If(formatsByArea.Count < 2, "The camera offers fewer than two formats.");
			SetActiveFormat(device, formatsByArea[0]);
			var activeFormat = device.ActiveFormat;
			scope.Access.CaptureDevice = null;

			await scope.Manager.UpdateCaptureResolution(new Size(100000, 100000), CancellationToken.None);

			Assert.Equal(activeFormat.Handle, device.ActiveFormat.Handle);
		});
	}

	[Fact]
	public async Task MatchesResolutionRequiresBothDimensionsToFit()
	{
		var camera = await GetFirstCameraOrSkipAsync();
		var format = camera.SupportedFormats[0];
		var (width, height) = GetDimensions(format);

		Assert.True(CameraManagerAccessor.MatchesResolution(format, new Size(width, height)));
		Assert.True(CameraManagerAccessor.MatchesResolution(format, new Size(width + 100, height + 100)));
		Assert.False(CameraManagerAccessor.MatchesResolution(format, new Size(width - 1, height)));
		Assert.False(CameraManagerAccessor.MatchesResolution(format, new Size(width, height - 1)));
		Assert.False(CameraManagerAccessor.MatchesResolution(format, new Size(width + 100, height - 1)));
		Assert.False(CameraManagerAccessor.MatchesResolution(format, Size.Zero));
	}

	[Fact]
	public async Task UpdateIsTorchOnWithoutATorchLeavesTheTorchOff()
	{
		await RunWithInitializedManagerAsync((scope, device) =>
		{
			Skip.If(device.TorchAvailable, "The camera has a torch: UpdateIsTorchOnTurnsTheTorchOnAndOff covers it.");

			scope.Manager.UpdateIsTorchOn(true);

			Assert.Equal(AVCaptureTorchMode.Off, device.TorchMode);
		});
	}

	[Fact]
	public async Task UpdateIsTorchOnTurnsTheTorchOnAndOff()
	{
		await RunWithInitializedManagerAsync((scope, device) =>
		{
			Skip.IfNot(device.TorchAvailable, $"The camera has no torch ({device.LocalizedName} reports TorchAvailable=false).");
			try
			{
				scope.Manager.UpdateIsTorchOn(true);
				Assert.Equal(AVCaptureTorchMode.On, device.TorchMode);

				scope.Manager.UpdateIsTorchOn(true);
				Assert.Equal(AVCaptureTorchMode.On, device.TorchMode);

				scope.Manager.UpdateIsTorchOn(false);
				Assert.Equal(AVCaptureTorchMode.Off, device.TorchMode);

				scope.Manager.UpdateIsTorchOn(false);
				Assert.Equal(AVCaptureTorchMode.Off, device.TorchMode);
			}
			finally
			{
				scope.Manager.UpdateIsTorchOn(false);
			}
		});
	}

	[Fact]
	public async Task UpdateIsTorchOnIsIgnoredUntilTheManagerIsInitialized()
	{
		await RunWithInitializedManagerAsync((scope, device) =>
		{
			Skip.IfNot(device.TorchAvailable, $"The camera has no torch ({device.LocalizedName} reports TorchAvailable=false).");
			scope.Access.IsInitialized = false;

			scope.Manager.UpdateIsTorchOn(true);

			Assert.Equal(AVCaptureTorchMode.Off, device.TorchMode);
		});
	}

	async Task<CameraInfo> GetFirstCameraOrSkipAsync()
	{
		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, cameras.Count > 0, $"Needs a camera ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		Skip.If(cameras[0].SupportedFormats.Count is 0, "The first camera offers no formats.");

		return cameras[0];
	}

	Task RunWithInitializedManagerAsync(Action<AppleCameraManagerScope, AVCaptureDevice> test) =>
		RunWithInitializedManagerAsync((scope, device) =>
		{
			test(scope, device);
			return Task.CompletedTask;
		});

	async Task RunWithInitializedManagerAsync(Func<AppleCameraManagerScope, AVCaptureDevice, Task> test)
	{
		var camera = await GetFirstCameraOrSkipAsync();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var device = camera.CaptureDevice ?? throw new InvalidOperationException("The camera has no capture device.");
			var originalFormat = device.ActiveFormat;
			var originalZoom = device.VideoZoomFactor;
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.View.SelectedCamera = camera;
			scope.Access.CaptureDevice = device;
			scope.Access.IsInitialized = true;

			try
			{
				await test(scope, device);
			}
			finally
			{
				RestoreDevice(device, originalFormat, originalZoom);
			}
		});
	}

	static void SetActiveFormat(AVCaptureDevice device, AVCaptureDeviceFormat format)
	{
		device.LockForConfiguration(out NSError? error);
		Assert.Null(error);
		device.ActiveFormat = format;
		device.UnlockForConfiguration();
	}

	// Changing the format resets the zoom factor, so the format is restored first.
	static void RestoreDevice(AVCaptureDevice device, AVCaptureDeviceFormat format, nfloat zoom)
	{
		device.LockForConfiguration(out NSError? error);
		if (error is not null)
		{
			return;
		}

		device.ActiveFormat = format;
		device.VideoZoomFactor = zoom;
		device.UnlockForConfiguration();
	}

	static (int Width, int Height) GetDimensions(AVCaptureDeviceFormat format)
	{
		var dimensions = ((CMVideoFormatDescription)format.FormatDescription).Dimensions;
		return (dimensions.Width, dimensions.Height);
	}

	static int GetArea(AVCaptureDeviceFormat format)
	{
		var (width, height) = GetDimensions(format);
		return width * height;
	}
}