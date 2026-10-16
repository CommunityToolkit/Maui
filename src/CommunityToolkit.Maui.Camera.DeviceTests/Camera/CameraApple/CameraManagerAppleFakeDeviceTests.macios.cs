using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CoreMedia;
using Foundation;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraManager.macios.cs device configuration (torch, zoom, capture resolution) against FakeCaptureDevice and FakeCaptureDeviceFormat
// (see AppleNativeFakes.macios.cs). The fakes make every branch reachable on the iOS simulator and on CI Macs without a camera:
// torch support, a device that cannot be locked, formats of known sizes. The same members are also tested against the real camera of a
// Mac in CameraManagerAppleDeviceTests.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerAppleFakeDeviceTests : CameraHandlerTestBase
{
	static readonly NSError lockFailure = new(new NSString("test-domain"), 11);

	[Fact]
	public async Task UpdateIsTorchOnTurnsTheTorchOnAndOffWithinOneLockEach()
	{
		await RunAsync(new FakeCaptureDevice { TorchIsAvailable = true }, device =>
		{
			var (scope, _) = device;
			scope.Manager.UpdateIsTorchOn(true);

			Assert.Equal(["lock", "torch=On", "unlock"], scope.Fake.Calls);
			Assert.Equal(AVCaptureTorchMode.On, scope.Fake.TorchMode);
			Assert.False(scope.Fake.IsLocked);

			scope.Manager.UpdateIsTorchOn(false);

			Assert.Equal(["lock", "torch=On", "unlock", "lock", "torch=Off", "unlock"], scope.Fake.Calls);
			Assert.Equal(AVCaptureTorchMode.Off, scope.Fake.TorchMode);
			Assert.Equal(0, scope.Fake.WritesWithoutLock);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdateIsTorchOnDoesNotTouchTheDeviceWhenTheTorchAlreadyHasTheRequestedState(bool torchIsOn)
	{
		await RunAsync(new FakeCaptureDevice { TorchIsAvailable = true, TorchIsActive = torchIsOn }, device =>
		{
			device.Scope.Manager.UpdateIsTorchOn(torchIsOn);

			Assert.Empty(device.Scope.Fake.Calls);
			Assert.Equal(torchIsOn, device.Scope.Fake.TorchIsActive);
		});
	}

	[Fact]
	public async Task UpdateIsTorchOnIsIgnoredWhenTheDeviceHasNoTorch()
	{
		await RunAsync(new FakeCaptureDevice { TorchIsAvailable = false }, device =>
		{
			device.Scope.Manager.UpdateIsTorchOn(true);

			Assert.Empty(device.Scope.Fake.Calls);
			Assert.False(device.Scope.Fake.TorchIsActive);
		});
	}

	[Fact]
	public async Task UpdateIsTorchOnIsIgnoredUntilTheManagerIsInitialized()
	{
		await RunAsync(new FakeCaptureDevice { TorchIsAvailable = true }, device =>
		{
			device.Scope.Access.IsInitialized = false;

			device.Scope.Manager.UpdateIsTorchOn(true);

			Assert.Empty(device.Scope.Fake.Calls);
			Assert.False(device.Scope.Fake.TorchIsActive);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdateIsTorchOnKeepsTheTorchWhenTheDeviceCannotBeLocked(bool torchIsOn)
	{
		await RunAsync(new FakeCaptureDevice { TorchIsAvailable = true, TorchIsActive = torchIsOn, LockError = lockFailure }, device =>
		{
			using var trace = new TraceRecorder();

			device.Scope.Manager.UpdateIsTorchOn(!torchIsOn);

			Assert.Equal(["lock-failed"], device.Scope.Fake.Calls);
			Assert.Equal(torchIsOn, device.Scope.Fake.TorchIsActive);
			Assert.Contains(trace.Lines, line => line.Contains("test-domain", StringComparison.Ordinal));
		});
	}

	[Theory]
	[InlineData(2.0)]
	[InlineData(1.0)]
	[InlineData(4.0)]
	[InlineData(3.25)]
	public async Task UpdateZoomAppliesFactorsInsideTheAvailableRangeIncludingTheLimits(double zoomFactor)
	{
		await RunAsync(new FakeCaptureDevice { MinimumZoomFactor = 1, MaximumZoomFactor = 4, CurrentZoomFactor = 1.5 }, device =>
		{
			device.Scope.Manager.UpdateZoom((float)zoomFactor);

			Assert.Equal(["lock", $"zoom={zoomFactor}", "unlock"], device.Scope.Fake.Calls);
			Assert.Equal(zoomFactor, device.Scope.Fake.CurrentZoomFactor);
			Assert.Equal(0, device.Scope.Fake.WritesWithoutLock);
		});
	}

	[Theory]
	[InlineData(0.99f)]
	[InlineData(0f)]
	[InlineData(-1f)]
	[InlineData(4.01f)]
	[InlineData(100f)]
	public async Task UpdateZoomIgnoresFactorsOutsideTheAvailableRange(float zoomFactor)
	{
		await RunAsync(new FakeCaptureDevice { MinimumZoomFactor = 1, MaximumZoomFactor = 4, CurrentZoomFactor = 1.5 }, device =>
		{
			device.Scope.Manager.UpdateZoom(zoomFactor);

			Assert.Empty(device.Scope.Fake.Calls);
			Assert.Equal(1.5, device.Scope.Fake.CurrentZoomFactor);
		});
	}

	[Fact]
	public async Task UpdateZoomIsIgnoredUntilTheManagerIsInitialized()
	{
		await RunAsync(new FakeCaptureDevice(), device =>
		{
			device.Scope.Access.IsInitialized = false;

			device.Scope.Manager.UpdateZoom(2f);

			Assert.Empty(device.Scope.Fake.Calls);
			Assert.Equal(1, device.Scope.Fake.CurrentZoomFactor);
		});
	}

	[Fact]
	public async Task UpdateZoomKeepsTheZoomWhenTheDeviceCannotBeLocked()
	{
		await RunAsync(new FakeCaptureDevice { LockError = lockFailure }, device =>
		{
			using var trace = new TraceRecorder();

			device.Scope.Manager.UpdateZoom(2f);

			Assert.Equal(["lock-failed"], device.Scope.Fake.Calls);
			Assert.Equal(1, device.Scope.Fake.CurrentZoomFactor);
			Assert.Contains(trace.Lines, line => line.Contains("test-domain", StringComparison.Ordinal));
		});
	}

	[Theory]
	[InlineData(1920, 1080, 1920, 1080)]
	[InlineData(2000, 1200, 1920, 1080)]
	[InlineData(1300, 800, 1280, 720)]
	[InlineData(100000, 100000, 3840, 2160)]
	public async Task UpdateCaptureResolutionSelectsTheLargestFormatThatFitsTheRequest(int requestedWidth, int requestedHeight, int expectedWidth, int expectedHeight)
	{
		await RunAsync(new FakeCaptureDevice(), device =>
		{
			var scope = device.Scope;
			scope.View.SelectedCamera = CreateCamera(scope.Fake, Format(640, 480), Format(3840, 2160), Format(1280, 720), Format(1920, 1080));

			UpdateResolution(scope, new Size(requestedWidth, requestedHeight));

			Assert.Equal(["lock", $"format={expectedWidth}x{expectedHeight}", "unlock"], scope.Fake.Calls);
			Assert.Equal($"{expectedWidth}x{expectedHeight}", FakeCaptureDeviceFormat.Describe(scope.Fake.ActiveFormat));
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionRequiresBothDimensionsToFit()
	{
		await RunAsync(new FakeCaptureDevice(), device =>
		{
			var scope = device.Scope;
			scope.View.SelectedCamera = CreateCamera(scope.Fake, Format(1920, 1080), Format(1280, 960));

			UpdateResolution(scope, new Size(1920, 1000));

			Assert.Equal("1280x960", FakeCaptureDeviceFormat.Describe(scope.Fake.ActiveFormat));
		});
	}

	// No format fits the request (also the default ImageCaptureResolution, Size.Zero): the device keeps the format that the session preset
	// (AVCaptureSessionPresetPhoto) chose. The largest format of all, which the toolkit used to choose without a photo output, can be
	// one that the photo output cannot capture from (#2920, #3037).
	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(639, 479)]
	public async Task UpdateCaptureResolutionKeepsTheActiveFormatWhenNoFormatFitsTheRequest(int requestedWidth, int requestedHeight)
	{
		var activeFormat = Format(800, 600);
		await RunAsync(new FakeCaptureDevice { CurrentFormat = activeFormat }, device =>
		{
			var scope = device.Scope;
			scope.View.SelectedCamera = CreateCamera(scope.Fake, Format(1280, 720), Format(3840, 2160), Format(640, 480));
			Assert.Null(scope.Access.PhotoOutput);

			UpdateResolution(scope, new Size(requestedWidth, requestedHeight));

			Assert.Equal(["lock", "unlock"], scope.Fake.Calls);
			Assert.Same(activeFormat, scope.Fake.CurrentFormat);
		});
	}

	// The photo output never chooses the format either: whatever photo pixel formats it reports, a request that no format fits keeps
	// the format of the session preset, and the toolkit does not ask it. This is what real iPhones have done since #2941, whose filter asked the
	// photo output for the file type "Jpeg", which is not an AVFileType, and so never kept a format.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task UpdateCaptureResolutionKeepsTheActiveFormatWhenNoFormatFitsTheRequestWhateverThePhotoOutputSupports(bool photoOutputSupportsTheJpegFormats)
	{
		var activeFormat = Format(800, 600);
		await RunAsync(new FakeCaptureDevice { CurrentFormat = activeFormat }, device =>
		{
			var scope = device.Scope;
			var jpegFormat = new FakeCaptureDeviceFormat(1280, 720, CMVideoCodecType.JPEG);
			var largerJpegFormat = new FakeCaptureDeviceFormat(1920, 1080, CMVideoCodecType.JPEG);
			var h264Format = new FakeCaptureDeviceFormat(3840, 2160, CMVideoCodecType.H264);
			scope.View.SelectedCamera = CreateCamera(scope.Fake, h264Format, jpegFormat, largerJpegFormat);
			var photoOutput = new FakePhotoOutput { SupportedPixelFormats = photoOutputSupportsTheJpegFormats ? [NSNumber.FromUInt32(jpegFormat.FormatDescription.MediaSubType)] : [] };
			scope.Access.PhotoOutput = photoOutput;

			UpdateResolution(scope, new Size(1, 1));

			Assert.Equal(["lock", "unlock"], scope.Fake.Calls);
			Assert.Same(activeFormat, scope.Fake.CurrentFormat);
			Assert.Empty(photoOutput.RequestedFileTypes);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionSelectsTheLargestFittingFormatWhateverThePhotoOutputSupports()
	{
		await RunAsync(new FakeCaptureDevice(), device =>
		{
			var scope = device.Scope;
			var fittingH264Format = new FakeCaptureDeviceFormat(1280, 720, CMVideoCodecType.H264);
			var tooLargeJpegFormat = new FakeCaptureDeviceFormat(3840, 2160, CMVideoCodecType.JPEG);
			scope.View.SelectedCamera = CreateCamera(scope.Fake, fittingH264Format, tooLargeJpegFormat);
			var photoOutput = new FakePhotoOutput { SupportedPixelFormats = [NSNumber.FromUInt32(tooLargeJpegFormat.FormatDescription.MediaSubType)] };
			scope.Access.PhotoOutput = photoOutput;

			UpdateResolution(scope, new Size(1920, 1080));

			// A format that fits the request is chosen without consulting the photo output.
			Assert.Equal(["lock", "format=1280x720", "unlock"], scope.Fake.Calls);
			Assert.Empty(photoOutput.RequestedFileTypes);
		});
	}

	[Theory]
	[InlineData(640, 480, 640, 480, true)]
	[InlineData(640, 480, 1920, 1080, true)]
	[InlineData(640, 480, 639, 480, false)]
	[InlineData(640, 480, 640, 479, false)]
	[InlineData(640, 480, 1920, 479, false)]
	[InlineData(640, 480, 0, 0, false)]
	public void MatchesResolutionRequiresBothDimensionsOfTheFormatToFit(int formatWidth, int formatHeight, int requestedWidth, int requestedHeight, bool expected)
	{
		var format = new FakeCaptureDeviceFormat(formatWidth, formatHeight);

		Assert.Equal(expected, CameraManagerAccessor.MatchesResolution(format, new Size(requestedWidth, requestedHeight)));
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithoutFormatsLocksAndUnlocksButKeepsTheActiveFormat()
	{
		var activeFormat = Format(800, 600);
		await RunAsync(new FakeCaptureDevice { CurrentFormat = activeFormat }, device =>
		{
			var scope = device.Scope;
			scope.View.SelectedCamera = CreateCamera(scope.Fake);

			UpdateResolution(scope, new Size(1920, 1080));

			Assert.Equal(["lock", "unlock"], scope.Fake.Calls);
			Assert.Same(activeFormat, scope.Fake.CurrentFormat);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionKeepsTheActiveFormatWhenTheDeviceCannotBeLocked()
	{
		var activeFormat = Format(800, 600);
		await RunAsync(new FakeCaptureDevice { CurrentFormat = activeFormat, LockError = lockFailure }, device =>
		{
			var scope = device.Scope;
			scope.View.SelectedCamera = CreateCamera(scope.Fake, Format(1920, 1080));
			using var trace = new TraceRecorder();

			UpdateResolution(scope, new Size(1920, 1080));

			Assert.Equal(["lock-failed"], scope.Fake.Calls);
			Assert.Same(activeFormat, scope.Fake.CurrentFormat);
			Assert.Contains(trace.Lines, line => line.Contains("test-domain", StringComparison.Ordinal));
		});
	}

	// The raise is modeled by FakeCaptureDevice's strict mode after Apple's documentation of -[AVCaptureDevice setActiveFormat:]
	// (NSInvalidArgumentException for a format that is not in the device's formats), reachable through the public CameraInfo constructor with
	// formats of another device. UpdateCaptureResolution locks the device, so it must unlock it also when setting the active format raises.
	[Fact]
	public async Task UpdateCaptureResolutionThatTheDeviceRejectsStillUnlocksTheDevice()
	{
		await RunAsync(new FakeCaptureDevice { RejectsFormatsOfOtherDevices = true, DeviceFormats = [Format(1280, 720)] }, device =>
		{
			var scope = device.Scope;
			scope.View.SelectedCamera = CreateCamera(scope.Fake, Format(640, 480));

			// Whether the raise should reach the caller is not the point here: only that the device is released.
			_ = Record.Exception(() => UpdateResolution(scope, new Size(640, 480)));

			Assert.Contains("format-rejected", scope.Fake.Calls);
			Assert.False(scope.Fake.IsLocked);
			Assert.Equal(scope.Fake.LockCount, scope.Fake.UnlockCount);
		});
	}

	// After StopCameraPreview a new SelectedCamera does not replace the capture device (UpdateCurrentCamera only restarts a running preview):
	// the formats of the selected camera must not be applied to the device of the stopped camera (AVFoundation raises
	// NSInvalidArgumentException for them); the next StartCameraPreview applies the resolution to the selected camera.
	[Fact]
	public async Task UpdateCaptureResolutionDoesNotApplyTheFormatsOfAnotherCameraToTheCaptureDevice()
	{
		using var selectedDevice = new FakeCaptureDevice { Identifier = "fake-selected-while-stopped" };
		var selectedFormat = Format(640, 480);
		selectedDevice.DeviceFormats = [selectedFormat];

		await RunAsync(new FakeCaptureDevice { RejectsFormatsOfOtherDevices = true, DeviceFormats = [Format(1280, 720)] }, device =>
		{
			var scope = device.Scope;
			scope.Access.IsInitialized = false;
			scope.View.SelectedCamera = CreateCamera(selectedDevice, selectedFormat);

			UpdateResolution(scope, new Size(640, 480));

			Assert.Empty(scope.Fake.Calls);
			Assert.Empty(selectedDevice.Calls);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithoutASelectedCameraDoesNotTouchTheDevice()
	{
		await RunAsync(new FakeCaptureDevice(), device =>
		{
			UpdateResolution(device.Scope, new Size(1920, 1080));

			Assert.Empty(device.Scope.Fake.Calls);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithoutACaptureDeviceDoesNotTouchTheDevice()
	{
		await RunAsync(new FakeCaptureDevice(), device =>
		{
			var scope = device.Scope;
			scope.View.SelectedCamera = CreateCamera(scope.Fake, Format(1920, 1080));
			scope.Access.CaptureDevice = null;

			UpdateResolution(scope, new Size(1920, 1080));

			Assert.Empty(scope.Fake.Calls);
		});
	}

	static FakeCaptureDeviceFormat Format(int width, int height) => new(width, height);

	// The macios implementation configures the device synchronously, so the returned ValueTask has completed when it returns.
	static void UpdateResolution(FakeDeviceScope scope, Size resolution)
	{
		var update = scope.Manager.UpdateCaptureResolution(resolution, CancellationToken.None);

		Assert.True(update.IsCompletedSuccessfully);
	}

	static CameraInfo CreateCamera(FakeCaptureDevice device, params AVCaptureDeviceFormat[] formats) =>
		new("Fake camera", $"fake-fake-{Guid.NewGuid()}", CameraPosition.Rear, false, 1f, 4f, [new Size(640, 480)], device, formats);

	// Runs the test on the UI thread with a manager that is initialized for the fake device, and disposes everything afterwards.
	async Task RunAsync(FakeCaptureDevice fake, Action<(FakeDeviceScope Scope, FakeCaptureDevice Device)> test)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new FakeDeviceScope(MauiContext, fake);

			test((scope, fake));
		});
	}

	sealed class FakeDeviceScope : IDisposable
	{
		readonly AppleCameraManagerScope inner;

		public FakeDeviceScope(IMauiContext mauiContext, FakeCaptureDevice fake)
		{
			inner = new AppleCameraManagerScope(mauiContext);
			Fake = fake;
			inner.Access.CaptureDevice = fake;
			inner.Access.IsInitialized = true;
		}

		public FakeCaptureDevice Fake { get; }

		public CameraManager Manager => inner.Manager;

		public CameraManagerAccessor Access => inner.Access;

		public CameraView View => inner.View;

		public void Dispose()
		{
			// Dispose would stop and release a session this scope never created; the fake device is not owned by the manager.
			inner.Access.CaptureDevice = null;
			inner.Dispose();
			Fake.Dispose();
		}
	}
}