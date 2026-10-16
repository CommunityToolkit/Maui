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

	// ---- lifecycle accessors (used by the shared lifecycle tests)

	/// <summary>
	/// The behavior made the view clickable and long clickable, and left the view's single click listener slot alone: it takes it in touch
	/// exploration mode only (see TouchBehaviorInputTests.TouchExplorationCompletesTheGestureThroughClickAndIgnoresRawTouches).
	/// </summary>
	protected static void AssertNativeHooksAttached(AView view)
	{
		Assert.True(view.Clickable, "The behavior must make the view clickable.");
		Assert.True(view.LongClickable, "The behavior must make the view long clickable.");
		Assert.False(view.HasOnClickListeners, "Outside touch exploration the behavior must leave the view's click listener slot alone.");
	}

	/// <summary>The behavior removed its click listener (the touch and key listeners can only be probed through events).</summary>
	protected static void AssertNativeHooksDetached(AView view) =>
		Assert.False(view.HasOnClickListeners, "The behavior must remove its click listener.");

	/// <summary>What the behavior changes on the platform view to take input: Android makes it clickable and long clickable.</summary>
	protected static string GetNativeInteractionState(IPlatformViewHandler handler)
	{
		var view = GetPlatformView(handler);
		return $"Clickable={view.Clickable}, LongClickable={view.LongClickable}";
	}

	/// <summary>
	/// Lets Java collect the native peers that the managed objects hold on to: a managed object with a Java peer is only released
	/// after both garbage collectors ran, and on API 28 that can take several rounds (the bridge processes the cross references lazily).
	/// </summary>
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

	/// <summary>
	/// Makes MAUI animate in this process. The pool emulators (like CI's) run with the global animator duration scale at 0, and MAUI then
	/// jumps to the end of every animation (it asks <c>ValueAnimator.AreAnimatorsEnabled</c>), so no timing, easing, cancellation or
	/// intermediate step can be observed.
	/// <para>
	/// The scale is switched on for this process only, through <c>ValueAnimator.setDurationScale</c> (a hidden API on the
	/// unsupported-app-usage list), and never through the global setting: nothing outlives the test process, so a crash cannot leave
	/// animations on for later runs on a shared emulator. <see cref="PlatformDisposeAsync"/> puts the previous scale back. MAUI's ticker
	/// learns about the change through its duration scale listener (API 33+) or, before that, through its energy saver callback, which
	/// re-reads the same API.
	/// </para>
	/// </summary>
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

	/// <summary>How the platform's image view scales its image, as a MAUI <see cref="Aspect"/>.</summary>
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

	// ---- native state

	/// <summary>
	/// True when MAUI made the element's platform view ignore touches (<c>InputTransparent</c>): layouts carry the flag themselves, any
	/// other view is wrapped in a <see cref="WrapperView"/> that carries it.
	/// </summary>
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

	/// <summary>The colour the platform view paints as its background; <see langword="null"/> when it has none.</summary>
	protected static Color? GetNativeBackgroundColor(IPlatformViewHandler handler)
	{
		var drawable = GetPlatformView(handler).Background;
		return drawable switch
		{
			ColorDrawable colorDrawable => Color.FromInt(colorDrawable.Color.ToArgb()),
			_ => null
		};
	}

	/// <summary>
	/// The colour the platform view shows at its centre, with its alpha: what the view looks like there, also when a drawable that is not a
	/// colour paints its background (a Button's).
	/// </summary>
	protected static async Task<Color> GetNativeCenterColorAsync(IPlatformViewHandler handler)
	{
		using var bitmap = await GetPlatformView(handler).ToBitmap(handler.MauiContext!);
		var color = bitmap.ColorAtPoint(bitmap.Width / 2, bitmap.Height / 2, includeAlpha: true);

		return Color.FromRgba(color.R, color.G, color.B, color.A);
	}

	// ---- native input (device independent units relative to the top left of the view)

	protected void NativePress(IPlatformViewHandler handler, double x = HostCenter, double y = HostCenter) =>
		NativePressOn(GetPlatformView(handler), x, y);

	/// <summary>Sends a touch-down to a platform view that was not created by a MAUI handler.</summary>
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

	/// <summary>A tap at the centre of the view, delivered as Android delivers a finger's (the view's own click follows in a posted message).</summary>
	protected Task NativeTapAsync(IPlatformViewHandler handler)
	{
		NativePress(handler);
		NativeRelease(handler);
		return Task.CompletedTask;
	}

	protected void NativeCancel(IPlatformViewHandler handler) =>
		Dispatch(GetPlatformView(handler), MotionEventActions.Cancel, HostCenter, HostCenter);

	/// <summary>
	/// A mouse entering the view, delivered the way Android delivers it: a hover is a generic motion event from a pointer source
	/// (<c>dispatchGenericMotionEvent</c> → <c>dispatchHoverEvent</c>), never a touch event, so the view's touch listener does not see it.
	/// </summary>
	protected Task NativeHoverEnterAsync(IPlatformViewHandler handler)
	{
		DispatchMouseHover(GetPlatformView(handler), MotionEventActions.HoverEnter);
		return Task.CompletedTask;
	}

	/// <inheritdoc cref="NativeHoverEnterAsync"/>
	protected Task NativeHoverExitAsync(IPlatformViewHandler handler)
	{
		DispatchMouseHover(GetPlatformView(handler), MotionEventActions.HoverExit);
		return Task.CompletedTask;
	}

	protected static bool DispatchMouseHover(AView view, MotionEventActions action)
	{
		var density = GetDensity(view);
		var now = SystemClock.UptimeMillis();
		using var motionEvent = MotionEvent.Obtain(now, now, action, (float)(HostCenter * density), (float)(HostCenter * density), 0)
			?? throw new InvalidOperationException("MotionEvent.Obtain returned null");
		motionEvent.SetSource(InputSourceType.Mouse);
		return view.DispatchGenericMotionEvent(motionEvent);
	}

	/// <summary>
	/// Hands a hover action straight to the view's touch listener (<c>DispatchTouchEvent</c>). Android never does this for a real hover
	/// (see <see cref="NativeHoverEnterAsync"/>); it is the only way to reach the HoverEnter / HoverExit arms of the behavior's touch
	/// listener, so only the tests that are explicitly about those arms use it.
	/// </summary>
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

	// ---- accessibility (the instrumentation's UiAutomation is the only way to switch an accessibility service on in a test)

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

	/// <summary>
	/// Runs <paramref name="body"/> while the system reports touch exploration (TalkBack style accessibility mode). The instrumentation's
	/// UiAutomation registers as an accessibility service that requests touch exploration; the request is withdrawn afterwards and the
	/// accessibility connection released (see <see cref="ReleaseAccessibilityServiceAsync"/>).
	/// </summary>
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

	/// <summary>
	/// Runs <paramref name="body"/> while an accessibility service runs that does NOT explore by touch: the AccessibilityManager is
	/// enabled, touch exploration is off.
	/// </summary>
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

	/// <summary>
	/// Once connected, the instrumentation's UiAutomation stays registered as an accessibility service for the rest of the process, which
	/// would leave <c>AccessibilityManager.IsEnabled</c> on for every later test of every category. From API 33 the connection is remade
	/// without accessibility (<c>UiAutomationFlags.DontUseAccessibility</c>): the same UiAutomation object stays, with whatever listener another
	/// test installed on it, and the next plain <c>UiAutomation</c> request reconnects it with accessibility. Before API 33 there is no public
	/// way: the hidden <c>UiAutomation.destroy()</c> disconnects it, and the instrumentation creates a new object on the next request, without
	/// the listeners of the old one. So before API 33 only a UiAutomation that these helpers connected themselves is destroyed (see
	/// <see cref="ownsUiAutomation"/>); one that somebody else connected stays (and with it the accessibility service), and so does one whose
	/// destroy the platform refuses.
	/// </summary>
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

	/// <summary>
	/// A TouchBehavior that can be attached to a platform view that MAUI knows nothing about: the native hierarchy is built by the
	/// test, so the test controls the parents and the lifetime of the platform view.
	/// </summary>
	protected sealed class DirectTouchBehavior : TouchBehavior
	{
		public void AttachDirect(VisualElement bindable, AView platformView) => OnAttachedTo(bindable, platformView);

		public void DetachDirect(VisualElement bindable, AView platformView) => OnDetachedFrom(bindable, platformView);

		public void DisposeDirect(bool disposing) => Dispose(disposing);

		/// <summary>Sets the behavior's view without running the effects that <c>Element</c> triggers.</summary>
		public void SetViewOnly(VisualElement? view) => View = view;
	}

	/// <summary>A test-built platform view (a FrameLayout child of a FrameLayout) for <see cref="DirectTouchBehavior"/>.</summary>
	protected static (AView Parent, AView Child) CreateDetachedPlatformViews()
	{
		var context = MauiProgramDefaults.DefaultContext;
		var parent = new FrameLayout(context);
		var child = new FrameLayout(context);
		parent.AddView(child);
		return (parent, child);
	}

	/// <summary>
	/// Disposes an attached <see cref="DirectTouchBehavior"/> the way an app does (<c>Dispose()</c> without a detach first) and reports
	/// whether the dispose left the behavior's accessibility listener registered with the system's <c>AccessibilityManager</c>.
	/// <para>
	/// A listener that stays registered after its managed peer was disposed crashes the whole process at the next accessibility or touch
	/// exploration change (NotSupportedException: Unable to activate instance of type AccessibilityListener), so this helper also
	/// removes it again, through JNI references of its own (the dispose drops the behavior's references to the listener and the manager).
	/// It must be used by every test that disposes an attached behavior.
	/// </para>
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

	/// <summary>Disposes an attached <see cref="DirectTouchBehavior"/> without leaving its accessibility listener registered.</summary>
	protected static void DisposeWhileAttached(DirectTouchBehavior behavior) => DisposeAttachedBehavior(behavior);

	/// <summary>The behavior's private accessibility listener (null when the view's context has no AccessibilityManager).</summary>
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

	/// <summary>A parent that records <see cref="ViewGroup.RequestDisallowInterceptTouchEvent"/>.</summary>
	protected sealed class RecordingViewGroup(Context context) : FrameLayout(context)
	{
		public List<bool> DisallowInterceptRequests { get; } = [];

		public override void RequestDisallowInterceptTouchEvent(bool disallowIntercept)
		{
			DisallowInterceptRequests.Add(disallowIntercept);
			base.RequestDisallowInterceptTouchEvent(disallowIntercept);
		}
	}

	/// <summary>A context whose <c>GetSystemService(AccessibilityService)</c> returns nothing, like a context of a process without one.</summary>
	protected sealed class NoAccessibilityServiceContext(Context baseContext) : ContextWrapper(baseContext)
	{
		public override Java.Lang.Object? GetSystemService(string? name) =>
			name == AccessibilityService ? null : base.GetSystemService(name);
	}
}