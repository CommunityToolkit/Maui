namespace CommunityToolkit.Maui.DeviceTests;

// Every test class carries [Category(TestCategory.X)]: "TestFilter=Category=X" skips every other constant of this class, so a
// test without a category (or with a string literal) would run in every single-category run.
// The class is partial so that each top-level test folder declares its constants in its own file (Behaviors/TestCategory.Behaviors.cs).
public static partial class TestCategory
{
	public const string DeviceTestsInfrastructure = nameof(DeviceTestsInfrastructure);

	// Headless runs never execute these tests, which need a person (pickers, permission prompts, hardware): MauiProgram adds this category to
	// every skip list. Run them from the visual runner by launching the app without arguments.
	public const string RequiresUserInteraction = nameof(RequiresUserInteraction);
}