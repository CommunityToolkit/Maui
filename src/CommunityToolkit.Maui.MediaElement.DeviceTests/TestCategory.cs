namespace CommunityToolkit.Maui.DeviceTests;

// Every test class carries [Category(TestCategory.X)]: "TestFilter=Category=X" skips every other constant of this class, so a
// test without a category (or with a string literal) would run in every single-category run.
// The class is partial: add a feature's constant to the TestCategory.<Folder>.cs partial of the top-level folder of its tests
// (MediaElement/TestCategory.MediaElement.cs), e.g.
//     public static partial class TestCategory { public const string MediaElement = nameof(MediaElement); }
public static partial class TestCategory
{
	// Smoke tests of the device-test infrastructure itself (runner, hosting, run-time skips).
	public const string DeviceTestsInfrastructure = nameof(DeviceTestsInfrastructure);

	// Tests that need a person (pickers, permission prompts, hardware). Headless runs never execute them (MauiProgram adds this
	// category to every skip list); run them from the visual runner by launching the app without arguments.
	public const string RequiresUserInteraction = nameof(RequiresUserInteraction);
}