using Android.AccessibilityServices;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Accessibility;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using FrameLayout = Android.Widget.FrameLayout;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class TouchBehaviorTestBase
{
	long touchDownTime;
	float? animatorScaleToRestore;

	protected static bool SupportsNativePointerInput => true;

	protected static string NativePointerInputUnavailableReason => "Native pointer input is available on this platform.";

	protected static AView GetPlatformView(IPlatformViewHandler handler) => (AView)handler.PlatformView!;

	protected static float GetDensity(AView view) => view.Context!.Resources!.DisplayMetrics!.Density;

	protected static void AssertNativeHooksAttached(AView view)
	{
		Assert.True(view.Clickable, "The behavior must make the view clickable.");
		Assert.True(view.LongClickable, "The behavior must make the view long clickable.");
		Assert.False(view.HasOnClickListeners, "Outside touch exploration the behavior must leave the view's click listener slot alone.");
	}

	// Only the click listener can be probed here: the touch and key listeners can only be probed through events.
	protected static void AssertNativeHooksDetached(AView view) =>
		Assert.False(view.HasOnClickListeners, "The behavior must remove its click listener.");

	protected static string GetNativeInteractionState(IPlatformViewHandler handler)
	{
		var view = GetPlatformView(handler);
		return $"Clickable={view.Clickable}, LongClickable={view.LongClickable}";
	}

	// A managed object with a Java peer is only released after both garbage collectors ran, and on API 28 that can take several rounds (the
	// bridge processes the cross references lazily).
	protected static async Task ReleaseNativeReferencesAsync()
	{
		for (var i = 0; i < 10; i++)
		{
			Java.Lang.JavaSystem.Gc();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await Task.Delay(20);
		}
	}

	// The pool emulators (like CI's) run with the global animator duration scale at 0, and MAUI then jumps to the end of every animation (it
	// asks ValueAnimator.AreAnimatorsEnabled), so no timing, easing, cancellation or intermediate step can be observed.
	// The scale is switched on for this process only, through ValueAnimator.setDurationScale (a hidden API on the unsupported-app-usage list),
	// and never through the global setting: nothing outlives the test process, so a crash cannot leave animations on for later runs on a
	// shared emulator. MAUI's ticker learns about the change through its duration scale listener (API 33+) or, before that, through its energy
	// saver callback, which re-reads the same API.
	protected async Task EnableAnimationsAsync(IPlatformViewHandler handler)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(26), "ValueAnimator.DurationScale and AreAnimatorsEnabled need API 26: animations cannot be switched on in-process here.");

		string? error = null;
		await InvokeOnMainThreadAsync(() =>
		{
			if (!ValueAnimator.AreAnimatorsEnabled())
			{
				var previous = ValueAnimator.DurationScale;
				error = TrySetAnimatorDurationScale(1f);
				if (error is null)
				{
					animatorScaleToRestore ??= previous;
				}
			}

			(handler.MauiContext?.Services.GetService(typeof(ITicker)) as IEnergySaverListener)?.OnStatusUpdated(false);
		});

		Skip.If(error is not null, $"The system's animations are off and could not be switched on for this process ({error}), so MAUI jumps to the end of every animation.");

		var ticker = handler.MauiContext?.Services.GetService(typeof(ITicker)) as ITicker;
		Assert.True(ValueAnimator.AreAnimatorsEnabled(), "Animations must be enabled for this process.");
		Assert.True(ticker is null || ticker.SystemEnabled, "MAUI's ticker must see the enabled animations.");
	}

	ValueTask PlatformDisposeAsync()
	{
		if (animatorScaleToRestore is not float scale)
		{
			return ValueTask.CompletedTask;
		}

		animatorScaleToRestore = null;
		return new ValueTask(InvokeOnMainThreadAsync(() => TrySetAnimatorDurationScale(scale)));
	}

	static string? TrySetAnimatorDurationScale(float scale)
	{
		try
		{
			using var valueAnimatorClass = Java.Lang.Class.FromType(typeof(ValueAnimator));
			using var setDurationScale = valueAnimatorClass.GetDeclaredMethod("setDurationScale", Java.Lang.Float.Type!)
				?? throw new InvalidOperationException("ValueAnimator.setDurationScale was not found.");
			setDurationScale.Invoke(null, new Java.Lang.Float(scale));
			return null;
		}
		catch (Java.Lang.Throwable ex)
		{
			return $"{ex.GetType().Name}: {ex.Message}";
		}
	}

	protected static Aspect GetNativeImageAspect(IPlatformViewHandler handler)
	{
		var scaleType = ((Android.Widget.ImageView)handler.PlatformView!).GetScaleType();

		if (scaleType == Android.Widget.ImageView.ScaleType.FitCenter)
		{
			return Aspect.AspectFit;
		}

		if (scaleType == Android.Widget.ImageView.ScaleType.CenterCrop)
		{
			return Aspect.AspectFill;
		}

		if (scaleType == Android.Widget.ImageView.ScaleType.FitXy)
		{
			return Aspect.Fill;
		}

		if (scaleType == Android.Widget.ImageView.ScaleType.Center)
		{
			return Aspect.Center;
		}

		throw new InvalidOperationException($"Unexpected scale type {scaleType}.");
	}

	// Layouts carry the InputTransparent flag themselves; any other view is wrapped in a WrapperView that carries it.
	protected static bool IsNativeInputTransparent(VisualElement element)
	{
		var handler = (IPlatformViewHandler)element.Handler!;

		return handler.ContainerView is WrapperView wrapper
			? wrapper.InputTransparent
			: handler.PlatformView is LayoutViewGroup layout && layout.InputTransparent;
	}

	protected static double GetNativeOpacity(IPlatformViewHandler handler) => GetPlatformView(handler).Alpha;

	protected static double GetNativeScale(IPlatformViewHandler handler)
	{
		var view = GetPlatformView(handler);
		Assert.Equal(view.ScaleX, view.ScaleY);
		return view.ScaleX;
	}

	// Android stores translations in pixels; the behavior works in device independent units.
	protected static double GetNativeTranslationX(IPlatformViewHandler handler)
	{
		var view = GetPlatformView(handler);
		return view.TranslationX / GetDensity(view);
	}

	protected static double GetNativeTranslationY(IPlatformViewHandler handler)
	{
		var view = GetPlatformView(handler);
		return view.TranslationY / GetDensity(view);
	}

	protected static double GetNativeRotation(IPlatformViewHandler handler) => GetPlatformView(handler).Rotation;

	protected static double GetNativeRotationX(IPlatformViewHandler handler) => GetPlatformView(handler).RotationX;

	protected static double GetNativeRotationY(IPlatformViewHandler handler) => GetPlatformView(handler).RotationY;

	protected static Color? GetNativeBackgroundColor(IPlatformViewHandler handler)
	{
		var drawable = GetPlatformView(handler).Background;
		return drawable switch
		{
			ColorDrawable colorDrawable => Color.FromInt(colorDrawable.Color.ToArgb()),
			_ => null
		};
	}

	// Unlike GetNativeBackgroundColor, this also works when a drawable that is not a colour paints the background (a Button's).
	protected static async Task<Color> GetNativeCenterColorAsync(IPlatformViewHandler handler)
	{
		using var bitmap = await GetPlatformView(handler).ToBitmap(handler.MauiContext!);
		var color = bitmap.ColorAtPoint(bitmap.Width / 2, bitmap.Height / 2, includeAlpha: true);

		return Color.FromRgba(color.R, color.G, color.B, color.A);
	}

	protected void NativePress(IPlatformViewHandler handler, double x = HostCenter, double y = HostCenter) =>
		NativePressOn(GetPlatformView(handler), x, y);

	protected void NativePressOn(AView view, double x = HostCenter, double y = HostCenter)
	{
		touchDownTime = SystemClock.UptimeMillis();
		Dispatch(view, MotionEventActions.Down, x, y);
	}

	protected void NativeMoveOn(AView view, double x, double y) => Dispatch(view, MotionEventActions.Move, x, y);

	protected void NativeReleaseOn(AView view, double x = HostCenter, double y = HostCenter) => Dispatch(view, MotionEventActions.Up, x, y);

	protected void NativeMove(IPlatformViewHandler handler, double x, double y) =>
		Dispatch(GetPlatformView(handler), MotionEventActions.Move, x, y);

	protected void NativeRelease(IPlatformViewHandler handler, double x = HostCenter, double y = HostCenter) =>
		Dispatch(GetPlatformView(handler), MotionEventActions.Up, x, y);

	// As for a finger's tap, Android posts the view's own click after the touch, so the click arrives after this returns.
	protected Task NativeTapAsync(IPlatformViewHandler handler)
	{
		NativePress(handler);
		NativeRelease(handler);
		return Task.CompletedTask;
	}

	protected void NativeCancel(IPlatformViewHandler handler) =>
		Dispatch(GetPlatformView(handler), MotionEventActions.Cancel, HostCenter, HostCenter);

	protected Task NativeHoverEnterAsync(IPlatformViewHandler handler)
	{
		DispatchMouseHover(GetPlatformView(handler), MotionEventActions.HoverEnter);
		return Task.CompletedTask;
	}

	protected Task NativeHoverExitAsync(IPlatformViewHandler handler)
	{
		DispatchMouseHover(GetPlatformView(handler), MotionEventActions.HoverExit);
		return Task.CompletedTask;
	}

	// Android delivers a hover as a generic motion event from a pointer source (dispatchGenericMotionEvent → dispatchHoverEvent), never as a
	// touch event, so the view's touch listener does not see it.
	protected static bool DispatchMouseHover(AView view, MotionEventActions action)
	{
		var density = GetDensity(view);
		var now = SystemClock.UptimeMillis();
		using var motionEvent = MotionEvent.Obtain(now, now, action, (float)(HostCenter * density), (float)(HostCenter * density), 0)
			?? throw new InvalidOperationException("MotionEvent.Obtain returned null");
		motionEvent.SetSource(InputSourceType.Mouse);
		return view.DispatchGenericMotionEvent(motionEvent);
	}

	// Android never hands a real hover to the touch listener (see DispatchMouseHover), but this is the only way to reach the HoverEnter /
	// HoverExit arms of the behavior's touch listener, so only the tests that are explicitly about those arms use it.
	protected bool DispatchHoverToTheTouchListener(AView view, MotionEventActions action) => Dispatch(view, action, HostCenter, HostCenter);

	protected bool Dispatch(AView view, MotionEventActions action, double x, double y)
	{
		var density = GetDensity(view);
		using var motionEvent = MotionEvent.Obtain(touchDownTime, SystemClock.UptimeMillis(), action, (float)(x * density), (float)(y * density), 0)
			?? throw new InvalidOperationException("MotionEvent.Obtain returned null");
		return view.DispatchTouchEvent(motionEvent);
	}

	protected static bool DispatchKey(AView view, Keycode keyCode, KeyEventActions action)
	{
		using var keyEvent = new KeyEvent(action, keyCode);
		return view.DispatchKeyEvent(keyEvent);
	}

	// Before API 33 (see ReleaseAccessibilityServiceAsync) set while the instrumentation's UiAutomation is one that these helpers connected
	// themselves: they asked for it while no accessibility service ran in the process, so the instrumentation created it for them. Only such
	// a UiAutomation may be destroyed again. One that was already connected belongs to somebody else (a real accessibility service of the
	// device, or a test that is still using it); the Toast tests release the one they connect at the end of each test (ToastObserver).
	static bool ownsUiAutomation;

	static UiAutomation RequireUiAutomation(AccessibilityManager accessibilityManager)
	{
		var instrumentation = MauiTestInstrumentation.Current;
		Skip.If(instrumentation is null, "An accessibility service can only be switched on through the instrumentation's UiAutomation (headless runs).");

		var serviceAlreadyRuns = accessibilityManager.IsEnabled;
		var uiAutomation = instrumentation.UiAutomation;
		Skip.If(uiAutomation is null, "The instrumentation has no UiAutomation, which is the only way to switch an accessibility service on in a test.");

		if (!serviceAlreadyRuns)
		{
			ownsUiAutomation = true;
		}

		return uiAutomation;
	}

	static AccessibilityManager GetAccessibilityManager(AView view) =>
		(AccessibilityManager)(view.Context!.GetSystemService(Context.AccessibilityService) ?? throw new InvalidOperationException("No AccessibilityManager."));

	protected static async Task WithTouchExplorationAsync(AView view, Func<Task> body)
	{
		var accessibilityManager = GetAccessibilityManager(view);
		var uiAutomation = RequireUiAutomation(accessibilityManager);
		var serviceInfo = uiAutomation.ServiceInfo ?? throw new InvalidOperationException("UiAutomation has no service info");
		var originalFlags = serviceInfo.Flags;

		try
		{
			serviceInfo.Flags = originalFlags | AccessibilityServiceFlags.RequestTouchExplorationMode;
			uiAutomation.ServiceInfo = serviceInfo;
			await WaitUntilAsync(() => accessibilityManager.IsEnabled && accessibilityManager.IsTouchExplorationEnabled, "Touch exploration was not switched on.");

			// The state change listeners are posted to the main looper after the manager flips its flags: let them run
			// before the test continues, and again before the behavior can be detached.
			await SettleAsync();
			await body();
		}
		finally
		{
			serviceInfo.Flags = originalFlags;
			uiAutomation.ServiceInfo = serviceInfo;
			await WaitUntilAsync(() => !accessibilityManager.IsTouchExplorationEnabled, "Touch exploration was not switched off.");
			await SettleAsync();
			await ReleaseAccessibilityServiceAsync(accessibilityManager);
		}
	}

	protected static async Task WithAccessibilityServiceWithoutTouchExplorationAsync(AView view, Func<Task> body)
	{
		var accessibilityManager = GetAccessibilityManager(view);
		_ = RequireUiAutomation(accessibilityManager);

		try
		{
			await WaitUntilAsync(() => accessibilityManager.IsEnabled, "The UiAutomation accessibility service did not connect.");
			Assert.False(accessibilityManager.IsTouchExplorationEnabled, "The UiAutomation service must not explore by touch here.");
			await SettleAsync();
			await body();
		}
		finally
		{
			await ReleaseAccessibilityServiceAsync(accessibilityManager);
		}
	}

	// Once connected, the instrumentation's UiAutomation stays registered as an accessibility service for the rest of the process, which would
	// leave AccessibilityManager.IsEnabled on for every later test of every category. From API 33 the connection is remade without
	// accessibility (UiAutomationFlags.DontUseAccessibility): the same UiAutomation object stays, with whatever listener another test
	// installed on it, and the next plain UiAutomation request reconnects it with accessibility. Before API 33 there is no public way: the
	// hidden UiAutomation.destroy() disconnects it, and the instrumentation creates a new object on the next request, without the listeners of
	// the old one. So before API 33 only a UiAutomation that these helpers connected themselves is destroyed (see ownsUiAutomation); one that
	// somebody else connected stays (and with it the accessibility service), and so does one whose destroy the platform refuses.
	protected static async Task ReleaseAccessibilityServiceAsync(AccessibilityManager accessibilityManager)
	{
		if (MauiTestInstrumentation.Current is not MauiTestInstrumentation instrumentation)
		{
			return;
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			instrumentation.GetUiAutomation(UiAutomationFlags.DontUseAccessibility);
			await WaitUntilAsync(() => !accessibilityManager.IsEnabled, "The UiAutomation accessibility service did not go away.", 5000);
			await SettleAsync();
			return;
		}

		if (!ownsUiAutomation || !TryDestroyUiAutomation(instrumentation))
		{
			return;
		}

		ownsUiAutomation = false;
		await AssertHelpers.Wait(() => !accessibilityManager.IsEnabled, timeout: 5000, interval: 50);
		await SettleAsync();
	}

	static bool TryDestroyUiAutomation(MauiTestInstrumentation instrumentation)
	{
		try
		{
			var uiAutomation = instrumentation.UiAutomation;
			if (uiAutomation is null)
			{
				return false;
			}

			using var destroy = uiAutomation.Class.GetDeclaredMethod("destroy");
			destroy?.Invoke(uiAutomation);
			return destroy is not null;
		}
		catch (Java.Lang.Throwable)
		{
			return false;
		}
	}

	// The native hierarchy is built by the test, so the test controls the parents and the lifetime of the platform view.
	protected sealed class DirectTouchBehavior : TouchBehavior
	{
		public void AttachDirect(VisualElement bindable, AView platformView) => OnAttachedTo(bindable, platformView);

		public void DetachDirect(VisualElement bindable, AView platformView) => OnDetachedFrom(bindable, platformView);

		public void DisposeDirect(bool disposing) => Dispose(disposing);

		/// <summary>Sets the behavior's view without running the effects that <c>Element</c> triggers.</summary>
		public void SetViewOnly(VisualElement? view) => View = view;
	}

	protected static (AView Parent, AView Child) CreateDetachedPlatformViews()
	{
		var context = MauiProgramDefaults.DefaultContext;
		var parent = new FrameLayout(context);
		var child = new FrameLayout(context);
		parent.AddView(child);
		return (parent, child);
	}

	/// <summary>
	/// A listener that stays registered after its managed peer was disposed crashes the whole process at the next accessibility or touch
	/// exploration change (NotSupportedException: Unable to activate instance of type AccessibilityListener), so this helper removes it
	/// again, through JNI references of its own (the dispose drops the behavior's references to the listener and the manager).
	/// Every test that disposes an attached behavior must use it.
	/// </summary>
	protected static (bool StateListenerLeftRegistered, bool TouchExplorationListenerLeftRegistered) DisposeAttachedBehavior(DirectTouchBehavior behavior)
	{
		var listener = GetAccessibilityListener(behavior) as Java.Lang.Object;
		var manager = typeof(TouchBehavior).GetField("accessibilityManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(behavior) as Java.Lang.Object;

		if (listener is null || manager is null)
		{
			behavior.DisposeDirect(true);
			return (false, false);
		}

		var listenerReference = JNIEnv.NewGlobalRef(listener.Handle);
		var managerReference = JNIEnv.NewGlobalRef(manager.Handle);

		try
		{
			behavior.DisposeDirect(true);

			// AccessibilityManager.remove...Listener returns whether the listener was registered.
			return (
				RemoveListener(managerReference, "removeAccessibilityStateChangeListener", "(Landroid/view/accessibility/AccessibilityManager$AccessibilityStateChangeListener;)Z", listenerReference),
				RemoveListener(managerReference, "removeTouchExplorationStateChangeListener", "(Landroid/view/accessibility/AccessibilityManager$TouchExplorationStateChangeListener;)Z", listenerReference));
		}
		finally
		{
			JNIEnv.DeleteGlobalRef(listenerReference);
			JNIEnv.DeleteGlobalRef(managerReference);
		}
	}

	protected static void DisposeWhileAttached(DirectTouchBehavior behavior) => DisposeAttachedBehavior(behavior);

	protected static object? GetAccessibilityListener(TouchBehavior behavior) =>
		typeof(TouchBehavior).GetField("accessibilityListener", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(behavior);

	static bool RemoveListener(IntPtr manager, string method, string signature, IntPtr listener)
	{
		var managerClass = JNIEnv.GetObjectClass(manager);

		try
		{
			var methodId = JNIEnv.GetMethodID(managerClass, method, signature);
			return JNIEnv.CallBooleanMethod(manager, methodId, new JValue(listener));
		}
		finally
		{
			JNIEnv.DeleteLocalRef(managerClass);
		}
	}

	protected sealed class RecordingViewGroup(Context context) : FrameLayout(context)
	{
		public List<bool> DisallowInterceptRequests { get; } = [];

		public override void RequestDisallowInterceptTouchEvent(bool disallowIntercept)
		{
			DisallowInterceptRequests.Add(disallowIntercept);
			base.RequestDisallowInterceptTouchEvent(disallowIntercept);
		}
	}

	protected sealed class NoAccessibilityServiceContext(Context baseContext) : ContextWrapper(baseContext)
	{
		public override Java.Lang.Object? GetSystemService(string? name) =>
			name == AccessibilityService ? null : base.GetSystemService(name);
	}
}