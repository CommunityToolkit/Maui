using System.Reflection;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Devices;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of StatusBarBehavior (and the BasePlatformBehavior it derives from): the behavior is attached to a page that is hosted in
// the runner's real window, so MAUI raises Loaded/Unloaded and attaches/detaches the PlatformBehavior like in an app. The behavior's
// platform code only exists on Android and iOS; on Mac Catalyst and Windows it is the BasePlatformBehavior and nothing else.
[Category(TestCategory.StatusBarBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class StatusBarBehaviorTests : StatusBarTestBase
{
	[Fact]
	public async Task BehaviorAttachesWhenThePageLoadsAndDetachesWhenItUnloads()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		page.Behaviors.Add(behavior);
		Assert.False(behavior.IsAttached);
		Assert.Equal(0, behavior.AttachedCount);

		await AttachPageAndRun(page, handler =>
		{
			Assert.True(behavior.IsAttached);
			Assert.Equal(1, behavior.AttachedCount);
			Assert.Equal(0, behavior.DetachedCount);
			Assert.Same(handler.PlatformView, behavior.AttachedPlatformView);
			return Task.CompletedTask;
		});
		await WaitForDetachAsync(behavior);

		Assert.False(behavior.IsAttached);
		Assert.Equal(1, behavior.AttachedCount);
		Assert.Equal(1, behavior.DetachedCount);
	}

	[Fact]
	public async Task BehaviorAddedToALoadedPageAttachesImmediately()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();

		await AttachPageAndRun(page, handler =>
		{
			Assert.Equal(0, behavior.AttachedCount);

			page.Behaviors.Add(behavior);

			Assert.True(behavior.IsAttached);
			Assert.Equal(1, behavior.AttachedCount);
			Assert.Same(handler.PlatformView, behavior.AttachedPlatformView);
			return Task.CompletedTask;
		});
		await WaitForDetachAsync(behavior);

		Assert.Equal(1, behavior.DetachedCount);
	}

	[Fact]
	public async Task BehaviorRemovedFromALoadedPageDetaches()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, _ =>
		{
			Assert.True(behavior.IsAttached);

			page.Behaviors.Remove(behavior);

			Assert.False(behavior.IsAttached);
			Assert.Equal(1, behavior.DetachedCount);
			Assert.Null(behavior.AttachedPage);
			return Task.CompletedTask;
		});
		await page.OnUnloadedAsync(TimeSpan.FromSeconds(10));

		Assert.Equal(1, behavior.AttachedCount);
		Assert.Equal(1, behavior.DetachedCount);
	}

	[Fact]
	public async Task BehaviorAttachesAgainWhenThePageIsLoadedAgain()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		page.Behaviors.Add(behavior);

		await AttachPageAndRun(page, static _ => Task.CompletedTask);
		await WaitForDetachAsync(behavior);
		Assert.Equal(1, behavior.AttachedCount);
		Assert.Equal(1, behavior.DetachedCount);

		await AttachPageAndRun(page, _ =>
		{
			Assert.True(behavior.IsAttached);
			Assert.Equal(2, behavior.AttachedCount);
			Assert.Same(page, behavior.AttachedPage);
			return Task.CompletedTask;
		});
		await WaitForDetachAsync(behavior, 2);

		Assert.False(behavior.IsAttached);
		Assert.Equal(2, behavior.DetachedCount);
	}

	[Fact]
	public async Task BehaviorKnowsThePageOnlyWhileAttached()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		page.Behaviors.Add(behavior);
		Assert.Null(behavior.AttachedPage);

		await AttachPageAndRun(page, _ =>
		{
			Assert.Same(page, behavior.AttachedPage);
			return Task.CompletedTask;
		});
		await WaitForDetachAsync(behavior);

		Assert.Null(behavior.AttachedPage);
	}

	[Fact]
	public async Task PagePropertyChangesReachTheBehaviorOnlyWhileAttached()
	{
		SetupBuilder();
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		page.Behaviors.Add(behavior);
		page.Title = "before attach";
		Assert.Empty(behavior.ViewPropertyChanges);

		await AttachPageAndRun(page, _ =>
		{
			page.Title = "while attached";

			Assert.Contains(behavior.ViewPropertyChanges, change => ReferenceEquals(change.Sender, page) && change.PropertyName == nameof(Page.Title));
			return Task.CompletedTask;
		});
		await WaitForDetachAsync(behavior);
		var changesWhileAttached = await InvokeOnMainThreadAsync(() => behavior.ViewPropertyChanges.Count);

		// The page keeps its handler after it left the window: change it on the UI thread like an app would.
		await InvokeOnMainThreadAsync(() => page.Title = "after detach");

		Assert.Equal(changesWhileAttached, await InvokeOnMainThreadAsync(() => behavior.ViewPropertyChanges.Count));
	}

	[Fact]
	public async Task BehaviorAndPageAreCollectedAfterTheBehaviorWasDetached()
	{
		SetupBuilder();

		var (behaviorReference, pageReference, platformViewReference) = await HostAndDetachNewBehaviorAsync();

		await AssertionExtensions.WaitForGC(behaviorReference, pageReference, platformViewReference);
	}

	// Creates the page and the behavior in this method only (no caller local keeps them alive), hosts the page until it unloads and
	// disconnects its handler, like the leak test pattern in the README.
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	async Task<(WeakReference Behavior, WeakReference Page, WeakReference PlatformView)> HostAndDetachNewBehaviorAsync()
	{
		var page = CreatePage();
		var behavior = await CreateBehaviorAsync();
		page.Behaviors.Add(behavior);
		var platformViewReference = new WeakReference(null);

		await AttachPageAndRun(page, handler =>
		{
			Assert.True(behavior.IsAttached);
			platformViewReference.Target = handler.PlatformView;
			return Task.CompletedTask;
		});
		await WaitForDetachAsync(behavior);

		await InvokeOnMainThreadAsync(() =>
		{
			((IElementHandler?)page.Handler)?.DisconnectHandler();
			page.Handler = null;
		});

		return (new WeakReference(behavior), new WeakReference(page), platformViewReference);
	}

	static ContentPage CreatePage() => new() { Content = new Label { Text = nameof(StatusBarBehaviorTests) } };

	// StatusBarBehavior changes the status bar when its properties change, so build it on the UI thread. On Android that can happen before the
	// test resets the window, so the window's original state is recorded first (ResetNativeStateAsync puts it back after the test).
	static Task<RecordingStatusBarBehavior> CreateBehaviorAsync(Action<RecordingStatusBarBehavior>? configure = null) =>
		InvokeOnMainThreadAsync(() =>
		{
#if ANDROID
			AndroidSystemBars.CaptureOriginalState(AndroidSystemBars.ActivityWindow);
#endif
			var behavior = new RecordingStatusBarBehavior();
			configure?.Invoke(behavior);
			return behavior;
		});
}