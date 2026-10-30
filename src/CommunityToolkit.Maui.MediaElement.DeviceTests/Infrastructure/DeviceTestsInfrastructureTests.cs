using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// Every run reports two Skipped tests from this class on purpose: RuntimeSkipIsReportedAsSkipped and one row of
// RuntimeSkipAppliesToASingleTheoryRow prove that run-time skips reach the results as Skipped.
[Category(TestCategory.DeviceTestsInfrastructure)]
[Collection(TestCollections.DisableParallelization)]
public class DeviceTestsInfrastructureTests : MediaElementHandlerTestBase
{
	static IServiceProvider? previousRowServices;
	static (bool IsAndroidForegroundServiceEnabled, AndroidViewType DefaultAndroidViewType)? optionsBeforeFirstRow;

	[Fact]
	public void RunnerUsesThisAppsTestOptions()
	{
		var options = TestServices.Services.GetRequiredService<TestOptions>();

		Assert.Contains(typeof(DeviceTestsInfrastructureTests).Assembly, options.Assemblies);
		Assert.Contains($"Category={TestCategory.RequiresUserInteraction}", options.SkipCategories);
		Assert.True(TestCollections.DeclaresDisableParallelizationCollection(typeof(DeviceTestsInfrastructureTests).Assembly));
	}

	[Fact]
	public void RunnerAppIsThePlatformApplicationWithMediaElement()
	{
		var platformApplication = IPlatformApplication.Current;

		Assert.NotNull(platformApplication);
		Assert.Same(Application.Current, platformApplication.Application);
		Assert.Equal(typeof(MediaElementHandler), platformApplication.Services.GetRequiredService<IMauiHandlersFactory>().GetHandlerType(typeof(MediaElement)));
	}

	[Fact]
	public void MediaElementHandlerResolvesFromTheTestMauiApp()
	{
		SetupBuilder();

		Assert.Equal(typeof(MediaElementHandler), MauiContext.Handlers.GetHandlerType(typeof(MediaElement)));
	}

	// Order-independent: whichever row runs second checks the row that ran first (other tests of this class may run in between,
	// and they restore what they change too).
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	public void EachTestDisposesItsMauiAppAndRestoresMediaElementOptions(int row)
	{
		Assert.InRange(row, 1, 2);
		var original = optionsBeforeFirstRow ??= (MediaElementOptions.IsAndroidForegroundServiceEnabled, MediaElementOptions.DefaultAndroidViewType);

		if (previousRowServices is not null)
		{
			Assert.Throws<ObjectDisposedException>(() => previousRowServices.GetService(typeof(IMauiHandlersFactory)));
			Assert.Equal(original, (MediaElementOptions.IsAndroidForegroundServiceEnabled, MediaElementOptions.DefaultAndroidViewType));
		}

		var changedViewType = original.DefaultAndroidViewType is AndroidViewType.SurfaceView ? AndroidViewType.TextureView : AndroidViewType.SurfaceView;
		SetupBuilder(configureMediaElementOptions: options =>
		{
			options.SetIsAndroidForegroundServiceEnabled(!original.IsAndroidForegroundServiceEnabled);
			options.SetDefaultAndroidViewType(changedViewType);
		});

		Assert.Equal((!original.IsAndroidForegroundServiceEnabled, changedViewType), (MediaElementOptions.IsAndroidForegroundServiceEnabled, MediaElementOptions.DefaultAndroidViewType));
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

	// On iOS and Mac Catalyst MauiMediaElement needs a parent Page (or a window in Application.Current.Windows) when its
	// platform view is created, so MediaElement tests host it in a ContentPage.
	[Fact]
	public async Task AttachAndRunHostsMediaElementInTheRealWindow()
	{
		SetupBuilder();

		var mediaElement = new MediaElement { WidthRequest = 200, HeightRequest = 100 };
		var page = new ContentPage { Content = new VerticalStackLayout { mediaElement } };
		int loadedCount = 0, unloadedCount = 0;
		var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var unloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		page.Loaded += (_, _) =>
		{
			loadedCount++;
			loaded.TrySetResult();
		};
		page.Unloaded += (_, _) =>
		{
			unloadedCount++;
			unloaded.TrySetResult();
		};

		await AttachAndRun(page, async _ =>
		{
			// OnLoadedAsync returns once the view is loaded, which can come before MAUI raises Loaded (CI run 38100792354, Android API 28):
			// wait for the counted event as well
			await loaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
			await page.OnLoadedAsync();

			Assert.IsType<MediaElementHandler>(mediaElement.Handler);
			Assert.IsType<MauiMediaElement>(mediaElement.Handler.PlatformView);
			Assert.True(mediaElement.IsLoadedOnPlatform());
			Assert.Equal(1, loadedCount);
		});

		await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(10));
		await page.OnUnloadedAsync();

		Assert.False(mediaElement.IsLoadedOnPlatform());
		Assert.Equal(1, unloadedCount);

		await DisconnectHandlersAsync(mediaElement, page);
	}

	// dotnet/maui's PlatformBehaviorTests.BasicLoadUnloadingWorks on a MediaElement.
	[Fact]
	public async Task PlatformBehaviorAttachesWhileTheViewIsInTheWindow()
	{
		SetupBuilder();

		var behavior = new CountingPlatformBehavior<MediaElement>();
		var mediaElement = new MediaElement { WidthRequest = 200, HeightRequest = 100 };
		mediaElement.Behaviors.Add(behavior);
		var page = new ContentPage { Content = new VerticalStackLayout { mediaElement } };

		Assert.False(behavior.IsAttached);

		await AttachAndRun(page, async _ =>
		{
			await mediaElement.OnLoadedAsync();

			Assert.True(behavior.IsAttached);
			Assert.Equal(1, behavior.AttachedCount);
			Assert.Same(mediaElement.Handler?.PlatformView, behavior.AttachedPlatformView);
		});

		// MAUI tracks the window of MauiMediaElement (a plain UIView on iOS and Mac Catalyst) less precisely than its own views:
		// there, Unloaded (and so the detach) is raised when the handler disconnects rather than when the view leaves the window.
		await DisconnectHandlersAsync(mediaElement, page);
		await AssertionExtensions.AssertEventually(() => !behavior.IsAttached, timeout: 2000, message: "The behavior did not detach.");

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

	// Releases the platform player.
	static Task DisconnectHandlersAsync(MediaElement mediaElement, Page page) =>
		InvokeOnMainThreadAsync(() =>
		{
			mediaElement.Handler?.DisconnectHandler();
			page.Handler?.DisconnectHandler();
		});
}