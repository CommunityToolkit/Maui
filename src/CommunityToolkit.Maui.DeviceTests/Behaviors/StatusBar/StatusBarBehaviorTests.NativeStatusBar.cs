#if ANDROID || IOS
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using Microsoft.Maui.Devices;

namespace CommunityToolkit.Maui.DeviceTests;

// StatusBarBehavior's platform code exists on Android and iOS only, and both implementations behave the same way, so these tests read
// the native status bar through the accessors of the platform partials.
public partial class StatusBarBehaviorTests
{
	[Fact]
	public async Task CreatingTheBehaviorWithDefaultPropertiesDoesNotTouchTheStatusBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await CreateBehaviorAsync();

		await AssertStatusBarUntouchedAsync();
	}

	[Fact]
	public async Task AttachedBehaviorShowsItsColorWhenItIsAppliedOnAttach()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarColor = Colors.Red);
		await ResetNativeStateAsync(); // setting the color already changed the status bar: start from a clean one
		page.Behaviors.Add(behavior);
		await AssertStatusBarUntouchedAsync(); // the page is not in a window yet, so the behavior is not attached

		await AttachPageAndRun(page, _ => AssertStatusBarShowsAsync(Colors.Red));
	}

	[Fact]
	public async Task ApplyOnPageNavigatedToShowsTheColorOnlyAfterTheNavigation()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior =>
		{
			behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;
			behavior.StatusBarColor = Colors.Red;
		});
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, async _ =>
		{
			await AssertStatusBarColorNotAppliedAsync();

			StatusBarEventRaisers.RaiseNavigatedTo(page);

			await AssertStatusBarShowsAsync(Colors.Red);
		});
	}

	[Fact]
	public async Task NavigationDoesNotApplyTheColorAgainWhenItIsAppliedOnAttach()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarColor = Colors.Red);
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, async _ =>
		{
			await AssertStatusBarShowsAsync(Colors.Red);
			StatusBar.SetColor(Colors.Green);
			await AssertStatusBarShowsAsync(Colors.Green);

			StatusBarEventRaisers.RaiseNavigatedTo(page);

			await AssertStatusBarShowsAsync(Colors.Green);
		});
	}

	[Fact]
	public async Task NavigationAfterTheBehaviorDetachedDoesNotApplyTheColor()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior =>
		{
			behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;
			behavior.StatusBarColor = Colors.Red;
		});
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);
		await AttachPageAndRun(page, static _ => Task.CompletedTask);
		await WaitForDetachAsync(behavior);
		Assert.False(behavior.IsAttached);

		await InvokeOnMainThreadAsync(() => StatusBarEventRaisers.RaiseNavigatedTo(page));

		await AssertStatusBarColorNotAppliedAsync();
	}

	[Fact]
	public async Task ColorChangeWhileAttachedUpdatesTheStatusBar()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, async _ =>
		{
			behavior.StatusBarColor = Colors.Blue;
			await AssertStatusBarShowsAsync(Colors.Blue);

			behavior.StatusBarColor = Colors.Green;
			await AssertStatusBarShowsAsync(Colors.Green);

			behavior.StatusBarColor = Colors.Transparent;
			await AssertStatusBarShowsAsync(Colors.Transparent);
		});
	}

	[Fact]
	public async Task ColorChangeUpdatesTheStatusBarEvenWhileTheBehaviorIsNotAttached()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await ResetNativeStateAsync();

		// The behavior forwards its property changes to the status bar even though it was never attached (the unit tests rely on it too).
		await InvokeOnMainThreadAsync(() => behavior.StatusBarColor = Colors.Red);
		await AssertStatusBarShowsAsync(Colors.Red);

		page.Behaviors.Add(behavior);
		await AttachPageAndRun(page, static _ => Task.CompletedTask);
		await WaitForDetachAsync(behavior);
		Assert.False(behavior.IsAttached);
		await InvokeOnMainThreadAsync(() => behavior.StatusBarColor = Colors.Blue);
		await AssertStatusBarShowsAsync(Colors.Blue);
	}

	[Fact]
	public async Task ApplyOnChangeDoesNotTouchTheStatusBar()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarColor = Colors.Red);
		await ResetNativeStateAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, async _ =>
		{
			await AssertStatusBarShowsAsync(Colors.Red);
			StatusBar.SetColor(Colors.Green);
			await AssertStatusBarShowsAsync(Colors.Green);

			behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;
			behavior.ApplyOn = StatusBarApplyOn.OnBehaviorAttachedTo;

			await AssertStatusBarShowsAsync(Colors.Green);
		});
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task PropertyChangedWithEmptyPropertyNameDoesNotTouchTheStatusBar(string propertyName)
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarColor = Colors.Red);
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() => behavior.RaisePropertyChanged(propertyName));

		await AssertStatusBarUntouchedAsync();
	}

	[Fact]
	public async Task PropertyChangedWithoutPropertyNameDoesNotTouchTheStatusBar()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarColor = Colors.Red);
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() => behavior.RaisePropertyChanged(null));

		await AssertStatusBarUntouchedAsync();
	}

	[Theory]
	[InlineData(nameof(VisualElement.Width))]
	[InlineData(nameof(VisualElement.Height))]
	public async Task PropertyChangedForWidthOrHeightAppliesTheColor(string propertyName)
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.StatusBarColor = Colors.Red);
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() => behavior.RaisePropertyChanged(propertyName));

		await AssertStatusBarShowsAsync(Colors.Red);
	}

	[Fact]
	public async Task PropertyChangedForAnotherPropertyDoesNotTouchTheStatusBar()
	{
		SetupBuilder();
		// A color and a style that both leave a trace when applied (the style turns the status bar icons dark where the platform shows it).
		var behavior = await CreateBehaviorAsync(static behavior =>
		{
			behavior.StatusBarColor = Colors.Red;
			behavior.StatusBarStyle = StatusBarStyle.DarkContent;
		});
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() => behavior.RaisePropertyChanged(nameof(VisualElement.IsVisible)));

		await AssertStatusBarUntouchedAsync();
	}
}
#endif