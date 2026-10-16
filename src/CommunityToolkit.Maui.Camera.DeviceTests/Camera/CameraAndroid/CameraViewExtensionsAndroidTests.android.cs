using Android.Content.PM;
using AndroidX.Camera.Core;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraViewExtensionsAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewExtensionsAndroidTests : CameraAndroidTestBase
{
	// CameraX: FLASH_MODE_AUTO = 0, FLASH_MODE_ON = 1, FLASH_MODE_OFF = 2.
	[Theory]
	[InlineData(CameraFlashMode.Off, 2)]
	[InlineData(CameraFlashMode.On, 1)]
	[InlineData(CameraFlashMode.Auto, 0)]
	public void ToPlatformMapsFlashModeToTheCameraXConstant(CameraFlashMode flashMode, int expected)
	{
		Assert.Equal(expected, flashMode.ToPlatform());
	}

	[Theory]
	[InlineData(CameraFlashMode.Off, 2)]
	[InlineData(CameraFlashMode.On, 1)]
	[InlineData(CameraFlashMode.Auto, 0)]
	public void ToPlatformValueIsAcceptedByTheNativeImageCapture(CameraFlashMode flashMode, int expected)
	{
		using var imageCapture = new ImageCapture.Builder().Build();

		// CameraX throws IllegalArgumentException for a value that is not one of its three flash modes.
		imageCapture.FlashMode = flashMode.ToPlatform();

		Assert.Equal(expected, imageCapture.FlashMode);
	}

	[Fact]
	public void ToPlatformMapsEveryFlashModeToADifferentConstant()
	{
		var values = Enum.GetValues<CameraFlashMode>().Select(mode => mode.ToPlatform()).ToList();

		Assert.Equal(3, values.Count);
		Assert.Equal(values.Count, values.Distinct().Count());
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(3)]
	[InlineData(42)]
	[InlineData(int.MaxValue)]
	public void ToPlatformThrowsNotSupportedExceptionForAnUndefinedFlashMode(int value)
	{
		var flashMode = (CameraFlashMode)value;

		var exception = Assert.Throws<NotSupportedException>(() => flashMode.ToPlatform());

		Assert.Equal($"Flash mode {value} not supported.", exception.Message);
	}

	[Fact]
	public void UpdateAvailabilitySetsIsAvailableWhenTheDeviceHasACamera()
	{
		var packageManager = AppContext.PackageManager;
		Assert.NotNull(packageManager);
		Skip.IfNot(packageManager.HasSystemFeature(PackageManager.FeatureCamera), "This device has no camera feature, so IsAvailable can not become true here.");

		var cameraView = new CameraView();
		Assert.False(cameraView.IsAvailable);

		((ICameraView)cameraView).UpdateAvailability(AppContext);

		Assert.True(cameraView.IsAvailable);
	}

	[Fact]
	public void UpdateAvailabilityClearsIsAvailableWhenTheDeviceHasNoCamera()
	{
		var packageManager = AppContext.PackageManager;
		Assert.NotNull(packageManager);
		Skip.If(packageManager.HasSystemFeature(PackageManager.FeatureCamera), "This device has the camera feature (every emulator in the pool does), so the false arm of HasSystemFeature can not be reached on it.");

		var cameraView = new CameraView();
		((ICameraView)cameraView).IsAvailable = true;

		((ICameraView)cameraView).UpdateAvailability(AppContext);

		Assert.False(cameraView.IsAvailable);
	}

	[Fact]
	public void UpdateAvailabilityClearsIsAvailableWhenThereIsNoPackageManager()
	{
		var context = new ConfigurableContext(AppContext) { HidePackageManager = true };
		var cameraView = new CameraView();
		((ICameraView)cameraView).IsAvailable = true;
		Assert.Null(context.PackageManager);

		((ICameraView)cameraView).UpdateAvailability(context);

		Assert.False(cameraView.IsAvailable);
	}

	[Fact]
	public void UpdateAvailabilityFollowsTheContextOnEveryCall()
	{
		var packageManager = AppContext.PackageManager;
		Assert.NotNull(packageManager);
		Skip.IfNot(packageManager.HasSystemFeature(PackageManager.FeatureCamera), "This device has no camera feature.");

		var hidden = new ConfigurableContext(AppContext) { HidePackageManager = true };
		var cameraView = new CameraView();

		((ICameraView)cameraView).UpdateAvailability(AppContext);
		Assert.True(cameraView.IsAvailable);

		((ICameraView)cameraView).UpdateAvailability(hidden);
		Assert.False(cameraView.IsAvailable);

		((ICameraView)cameraView).UpdateAvailability(AppContext);
		Assert.True(cameraView.IsAvailable);
	}
}