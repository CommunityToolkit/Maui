using System.Runtime.InteropServices;
using CommunityToolkit.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: BadgeImplementation updates the badge of the app's tile and taskbar button through BadgeUpdateManager: a count of zero clears it,
// any other count sends a "badgeNumber" notification. Windows offers no way to read the badge back, so these tests assert that every kind of
// call is accepted. BadgeUpdateManager needs package identity: an unpackaged app (the CI's default) skips them visibly.
public partial class BadgeTests
{
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

		await InvokeOnMainThreadAsync(() => Badge.SetCount(count));
	}

	[Fact]
	public async Task SetCountAcceptsAClearAfterACountAndACountAfterAClear()
	{
		RequirePackageIdentity();
		AddCleanup(() => Badge.SetCount(0));
		var implementation = new BadgeImplementation();

		await InvokeOnMainThreadAsync(() =>
		{
			implementation.SetCount(5);
			implementation.SetCount(0);
			implementation.SetCount(0);
			implementation.SetCount(7);
			Badge.Default.SetCount(8);
		});
	}

	[Fact]
	public async Task SetCountFromABackgroundThreadIsAccepted()
	{
		RequirePackageIdentity();
		AddCleanup(() => Badge.SetCount(0));

		await Task.Run(() => Badge.SetCount(3));
	}

	static void RequirePackageIdentity()
	{
		bool packaged;
		try
		{
			_ = Windows.ApplicationModel.Package.Current.Id;
			packaged = true;
		}
		catch (InvalidOperationException)
		{
			packaged = false;
		}
		catch (COMException)
		{
			packaged = false;
		}

		Skip.IfNot(packaged, "BadgeUpdateManager needs package identity; this app runs unpackaged (eng/devices/windows.ps1 -Packaging unpackaged).");
	}
}