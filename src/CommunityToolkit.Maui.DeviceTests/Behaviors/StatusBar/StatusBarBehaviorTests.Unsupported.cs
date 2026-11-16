#if MACCATALYST || WINDOWS
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Devices;

namespace CommunityToolkit.Maui.DeviceTests;

// On Mac Catalyst and Windows StatusBarBehavior is the BasePlatformBehavior and nothing else (its attach, detach and property-change
// overrides are not compiled). StatusBar throws NotSupportedException on these platforms, so every test below fails if the behavior
// reaches StatusBar.
public partial class StatusBarBehaviorTests
{
	[Fact]
	public async Task ChangingThePropertiesOfTheBehaviorDoesNotReachTheStatusBar()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			behavior.StatusBarColor = Colors.Red;
			behavior.StatusBarStyle = StatusBarStyle.DarkContent;
			behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;
			behavior.RaisePropertyChanged(nameof(StatusBarBehavior.StatusBarColor));
			behavior.RaisePropertyChanged(nameof(StatusBarBehavior.StatusBarStyle));
			behavior.RaisePropertyChanged(nameof(VisualElement.Width));
			behavior.RaisePropertyChanged(null);
		});

		Assert.Equal(Colors.Red, behavior.StatusBarColor);
		Assert.Equal(StatusBarStyle.DarkContent, behavior.StatusBarStyle);
		Assert.Equal(StatusBarApplyOn.OnPageNavigatedTo, behavior.ApplyOn);
	}

	[Theory]
	[InlineData(StatusBarApplyOn.OnBehaviorAttachedTo)]
	[InlineData(StatusBarApplyOn.OnPageNavigatedTo)]
	public async Task AttachedBehaviorDoesNotReachTheStatusBarOnAttachNavigationOrDisplayChange(StatusBarApplyOn applyOn)
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		await InvokeOnMainThreadAsync(() =>
		{
			behavior.StatusBarColor = Colors.Red;
			behavior.StatusBarStyle = StatusBarStyle.DarkContent;
			behavior.ApplyOn = applyOn;
		});
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, _ =>
		{
			Assert.True(behavior.IsAttached);

			StatusBarEventRaisers.RaiseNavigatedTo(page);
			StatusBarEventRaisers.RaiseMainDisplayInfoChanged();

			Assert.True(behavior.IsAttached);
			return Task.CompletedTask;
		});
		await WaitForDetachAsync(behavior);

		Assert.False(behavior.IsAttached);
		Assert.Equal(1, behavior.AttachedCount);
		Assert.Equal(1, behavior.DetachedCount);
	}

	[Fact]
	public async Task NavigatedToHandlerDoesNotReachTheStatusBarWhenTheColorIsAppliedOnAttach()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync(static behavior =>
		{
			behavior.ApplyOn = StatusBarApplyOn.OnBehaviorAttachedTo;
			behavior.StatusBarColor = Colors.Red;
		});

		// Nothing subscribes this handler on this platform, so it is called directly: with ApplyOn = OnBehaviorAttachedTo it must not call StatusBar.
		var exception = await InvokeOnMainThreadAsync(() =>
			Record.Exception(() => StatusBarEventRaisers.InvokeBehaviorMember(behavior, "OnPageNavigatedTo", null, StatusBarEventRaisers.CreateNavigatedToEventArgs())));

		Assert.Null(exception);
	}

	[Fact]
	public async Task NavigatedToHandlerReachesTheStatusBarWhenTheColorIsAppliedOnNavigation()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync(static behavior => behavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo);

		var exception = await InvokeOnMainThreadAsync(() =>
			Assert.Throws<NotSupportedException>(() => StatusBarEventRaisers.InvokeBehaviorMember(behavior, "OnPageNavigatedTo", null, StatusBarEventRaisers.CreateNavigatedToEventArgs())));

		Assert.Equal(unsupportedStatusBarMessage, exception.Message);
	}

	[Fact]
	public async Task DisplayInfoChangedHandlerThrowsNotSupportedException()
	{
		SetupBuilder();
		var behavior = await CreateBehaviorAsync();

		// Nothing subscribes this handler on this platform either; its body is the defensive "not supported" arm.
		var exception = await InvokeOnMainThreadAsync(() =>
			Assert.Throws<NotSupportedException>(() => StatusBarEventRaisers.InvokeBehaviorMember(behavior, "OnMainDisplayInfoChanged", null, new DisplayInfoChangedEventArgs(DeviceDisplay.Current.MainDisplayInfo))));

		Assert.Equal("StatusBarBehavior is not supported on the current platform.", exception.Message);
	}
}
#endif