using System.Reflection;
using Android.Content;
using Android.Content.PM;
using CommunityToolkit.Maui.ApplicationModel;
using AndroidApplication = Android.App.Application;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: BadgeImplementation asks BadgeFactory for the provider of the launcher that resolves the HOME intent, and the default provider
// broadcasts android.intent.action.BADGE_COUNT_UPDATE when a receiver for it is installed. The broadcast is observed with a receiver that
// the test registers, and the receiver that makes the package manager list a listener is the test app's own (disabled by default).
// Run on API 28 and API 36: the launcher lookup and the receiver lookup take different branches from API 33.
public partial class BadgeTests
{
	const string badgeAction = "android.intent.action.BADGE_COUNT_UPDATE";
	const string launchIntentMissing = "Unable to get launch intent component for package";
	const string noBadgeSupport = "Current launcher doesn't support badge counter";

	public BadgeTests()
	{
		// A previous run may have been killed while a component was switched off.
		FilesBadgeComponents.RestoreManifestState();
	}

	static string PackageName => AndroidApplication.Context.PackageName!;

	static string LaunchActivityClassName => AndroidApplication.Context.PackageManager!.GetLaunchIntentForPackage(PackageName)!.Component!.ClassName!;

	// -- BadgeFactory

	[Fact]
	public void GetBadgeProviderReturnsTheDefaultProviderWhenNothingIsRegistered()
	{
		using var isolated = FilesBadgeFactoryState.Isolate();

		var provider = BadgeFactory.GetBadgeProvider();

		Assert.IsType<DefaultBadgeProvider>(provider);
		Assert.Same(provider, BadgeFactory.GetBadgeProvider());
	}

	[Fact]
	public void GetBadgeProviderReturnsTheProviderRegisteredForTheLauncher()
	{
		using var isolated = FilesBadgeFactoryState.Isolate();
		var registered = new RecordingBadgeProvider();

		BadgeFactory.SetBadgeProvider(LauncherTypeOfBadgeFactory(), registered);

		Assert.Same(registered, BadgeFactory.GetBadgeProvider());
	}

	[Fact]
	public void SetBadgeProviderReplacesTheProviderOfTheSameLauncher()
	{
		using var isolated = FilesBadgeFactoryState.Isolate();
		var first = new RecordingBadgeProvider();
		var second = new RecordingBadgeProvider();

		BadgeFactory.SetBadgeProvider(LauncherTypeOfBadgeFactory(), first);
		BadgeFactory.SetBadgeProvider(LauncherTypeOfBadgeFactory(), second);

		Assert.Same(second, BadgeFactory.GetBadgeProvider());
	}

	[Fact]
	public void GetBadgeProviderIgnoresTheProviderOfAnotherLauncher()
	{
		using var isolated = FilesBadgeFactoryState.Isolate();

		BadgeFactory.SetBadgeProvider("com.communitytoolkit.maui.devicetests.other.launcher", new RecordingBadgeProvider());

		Assert.IsType<DefaultBadgeProvider>(BadgeFactory.GetBadgeProvider());
	}

	[Fact]
	public void GetBadgeProviderResolvesTheLauncherWithTheFlagsOfTheAndroidVersion()
	{
		using var isolated = FilesBadgeFactoryState.Isolate();
		var (defaultOnly, unrestricted) = ResolveHomePackages();
		var byDefaultOnly = new RecordingBadgeProvider();
		var byUnrestricted = new RecordingBadgeProvider();
		if (defaultOnly is not null)
		{
			BadgeFactory.SetBadgeProvider(defaultOnly, byDefaultOnly);
		}

		if (unrestricted is not null && unrestricted != defaultOnly)
		{
			BadgeFactory.SetBadgeProvider(unrestricted, byUnrestricted);
		}

		var provider = BadgeFactory.GetBadgeProvider();

		// From Android 13 the toolkit resolves the HOME activity with ResolveInfoFlags.Of(0), before that with MatchDefaultOnly. When the
		// flags give different answers only the right one has a provider here. On the emulators of the pool (API 28 and 36) both flags
		// resolve the same package, so this cannot tell the two flag arguments apart; it does check that the resolved HOME package, not
		// the app's own package (the fallback), is the launcher type.
		RecordingBadgeProvider? expected = OperatingSystem.IsAndroidVersionAtLeast(33)
			? (unrestricted is null ? null : unrestricted == defaultOnly ? byDefaultOnly : byUnrestricted)
			: (defaultOnly is null ? null : byDefaultOnly);
		if (expected is null)
		{
			Assert.IsType<DefaultBadgeProvider>(provider);
		}
		else
		{
			Assert.Same(expected, provider);
		}
	}

