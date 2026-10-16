namespace CommunityToolkit.Maui.DeviceTests;

// "TestFilter=Category=X" skips every other constant of this class: a test without one of them (or with a string literal) would
// run in every single-category run.
public static partial class TestCategory
{
	public const string DeviceTestsInfrastructure = nameof(DeviceTestsInfrastructure);

	public const string RequiresUserInteraction = nameof(RequiresUserInteraction);
}