using CommunityToolkit.Maui.ApplicationModel;
using Foundation;
using UIKit;
using UserNotifications;

namespace CommunityToolkit.Maui.DeviceTests;

// The system's answer to the badge authorization request and to the badge count depends on the app's notification settings: the iOS
// simulator accepts badge counts without consent, a device needs the person's consent, and a Mac Catalyst app that may not send
// notifications gets "Notifications are not allowed for this application" for the authorization request (and, depending on the Mac, for
// the badge count too). So each test first asks the system itself, then asserts what the toolkit does for that answer; the other answer is
// a visible skip.
//
// On iOS every call of SetCount also asks for notification authorization, which shows the system's "Would Like to Send You Notifications"
// prompt until someone answers it. A headless run cannot answer it (simctl cannot pre-grant notifications), and the unanswered prompt makes
// the app inactive, which breaks every later test of the launch that hosts a view. So on iOS the tests that call SetCount need a person
// (RequiresUserInteraction), which a headless run does not run. Mac Catalyst shows no prompt, so there they all run headless.
public partial class BadgeTests
{
	const string authorizationError = "Error Requesting Authorization to Set Badge Count";
	const string badgeError = "Error setting the Badge Count";
	const string invalidCount = "Badge count is invalid";

	// The toolkit only writes the answers of the system to Trace. Whatever the system answers here (the tests below take one of the two arms,
	// and on iOS they need a person), both arms of both callbacks run with an NSError of the test's own.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void AuthorizationAnswerIsTracedOnlyWhenItHasAnError(bool hasError)
	{
		using var trace = new FilesTraceCapture();
		using var error = hasError ? CreateError("Badges are not allowed for this app") : null;

		BadgeImplementation.TraceAuthorizationError(error);

		AssertTraced(trace, authorizationError, hasError ? "Badges are not allowed for this app" : null);
		Assert.False(trace.Contains(badgeError));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void SetBadgeCountAnswerIsTracedOnlyWhenItHasAnError(bool hasError)
	{
		using var trace = new FilesTraceCapture();
		using var error = hasError ? CreateError("Badge count is not allowed") : null;

		BadgeImplementation.TraceSetBadgeCountError(error);

		AssertTraced(trace, badgeError, hasError ? "Badge count is not allowed" : null);
		Assert.False(trace.Contains(authorizationError));
	}

	static NSError CreateError(string description) =>
		new(new NSString("CommunityToolkit.Maui.DeviceTests"), 1, NSDictionary.FromObjectAndKey(new NSString(description), NSError.LocalizedDescriptionKey));

	static void AssertTraced(FilesTraceCapture trace, string prefix, string? description)
	{
		if (description is null)
		{
			Assert.False(trace.Contains(prefix), string.Join(Environment.NewLine, trace.Lines));
			return;
		}

		Assert.Single(trace.Lines, line => line.StartsWith(prefix, StringComparison.Ordinal) && line.Contains(description, StringComparison.Ordinal));
	}

#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[Theory]
	[InlineData(1u)]
	[InlineData(5u)]
	[InlineData(99u)]
	[InlineData(12345u)]
	public async Task SetCountShowsTheCountOnTheAppIcon(uint count)
	{
		await RequireAcceptedBadgesAsync();
		using var trace = new FilesTraceCapture();

		Badge.SetCount(count);

		await AssertBadgeNumberAsync((int)count);
		Assert.False(trace.Contains(badgeError), string.Join(Environment.NewLine, trace.Lines));
	}

#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[Fact]
	public async Task SetCountZeroClearsTheBadge()
	{
		await RequireAcceptedBadgesAsync();
		Badge.SetCount(7);
		await AssertBadgeNumberAsync(7);

		Badge.SetCount(0);

		await AssertBadgeNumberAsync(0);
	}

#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[Fact]
	public async Task SetCountReplacesThePreviousCountAndRepeatingItChangesNothing()
	{
		await RequireAcceptedBadgesAsync();

		Badge.SetCount(4);
		await AssertBadgeNumberAsync(4);
		Badge.SetCount(9);
		await AssertBadgeNumberAsync(9);
		Badge.SetCount(9);
		await AssertBadgeNumberAsync(9);
	}

#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[Fact]
	public async Task EveryEntryPointOfBadgeSetsTheCount()
	{
		await RequireAcceptedBadgesAsync();

		Badge.SetCount(3);
		await AssertBadgeNumberAsync(3);
		Badge.Default.SetCount(4);
		await AssertBadgeNumberAsync(4);
		new BadgeImplementation().SetCount(5);
		await AssertBadgeNumberAsync(5);
	}

#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[Fact]
	public async Task SetCountFromABackgroundThreadSetsTheCount()
	{
		await RequireAcceptedBadgesAsync();

		await Task.Run(() => Badge.SetCount(6));

		await AssertBadgeNumberAsync(6);
	}

#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[Fact]
	public async Task SetCountTracesTheErrorThatTheSystemGivesForTheBadgeCount()
	{
		AddCleanup(ResetBadgeAsync);
		var systemError = await SystemSetBadgeCountAsync(5);
		Skip.If(systemError is null, "This system accepts badge counts for this app (the iOS simulator, or a Mac where the app may badge its icon), so the error callback of SetBadgeCount does not run here.");
		using var trace = new FilesTraceCapture();

		Badge.SetCount(5);

		await AssertionExtensions.AssertEventually(
			() => trace.Lines.Any(line => line.StartsWith(badgeError, StringComparison.Ordinal) && line.Contains(systemError.LocalizedDescription, StringComparison.Ordinal)),
			timeout: 10000,
			message: $"The toolkit did not trace the system's answer '{systemError.LocalizedDescription}'. Trace: {string.Join(" | ", trace.Lines)}");
	}

#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[Fact]
	public async Task SetCountTracesTheErrorThatTheSystemGivesForTheAuthorizationRequest()
	{
		AddCleanup(ResetBadgeAsync);
		var systemError = await SystemRequestBadgeAuthorizationAsync();
		Skip.If(systemError is null, "This system grants (or already granted) badge authorization to this app, so the error callback of RequestAuthorization does not run here.");
		using var trace = new FilesTraceCapture();

		Badge.SetCount(0);

		await AssertionExtensions.AssertEventually(
			() => trace.Lines.Any(line => line.StartsWith(authorizationError, StringComparison.Ordinal) && line.Contains(systemError.LocalizedDescription, StringComparison.Ordinal)),
			timeout: 10000,
			message: $"The toolkit did not trace the system's answer '{systemError.LocalizedDescription}'. Trace: {string.Join(" | ", trace.Lines)}");
	}

#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[Fact]
	public async Task SetCountTracesNothingWhenTheSystemAnswersTheAuthorizationRequestWithoutAnError()
	{
		AddCleanup(ResetBadgeAsync);
		var systemError = await SystemRequestBadgeAuthorizationAsync();
		Skip.IfNot(systemError is null, $"This system refuses badge authorization to this app ('{systemError?.LocalizedDescription}'), so the toolkit's authorization callback takes its error arm here, which SetCountTracesTheErrorThatTheSystemGivesForTheAuthorizationRequest covers.");
		using var trace = new FilesTraceCapture();

		Badge.SetCount(0);

		// The toolkit's callback has no visible effect when the system answers without an error. The notification center answers an app's
		// authorization requests in the order they were made, so once a request made after the toolkit's is answered, the toolkit's is too.
		Assert.Null(await SystemRequestBadgeAuthorizationAsync());
		Assert.False(trace.Contains(authorizationError), string.Join(Environment.NewLine, trace.Lines));
	}

	[Theory]
#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[InlineData(2147483648u)]
	[InlineData(uint.MaxValue)]
	public async Task SetCountNeverHandsTheSystemANegativeCount(uint count)
	{
		using var trace = new FilesTraceCapture();
		AddCleanup(ResetBadgeAsync);
		var before = await GetBadgeNumberAsync();

		Badge.SetCount(count);

		// The system answers with an error (for example on a Mac where the app may not badge its icon) or shows a new badge.
		await AssertionExtensions.AssertEventuallyAsync(
			async () => trace.Contains(badgeError) || await GetBadgeNumberAsync() != before,
			timeout: 10000,
			message: $"The system did not answer the badge count. Trace: {string.Join(" | ", trace.Lines)}");
		Assert.False(trace.Contains(invalidCount), string.Join(Environment.NewLine, trace.Lines));
	}

#if IOS
	[Category(TestCategory.RequiresUserInteraction)]
#endif
	[Fact]
	public async Task SetCountBelowIOS17SetsTheIconBadgeNumberDirectly()
	{
		Skip.If(OperatingSystem.IsIOSVersionAtLeast(17), "The branch runs below iOS 17 only (and Mac Catalyst reports iOS 17 or later): the simulators of the pool are iOS 27.");
		AddCleanup(ResetBadgeAsync);

		Badge.SetCount(6);

		Assert.Equal(6, await GetBadgeNumberAsync());
	}

	// SetBadgeCount with 0 answers without an error when the system accepts badge counts from this app.
	async Task<bool> AcceptsBadgesAsync()
	{
		AddCleanup(ResetBadgeAsync);
		return await SystemSetBadgeCountAsync(0) is null;
	}

	async Task RequireAcceptedBadgesAsync() =>
		Skip.IfNot(await AcceptsBadgesAsync(), "The system rejects badge counts for this app (UNErrorDomain 1, notifications are not allowed): showing one needs the user's consent, which a headless run cannot give. SetCountTracesTheErrorThatTheSystemGivesForTheBadgeCount covers this system.");

	static Task<NSError?> SystemSetBadgeCountAsync(nint count)
	{
		if (!OperatingSystem.IsIOSVersionAtLeast(16))
		{
			throw new SkipException("UNUserNotificationCenter.SetBadgeCount, which tells whether the system accepts badge counts, needs iOS 16.");
		}

		var answer = new TaskCompletionSource<NSError?>(TaskCreationOptions.RunContinuationsAsynchronously);
		UNUserNotificationCenter.Current.SetBadgeCount(count, error => answer.TrySetResult(error));
		return answer.Task.WaitAsync(TimeSpan.FromSeconds(10));
	}

	// The system answers only once the notification permission prompt that it may show is answered. On iOS the callers are
	// RequiresUserInteraction tests and wait for a person (two minutes). Mac Catalyst runs them headless: there a request that is not
	// answered within 10 seconds is waiting for macOS's permission prompt, which a headless run cannot answer, so the test skips visibly.
	static async Task<NSError?> SystemRequestBadgeAuthorizationAsync()
	{
		var answer = new TaskCompletionSource<NSError?>(TaskCreationOptions.RunContinuationsAsynchronously);
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (_, error) => answer.TrySetResult(error));
#if MACCATALYST
		try
		{
			return await answer.Task.WaitAsync(TimeSpan.FromSeconds(10));
		}
		catch (TimeoutException)
		{
			throw new SkipException("macOS has not answered the badge authorization request after 10 seconds: it is waiting for a person to answer its notification permission prompt for this app, which a headless run cannot do.");
		}
#else
		return await answer.Task.WaitAsync(TimeSpan.FromMinutes(2));
#endif
	}

	static Task<int> GetBadgeNumberAsync() => MainThread.InvokeOnMainThreadAsync(() => (int)UIApplication.SharedApplication.ApplicationIconBadgeNumber);

	static Task AssertBadgeNumberAsync(int expected) =>
		AssertionExtensions.AssertEventuallyAsync(async () => await GetBadgeNumberAsync() == expected, timeout: 10000, message: $"The app icon badge never became {expected}.");

	static async Task ResetBadgeAsync()
	{
		var error = await SystemSetBadgeCountAsync(0);
		if (error is null)
		{
			await AssertionExtensions.AssertEventuallyAsync(async () => await GetBadgeNumberAsync() is 0, timeout: 10000, message: "The app icon badge was not cleared.");
		}
	}
}