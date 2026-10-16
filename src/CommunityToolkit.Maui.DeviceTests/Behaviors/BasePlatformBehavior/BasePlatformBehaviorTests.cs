#if IOS || MACCATALYST
using MismatchedPlatformView = UIKit.UIButton;
using PlatformView = UIKit.UIView;
#elif ANDROID
using MismatchedPlatformView = Android.Widget.ImageView;
using PlatformView = Android.Views.View;
#elif WINDOWS
using MismatchedPlatformView = Microsoft.UI.Xaml.Controls.Button;
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#endif
using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of BasePlatformBehavior<TView, TPlatformView>, the base class of the toolkit's platform behaviors (StatusBarBehavior,
// IconTintColorBehavior, TouchBehavior, ...), with probe behaviors on a Label: MAUI attaches a PlatformBehavior when its view loads
// (and only if the view's platform view has the behavior's TPlatformView type), the base class then tracks the view and forwards the
// view's property changes to OnViewPropertyChanged until the behavior detaches.
[Category(TestCategory.BasePlatformBehavior)]
[Collection(TestCollections.DisableParallelization)]
public class BasePlatformBehaviorTests : StatusBarTestBase
{
	// The device tests share a busy machine: give a view generously long to raise Loaded.
	static readonly TimeSpan loadTimeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task BehaviorAttachesWhenTheViewLoadsAndDetachesWhenItUnloads()
	{
		SetupBuilder();
		var label = new Label { Text = "probe" };
		var behavior = new ProbeBehavior();
		label.Behaviors.Add(behavior);
		Assert.Equal(0, behavior.AttachedCount);
		Assert.Null(behavior.TrackedView);

		await AttachViewAndRun(label, async handler =>
		{
			await label.OnLoadedAsync(loadTimeout);

			Assert.True(behavior.IsAttached);
			Assert.Equal(1, behavior.AttachedCount);
			Assert.Same(handler.PlatformView, behavior.AttachedPlatformView);
			Assert.Same(label, behavior.TrackedView);
		});
		await WaitForDetachAsync(behavior);

		Assert.False(behavior.IsAttached);
		Assert.Equal(1, behavior.AttachedCount);
		Assert.Equal(1, behavior.DetachedCount);
		Assert.Null(behavior.TrackedView);
	}

	[Fact]
	public async Task BehaviorAddedToALoadedViewAttachesImmediatelyAndDetachesWhenItIsRemoved()
	{
		SetupBuilder();
		var label = new Label { Text = "probe" };
		var behavior = new ProbeBehavior();

		await AttachViewAndRun(label, async _ =>
		{
			await label.OnLoadedAsync(loadTimeout);
			Assert.False(behavior.IsAttached);

			label.Behaviors.Add(behavior);

			Assert.True(behavior.IsAttached);
			Assert.Same(label, behavior.TrackedView);

			label.Behaviors.Remove(behavior);

			Assert.False(behavior.IsAttached);
			Assert.Equal(1, behavior.DetachedCount);
			Assert.Null(behavior.TrackedView);
		});
	}

	[Fact]
	public async Task ViewPropertyChangesAreForwardedOnlyWhileTheBehaviorIsAttached()
	{
		SetupBuilder();
		var label = new Label { Text = "probe" };
		var behavior = new ProbeBehavior();
		label.Behaviors.Add(behavior);
		label.Text = "before attach";
		Assert.Empty(behavior.ViewPropertyChanges);

		await AttachViewAndRun(label, async _ =>
		{
			await label.OnLoadedAsync(loadTimeout);

			label.Text = "while attached";

			var change = Assert.Single(behavior.ViewPropertyChanges, static change => change.PropertyName == nameof(Label.Text));
			Assert.Same(label, change.Sender);
		});
		await WaitForDetachAsync(behavior);
		var changesWhileAttached = await InvokeOnMainThreadAsync(() => behavior.ViewPropertyChanges.Count);

		// The label keeps its handler after it left the window: change it on the UI thread like an app would.
		await InvokeOnMainThreadAsync(() => label.Text = "after detach");

		Assert.Equal(changesWhileAttached, await InvokeOnMainThreadAsync(() => behavior.ViewPropertyChanges.Count));
	}

	[Fact]
	public async Task ViewPropertyChangeAfterTheBehaviorWasRemovedIsNotForwarded()
	{
		SetupBuilder();
		var label = new Label { Text = "probe" };
		var behavior = new ProbeBehavior();
		label.Behaviors.Add(behavior);

		await AttachViewAndRun(label, async _ =>
		{
			await label.OnLoadedAsync(loadTimeout);
			label.Behaviors.Remove(behavior);
			var changesBeforeRemoval = behavior.ViewPropertyChanges.Count;

			label.Text = "after removal";

			Assert.Equal(changesBeforeRemoval, behavior.ViewPropertyChanges.Count);
		});
	}

