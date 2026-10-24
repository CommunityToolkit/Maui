using System.Reflection;
using Android.Content;
using Android.Views.Accessibility;
using CommunityToolkit.Maui.Behaviors;
using FrameLayout = Android.Widget.FrameLayout;
using ViewGroup = Android.Views.ViewGroup;

namespace CommunityToolkit.Maui.DeviceTests;

// The views are built by the tests (not by MAUI handlers), because a Dispose that misbehaves would otherwise take the test's own view
// hierarchy with it.
public partial class TouchBehaviorLifecycleTests
{
	// Dispose may only release what the behavior owns: the managed wrappers of its view and of that view's parent belong to whoever created
	// them, and a disposed wrapper has no native handle and cannot be used any more.
	[Fact]
	public async Task DisposeLeavesTheNativeViewsUsable()
	{
		SetupBuilder();

		var context = MauiProgramDefaults.DefaultContext;
		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			using var parent = new FrameLayout(context);
			using var child = new FrameLayout(context);
			parent.AddView(child);
			behavior.AttachDirect(element, child);

			DisposeAttachedBehavior(behavior);

			Assert.NotEqual(IntPtr.Zero, child.Handle);
			Assert.NotEqual(IntPtr.Zero, parent.Handle);
			child.Alpha = 0.5f;
			Assert.Equal(0.5f, child.Alpha);
		});
	}

	// A disposed listener that stays registered makes the next touch exploration change call into a peer that no longer exists, and the
	// process dies with NotSupportedException (Unable to activate instance of type AccessibilityListener).
	[Fact]
	public async Task DisposeUnregistersTheAccessibilityListener()
	{
		SetupBuilder();

		var context = MauiProgramDefaults.DefaultContext;
		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			using var parent = new FrameLayout(context);
			using var child = new FrameLayout(context);
			parent.AddView(child);
			behavior.AttachDirect(element, child);

			var (stateListenerLeftRegistered, touchExplorationListenerLeftRegistered) = DisposeAttachedBehavior(behavior);

			Assert.False(stateListenerLeftRegistered, "Dispose left the accessibility state listener registered with the AccessibilityManager.");
			Assert.False(touchExplorationListenerLeftRegistered, "Dispose left the touch exploration listener registered with the AccessibilityManager.");
		});
	}

	[Fact]
	public async Task FinalizerPathKeepsTheNativeViews()
	{
		SetupBuilder();

		var context = MauiProgramDefaults.DefaultContext;
		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var parent = new FrameLayout(context);
			using var child = new FrameLayout(context);
			parent.AddView(child);
			behavior.AttachDirect(element, child);

			try
			{
				await WithTouchExplorationAsync(child, () =>
				{
					Assert.True(child.HasOnClickListeners, "Touch exploration needs the behavior's click listener.");

					behavior.DisposeDirect(false);

					Assert.NotEqual(IntPtr.Zero, child.Handle);
					Assert.NotEqual(IntPtr.Zero, parent.Handle);
					Assert.True(child.HasOnClickListeners, "Dispose(false) must leave the attached behavior alone.");
					Assert.NotNull(GetAccessibilityListener(behavior));
					return Task.CompletedTask;
				});
			}
			finally
			{
				behavior.DetachDirect(element, child);
				behavior.DisposeDirect(true);
			}
		});
	}

	// MAUI runs the detach after an app disposed the behavior (Dispose(), then the page goes away).
	[Fact]
	public async Task DetachingAfterDisposeDoesNotThrow()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var (parent, child) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, child);
			SetHoverSupported(behavior, true);
			DisposeAttachedBehavior(behavior);

			var exception = Record.Exception(() => behavior.DetachDirect(element, child));

			Assert.Null(exception);
			Assert.False(GetHoverSupported(behavior), "The detach must reset the hover tracking.");
			Assert.Null(behavior.Element);

			RemoveTheListenersOfTheJavaView(parent);
		});
	}

	// OnDetachedFrom's catch (ObjectDisposedException): the managed wrapper of the platform view was disposed by somebody else (the Java view
	// lives on in its parent), so the first "view.Touch -= OnTouch" throws. The test disposes its own wrapper, so the arm is reached whatever
	// the behavior's Dispose does.
	// The hover tracking reset must not move before the catch, and the accessibility listener teardown must not move after the listeners of
	// the view: the exception would skip them.
	[Fact]
	public async Task DetachingFromAViewWhoseWrapperWasDisposedDoesNotThrow()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var (parent, child) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, child);
			SetHoverSupported(behavior, true);
			Assert.NotNull(GetAccessibilityListener(behavior));
			child.Dispose();

			var exception = Record.Exception(() => behavior.DetachDirect(element, child));

			Assert.Null(exception);
			Assert.False(GetHoverSupported(behavior), "The detach must reset the hover tracking even when the view's wrapper is gone.");
			Assert.Null(GetAccessibilityListener(behavior));

			RemoveTheListenersOfTheJavaView(parent);
		});
	}

	// A listener left registered with the system's AccessibilityManager after OnDetachedFrom would still pass touch exploration changes to the
	// behavior, and a disposed one would crash the next time touch exploration changes.
	[Fact]
	public async Task AccessibilityChangesAfterDetachDoNotReachTheBehavior()
	{
		SetupBuilder();

		var behavior = new TouchBehavior { IsEnabled = false };
		var host = CreateHost();

		await RunAttachedAsync(host, behavior, async handler =>
		{
			var view = GetPlatformView(handler);
			host.Behaviors.Remove(behavior);

			await WithTouchExplorationAsync(view, () =>
			{
				Assert.False(view.HasOnClickListeners);
				return Task.CompletedTask;
			});

			Assert.False(view.HasOnClickListeners);
		});
	}

	[Fact]
	public async Task UpdateClickHandlerIgnoresAViewThatIsGone()
	{
		SetupBuilder();

		var element = new Grid();

		await InvokeOnMainThreadAsync(async () =>
		{
			// After a Dispose the behavior's view field is null.
			var disposed = new DirectTouchBehavior();
			var (_, first) = CreateDetachedPlatformViews();
			disposed.AttachDirect(element, first);
			DisposeAttachedBehavior(disposed);
			Assert.Null(Record.Exception(() => InvokeUpdateClickHandler(disposed)));

			// Somebody else released the managed peer of the view (its Java object lives on in the parent).
			var behavior = new DirectTouchBehavior();
			var (parent, second) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, second);
			Android.Views.View? liveView = null;

			await WithTouchExplorationAsync(second, () =>
			{
				Assert.True(second.HasOnClickListeners);
				second.Dispose();

				Assert.Null(Record.Exception(() => InvokeUpdateClickHandler(behavior)));

				// UpdateClickHandler returned before it removed anything: the Java view still has the click listener.
				liveView = ((ViewGroup)parent).GetChildAt(0) ?? throw new InvalidOperationException("The child is gone.");
				Assert.True(liveView.HasOnClickListeners);
				return Task.CompletedTask;
			});

			behavior.DetachDirect(element, liveView ?? throw new InvalidOperationException("The child is gone."));
			RemoveTheListenersOfTheJavaView(parent);
		});
	}

	// After a detach the behavior still knows its (live) view but has no accessibility manager any more: refreshing the click listener then
	// must not put it back, even while touch exploration is on (a late accessibility callback would get there).
	[Fact]
	public async Task UpdateClickHandlerAfterTheDetachDoesNotRegisterTheClickListener()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(async () =>
		{
			var (_, child) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, child);

			await WithTouchExplorationAsync(child, () =>
			{
				Assert.True(child.HasOnClickListeners);

				behavior.DetachDirect(element, child);
				Assert.Null(behavior.Element);
				Assert.False(child.HasOnClickListeners);

				InvokeUpdateClickHandler(behavior);

				Assert.False(child.HasOnClickListeners, "A detached behavior must not register the click listener again.");
				return Task.CompletedTask;
			});
		});
	}

	// In touch exploration mode the behavior takes the click listener only from a view without one of its own: the view drops its own
	// listener, and a callback is what makes the behavior look at the slot again.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task AccessibilityListenerCallbacksRefreshTheClickHandler(bool accessibilityStateCallback)
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(async () =>
		{
			var (_, child) = CreateDetachedPlatformViews();
			using var ownListener = new CountingClickListener();
			child.SetOnClickListener(ownListener);
			behavior.AttachDirect(element, child);

			try
			{
				await WithTouchExplorationAsync(child, () =>
				{
					var listener = GetAccessibilityListener(behavior) ?? throw new InvalidOperationException("The behavior has no accessibility listener.");

					child.PerformClick();
					Assert.Equal(1, ownListener.Clicks);

					child.SetOnClickListener(null);
					Assert.False(child.HasOnClickListeners, "Nothing refreshed the behavior's click listener yet.");

					NotifyAccessibilityChange(listener, accessibilityStateCallback);
					Assert.True(child.HasOnClickListeners, "The callback must give a view without a click listener of its own the behavior's.");
					return Task.CompletedTask;
				});

				Assert.False(child.HasOnClickListeners, "The behavior's click listener must go away with touch exploration.");
			}
			finally
			{
				behavior.DetachDirect(element, child);
			}
		});
	}

	[Fact]
	public async Task AccessibilityListenerForgetsTheBehaviorOnlyWhenItIsDisposed()
	{
		SetupBuilder();

		var behavior = new DirectTouchBehavior();
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var (_, child) = CreateDetachedPlatformViews();
			behavior.AttachDirect(element, child);
			var listener = GetAccessibilityListener(behavior) ?? throw new InvalidOperationException("The behavior has no accessibility listener.");

			Assert.Same(behavior, GetListenerTarget(listener));
			behavior.IsEnabled = false;
			NotifyAccessibilityChange(listener, accessibilityState: true);
			Assert.False(child.HasOnClickListeners);
			behavior.IsEnabled = true;

			behavior.DetachDirect(element, child);
			Assert.Null(GetListenerTarget(listener));

			// The system may already have queued a callback when the listener is removed.
			Assert.Null(Record.Exception(() =>
			{
				NotifyAccessibilityChange(listener, accessibilityState: true);
				NotifyAccessibilityChange(listener, accessibilityState: false);
			}));
			Assert.False(child.HasOnClickListeners);
		});
	}

	// A finalizer cannot be triggered on purpose, so the protected Dispose(false) is called directly on a listener registered nowhere.
	[Fact]
	public async Task AccessibilityListenerFinalizerPathKeepsTheBehavior()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();

		await InvokeOnMainThreadAsync(() =>
		{
			var listenerType = typeof(TouchBehavior).GetNestedType("AccessibilityListener", BindingFlags.NonPublic)
				?? throw new TypeLoadException("TouchBehavior.AccessibilityListener was not found.");
			var listener = (Java.Lang.Object)(Activator.CreateInstance(listenerType, BindingFlags.Instance | BindingFlags.NonPublic, null, [behavior], null)
				?? throw new InvalidOperationException("The listener could not be created."));
			var dispose = listenerType.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(bool)])
				?? throw new MissingMethodException(listenerType.Name, "Dispose(bool)");

			dispose.Invoke(listener, [false]);
			Assert.Same(behavior, GetListenerTarget(listener));

			listener.Dispose();
			Assert.Null(GetListenerTarget(listener));
		});
	}

	[Fact]
	public async Task BehaviorWorksOnAViewWhoseContextHasNoAccessibilityService()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new DirectTouchBehavior { Command = command };
		var element = new Grid();

		await InvokeOnMainThreadAsync(() =>
		{
			var context = new NoAccessibilityServiceContext(MauiProgramDefaults.DefaultContext);
			var parent = new FrameLayout(context);
			var child = new FrameLayout(context);
			parent.AddView(child);
			Assert.Null(child.Context?.GetSystemService(Context.AccessibilityService));

			behavior.AttachDirect(element, child);
			Exception? detach;

			try
			{
				Assert.Null(GetAccessibilityListener(behavior));
				AssertNativeHooksAttached(child);

				NativePressOn(child);
				NativeReleaseOn(child);
				Assert.Equal(1, command.ExecuteCount);
			}
			finally
			{
				detach = Record.Exception(() => behavior.DetachDirect(element, child));
			}

			Assert.Null(detach);
			Assert.False(child.HasOnClickListeners);
		});
	}

	// Cleanup through a fresh wrapper of the Java child (the old wrapper may be disposed): a detach through a disposed wrapper cannot remove
	// the behavior's listeners, and the test-built views must not keep the behavior reachable through them.
	static void RemoveTheListenersOfTheJavaView(Android.Views.View parent)
	{
		var liveView = ((ViewGroup)parent).GetChildAt(0) ?? throw new InvalidOperationException("The child is gone.");
		liveView.SetOnTouchListener(null);
		liveView.SetOnClickListener(null);
		liveView.SetOnKeyListener(null);
		liveView.SetOnGenericMotionListener(null);
	}

	sealed class CountingClickListener : Java.Lang.Object, Android.Views.View.IOnClickListener
	{
		public int Clicks { get; private set; }

		public void OnClick(Android.Views.View? v) => Clicks++;
	}

	static void InvokeUpdateClickHandler(TouchBehavior behavior) =>
		(typeof(TouchBehavior).GetMethod("UpdateClickHandler", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingMethodException(nameof(TouchBehavior), "UpdateClickHandler")).Invoke(behavior, null);

	static void NotifyAccessibilityChange(object listener, bool accessibilityState)
	{
		if (accessibilityState)
		{
			((AccessibilityManager.IAccessibilityStateChangeListener)listener).OnAccessibilityStateChanged(false);
		}
		else
		{
			((AccessibilityManager.ITouchExplorationStateChangeListener)listener).OnTouchExplorationStateChanged(false);
		}
	}

	static TouchBehavior? GetListenerTarget(object listener)
	{
		var field = listener.GetType().GetField("platformTouchBehaviorReference", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(listener.GetType().Name, "platformTouchBehaviorReference");
		var reference = (WeakReference<TouchBehavior?>)(field.GetValue(listener) ?? throw new InvalidOperationException("No weak reference."));
		return reference.TryGetTarget(out var target) ? target : null;
	}

	static bool GetHoverSupported(TouchBehavior behavior) =>
		(bool)(typeof(TouchBehavior).GetField("isHoverSupported", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(behavior)
			?? throw new MissingFieldException(nameof(TouchBehavior), "isHoverSupported"));

	static void SetHoverSupported(TouchBehavior behavior, bool value) =>
		(typeof(TouchBehavior).GetField("isHoverSupported", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new MissingFieldException(nameof(TouchBehavior), "isHoverSupported")).SetValue(behavior, value);
}