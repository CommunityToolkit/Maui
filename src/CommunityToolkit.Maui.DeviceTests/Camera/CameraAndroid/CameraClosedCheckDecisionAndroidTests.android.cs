using System.Globalization;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraClosedCheckAndroid)]
public class CameraClosedCheckDecisionAndroidTests
{
	static readonly TimeSpan window = TimeSpan.FromMilliseconds(600);

	static CameraAvailabilitySample Free(string id, double seconds) => new(id, true, TimeSpan.FromSeconds(seconds));

	static CameraAvailabilitySample InUse(string id) => new(id, false, TimeSpan.FromSeconds(10));

	[Fact]
	public void EveryCameraThatHasBeenFreeForTheWindowIsClosed()
	{
		var result = CameraClosedCheck.Evaluate(["0", "1"], [Free("0", 5), Free("1", 0.6)], window);

		Assert.Equal(CameraClosedVerdict.Closed, result.Verdict);
	}

	[Fact]
	public void ACameraThatWasFreeBeforeTheCheckStartedIsSettlingUntilTheCheckHasWatchedItForTheWindow()
	{
		var result = CameraClosedCheck.Evaluate(["0"], [Free("0", 30)], window, TimeSpan.FromMilliseconds(100));
		var watchedLongEnough = CameraClosedCheck.Evaluate(["0"], [Free("0", 30)], window, TimeSpan.FromMilliseconds(600));

		Assert.Equal(CameraClosedVerdict.Settling, result.Verdict);
		Assert.Equal(TimeSpan.FromMilliseconds(500), result.RemainingStableTime);
		Assert.Equal(CameraClosedVerdict.Closed, watchedLongEnough.Verdict);
	}

	[Fact]
	public void ACameraThatBecameFreeDuringTheCheckIsSettlingForTheTimeSinceItWasFreed()
	{
		var result = CameraClosedCheck.Evaluate(["0"], [Free("0", 0.2)], window, TimeSpan.FromSeconds(10));

		Assert.Equal(CameraClosedVerdict.Settling, result.Verdict);
		Assert.Equal(TimeSpan.FromMilliseconds(400), result.RemainingStableTime);
	}

	[Fact]
	public void ACameraThatIsInUseIsNotClosed()
	{
		var result = CameraClosedCheck.Evaluate(["0", "1"], [Free("0", 5), InUse("1")], window);

		Assert.Equal(CameraClosedVerdict.InUse, result.Verdict);
		Assert.Contains("1=in use", result.Description, StringComparison.Ordinal);
	}

	[Fact]
	public void ACameraThatWasFreedAMomentAgoIsSettlingForTheRestOfTheWindow()
	{
		var result = CameraClosedCheck.Evaluate(["0"], [Free("0", 0.1)], window);

		Assert.Equal(CameraClosedVerdict.Settling, result.Verdict);
		Assert.Equal(TimeSpan.FromMilliseconds(500), result.RemainingStableTime);
	}

	[Fact]
	public void ACameraWithoutAnAvailabilityReportIsUnknownNotClosed()
	{
		var result = CameraClosedCheck.Evaluate(["0", "1"], [Free("0", 5)], window);

		Assert.Equal(CameraClosedVerdict.Unknown, result.Verdict);
		Assert.Contains("1=no availability reported", result.Description, StringComparison.Ordinal);
	}

	[Fact]
	public void ACameraThatIsInUseOutweighsOneThatIsSettling()
	{
		var result = CameraClosedCheck.Evaluate(["0", "1"], [Free("0", 0.1), InUse("1")], window);

		Assert.Equal(CameraClosedVerdict.InUse, result.Verdict);
	}

	[Fact]
	public void ADeviceWithoutCamerasIsClosed()
	{
		Assert.Equal(CameraClosedVerdict.Closed, CameraClosedCheck.Evaluate([], [], window).Verdict);
	}

	[Fact]
	public void AClosedCameraWhoseCameraXStateIsOpeningIsADroppedClosedPublication()
	{
		var closed = CameraClosedCheck.Evaluate(["0", "1"], [Free("0", 5), Free("1", 5)], window);

		var dropped = CameraClosedCheck.DescribeDroppedClosedPublication(closed, [("0", "CLOSED"), ("1", "OPENING")]);

		Assert.NotNull(dropped);
		Assert.Contains("1=OPENING", dropped, StringComparison.Ordinal);
		Assert.DoesNotContain("0=CLOSED", dropped, StringComparison.Ordinal);
	}

	[Fact]
	public void AClosedCameraWhoseCameraXStateIsPendingOpenIsADroppedClosedPublication()
	{
		var closed = CameraClosedCheck.Evaluate(["1"], [Free("1", 5)], window);

		var dropped = CameraClosedCheck.DescribeDroppedClosedPublication(closed, [("1", "PENDING_OPEN")]);

		Assert.NotNull(dropped);
		Assert.Contains("1=PENDING_OPEN", dropped, StringComparison.Ordinal);
	}

	[Fact]
	public void NothingIsDroppedWhenCameraXAgreesOrTheCameraIsNotClosed()
	{
		var closed = CameraClosedCheck.Evaluate(["0"], [Free("0", 5)], window);
		var inUse = CameraClosedCheck.Evaluate(["0"], [InUse("0")], window);

		Assert.Null(CameraClosedCheck.DescribeDroppedClosedPublication(closed, [("0", "CLOSED")]));
		Assert.Null(CameraClosedCheck.DescribeDroppedClosedPublication(closed, [("0", "UNKNOWN")]));
		Assert.Null(CameraClosedCheck.DescribeDroppedClosedPublication(inUse, [("0", "OPENING")]));
	}

	[Fact]
	public void TheDescriptionsFormatInvariantlyUnderTurkishCulture()
	{
		var culture = CultureInfo.CurrentCulture;
		var uiCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");

			var result = CameraClosedCheck.Evaluate(["0"], [Free("0", 0.3)], window);
			var dropped = CameraClosedCheck.DescribeDroppedClosedPublication(CameraClosedCheck.Evaluate(["0"], [Free("0", 1.5)], window), [("0", "OPENING")]);

			Assert.Contains("0=free for 0.3 s", result.Description, StringComparison.Ordinal);
			Assert.NotNull(dropped);
			Assert.Contains("free for 1.5 s", dropped, StringComparison.Ordinal);
			Assert.Contains("0=OPENING", dropped, StringComparison.Ordinal);
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
		}
	}
}