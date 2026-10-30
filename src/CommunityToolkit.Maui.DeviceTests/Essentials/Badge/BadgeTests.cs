using CommunityToolkit.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

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