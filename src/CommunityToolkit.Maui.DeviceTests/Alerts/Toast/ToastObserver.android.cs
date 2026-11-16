using Android.App;
using Android.Text;
using Android.Text.Style;
using Android.Views.Accessibility;
using CommunityToolkit.Maui.DeviceTests.Runners.HeadlessRunner;
using Java.Interop;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using Toast = Android.Widget.Toast;

namespace CommunityToolkit.Maui.DeviceTests;

// Android sends an accessibility event for every toast it displays; it carries the text passed to Toast.makeText, including its spans,
// and the package of the app.
public sealed record ToastEvent(string Text, string ClassName, string PackageName, IReadOnlyList<ToastSizeSpan> SizeSpans);

public sealed record ToastSizeSpan(int Size, bool IsDip, int Start, int End);

// The lifetime of a toast comes from Toast.Callback on Android 11 and later, and from the attachment of the toast's view to the window
// before that (older toasts own a real view, newer ones have none). Both keep working after a test disposed the Java peer of the toast,
// because the callback and the view are Java objects of their own.
public sealed class ObservedToast
{
	readonly Android.Views.View? legacyView;

	bool isShown;
	bool isHidden;

	public ObservedToast(Toast platformToast)
	{
		Platform = platformToast;
		legacyView = OperatingSystem.IsAndroidVersionAtLeast(30) ? null : platformToast.View;
	}

	public Toast Platform { get; }

	public ToastEvent? Event { get; internal set; }

	public bool IsDisplayed => OperatingSystem.IsAndroidVersionAtLeast(30)
		? isShown && !isHidden
		: legacyView?.IsAttachedToWindow == true;

	internal void MarkShown() => isShown = true;

	internal void MarkHidden() => isHidden = true;
}

// Side effect: connecting the instrumentation's UiAutomation switches an accessibility service on, and AccessibilityManager.IsEnabled
// stays true in the test process until that connection is undone. Left alone, every test that runs after the first Toast test, of any
// area of the app, would run with accessibility enabled (views send accessibility events, and code that branches on
// AccessibilityManager.IsEnabled takes its accessibility arm; e.g. TouchBehaviorInputTests.RawTouchesAreHandledWhileNoAccessibilityServiceRuns
// cannot run then before API 33). So each test that observes toasts switches it off again (ReleaseAccessibilityServiceAsync).
public sealed class ToastObserver : IDisposable
{
	// UiAutomation delivers every accessibility event of the device, on the main thread, to ONE listener. The Java peer of that listener
	// must outlive every test: an event that is still queued when a test ends would call into a disposed peer and abort the whole
	// process ("Unable to activate instance of type ... from native handle"). So the listener is created once and never disposed, and
	// each observer only subscribes to it. It is installed on whatever UiAutomation object the instrumentation hands out: before API 33
	// releasing the accessibility service destroys that object, and the next request creates a new one without a listener.
	static readonly object installLock = new();
	static AccessibilityListener? sharedListener;

	readonly AccessibilityListener listener;
	readonly List<ToastEvent> events = [];
	readonly List<ObservedToast> toasts = [];

	ToastObserver(AccessibilityListener listener)
	{
		this.listener = listener;
		listener.Subscribe(this);
	}

	public IReadOnlyList<ObservedToast> Toasts => toasts;

	public IReadOnlyList<ToastEvent> Events
	{
		get
		{
			lock (events)
			{
				return [.. events];
			}
		}
	}

	public static ToastObserver Create(UiAutomation automation)
	{
		lock (installLock)
		{
			sharedListener ??= new AccessibilityListener();

			// Installing the same listener again on the same UiAutomation changes nothing; a new UiAutomation (see sharedListener) needs it.
			automation.SetOnAccessibilityEventListener(sharedListener);

			return new ToastObserver(sharedListener);
		}
	}

