using AVFoundation;
using CommunityToolkit.Maui.Core;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraManager.GetVideoOrientation, UpdateVideoOrientation and PreviewView.UpdatePreviewVideoOrientation (macios) with scripted UIKit answers
// (AppleInterfaceOrientationStub, ApplePreviewLayerConnectionStub): every interface orientation from each source the manager reads (the
// window scene before OS 26, its effective geometry from OS 26, the status bar without a window scene), on both sides of the OS 26 check
// (AppleRuntimeOsVersionOverride). The simulator cannot turn an iPhone upside down and Mac Catalyst windows cannot be rotated at all;
// CameraManagerAppleTests rotates the real window scene where that is possible.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerAppleOrientationTests : CameraHandlerTestBase
{
	public enum OrientationSource
	{
		EffectiveGeometryFromOs26,
		WindowSceneBeforeOs26,
		StatusBarFromOs26,
		StatusBarBeforeOs26
	}

	[Theory]
	[MemberData(nameof(OrientationCases))]
	public async Task VideoOrientationMapsEveryInterfaceOrientationOfEveryOrientationSource(OrientationSource source, UIInterfaceOrientation interfaceOrientation, AVCaptureVideoOrientation expected)
	{
		SkipUnlessTheRealOsIsAtLeast26();

		var actual = await InvokeOnMainThreadAsync(() =>
		{
			// The other sources report an orientation that maps to something else, so reading the wrong source fails the test.
			var decoy = interfaceOrientation is UIInterfaceOrientation.LandscapeRight ? UIInterfaceOrientation.LandscapeLeft : UIInterfaceOrientation.LandscapeRight;
			using var stub = new AppleInterfaceOrientationStub(decoy, decoy, decoy);
			using var olderOs = source is OrientationSource.WindowSceneBeforeOs26 or OrientationSource.StatusBarBeforeOs26 ? new AppleRuntimeOsVersionOverride(new Version(25, 0)) : null;

			switch (source)
			{
				case OrientationSource.EffectiveGeometryFromOs26:
					stub.GeometryOrientation = interfaceOrientation;
					break;
				case OrientationSource.WindowSceneBeforeOs26:
					stub.SceneOrientation = interfaceOrientation;
					break;
				default:
					stub.HideScenes = true;
					stub.StatusBarOrientation = interfaceOrientation;
					break;
			}

			return CameraManagerAccessor.GetVideoOrientation();
		});

		Assert.Equal(expected, actual);
	}

	// A version of 0 runs on the real OS; 26.0 is the first version of the effective geometry.
	[Theory]
	[InlineData(0, 0)]
	[InlineData(26, 0)]
	public async Task VideoOrientationFromOs26IgnoresTheInterfaceOrientationOfTheSceneItself(int major, int minor)
	{
		SkipUnlessTheRealOsIsAtLeast26();

		var actual = await InvokeOnMainThreadAsync(() =>
		{
			using var stub = new AppleInterfaceOrientationStub(UIInterfaceOrientation.LandscapeLeft, UIInterfaceOrientation.Portrait, UIInterfaceOrientation.LandscapeRight);
			using var os = major is 0 ? null : new AppleRuntimeOsVersionOverride(new Version(major, minor));

			return CameraManagerAccessor.GetVideoOrientation();
		});

		Assert.Equal(AVCaptureVideoOrientation.Portrait, actual);
	}

	[Theory]
	[InlineData(25, 0)]
	[InlineData(25, 99)]
	public async Task VideoOrientationBeforeOs26IgnoresTheEffectiveGeometry(int major, int minor)
	{
		SkipUnlessTheRealOsIsAtLeast26();

		var actual = await InvokeOnMainThreadAsync(() =>
		{
			using var stub = new AppleInterfaceOrientationStub(UIInterfaceOrientation.LandscapeRight, UIInterfaceOrientation.Portrait, UIInterfaceOrientation.LandscapeLeft);
			using var olderOs = new AppleRuntimeOsVersionOverride(new Version(major, minor));

			return CameraManagerAccessor.GetVideoOrientation();
		});

		Assert.Equal(AVCaptureVideoOrientation.LandscapeRight, actual);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task VideoOrientationWithoutAWindowSceneFollowsTheStatusBarOnEveryOs(bool isOlderThanOs26)
	{
		SkipUnlessTheRealOsIsAtLeast26();

		var actual = await InvokeOnMainThreadAsync(() =>
		{
			using var stub = new AppleInterfaceOrientationStub(UIInterfaceOrientation.Portrait, UIInterfaceOrientation.Portrait, UIInterfaceOrientation.LandscapeLeft) { HideScenes = true };
			using var olderOs = isOlderThanOs26 ? new AppleRuntimeOsVersionOverride(new Version(25, 0)) : null;
			Assert.Empty(UIApplication.SharedApplication.ConnectedScenes);

			return CameraManagerAccessor.GetVideoOrientation();
		});

		Assert.Equal(AVCaptureVideoOrientation.LandscapeLeft, actual);
	}

	[Fact]
	public async Task OrientationChangeNotificationReadsTheOrientationThatUiKitReportsAtThatMoment()
	{
		SkipUnlessTheRealOsIsAtLeast26();
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var platformView = scope.Manager.CreatePlatformView();
			var connection = new FakeCaptureConnection();
			using var previewConnection = new ApplePreviewLayerConnectionStub(platformView.Layer, connection);
			using var stub = new AppleInterfaceOrientationStub(UIInterfaceOrientation.Portrait, UIInterfaceOrientation.LandscapeRight, UIInterfaceOrientation.Portrait);

			AppleCameraEnvironment.PostOrientationDidChangeNotification();

			Assert.Equal(AVCaptureVideoOrientation.LandscapeRight, scope.Access.VideoOrientation);
			Assert.Equal(AVCaptureVideoOrientation.LandscapeRight, connection.Orientation);

			stub.GeometryOrientation = UIInterfaceOrientation.PortraitUpsideDown;
			AppleCameraEnvironment.PostOrientationDidChangeNotification();

			Assert.Equal(AVCaptureVideoOrientation.PortraitUpsideDown, scope.Access.VideoOrientation);
			Assert.Equal(AVCaptureVideoOrientation.PortraitUpsideDown, connection.Orientation);
		});
	}

	[Fact]
	public async Task PreviewLayoutAppliesTheCurrentOrientationToThePreviewConnection()
	{
		SkipUnlessTheRealOsIsAtLeast26();
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var platformView = scope.Manager.CreatePlatformView();
			scope.Access.VideoOrientation = (AVCaptureVideoOrientation)99;
			var connection = new FakeCaptureConnection();
			using var previewConnection = new ApplePreviewLayerConnectionStub(platformView.Layer, connection);
			using var stub = new AppleInterfaceOrientationStub(UIInterfaceOrientation.Portrait, UIInterfaceOrientation.LandscapeLeft, UIInterfaceOrientation.Portrait);

			platformView.Frame = new CoreGraphics.CGRect(0, 0, 160, 90);
			platformView.SetNeedsLayout();
			platformView.LayoutIfNeeded();

			// The layout reads the orientation itself; it does not change what the manager stored.
			Assert.Equal(AVCaptureVideoOrientation.LandscapeLeft, connection.Orientation);
			Assert.True(previewConnection.ReadCount >= 2);
			Assert.Equal((AVCaptureVideoOrientation)99, scope.Access.VideoOrientation);
		});
	}

	[Fact]
	public async Task UpdateVideoOrientationWithoutAPreviewViewStoresTheOrientationAndDoesNothingElse()
	{
		SkipUnlessTheRealOsIsAtLeast26();
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			Assert.Null(scope.Access.PreviewView);
			scope.Access.VideoOrientation = (AVCaptureVideoOrientation)99;
			using var stub = new AppleInterfaceOrientationStub(UIInterfaceOrientation.Portrait, UIInterfaceOrientation.LandscapeRight, UIInterfaceOrientation.Portrait);

			scope.Access.UpdateVideoOrientation();

			Assert.Equal(AVCaptureVideoOrientation.LandscapeRight, scope.Access.VideoOrientation);
			Assert.Null(scope.Access.PreviewView);
		});
	}

	public static IEnumerable<object[]> OrientationCases()
	{
		(UIInterfaceOrientation Interface, AVCaptureVideoOrientation Video)[] mappings =
		[
			(UIInterfaceOrientation.Portrait, AVCaptureVideoOrientation.Portrait),
			(UIInterfaceOrientation.PortraitUpsideDown, AVCaptureVideoOrientation.PortraitUpsideDown),
			(UIInterfaceOrientation.LandscapeRight, AVCaptureVideoOrientation.LandscapeRight),
			(UIInterfaceOrientation.LandscapeLeft, AVCaptureVideoOrientation.LandscapeLeft),
			(UIInterfaceOrientation.Unknown, AVCaptureVideoOrientation.Portrait)
		];

		foreach (var source in Enum.GetValues<OrientationSource>())
		{
			foreach (var (interfaceOrientation, videoOrientation) in mappings)
			{
				yield return [source, interfaceOrientation, videoOrientation];
			}
		}
	}

	// The OS 26 arm runs on the real OS; the older arm needs AppleRuntimeOsVersionOverride to go back, never forward.
	static void SkipUnlessTheRealOsIsAtLeast26() =>
		Skip.IfNot(OperatingSystem.IsIOSVersionAtLeast(26) || OperatingSystem.IsMacCatalystVersionAtLeast(26),
			"Needs iOS or Mac Catalyst 26 or later: the scripted orientation sources are read through the OS 26 code path, and the older one by overriding the version downwards.");
}