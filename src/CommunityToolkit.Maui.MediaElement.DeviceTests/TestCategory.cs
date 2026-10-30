namespace CommunityToolkit.Maui.DeviceTests;

// Every test class carries [Category(TestCategory.X)]: "TestFilter=Category=X" skips every other constant of this class, so a
// test without a category (or with a string literal) would run in every single-category run.
public static partial class TestCategory
{
	public const string DeviceTestsInfrastructure = nameof(DeviceTestsInfrastructure);

	public const string RequiresUserInteraction = nameof(RequiresUserInteraction);
}