using CommunityToolkit.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

// Badge on a device. BadgeImplementation has one implementation per platform: Android hands the count to a provider that is chosen by
// the launcher (BadgeFactory), iOS/Mac Catalyst call UNUserNotificationCenter, and Windows updates the app's tile badge. The platform partials
// (BadgeTests.android.cs, .macios.cs, .windows.cs) assert what the system shows or receives; this file holds what is the same everywhere.
[Category(TestCategory.Badge)]
[Collection(TestCollections.DisableParallelization)]
public partial class BadgeTests : FilesTestBase
{
	[Fact]
	public void DefaultIsThePlatformImplementationAndStaysTheSameInstance()
	{
		var badge = Badge.Default;

		Assert.IsType<BadgeImplementation>(badge);
		Assert.Same(badge, Badge.Default);
	}

	[Fact]
	public void StaticSetCountGoesToTheDefaultImplementation()
	{
		var original = Badge.Default;
		var recording = new RecordingBadge();
		AddCleanup(() => Badge.SetDefault(original));

		Badge.SetDefault(recording);
		Badge.SetCount(3);
		Badge.Default.SetCount(0);

		Assert.Same(recording, Badge.Default);
		Assert.Equal([3u, 0u], recording.Counts);
	}

	sealed class RecordingBadge : IBadge
	{
		public List<uint> Counts { get; } = [];

		public void SetCount(uint count) => Counts.Add(count);
	}
}