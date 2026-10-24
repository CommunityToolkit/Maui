using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class StatusBarBehaviorTests
{
	[Fact]
	public async Task AttachedBehaviorAppliesItsStyleWhenItIsAppliedOnAttach()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarStyle = StatusBarStyle.DarkContent);
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);
		await AssertStatusBarStyleAsync(StatusBarStyle.Default);

		await AttachPageAndRun(page, _ => AssertStatusBarStyleAsync(StatusBarStyle.DarkContent));
	}

	[Fact]
	public async Task ApplyOnPageNavigatedToAppliesTheStyleOnlyAfterTheNavigation()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior =>
		{
			behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;
			behavior.StatusBarStyle = StatusBarStyle.DarkContent;
		});
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, async _ =>
		{
			await AssertStatusBarStyleAsync(StatusBarStyle.Default);

			StatusBarEventRaisers.RaiseNavigatedTo(page);

			await AssertStatusBarStyleAsync(StatusBarStyle.DarkContent);
		});
	}

	[Fact]
	public async Task StyleChangeWhileAttachedUpdatesTheStatusBarStyle()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, async _ =>
		{
			behavior.StatusBarStyle = StatusBarStyle.DarkContent;
			await AssertStatusBarStyleAsync(StatusBarStyle.DarkContent);

			behavior.StatusBarStyle = StatusBarStyle.LightContent;
			await AssertStatusBarStyleAsync(StatusBarStyle.LightContent);

			behavior.StatusBarStyle = StatusBarStyle.DarkContent;
			behavior.StatusBarStyle = StatusBarStyle.Default;
			await AssertStatusBarStyleAsync(StatusBarStyle.Default);
		});
	}

	[Fact]
	public async Task ApplyOnChangeDoesNotApplyTheStyleAgain()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarStyle = StatusBarStyle.DarkContent);
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, async _ =>
		{
			await AssertStatusBarStyleAsync(StatusBarStyle.DarkContent);
			StatusBar.SetStyle(StatusBarStyle.LightContent);
			await AssertStatusBarStyleAsync(StatusBarStyle.LightContent);

			behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;

			await AssertStatusBarStyleAsync(StatusBarStyle.LightContent);
		});
	}

	[Fact]
	public async Task PropertyChangesWithoutCurrentActivityDoNothing()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		// Without a current activity StatusBar would throw InvalidOperationException, so the behavior must skip it.
		await InvokeOnMainThreadAsync(() => AndroidSystemBars.WithoutCurrentActivity(() =>
		{
			behavior.StatusBarColor = Colors.Red;
			behavior.StatusBarStyle = StatusBarStyle.DarkContent;
			return true;
		}));

		await AssertStatusBarUntouchedAsync();
	}

	[Fact]
	public async Task DisplayInfoChangeResizesTheOverlayOnlyWhileTheBehaviorIsAttached()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "The status bar overlay (and the behavior's UpdateBarSize call) exist from Android 15 (API 35); this device is older.");
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarColor = Colors.Red);
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, async _ =>
		{
			await AssertStatusBarShowsAsync(Colors.Red);
			ShrinkOverlay();

			StatusBarEventRaisers.RaiseMainDisplayInfoChanged();

			Assert.Equal(ExpectedOverlayHeight(AndroidSystemBars.ActivityWindow), OverlayHeight());
		});
		await WaitForDetachAsync(behavior);
		await InvokeOnMainThreadAsync(static () =>
		{
			ShrinkOverlay();
			StatusBarEventRaisers.RaiseMainDisplayInfoChanged();

			Assert.Equal(1, OverlayHeight());
		});
	}

	[Fact]
	public async Task DisplayInfoChangeBeforeAndroid15LeavesTheStatusBarAlone()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(35), "Android 15 and later resize the overlay (see DisplayInfoChangeResizesTheOverlayOnlyWhileTheBehaviorIsAttached).");
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarColor = Colors.Red);
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, async _ =>
		{
			await AssertStatusBarShowsAsync(Colors.Red);
			// Before Android 15 there is no overlay and StatusBar would throw NotSupportedException when asked to size one:
			// the behavior must not ask. A view with the overlay's tag makes the call observable.
			var overlay = new Android.Views.View(AndroidSystemBars.Activity) { Tag = AndroidSystemBars.StatusBarOverlayTag, LayoutParameters = new Android.Widget.FrameLayout.LayoutParams(1, 1) };
			AndroidSystemBars.Decor(AndroidSystemBars.ActivityWindow).AddView(overlay);

			StatusBarEventRaisers.RaiseMainDisplayInfoChanged();

			Assert.Equal(1, overlay.LayoutParameters?.Height);
			AndroidSystemBars.Decor(AndroidSystemBars.ActivityWindow).RemoveView(overlay);
			await AssertStatusBarShowsAsync(Colors.Red);
		});
	}

	static void ShrinkOverlay()
	{
		var overlay = AndroidSystemBars.FindOverlay(AndroidSystemBars.ActivityWindow, AndroidSystemBars.StatusBarOverlayTag) ?? throw new InvalidOperationException("No status bar overlay.");
		Assert.NotNull(overlay.LayoutParameters);
		overlay.LayoutParameters.Height = 1;
	}

	static int? OverlayHeight() => AndroidSystemBars.FindOverlay(AndroidSystemBars.ActivityWindow, AndroidSystemBars.StatusBarOverlayTag)?.LayoutParameters?.Height;
}