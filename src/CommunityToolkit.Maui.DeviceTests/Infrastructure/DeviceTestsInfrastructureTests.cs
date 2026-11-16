using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;
#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif

namespace CommunityToolkit.Maui.DeviceTests;

// Every run reports two Skipped tests from this class on purpose: RuntimeSkipIsReportedAsSkipped and one row of
// RuntimeSkipAppliesToASingleTheoryRow prove that run-time skips reach the results as Skipped.
[Category(TestCategory.DeviceTestsInfrastructure)]
[Collection(TestCollections.DisableParallelization)]
public class DeviceTestsInfrastructureTests : CommunityToolkitHandlerTestBase
{
	static IServiceProvider? previousRowServices;
	static bool? converterExceptionsSuppressedBeforeFirstRow;

	[Fact]
	public void RunnerUsesThisAppsTestOptions()
	{
		var options = TestServices.Services.GetRequiredService<TestOptions>();

		Assert.Contains(typeof(DeviceTestsInfrastructureTests).Assembly, options.Assemblies);
		Assert.Contains($"Category={TestCategory.RequiresUserInteraction}", options.SkipCategories);
		Assert.True(TestCollections.DeclaresDisableParallelizationCollection(typeof(DeviceTestsInfrastructureTests).Assembly));
	}

	[Fact]
	public void RunnerAppIsThePlatformApplicationWithTheToolkit()
	{
		var platformApplication = IPlatformApplication.Current;

		Assert.NotNull(platformApplication);
		Assert.Same(Application.Current, platformApplication.Application);
		Assert.NotNull(platformApplication.Services.GetService<IPopupService>());
	}

	[Fact]
	public async Task ToolkitHandlerResolvesFromTheTestMauiApp()
	{
		SetupBuilder();

		var handler = await CreateHandlerAsync(new DrawingView());

		Assert.IsType<DrawingViewHandler>(handler);
		Assert.IsType<MauiDrawingView>(handler.PlatformView);
	}

	[Fact]
	public void ToolkitOptionsPassedToSetupBuilderApplyToTheTest()
	{
		var original = Options.ShouldSuppressExceptionsInConverters;

		SetupBuilder(configureToolkitOptions: options => options.SetShouldSuppressExceptionsInConverters(!original));

		Assert.Equal(!original, Options.ShouldSuppressExceptionsInConverters);
	}

	// Order-independent: whichever row runs second checks the row that ran first (other tests of this class may run in between,
	// and they restore what they change too).
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public void EachTestDisposesItsMauiAppAndRestoresToolkitOptions(int row)
	{
		Assert.InRange(row, 1, 2);
		var original = converterExceptionsSuppressedBeforeFirstRow ??= Options.ShouldSuppressExceptionsInConverters;

		if (previousRowServices is not null)
		{
			Assert.Throws<ObjectDisposedException>(() => previousRowServices.GetService(typeof(IMauiHandlersFactory)));
			Assert.Equal(original, Options.ShouldSuppressExceptionsInConverters);
		}

		SetupBuilder(configureToolkitOptions: options => options.SetShouldSuppressExceptionsInConverters(!original));

		Assert.Equal(!original, Options.ShouldSuppressExceptionsInConverters);
		previousRowServices = ApplicationServices;
	}

	[Fact]
	public void EveryTestHasOneFeatureCategory()
	{
		var violations = typeof(TestCategory).GetCategoryRuleViolations(typeof(DeviceTestsInfrastructureTests).Assembly, TestCategory.RequiresUserInteraction);

		Assert.True(violations.Count is 0, string.Join(Environment.NewLine, violations));
	}

	// A misspelled capability name in a CI job would silently skip or require nothing.
	[Fact]
	public void DeclaredCapabilitiesAreValid()
	{
		var problems = DeviceTestCapabilities.GetDeclarationProblems();

		Assert.True(problems.Count is 0, string.Join(Environment.NewLine, problems));
	}

	[Fact]
	public void StaticPropertySnapshotRestoresToolkitOptions()
	{
		var original = Options.ShouldSuppressExceptionsInBehaviors;
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Options)).Capture(typeof(Core.Options));

		MauiApp.CreateBuilder().UseMauiCommunityToolkit(options => options.SetShouldSuppressExceptionsInBehaviors(!original));
		Assert.Equal(!original, Options.ShouldSuppressExceptionsInBehaviors);

		snapshot.Restore();

		Assert.Equal(original, Options.ShouldSuppressExceptionsInBehaviors);
		Assert.True(snapshot.Count > 0);
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

		await AttachAndRun(label, async handler =>
		{
			// OnLoadedAsync returns once the view is loaded, which can come before MAUI raises Loaded (CI run 38100792354, Android API 28),
			// so wait for the counted event as well.
			await loaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
			await label.OnLoadedAsync();

			Assert.True(label.IsLoaded);
			Assert.True(label.IsLoadedOnPlatform());
			Assert.Equal(1, loadedCount);
			Assert.Equal(0, unloadedCount);
		});

		await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
		await label.OnUnloadedAsync();

		Assert.False(label.IsLoadedOnPlatform());
		Assert.Equal(1, loadedCount);
		Assert.Equal(1, unloadedCount);
	}

	// dotnet/maui's PlatformBehaviorTests.BasicLoadUnloadingWorks with a toolkit behavior.
	[Fact]
	public async Task PlatformBehaviorAttachesWhileTheViewIsInTheWindow()
	{
		SetupBuilder();

		var behavior = new CountingIconTintColorBehavior { TintColor = Colors.Red };
		var image = new Image { WidthRequest = 40, HeightRequest = 40 };
		image.Behaviors.Add(behavior);

		Assert.Equal(0, behavior.AttachedCount);

		await AttachAndRun(image, async handler =>
		{
			await image.OnLoadedAsync();

			Assert.Equal(1, behavior.AttachedCount);
			Assert.Equal(0, behavior.DetachedCount);
			Assert.Same(handler.PlatformView, behavior.AttachedPlatformView);
		});

		await image.OnUnloadedAsync();

		Assert.Equal(1, behavior.AttachedCount);
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

	sealed class CountingIconTintColorBehavior : IconTintColorBehavior
	{
		public int AttachedCount { get; private set; }

		public int DetachedCount { get; private set; }

		public PlatformView? AttachedPlatformView { get; private set; }

		protected override void OnAttachedTo(View bindable, PlatformView platformView)
		{
			base.OnAttachedTo(bindable, platformView);
			AttachedCount++;
			AttachedPlatformView = platformView;
		}

		protected override void OnDetachedFrom(View bindable, PlatformView platformView)
		{
			base.OnDetachedFrom(bindable, platformView);
			DetachedCount++;
		}
	}
}