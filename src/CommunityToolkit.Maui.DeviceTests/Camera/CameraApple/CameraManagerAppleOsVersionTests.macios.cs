using AVFoundation;
using CommunityToolkit.Maui.Core;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// The branches for iOS 16 and older run through AppleSystemVersionOverride because the pools only offer iOS 27 and Mac Catalyst 27, and the motion
// manager is a FakeMotionManager with a scripted reading because simulators and Macs have no accelerometer.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerAppleOsVersionTests : CameraHandlerTestBase
{
	static readonly Version ios16 = new(16, 4);

	// The first version with an AVCaptureDeviceRotationCoordinator, where the checks switch to it.
	static readonly Version ios17 = new(17, 0);

	[Fact]
	public async Task CreatePlatformViewBeforeIos17StartsTheAccelerometerOfTheExistingMotionManager()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var motionManager = new FakeMotionManager();
			scope.Access.MotionManager = motionManager;

			using (new AppleSystemVersionOverride(ios16))
			{
				scope.Manager.CreatePlatformView();
			}

			Assert.Same(motionManager, scope.Access.MotionManager);
			Assert.Equal(1, motionManager.StartCount);
		});
	}

	[Fact]
	public async Task CreatePlatformViewBeforeIos17CreatesAMotionManagerAndReleasesItWhenDisposed()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			Assert.Null(scope.Access.MotionManager);

			using (new AppleSystemVersionOverride(ios16))
			{
				scope.Manager.CreatePlatformView();
			}

			var motionManager = scope.Access.MotionManager ?? throw new InvalidOperationException("No motion manager was created.");
			Assert.NotEqual(NativeHandle.Zero, motionManager.Handle);

			scope.Manager.Dispose();

			Assert.Equal(NativeHandle.Zero, motionManager.Handle);
			Assert.Null(scope.Access.MotionManager);
		});
	}

	[Fact]
	public async Task CreatePlatformViewFromIos17LeavesTheMotionManagerAlone()
	{
		Skip.IfNot(UIKit.UIDevice.CurrentDevice.CheckSystemVersion(17, 0), "Needs iOS or Mac Catalyst 17 or later (the real OS version).");
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var withoutManager = new AppleCameraManagerScope(MauiContext);
			withoutManager.Manager.CreatePlatformView();
			Assert.Null(withoutManager.Access.MotionManager);

			using var withManager = new AppleCameraManagerScope(MauiContext);
			var motionManager = new FakeMotionManager();
			withManager.Access.MotionManager = motionManager;
			withManager.Manager.CreatePlatformView();
			Assert.Equal(0, motionManager.StartCount);
		});
	}

	[Fact]
	public async Task CreatePlatformViewOnIos17ItselfLeavesTheMotionManagerAlone()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var motionManager = new FakeMotionManager();
			scope.Access.MotionManager = motionManager;

			using (new AppleSystemVersionOverride(ios17))
			{
				scope.Manager.CreatePlatformView();
			}

			Assert.Equal(0, motionManager.StartCount);
		});
	}

	[Fact]
	public async Task DisposeStopsAndReleasesTheMotionManager()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var motionManager = new FakeMotionManager();
			scope.Access.MotionManager = motionManager;

			scope.Manager.Dispose();

			Assert.Equal(1, motionManager.StopCount);
			Assert.True(motionManager.WasDisposed);
			Assert.Null(scope.Access.MotionManager);
		});
	}

	[Theory]
	[InlineData(0.0, -1.0, AVCaptureVideoOrientation.Portrait)]
	[InlineData(0.0, 1.0, AVCaptureVideoOrientation.PortraitUpsideDown)]
	[InlineData(1.0, 0.0, AVCaptureVideoOrientation.LandscapeLeft)]
	[InlineData(-1.0, 0.0, AVCaptureVideoOrientation.LandscapeRight)]
	public async Task ConnectionBeforeIos17IsOrientedFromTheAccelerometerReading(double x, double y, AVCaptureVideoOrientation expected)
	{
		await RunWithConnectionAsync((scope, output, connection) =>
		{
			scope.Access.MotionManager = new FakeMotionManager { Reading = new FakeAccelerometerData(x, y) };
			bool configured;

			using (new AppleSystemVersionOverride(ios16))
			{
				configured = scope.Access.TryConfigureAVCaptureConnection(output, out _);
			}

			Assert.True(configured);
			Assert.Equal(expected, connection.Orientation);
			Assert.Equal(FakeCaptureConnection.UntouchedRotationAngle, connection.RotationAngle);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ConnectionBeforeIos17WithoutAnAccelerometerReadingKeepsItsVideoOrientation(bool hasMotionManager)
	{
		await RunWithConnectionAsync((scope, output, connection) =>
		{
			scope.Access.MotionManager = hasMotionManager ? new FakeMotionManager { Reading = null } : null;
			bool configured;

			using (new AppleSystemVersionOverride(ios16))
			{
				configured = scope.Access.TryConfigureAVCaptureConnection(output, out _);
			}

			Assert.True(configured);
			Assert.Equal((AVCaptureVideoOrientation)99, connection.Orientation);
		});
	}

	[Fact]
	public async Task ConnectionFromIos17FollowsTheRotationCoordinatorAndIgnoresTheAccelerometer()
	{
		Skip.IfNot(UIKit.UIDevice.CurrentDevice.CheckSystemVersion(17, 0), "Needs iOS or Mac Catalyst 17 or later (the real OS version).");

		await RunWithConnectionAsync((scope, output, connection) =>
		{
			scope.Access.MotionManager = new FakeMotionManager { Reading = new FakeAccelerometerData(1.0, 0.0) };
			scope.Access.RotationCoordinator = new FakeRotationCoordinator(270);

			Assert.True(scope.Access.TryConfigureAVCaptureConnection(output, out _));

			Assert.Equal(270, connection.RotationAngle);
			Assert.Equal((AVCaptureVideoOrientation)99, connection.Orientation);
		});
	}

	[Fact]
	public async Task ConnectionOnIos17ItselfFollowsTheRotationCoordinatorAndIgnoresTheAccelerometer()
	{
		await RunWithConnectionAsync((scope, output, connection) =>
		{
			scope.Access.MotionManager = new FakeMotionManager { Reading = new FakeAccelerometerData(1.0, 0.0) };
			scope.Access.RotationCoordinator = new FakeRotationCoordinator(270);
			bool configured;

			using (new AppleSystemVersionOverride(ios17))
			{
				configured = scope.Access.TryConfigureAVCaptureConnection(output, out _);
			}

			Assert.True(configured);
			Assert.Equal(270, connection.RotationAngle);
			Assert.Equal((AVCaptureVideoOrientation)99, connection.Orientation);
		});
	}

	async Task RunWithConnectionAsync(Action<AppleCameraManagerScope, FakePhotoOutput, FakeCaptureConnection> test)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var connection = new FakeCaptureConnection();
			using var output = new FakePhotoOutput { VideoConnection = connection };

			test(scope, output, connection);
		});
	}
}