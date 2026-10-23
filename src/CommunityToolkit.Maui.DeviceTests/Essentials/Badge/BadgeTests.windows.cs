using System.Runtime.InteropServices;
using CommunityToolkit.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows offers no way to read the badge back, so these tests assert that every kind of call is accepted.
public partial class BadgeTests
{
	const string unpackagedAppTrace = "Unable to set the badge count: Windows shows badges only for apps with package identity, and this app is unpackaged";

	[Theory]
	[InlineData(0u)]
	[InlineData(1u)]
	[InlineData(99u)]
	[InlineData(100u)]
	[InlineData(uint.MaxValue)]
	public async Task SetCountAcceptsEveryCountOfAPackagedApp(uint count)
	{
		RequirePackageIdentity();
		AddCleanup(() => Badge.SetCount(0));
		using var trace = new FilesTraceCapture();

		await InvokeOnMainThreadAsync(() => Badge.SetCount(count));

		AssertNoUnpackagedAppTrace(trace);
	}

	[Fact]
	public async Task SetCountAcceptsAClearAfterACountAndACountAfterAClear()
	{
		RequirePackageIdentity();
		AddCleanup(() => Badge.SetCount(0));
		using var trace = new FilesTraceCapture();
		var implementation = new BadgeImplementation();

		await InvokeOnMainThreadAsync(() =>
		{
			implementation.SetCount(5);
			implementation.SetCount(0);
			implementation.SetCount(0);
			implementation.SetCount(7);
			Badge.Default.SetCount(8);
		});

		AssertNoUnpackagedAppTrace(trace);
	}

	[Fact]
	public async Task SetCountFromABackgroundThreadIsAccepted()
	{
		RequirePackageIdentity();
		AddCleanup(() => Badge.SetCount(0));
		using var trace = new FilesTraceCapture();

		await Task.Run(() => Badge.SetCount(3));

		AssertNoUnpackagedAppTrace(trace);
	}

	[Theory]
	[InlineData(0u)]
	[InlineData(1u)]
	[InlineData(uint.MaxValue)]
	public async Task SetCountOfAnUnpackagedAppTracesAndDoesNotThrow(uint count)
	{
		Skip.If(HasPackageIdentity(), "This app has package identity, so SetCount sets the badge instead of tracing; run this test unpackaged (eng/devices/windows.ps1 -Packaging unpackaged).");
		using var trace = new FilesTraceCapture();

		await InvokeOnMainThreadAsync(() =>
		{
			new BadgeImplementation().SetCount(count);
			Badge.Default.SetCount(count);
		});
		await Task.Run(() => Badge.SetCount(count));

		var tracedCalls = trace.Lines.Count(line => line.Contains(unpackagedAppTrace, StringComparison.Ordinal));
		Assert.Equal(3, tracedCalls);
	}

	// A packaged app that wrongly took the unpackaged branch would pass the tests of a packaged app without ever reaching BadgeUpdateManager
	static void AssertNoUnpackagedAppTrace(FilesTraceCapture trace) =>
		Assert.False(trace.Contains(unpackagedAppTrace), string.Join(Environment.NewLine, trace.Lines));

	static void RequirePackageIdentity() =>
		Skip.IfNot(HasPackageIdentity(), "BadgeUpdateManager needs package identity; this app runs unpackaged (eng/devices/windows.ps1 -Packaging unpackaged).");

	static bool HasPackageIdentity()
	{
		try
		{
			_ = Windows.ApplicationModel.Package.Current.Id;
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (COMException)
		{
			return false;
		}
	}
}