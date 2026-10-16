using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// Smoke tests of the device-test infrastructure. Every run reports two Skipped tests from this class on purpose:
// RuntimeSkipIsReportedAsSkipped and one row of RuntimeSkipAppliesToASingleTheoryRow prove that run-time skips reach the results as Skipped.
// None of them creates a CameraView platform view: connecting it starts a camera session (and a permission prompt).
[Category(TestCategory.DeviceTestsInfrastructure)]
[Collection(TestCollections.DisableParallelization)]
public class DeviceTestsInfrastructureTests : CameraHandlerTestBase
{
	static IServiceProvider? previousRowServices;

	[Fact]
	public void RunnerUsesThisAppsTestOptions()
	{
		var options = TestServices.Services.GetRequiredService<TestOptions>();

		Assert.Contains(typeof(DeviceTestsInfrastructureTests).Assembly, options.Assemblies);
		Assert.Contains($"Category={TestCategory.RequiresUserInteraction}", options.SkipCategories);
		Assert.True(TestCollections.DeclaresDisableParallelizationCollection(typeof(DeviceTestsInfrastructureTests).Assembly));
	}

	[Fact]
	public void RunnerAppIsThePlatformApplicationWithTheCameraProvider()
	{
		var platformApplication = IPlatformApplication.Current;

		Assert.NotNull(platformApplication);
		Assert.Same(Application.Current, platformApplication.Application);
		Assert.NotNull(platformApplication.Services.GetService<ICameraProvider>());
	}

	// Order-independent: whichever row runs second checks the row that ran first.
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public void EachTestDisposesItsMauiApp(int row)
	{
		Assert.InRange(row, 1, 2);

		if (previousRowServices is not null)
		{
			// The previous row's teardown (HandlerTestBasement.DisposeAsync) disposed its MauiApp.
			Assert.Throws<ObjectDisposedException>(() => previousRowServices.GetService(typeof(IMauiHandlersFactory)));
		}

		SetupBuilder();

		previousRowServices = ApplicationServices;
	}

	[Fact]
	public void EveryTestHasOneFeatureCategory()
	{
		var violations = typeof(TestCategory).GetCategoryRuleViolations(typeof(DeviceTestsInfrastructureTests).Assembly, TestCategory.RequiresUserInteraction);

		Assert.True(violations.Count is 0, string.Join(Environment.NewLine, violations));
	}

	// The capabilities that a CI job declares missing or required (DeviceTestCapabilities) must be ones the tests know: a misspelled name
	// would silently skip or require nothing.
	[Fact]
	public void DeclaredCapabilitiesAreValid()
	{
		var problems = DeviceTestCapabilities.GetDeclarationProblems();

		Assert.True(problems.Count is 0, string.Join(Environment.NewLine, problems));
	}

	// Instantiating the handler runs its ICameraProvider lookup through IPlatformApplication.Current, without creating the
	// platform view.
	[Fact]
	public void CameraViewHandlerResolvesFromTheTestMauiApp()
	{
		SetupBuilder();

		Assert.IsType<CameraViewHandler>(MauiContext.Handlers.GetHandler(typeof(CameraView)));
	}

	[Fact]
	public async Task AttachAndRunHostsTheViewInTheRealWindow()
	{
		SetupBuilder();

		var label = new Label { Text = nameof(AttachAndRunHostsTheViewInTheRealWindow) };
		int loadedCount = 0, unloadedCount = 0;
		var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var unloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		label.Loaded += (_, _) =>
		{
			loadedCount++;
			loaded.TrySetResult();
		};
		label.Unloaded += (_, _) =>
		{
			unloadedCount++;
			unloaded.TrySetResult();
		};

		await AttachAndRun(label, async _ =>
		{
			// OnLoadedAsync returns once the view is loaded, which can come before MAUI raises Loaded (CI run 38100792354, Android API 28):
			// wait for the counted event as well
			await loaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
			await label.OnLoadedAsync();

			Assert.True(label.IsLoaded);
			Assert.True(label.IsLoadedOnPlatform());
			Assert.Equal(1, loadedCount);
		});

		await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
		await label.OnUnloadedAsync();

		Assert.False(label.IsLoadedOnPlatform());
		Assert.Equal(1, unloadedCount);
	}

	// dotnet/maui's PlatformBehaviorTests.BasicLoadUnloadingWorks.
	[Fact]
	public async Task PlatformBehaviorAttachesWhileTheViewIsInTheWindow()
	{
		SetupBuilder();

		var behavior = new CountingPlatformBehavior<Label>();
		var label = new Label { Text = nameof(PlatformBehaviorAttachesWhileTheViewIsInTheWindow) };
		label.Behaviors.Add(behavior);

		Assert.False(behavior.IsAttached);

		await AttachAndRun(label, async handler =>
		{
			await label.OnLoadedAsync();

			Assert.True(behavior.IsAttached);
			Assert.Equal(1, behavior.AttachedCount);
			Assert.Same(handler.PlatformView, behavior.AttachedPlatformView);
		});

		await label.OnUnloadedAsync();

		Assert.False(behavior.IsAttached);
		Assert.Equal(1, behavior.DetachedCount);
	}

	[Fact]
	public void RuntimeSkipIsReportedAsSkipped() =>
		Skip.If(true, "Expected skip: proves that a run-time skip is reported as Skipped, not Passed.");

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RuntimeSkipAppliesToASingleTheoryRow(bool skip)
	{
		Skip.If(skip, "Expected skip: proves that a run-time skip of one theory row leaves the other rows passing.");

		Assert.False(skip);
	}
}