	[Fact]
	public async Task ExceptionThrownByOnViewPropertyChangedReachesTheCodeThatChangedTheProperty()
	{
		SetupBuilder();
		var label = new Label { Text = "probe" };
		var behavior = new ProbeBehavior { ExceptionToThrow = new InvalidOperationException("probe failure") };
		label.Behaviors.Add(behavior);

		await AttachViewAndRun(label, async _ =>
		{
			await label.OnLoadedAsync(loadTimeout);

			var exception = Assert.Throws<InvalidOperationException>(() => label.Text = "trigger");

			Assert.Equal("probe failure", exception.Message);
			Assert.Equal("trigger", label.Text);
		});
	}

	[Fact]
	public async Task ExceptionThrownByOnViewPropertyChangedIsTracedWhenExceptionsInBehaviorsAreSuppressed()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldSuppressExceptionsInBehaviors(true));
		var label = new Label { Text = "probe" };
		var behavior = new ProbeBehavior { ExceptionToThrow = new InvalidOperationException("probe failure") };
		label.Behaviors.Add(behavior);

		await AttachViewAndRun(label, async _ =>
		{
			await label.OnLoadedAsync(loadTimeout);

			var listener = CapturingTraceListener.Capture(() => label.Text = "trigger");

			Assert.Equal("trigger", label.Text);
			Assert.Contains(listener.Messages, static message => message.Contains("probe failure", StringComparison.Ordinal));
			Assert.Contains(behavior.ViewPropertyChanges, static change => change.PropertyName == nameof(Label.Text));
		});
	}

	[Fact]
	public async Task BehaviorInterfaceExposesTheViewOnlyWhileAttached()
	{
		SetupBuilder();
		var label = new Label { Text = "probe" };
		var behavior = new ProbeBehavior();
		label.Behaviors.Add(behavior);
		Assert.Null(ViewOfBehaviorInterface(behavior));

		await AttachViewAndRun(label, async _ =>
		{
			await label.OnLoadedAsync(loadTimeout);

			Assert.Same(label, ViewOfBehaviorInterface(behavior));
		});
		await WaitForDetachAsync(behavior);

		Assert.Null(ViewOfBehaviorInterface(behavior));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ViewPropertyChangeOfAnotherElementThrowsArgumentExceptionEvenWhenExceptionsInBehaviorsAreSuppressed(bool suppressExceptionsInBehaviors)
	{
		SetupBuilder(configureToolkitOptions: options => options.SetShouldSuppressExceptionsInBehaviors(suppressExceptionsInBehaviors));
		var label = new Label { Text = "probe" };
		var behavior = new ProbeBehavior();
		label.Behaviors.Add(behavior);

		await AttachViewAndRun(label, async _ =>
		{
			await label.OnLoadedAsync(loadTimeout);
			var changesBefore = behavior.ViewPropertyChanges.Count;

			// The behavior listens to the label's PropertyChanged event; an event that names another element as its sender is rejected before
			// the (suppressible) OnViewPropertyChanged call.
			var exception = Assert.Throws<TargetInvocationException>(() => RaiseViewPropertyChanged(behavior, new Button(), nameof(Label.Text)));

			var argumentException = Assert.IsType<ArgumentException>(exception.InnerException);
			Assert.Equal($"Behavior can only be attached to {typeof(Label)}", argumentException.Message);
			Assert.Equal(changesBefore, behavior.ViewPropertyChanges.Count);
		});
	}

	[Fact]
	public async Task BehaviorForAnotherPlatformViewTypeNeverAttaches()
	{
		SetupBuilder();
		var label = new Label { Text = "probe" };
		var behavior = new MismatchedBehavior();
		label.Behaviors.Add(behavior);

		await AttachViewAndRun(label, async handler =>
		{
			await label.OnLoadedAsync(loadTimeout);
			Assert.IsNotType<MismatchedPlatformView>(handler.PlatformView, exactMatch: false);

			label.Text = "while loaded";

			Assert.Equal(0, behavior.AttachedCount);
			Assert.Null(behavior.TrackedView);
			Assert.Empty(behavior.ViewPropertyChanges);
		});
		await label.OnUnloadedAsync(loadTimeout);

		Assert.Equal(0, behavior.AttachedCount);
		Assert.Equal(0, behavior.DetachedCount);
	}

	[Fact]
	public async Task BehaviorLabelAndPlatformViewAreCollectedAfterTheBehaviorWasDetached()
	{
		SetupBuilder();

		var (behaviorReference, labelReference, platformViewReference) = await HostAndDetachNewBehaviorAsync();

		await AssertionExtensions.WaitForGC(behaviorReference, labelReference, platformViewReference);
	}

	// Creates the label and the behavior in this method only (no caller local keeps them alive), hosts the label until it unloads and
	// disconnects its handler, like the leak test pattern in the README.
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	async Task<(WeakReference Behavior, WeakReference Label, WeakReference PlatformView)> HostAndDetachNewBehaviorAsync()
	{
		var label = new Label { Text = "probe" };
		var behavior = new ProbeBehavior();
		label.Behaviors.Add(behavior);
		var platformViewReference = new WeakReference(null);

		await AttachViewAndRun(label, async handler =>
		{
			await label.OnLoadedAsync(loadTimeout);
			Assert.True(behavior.IsAttached);
			platformViewReference.Target = handler.PlatformView;
		});
		await WaitForDetachAsync(behavior);

		await InvokeOnMainThreadAsync(() =>
		{
			((IElementHandler?)label.Handler)?.DisconnectHandler();
			label.Handler = null;
		});

		return (new WeakReference(behavior), new WeakReference(label), platformViewReference);
	}

	// MAUI detaches the probe from the label's Unloaded event on the UI thread; the label's unloaded state flips before that handler ran.
	Task WaitForDetachAsync(ProbeBehavior behavior, int detachedCount = 1) =>
		AssertEventuallyOnMainThread(() => behavior.DetachedCount >= detachedCount && !behavior.IsAttached, $"The behavior was not detached {detachedCount} time(s) (DetachedCount = {behavior.DetachedCount}, IsAttached = {behavior.IsAttached}).");

	// ICommunityToolkitBehavior<TView>.View and .OnViewPropertyChanged(object, ...) are protected interface members (the behaviors, not their
	// users, call them), so the tests reach them through reflection.
	static object? ViewOfBehaviorInterface(ProbeBehavior behavior) =>
		(typeof(ICommunityToolkitBehavior<Label>).GetProperty("View", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("ICommunityToolkitBehavior<TView> no longer has a protected View property."))
		.GetValue(behavior);

	static void RaiseViewPropertyChanged(ProbeBehavior behavior, object sender, string propertyName) =>
		(typeof(ICommunityToolkitBehavior<Label>).GetMethod("OnViewPropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(object), typeof(PropertyChangedEventArgs)])
			?? throw new InvalidOperationException("ICommunityToolkitBehavior<TView> no longer has a protected OnViewPropertyChanged(object, PropertyChangedEventArgs)."))
		.Invoke(behavior, [sender, new PropertyChangedEventArgs(propertyName)]);

	/// <summary>A behavior for every Label, which records what BasePlatformBehavior gives it.</summary>
	sealed class ProbeBehavior : BasePlatformBehavior<Label>
	{
		public int AttachedCount { get; private set; }

		public int DetachedCount { get; private set; }

		public bool IsAttached { get; private set; }

		public PlatformView? AttachedPlatformView { get; private set; }

		/// <summary>The protected <c>View</c> of the base class: the label while attached, <see langword="null"/> otherwise.</summary>
		public Label? TrackedView => View;

		public List<(Label Sender, string? PropertyName)> ViewPropertyChanges { get; } = [];

		/// <summary>Thrown by <see cref="OnViewPropertyChanged"/> after it recorded the change.</summary>
		public Exception? ExceptionToThrow { get; init; }

		protected override void OnAttachedTo(Label bindable, PlatformView platformView)
		{
			base.OnAttachedTo(bindable, platformView);
			IsAttached = true;
			AttachedCount++;
			AttachedPlatformView = platformView;
		}

		protected override void OnDetachedFrom(Label bindable, PlatformView platformView)
		{
			base.OnDetachedFrom(bindable, platformView);
			IsAttached = false;
			DetachedCount++;
		}

		protected override void OnViewPropertyChanged(Label sender, PropertyChangedEventArgs e)
		{
			base.OnViewPropertyChanged(sender, e);
			ViewPropertyChanges.Add((sender, e.PropertyName));

			if (ExceptionToThrow is not null)
			{
				throw ExceptionToThrow;
			}
		}
	}

	/// <summary>A behavior that needs a platform view (a button, an image view) that a label never has.</summary>
	sealed class MismatchedBehavior : BasePlatformBehavior<Label, MismatchedPlatformView>
	{
		public int AttachedCount { get; private set; }

		public int DetachedCount { get; private set; }

		public Label? TrackedView => View;

		public List<(Label Sender, string? PropertyName)> ViewPropertyChanges { get; } = [];

		protected override void OnAttachedTo(Label bindable, MismatchedPlatformView platformView)
		{
			base.OnAttachedTo(bindable, platformView);
			AttachedCount++;
		}

		protected override void OnDetachedFrom(Label bindable, MismatchedPlatformView platformView)
		{
			base.OnDetachedFrom(bindable, platformView);
			DetachedCount++;
		}

		protected override void OnViewPropertyChanged(Label sender, PropertyChangedEventArgs e)
		{
			base.OnViewPropertyChanged(sender, e);
			ViewPropertyChanges.Add((sender, e.PropertyName));
		}
	}
}