	// Returns true even when the platform refuses the hidden destroy() (before API 33): the service then stays on, as it did before this
	// method existed.
	public static async Task<bool> ReleaseAccessibilityServiceAsync(AccessibilityManager accessibilityManager)
	{
		if (MauiTestInstrumentation.Current is not MauiTestInstrumentation instrumentation)
		{
			return !accessibilityManager.IsEnabled;
		}

		// Both calls disconnect the UiAutomation, and from API 33 reconnect it, which waits for callbacks on the main looper: never on the UI thread.
		var released = await Task.Run(() =>
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(33))
			{
				instrumentation.GetUiAutomation(UiAutomationFlags.DontUseAccessibility);
				return true;
			}

			return TryDestroyUiAutomation(instrumentation);
		});

		return !released || await Wait(() => !accessibilityManager.IsEnabled, timeout: 5000, interval: 50);
	}

	// Before API 33 there is no public way to disconnect the instrumentation's UiAutomation; TouchBehaviorTestBase.TryDestroyUiAutomation does the same.
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
			// The platform refuses the hidden method (hidden API restrictions): the accessibility service stays on, which
			// ReleaseAccessibilityServiceAsync accepts.
			return false;
		}
	}

	// Call it right after the toolkit showed the toast: the accessibility event of the toast is taken to be the first one after the
	// current count of events.
	public async Task<ObservedToast> WaitForDisplayAsync(Toast platformToast)
	{
		var observed = new ObservedToast(platformToast);
		var eventsBefore = Events.Count;
		toasts.Add(observed);

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			platformToast.AddCallback(new ToastCallback(observed));
		}

		await AssertEventually(() => observed.IsDisplayed, timeout: 10000, message: "The system never displayed the toast.");
		await AssertEventually(() => Events.Count > eventsBefore, timeout: 10000, message: "The system never sent the accessibility event of the toast.");
		observed.Event = Events[eventsBefore];

		return observed;
	}

	// A toast whose Java peer a test disposed while it was displayed cannot be cancelled: the system displays it until it times out
	// (3.5 s for a long toast) and queues every later toast behind it, so the next test would start with it still on screen.
	public async Task<bool> WaitForToastsThatCannotBeCancelledAsync()
	{
		var stuck = toasts.Where(toast => toast.Platform.Handle == IntPtr.Zero && toast.IsDisplayed).ToList();
		if (stuck.Count == 0)
		{
			return false;
		}

		await Wait(() => stuck.All(toast => !toast.IsDisplayed), timeout: 10000);
		return true;
	}

	public void Dispose() => listener.Unsubscribe(this);

	void Receive(ToastEvent toastEvent)
	{
		lock (events)
		{
			events.Add(toastEvent);
		}
	}

	sealed class ToastCallback(ObservedToast observed) : Toast.Callback
	{
		public override void OnToastShown() => observed.MarkShown();

		public override void OnToastHidden() => observed.MarkHidden();
	}

	sealed class AccessibilityListener : Java.Lang.Object, UiAutomation.IOnAccessibilityEventListener
	{
		readonly List<ToastObserver> observers = [];

		public AccessibilityListener()
		{
		}

		// The activation constructor the runtime uses when Java calls into a managed peer that was never created from managed code.
		public AccessibilityListener(IntPtr handle, Android.Runtime.JniHandleOwnership transfer) : base(handle, transfer)
		{
		}

		public void Subscribe(ToastObserver observer)
		{
			lock (observers)
			{
				observers.Add(observer);
			}
		}

		public void Unsubscribe(ToastObserver observer)
		{
			lock (observers)
			{
				observers.Remove(observer);
			}
		}

		public void OnAccessibilityEvent(AccessibilityEvent? e)
		{
			var className = e?.ClassName?.ToString() ?? string.Empty;
			if (e is null || !className.StartsWith("android.widget.Toast", StringComparison.Ordinal))
			{
				return;
			}

			var text = e.Text?.FirstOrDefault();
			var toastEvent = new ToastEvent(text?.ToString() ?? string.Empty, className, e.PackageName?.ToString() ?? string.Empty, ReadSizeSpans(text));
			lock (observers)
			{
				foreach (var observer in observers)
				{
					observer.Receive(toastEvent);
				}
			}
		}

		static List<ToastSizeSpan> ReadSizeSpans(Java.Lang.ICharSequence? text)
		{
			var result = new List<ToastSizeSpan>();
			if (text is null)
			{
				return result;
			}

			try
			{
				var spanned = text.JavaCast<ISpanned>();
				var spans = spanned.GetSpans(0, text.Length(), Java.Lang.Class.FromType(typeof(AbsoluteSizeSpan)));
				foreach (var span in spans ?? [])
				{
					var sizeSpan = span.JavaCast<AbsoluteSizeSpan>();
					result.Add(new ToastSizeSpan(sizeSpan.Size, sizeSpan.Dip, spanned.GetSpanStart(span), spanned.GetSpanEnd(span)));
				}
			}
			catch (InvalidCastException)
			{
				// The text carries no spans.
			}

			return result;
		}
	}
}