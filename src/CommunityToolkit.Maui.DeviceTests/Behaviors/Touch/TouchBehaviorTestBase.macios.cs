using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Behaviors;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class TouchBehaviorTestBase
{
	const string objCLibrary = "/usr/lib/libobjc.dylib";

	UITouch? activeTouch;
	UIEvent? activeEvent;

	protected static bool SupportsNativePointerInput => true;

	protected static string NativePointerInputUnavailableReason => "Native pointer input is available on this platform.";

	protected static UIView GetPlatformView(IPlatformViewHandler handler) => (UIView)handler.PlatformView!;

	/// <summary>The gesture recognizer the behavior adds for touches (a private nested type, so it is found by name).</summary>
	protected static UIGestureRecognizer GetTapRecognizer(UIView view) =>
		Assert.Single(view.GestureRecognizers ?? [], static recognizer => recognizer.GetType().Name == "TouchUITapGestureRecognizer");

	protected static UIHoverGestureRecognizer GetHoverRecognizer(UIView view) =>
		Assert.Single((view.GestureRecognizers ?? []).OfType<UIHoverGestureRecognizer>());

	protected static int CountTapRecognizers(UIView view) =>
		(view.GestureRecognizers ?? []).Count(static recognizer => recognizer.GetType().Name == "TouchUITapGestureRecognizer");

	protected static int CountHoverRecognizers(UIView view) =>
		(view.GestureRecognizers ?? []).OfType<UIHoverGestureRecognizer>().Count();

	// ---- lifecycle accessors (used by the shared lifecycle tests)

	/// <summary>The behavior added one touch recognizer and one hover recognizer to the view and enabled user interaction.</summary>
	protected static void AssertNativeHooksAttached(UIView view)
	{
		Assert.Equal(1, CountTapRecognizers(view));
		Assert.Equal(1, CountHoverRecognizers(view));
		Assert.True(view.UserInteractionEnabled, "The behavior must enable user interaction on the view.");
	}

	/// <summary>The behavior removed both of its recognizers from the view.</summary>
	protected static void AssertNativeHooksDetached(UIView view)
	{
		Assert.Equal(0, CountTapRecognizers(view));
		Assert.Equal(0, CountHoverRecognizers(view));
	}

	/// <summary>What the behavior changes on the platform view to take input: iOS / Mac Catalyst enable user interaction.</summary>
	protected static string GetNativeInteractionState(IPlatformViewHandler handler) =>
		$"UserInteractionEnabled={GetPlatformView(handler).UserInteractionEnabled}";

	/// <summary>No-op on Apple platforms: managed objects with a native peer are released by the normal collections.</summary>
	protected static Task ReleaseNativeReferencesAsync() => Task.CompletedTask;

	/// <summary>No-op on Apple platforms: Core Animation always animates (there is no system switch that makes MAUI skip animations).</summary>
	protected Task EnableAnimationsAsync(IPlatformViewHandler handler) => Task.CompletedTask;

	/// <summary>A test-built platform view (a UIView inside a UIView) for <see cref="DirectTouchBehavior"/>.</summary>
	protected static (UIView Parent, UIView Child) CreateDetachedPlatformViews()
	{
		var parent = new UIView(new CGRect(0, 0, 200, 200));
		var child = new UIView(new CGRect(0, 0, HostSize, HostSize));
		parent.AddSubview(child);
		return (parent, child);
	}

	/// <summary>
	/// Records every state the hover recognizer reports to its targets (a second target next to the behavior's own), so a test can wait
	/// until UIKit delivered a state change to the behavior as well.
	/// </summary>
	protected static List<UIGestureRecognizerState> RecordHoverStates(IPlatformViewHandler handler)
	{
		var hover = GetHoverRecognizer(GetPlatformView(handler));
		var states = new List<UIGestureRecognizerState>();
		hover.AddTarget(() => states.Add(hover.State));
		return states;
	}

	/// <summary>How the platform's image view scales its image, as a MAUI <see cref="Aspect"/>.</summary>
	protected static Aspect GetNativeImageAspect(IPlatformViewHandler handler) =>
		((UIImageView)handler.PlatformView!).ContentMode switch
		{
			UIViewContentMode.ScaleAspectFit => Aspect.AspectFit,
			UIViewContentMode.ScaleAspectFill => Aspect.AspectFill,
			UIViewContentMode.ScaleToFill => Aspect.Fill,
			UIViewContentMode.Center => Aspect.Center,
			var other => throw new InvalidOperationException($"Unexpected content mode {other}.")
		};

	// ---- native state

	/// <summary>True when MAUI turned user interaction off on the element's platform view (<c>InputTransparent</c>).</summary>
	protected static bool IsNativeInputTransparent(VisualElement element) =>
		!((UIView)((IPlatformViewHandler)element.Handler!).PlatformView!).UserInteractionEnabled;

	protected static double GetNativeOpacity(IPlatformViewHandler handler) => GetPlatformView(handler).Alpha;

	protected static CATransform3D GetNativeTransform(IPlatformViewHandler handler) => GetPlatformView(handler).Layer.Transform;

	protected static double GetNativeScale(IPlatformViewHandler handler)
	{
		var transform = GetNativeTransform(handler);
		Assert.Equal((double)transform.M11, transform.M22, 4);
		return transform.M11;
	}

	protected static double GetNativeTranslationX(IPlatformViewHandler handler) => GetNativeTransform(handler).M41;

	protected static double GetNativeTranslationY(IPlatformViewHandler handler) => GetNativeTransform(handler).M42;

	// Rotations are stored in the layer's 3D transform: a rotation about z fills the m11/m12 block, about x the m22/m23 block and
	// about y the m11/m31 block (CATransform3DMakeRotation).
	protected static double GetNativeRotation(IPlatformViewHandler handler)
	{
		var transform = GetNativeTransform(handler);
		return Math.Atan2(transform.M12, transform.M11) * 180 / Math.PI;
	}

	protected static double GetNativeRotationX(IPlatformViewHandler handler)
	{
		var transform = GetNativeTransform(handler);
		return Math.Atan2(transform.M23, transform.M22) * 180 / Math.PI;
	}

	protected static double GetNativeRotationY(IPlatformViewHandler handler)
	{
		var transform = GetNativeTransform(handler);
		return Math.Atan2(transform.M31, transform.M11) * 180 / Math.PI;
	}

	/// <summary>The colour the platform view paints as its background; <see langword="null"/> when it has none.</summary>
	protected static Color? GetNativeBackgroundColor(IPlatformViewHandler handler) => GetPlatformView(handler).BackgroundColor?.ToColor();

	/// <summary>
	/// The colour the platform view shows at its centre, with its alpha: what the view looks like there, also when it paints its background
	/// in another way than its BackgroundColor.
	/// </summary>
	protected static async Task<Color> GetNativeCenterColorAsync(IPlatformViewHandler handler)
	{
		using var bitmap = await GetPlatformView(handler).ToBitmap(handler.MauiContext!);
		var image = bitmap.CGImage ?? throw new InvalidOperationException("The rendered view has no CGImage.");
		var color = bitmap.ColorAtPoint((int)(image.Width / 2), (int)(image.Height / 2));
		color.GetRGBA(out var red, out var green, out var blue, out var alpha);

		return new Color((float)red, (float)green, (float)blue, (float)alpha);
	}

	// ---- native input (device independent units relative to the top left of the view)
	//
	// UIKit cannot be handed a touch from the outside, so the tests build UITouch/UIEvent objects the way UIKit would (alloc/init plus
	// the private setters UIKit itself uses) and send them to the recognizer through its public touchesBegan/Moved/Ended/Cancelled
	// entry points. The recognizer therefore runs exactly the code a real touch runs.

	protected void NativePress(IPlatformViewHandler handler, double x = HostCenter, double y = HostCenter)
	{
		var view = GetPlatformView(handler);
		ReleaseTouch();
		activeEvent = CreateObject<UIEvent>("UIEvent");
		activeTouch = CreateTouch(view, x, y, UITouchPhase.Began);
		GetTapRecognizer(view).TouchesBegan(new NSSet(activeTouch), activeEvent);
	}

	protected void NativeMove(IPlatformViewHandler handler, double x, double y)
	{
		var view = GetPlatformView(handler);
		var touch = activeTouch ?? throw new InvalidOperationException("NativePress must run before NativeMove.");
		UpdateTouch(touch, view, x, y, UITouchPhase.Moved);
		GetTapRecognizer(view).TouchesMoved(new NSSet(touch), activeEvent!);
	}

	protected void NativeRelease(IPlatformViewHandler handler, double x = HostCenter, double y = HostCenter)
	{
		var view = GetPlatformView(handler);
		var touch = activeTouch ?? throw new InvalidOperationException("NativePress must run before NativeRelease.");
		UpdateTouch(touch, view, x, y, UITouchPhase.Ended);
		GetTapRecognizer(view).TouchesEnded(new NSSet(touch), activeEvent!);
		ReleaseTouch();
	}

	protected void NativeCancel(IPlatformViewHandler handler)
	{
		var view = GetPlatformView(handler);
		var touch = activeTouch ?? throw new InvalidOperationException("NativePress must run before NativeCancel.");
		UpdateTouch(touch, view, HostCenter, HostCenter, UITouchPhase.Cancelled);
		GetTapRecognizer(view).TouchesCancelled(new NSSet(touch), activeEvent!);
		ReleaseTouch();
	}

	/// <summary>A tap at the centre of the view, delivered through UIKit as a finger's (see <see cref="TapThroughUIKitAsync"/>).</summary>
	protected static Task NativeTapAsync(IPlatformViewHandler handler) => TapThroughUIKitAsync(GetPlatformView(handler));

	/// <summary>
	/// A tap at the centre of <paramref name="view"/> that goes through UIKit's own event delivery, as a finger's does: the window hit-tests it,
	/// hands it to the gesture recognizers and then to the view (a UIButton tracks it and sends its control events). The touch is built the way
	/// UIKit builds one, with an IOHIDEvent digitizer event behind it, and sent through <c>UIApplication.sendEvent:</c> with the application's
	/// touches event (private API, which the UI test frameworks for UIKit use the same way). <see cref="NativePress"/> and
	/// <see cref="NativeRelease"/> only reach the behavior's recognizer.
	/// </summary>
	protected static async Task TapThroughUIKitAsync(UIView view)
	{
		var window = view.Window ?? throw new InvalidOperationException("The view is not in a window.");
		var pointInWindow = view.ConvertPointToView(new CGPoint(view.Bounds.Width / 2, view.Bounds.Height / 2), null);
		var hitView = window.HitTest(pointInWindow, null) ?? throw new InvalidOperationException("Nothing in the window takes the touch.");
		var application = UIApplication.SharedApplication;
		RequireSelector(application, "_touchesEvent");

		using var touch = CreateObject<UITouch>("UITouch");
		SendMessage(touch, "setWindow:", window.Handle);
		SendMessage(touch, "setTapCount:", 1);
		SendMessage(touch, "_setLocationInWindow:resetPrevious:", pointInWindow, true);
		SendMessage(touch, "setView:", hitView.Handle);
		SendMessage(touch, "_setIsFirstTouchForView:", true);

		if (touch.RespondsToSelector(new Selector("setGestureView:")))
		{
			SendMessage(touch, "setGestureView:", hitView.Handle);
		}

		SendThroughUIKit(application, touch, pointInWindow, UITouchPhase.Began);

		// A finger rests on the screen for a moment; UIKit reports the touch's end in a later turn of the run loop
		await Task.Delay(50);

		SendThroughUIKit(application, touch, pointInWindow, UITouchPhase.Ended);
		await Task.Yield();
	}

	static void SendThroughUIKit(UIApplication application, UITouch touch, CGPoint pointInWindow, UITouchPhase phase)
	{
		SendMessage(touch, "setTimestamp:", NSProcessInfo.ProcessInfo.SystemUptime);
		SendMessage(touch, "setPhase:", (nint)(long)phase);

		var hidEvent = CreateDigitizerEvent(pointInWindow, isTouching: phase is UITouchPhase.Began or UITouchPhase.Moved or UITouchPhase.Stationary);

		try
		{
			if (touch.RespondsToSelector(new Selector("_setHidEvent:")))
			{
				SendMessage(touch, "_setHidEvent:", hidEvent);
			}

			var touchesEvent = Runtime.GetNSObject<UIEvent>(MessageReturningIntPtr(application.Handle, Selector.GetHandle("_touchesEvent")))
				?? throw new InvalidOperationException("UIApplication has no touches event.");
			RequireSelector(touchesEvent, "_clearTouches");
			RequireSelector(touchesEvent, "_addTouch:forDelayedDelivery:");
			MessageVoid(touchesEvent.Handle, Selector.GetHandle("_clearTouches"));
			SendMessage(touchesEvent, "_setHIDEvent:", hidEvent);
			MessageIntPtrBool(touchesEvent.Handle, Selector.GetHandle("_addTouch:forDelayedDelivery:"), touch.Handle, false);

			application.SendEvent(touchesEvent);
		}
		finally
		{
			CFRelease(hidEvent);
		}
	}

	// A hand event with one finger at the point, as the digitizer reports it (IOKit's IOHIDEventCreateDigitizerEvent and
	// IOHIDEventCreateDigitizerFingerEventWithQuality; the field and mask values are IOKit's)
	static IntPtr CreateDigitizerEvent(CGPoint pointInWindow, bool isTouching)
	{
		const uint transducerTypeHand = 3;
		const uint eventMaskRangeAndTouch = 3;
		const uint eventMaskTouch = 2;
		const uint fieldDigitizerIsDisplayIntegrated = (11u << 16) + 25;

		var timeStamp = mach_absolute_time();
		var touching = (byte)(isTouching ? 1 : 0);
		var hand = IOHIDEventCreateDigitizerEvent(IntPtr.Zero, timeStamp, transducerTypeHand, 0, 0, eventMaskTouch, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		IOHIDEventSetIntegerValue(hand, fieldDigitizerIsDisplayIntegrated, 1);

		var finger = IOHIDEventCreateDigitizerFingerEventWithQuality(IntPtr.Zero, timeStamp, 0, 2, eventMaskRangeAndTouch, pointInWindow.X, pointInWindow.Y, 0, 0, 0, 5, 5, 1, 1, 1, touching, touching, 0);
		IOHIDEventSetIntegerValue(finger, fieldDigitizerIsDisplayIntegrated, 1);
		IOHIDEventAppendEvent(hand, finger, 0);
		CFRelease(finger);

		return hand;
	}

	const string ioKitLibrary = "/System/Library/Frameworks/IOKit.framework/IOKit";

	[DllImport(ioKitLibrary)]
	static extern IntPtr IOHIDEventCreateDigitizerEvent(IntPtr allocator, ulong timeStamp, uint transducerType, uint index, uint identity, uint eventMask, uint buttonMask, double x, double y, double z, double tipPressure, double barrelPressure, byte range, byte touch, uint options);

	[DllImport(ioKitLibrary)]
	static extern IntPtr IOHIDEventCreateDigitizerFingerEventWithQuality(IntPtr allocator, ulong timeStamp, uint index, uint identity, uint eventMask, double x, double y, double z, double tipPressure, double twist, double minorRadius, double majorRadius, double quality, double density, double irregularity, byte range, byte touch, uint options);

	[DllImport(ioKitLibrary)]
	static extern void IOHIDEventAppendEvent(IntPtr parent, IntPtr child, uint options);

	[DllImport(ioKitLibrary)]
	static extern void IOHIDEventSetIntegerValue(IntPtr hidEvent, uint field, int value);

	[DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
	static extern void CFRelease(IntPtr cf);

	[DllImport("/usr/lib/libSystem.dylib")]
	static extern ulong mach_absolute_time();

	[DllImport(objCLibrary, EntryPoint = "objc_msgSend")]
	static extern void MessageIntPtrBool(IntPtr receiver, IntPtr selector, IntPtr argument, [MarshalAs(UnmanagedType.I1)] bool flag);

	// UIHoverGestureRecognizer reports through its target/action pair; UIKit delivers the action after the state was set.
	protected async Task NativeHoverEnterAsync(IPlatformViewHandler handler) =>
		await SetHoverStateAsync(handler, UIGestureRecognizerState.Began);

	protected async Task NativeHoverMoveAsync(IPlatformViewHandler handler) =>
		await SetHoverStateAsync(handler, UIGestureRecognizerState.Changed);

	protected async Task NativeHoverExitAsync(IPlatformViewHandler handler) =>
		await SetHoverStateAsync(handler, UIGestureRecognizerState.Ended);

	protected static async Task SetHoverStateAsync(IPlatformViewHandler handler, UIGestureRecognizerState state)
	{
		var hover = GetHoverRecognizer(GetPlatformView(handler));
		await WaitUntilAsync(() => hover.State is UIGestureRecognizerState.Possible or UIGestureRecognizerState.Began or UIGestureRecognizerState.Changed, "The hover recognizer is not ready for a new state.");
		hover.State = state;
	}

	protected void ReleaseTouch()
	{
		activeTouch?.Dispose();
		activeTouch = null;
		activeEvent?.Dispose();
		activeEvent = null;
	}

	ValueTask PlatformDisposeAsync()
	{
		ReleaseTouch();
		return ValueTask.CompletedTask;
	}

	protected static UITouch CreateTouch(UIView view, double x, double y, UITouchPhase phase)
	{
		var touch = CreateObject<UITouch>("UITouch");
		var window = view.Window;
		SendMessage(touch, "setWindow:", window?.Handle ?? IntPtr.Zero);
		SendMessage(touch, "setView:", view.Handle);
		SendMessage(touch, "setTapCount:", 1);
		SendMessage(touch, "setTimestamp:", NSProcessInfo.ProcessInfo.SystemUptime);
		SendMessage(touch, "_setIsFirstTouchForView:", true);
		UpdateTouch(touch, view, x, y, phase);
		return touch;
	}

	protected static void UpdateTouch(UITouch touch, UIView view, double x, double y, UITouchPhase phase)
	{
		var pointInWindow = view.ConvertPointToView(new CGPoint(x, y), null);
		SendMessage(touch, "setPhase:", (nint)(long)phase);
		SendMessage(touch, "_setLocationInWindow:resetPrevious:", pointInWindow, false);
	}

	protected static T CreateObject<T>(string className) where T : NSObject
	{
		var allocated = MessageReturningIntPtr(Class.GetHandle(className), Selector.GetHandle("alloc"));
		var initialized = MessageReturningIntPtr(allocated, Selector.GetHandle("init"));
		var instance = Runtime.GetNSObject<T>(initialized) ?? throw new InvalidOperationException($"Could not create {className}");

		// The managed wrapper took its own reference; give back the one alloc/init returned.
		MessageVoid(initialized, Selector.GetHandle("release"));
		return instance;
	}

	static void RequireSelector(NSObject target, string selector) =>
		Skip.IfNot(target.RespondsToSelector(new Selector(selector)), $"This OS version no longer has UIKit's private {selector}; the touch cannot be built.");

	protected static void SendMessage(NSObject target, string selector, IntPtr argument)
	{
		RequireSelector(target, selector);
		MessageIntPtr(target.Handle, Selector.GetHandle(selector), argument);
	}

	protected static void SendMessage(NSObject target, string selector, double argument)
	{
		RequireSelector(target, selector);
		MessageDouble(target.Handle, Selector.GetHandle(selector), argument);
	}

	protected static void SendMessage(NSObject target, string selector, bool argument)
	{
		RequireSelector(target, selector);
		MessageBool(target.Handle, Selector.GetHandle(selector), argument);
	}

	protected static void SendMessage(NSObject target, string selector, CGPoint point, bool flag)
	{
		RequireSelector(target, selector);
		MessagePointBool(target.Handle, Selector.GetHandle(selector), point, flag);
	}

	[DllImport(objCLibrary, EntryPoint = "objc_msgSend")]
	static extern IntPtr MessageReturningIntPtr(IntPtr receiver, IntPtr selector);

	[DllImport(objCLibrary, EntryPoint = "objc_msgSend")]
	static extern void MessageVoid(IntPtr receiver, IntPtr selector);

	[DllImport(objCLibrary, EntryPoint = "objc_msgSend")]
	static extern void MessageIntPtr(IntPtr receiver, IntPtr selector, IntPtr argument);

	[DllImport(objCLibrary, EntryPoint = "objc_msgSend")]
	static extern void MessageDouble(IntPtr receiver, IntPtr selector, double argument);

	[DllImport(objCLibrary, EntryPoint = "objc_msgSend")]
	static extern void MessageBool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool argument);

	[DllImport(objCLibrary, EntryPoint = "objc_msgSend")]
	static extern void MessagePointBool(IntPtr receiver, IntPtr selector, CGPoint point, [MarshalAs(UnmanagedType.I1)] bool flag);

	/// <summary>Disposes an attached <see cref="DirectTouchBehavior"/> the way an app does (<c>Dispose()</c> without a detach first).</summary>
	protected static void DisposeWhileAttached(DirectTouchBehavior behavior) => behavior.DisposeDirect(true);

	/// <summary>
	/// A TouchBehavior that can be attached to a platform view that MAUI knows nothing about: the native hierarchy is built by the
	/// test, so the test controls the parents and the lifetime of the platform view.
	/// </summary>
	protected sealed class DirectTouchBehavior : TouchBehavior
	{
		public void AttachDirect(VisualElement bindable, UIView platformView) => OnAttachedTo(bindable, platformView);

		public void DetachDirect(VisualElement bindable, UIView platformView) => OnDetachedFrom(bindable, platformView);

		public void DisposeDirect(bool disposing) => Dispose(disposing);

		/// <summary>Sets the behavior's view without running the effects that <c>Element</c> triggers.</summary>
		public void SetViewOnly(VisualElement? view) => View = view;
	}
}