	[Fact]
	public void GetBadgeProviderUsesTheOwnPackageWhenNoHomeActivityResolves()
	{
		var (defaultOnly, unrestricted) = ResolveHomePackages();
		var resolved = OperatingSystem.IsAndroidVersionAtLeast(33) ? unrestricted : defaultOnly;
		Skip.IfNot(resolved is null, $"A HOME activity is visible to the app on this device ({resolved}), so BadgeFactory does not fall back to the app's own package name; the fallback needs a device where no HOME activity resolves (the API 28 emulator resolves com.android.launcher3 and the API 36 emulator com.google.android.apps.nexuslauncher).");
		using var isolated = FilesBadgeFactoryState.Isolate();
		var registered = new RecordingBadgeProvider();

		BadgeFactory.SetBadgeProvider(PackageName, registered);

		Assert.Same(registered, BadgeFactory.GetBadgeProvider());
	}

	// From Android 11 package visibility hides the launcher from an app without a <queries> entry for the HOME intent, and the lookup then
	// resolves Settings' FallbackHome (com.android.settings). CommunityToolkit.Maui.Core's AndroidManifest.xml declares that query.
	[Fact]
	public void GetBadgeProviderResolvesTheLauncherThatIsShownOnAndroid11AndLater()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(30), "Package visibility starts with Android 11 (API 30).");

		Assert.NotEqual("com.android.settings", LauncherTypeOfBadgeFactory());
	}

	// -- BadgeImplementation

	[Theory]
	[InlineData(0u)]
	[InlineData(1u)]
	[InlineData(99u)]
	[InlineData(uint.MaxValue)]
	public void SetCountHandsTheCountToTheProviderOfTheLauncher(uint count)
	{
		using var isolated = FilesBadgeFactoryState.Isolate();
		var registered = new RecordingBadgeProvider();
		BadgeFactory.SetBadgeProvider(LauncherTypeOfBadgeFactory(), registered);

		new BadgeImplementation().SetCount(count);
		Badge.Default.SetCount(count);
		Badge.SetCount(count);

		Assert.Equal([count, count, count], registered.Counts);
	}

	[Fact]
	public void SetCountLooksTheProviderUpAgainOnEveryCall()
	{
		using var isolated = FilesBadgeFactoryState.Isolate();
		var late = new RecordingBadgeProvider();
		var implementation = new BadgeImplementation();

		implementation.SetCount(1);
		BadgeFactory.SetBadgeProvider(LauncherTypeOfBadgeFactory(), late);
		implementation.SetCount(2);

		Assert.Equal([2u], late.Counts);
	}

	// -- DefaultBadgeProvider

	[Fact]
	public async Task SetCountDoesNotBroadcastWhenNoReceiverListensForTheBadgeAction()
	{
		using var trace = new FilesTraceCapture();
		var capture = StartCapture();

		new DefaultBadgeProvider().SetCount(7);
		await capture.SettleAsync();

		Assert.Empty(capture.Badges);
		Assert.True(trace.Contains(noBadgeSupport), string.Join(Environment.NewLine, trace.Lines));
	}

	[Theory]
	[InlineData(0u)]
	[InlineData(1u)]
	[InlineData(99u)]
	[InlineData(2147483647u)]
	public async Task SetCountBroadcastsTheCountToTheLauncherWhenAReceiverListens(uint count)
	{
		using var enabled = FilesBadgeComponents.EnableBadgeReceiver();
		using var trace = new FilesTraceCapture();
		var capture = StartCapture();

		new DefaultBadgeProvider().SetCount(count);
		await capture.SettleAsync();

		var broadcast = Assert.Single(capture.Badges);
		Assert.Equal(badgeAction, broadcast.Action);
		Assert.Equal(PackageName, broadcast.GetStringExtra("badge_count_package_name"));
		Assert.Equal(LaunchActivityClassName, broadcast.GetStringExtra("badge_count_class_name"));
		Assert.Equal(count, BadgeCount(broadcast));
		Assert.False(trace.Contains(noBadgeSupport) || trace.Contains(launchIntentMissing), string.Join(Environment.NewLine, trace.Lines));
	}

	// Launchers read the count as an int, so a count above int.MaxValue cannot be handed over as it is; the rule this test expects for it
	// is the largest count a launcher can read (int.MaxValue).
	[Theory]
	[InlineData(7u, 7)]
	[InlineData(2147483648u, int.MaxValue)]
	[InlineData(uint.MaxValue, int.MaxValue)]
	public async Task SetCountBroadcastsTheCountAsAnIntExtra(uint count, int expected)
	{
		using var enabled = FilesBadgeComponents.EnableBadgeReceiver();
		var capture = StartCapture();

		new DefaultBadgeProvider().SetCount(count);
		await capture.SettleAsync();

		Assert.Equal(expected, Assert.Single(capture.Badges).GetIntExtra("badge_count", -1));
	}

	[Fact]
	public async Task SetCountBroadcastsOnceForEveryCall()
	{
		using var enabled = FilesBadgeComponents.EnableBadgeReceiver();
		var capture = StartCapture();
		var provider = new DefaultBadgeProvider();

		provider.SetCount(3);
		provider.SetCount(0);
		provider.SetCount(5);
		await capture.SettleAsync();

		Assert.Equal([3L, 0L, 5L], capture.Badges.Select(BadgeCount));
	}

	[Fact]
	public async Task SetCountReportsAMissingLaunchIntentAndDoesNotBroadcast()
	{
		using var enabled = FilesBadgeComponents.EnableBadgeReceiver();
		using var launcherOff = FilesBadgeComponents.DisableLauncherActivity();
		using var trace = new FilesTraceCapture();
		var capture = StartCapture();
		Assert.Null(AndroidApplication.Context.PackageManager!.GetLaunchIntentForPackage(PackageName));

		new DefaultBadgeProvider().SetCount(7);
		await capture.SettleAsync();

		Assert.Empty(capture.Badges);
		Assert.True(trace.Contains($"{launchIntentMissing} {PackageName}"), string.Join(Environment.NewLine, trace.Lines));
	}

	[Fact]
	public async Task BadgeSetCountReachesTheLauncherThroughTheDefaultProvider()
	{
		using var isolated = FilesBadgeFactoryState.Isolate();
		using var enabled = FilesBadgeComponents.EnableBadgeReceiver();
		var capture = StartCapture();

		Badge.SetCount(42);
		await capture.SettleAsync();

		var broadcast = Assert.Single(capture.Badges);
		Assert.Equal(42L, BadgeCount(broadcast));
		Assert.Equal(PackageName, broadcast.GetStringExtra("badge_count_package_name"));
	}

	// -- helpers

	FilesBadgeBroadcastCapture StartCapture()
	{
		var capture = new FilesBadgeBroadcastCapture();
		AddCleanup(capture.Stop);
		return capture;
	}

	/// <summary>
	/// The launcher type that BadgeFactory resolves (its private GetLauncherType). The lookup tests register their provider under it, so
	/// they test the lookup whatever the resolution returns; the resolution has its own tests.
	/// </summary>
	static string LauncherTypeOfBadgeFactory()
	{
		var method = typeof(BadgeFactory).GetMethod("GetLauncherType", BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException("BadgeFactory.GetLauncherType was not found.");
		return (string?)method.Invoke(null, null) ?? throw new InvalidOperationException("BadgeFactory.GetLauncherType returned null.");
	}

	/// <summary>
	/// The count of a badge broadcast, whatever type the extra has (Bundle.getLong returns the default for an Integer and getInt for a
	/// Long, and logs a type warning), so these tests do not depend on the type, which SetCountBroadcastsTheCountAsAnIntExtra is about.
	/// </summary>
	static long BadgeCount(Intent broadcast)
	{
		Assert.True(broadcast.HasExtra("badge_count"), "The broadcast has no badge_count extra.");
		var asLong = broadcast.GetLongExtra("badge_count", long.MinValue);
		if (asLong != long.MinValue)
		{
			return asLong;
		}

		var asInt = broadcast.GetIntExtra("badge_count", int.MinValue);
		return asInt != int.MinValue ? asInt : throw new InvalidOperationException("The badge_count extra is neither a long nor an int.");
	}

	/// <summary>The package of the HOME activity, resolved with MatchDefaultOnly and with no flags.</summary>
	static (string? DefaultOnly, string? Unrestricted) ResolveHomePackages()
	{
		var packageManager = AndroidApplication.Context.PackageManager!;
		using var home = new Intent(Intent.ActionMain);
		home.AddCategory(Intent.CategoryHome);

		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			return (
				packageManager.ResolveActivity(home, PackageManager.ResolveInfoFlags.Of((long)PackageInfoFlags.MatchDefaultOnly))?.ActivityInfo?.PackageName,
				packageManager.ResolveActivity(home, PackageManager.ResolveInfoFlags.Of(0))?.ActivityInfo?.PackageName);
		}

		return (
			packageManager.ResolveActivity(home, PackageInfoFlags.MatchDefaultOnly)?.ActivityInfo?.PackageName,
			packageManager.ResolveActivity(home, 0)?.ActivityInfo?.PackageName);
	}

	sealed class RecordingBadgeProvider : IBadgeProvider
	{
		public List<uint> Counts { get; } = [];

		public void SetCount(uint count) => Counts.Add(count);
	}

	/// <summary>BadgeFactory keeps its providers in a private static table with no way to remove one; a test borrows the table and gives it back.</summary>
	sealed class FilesBadgeFactoryState : IDisposable
	{
		readonly Dictionary<string, IBadgeProvider> table;
		readonly Dictionary<string, IBadgeProvider> original;

		FilesBadgeFactoryState()
		{
			table = (Dictionary<string, IBadgeProvider>)typeof(BadgeFactory).GetField("providers", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
			original = new Dictionary<string, IBadgeProvider>(table);
			table.Clear();
		}

		public static FilesBadgeFactoryState Isolate() => new();

		public void Dispose()
		{
			table.Clear();
			foreach (var (launcher, provider) in original)
			{
				table[launcher] = provider;
			}
		}
	}
}