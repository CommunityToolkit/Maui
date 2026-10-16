#pragma warning disable CA1416, CA1422, CS0618, CS0672 // the spy window overrides every abstract Window member, obsolete ones included
using System.Diagnostics;
using System.Reflection;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.LifecycleEvents;
using AColor = Android.Graphics.Color;
using ADialog = Android.App.Dialog;
using ADialogFragment = AndroidX.Fragment.App.DialogFragment;
using AFragment = AndroidX.Fragment.App.Fragment;
using AFragmentManager = AndroidX.Fragment.App.FragmentManager;
using AView = Android.Views.View;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// Test doubles and hosting helpers shared by the Android fragment tests (DialogFragmentService, FragmentLifecycleManager and the
// Android block of Core's AppBuilderExtensions).

/// <summary>A DialogFragment that creates a real dialog, or the dialog its factory returns.</summary>
public class ProbeDialogFragment : ADialogFragment
{
	public ProbeDialogFragment()
	{
	}

	protected ProbeDialogFragment(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
	{
	}

	public Func<Context, ADialog>? DialogFactory { get; set; }

	public override ADialog OnCreateDialog(Bundle? savedInstanceState) =>
		DialogFactory?.Invoke(RequireContext()) ?? new AppCompatDialog(RequireContext());
}

/// <summary>A real dialog whose Window property (what managed code reads) is the given spy window, or null.</summary>
public sealed class SpyDialog : AppCompatDialog
{
	public SpyDialog(Context context) : base(context)
	{
	}

	public AWindow? SpyWindow { get; set; }

	/// <summary>Makes the Window property that managed code reads return null (the framework still has its window).</summary>
	public bool ReportsNoWindow { get; set; }

	public override AWindow? Window => ReportsNoWindow ? null : SpyWindow ?? base.Window;
}

/// <summary>A fragment (not a DialogFragment) with a view, so every lifecycle callback fires.</summary>
public class PlainFragment : AFragment
{
	public PlainFragment()
	{
	}

	protected PlainFragment(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
	{
	}

	public override AView OnCreateView(Android.Views.LayoutInflater inflater, Android.Views.ViewGroup? container, Bundle? savedInstanceState) => new(RequireContext());
}

/// <summary>A plain android.app.Activity (not an AppCompatActivity) that was never attached; only the members the toolkit reads are usable.</summary>
public sealed class PlainActivity : Android.App.Activity
{
	public PlainActivity()
	{
	}

	public override string LocalClassName => "Test.PlainActivity";
}

/// <summary>
/// An AppCompatActivity that was never attached to the framework, whose Window and SupportFragmentManager are whatever the test says.
/// The toolkit only reads those two members, from managed code, so the overrides are what it sees.
/// </summary>
public sealed class SpyActivity : AppCompatActivity
{
	public SpyActivity()
	{
	}

	public AWindow? SpyWindow { get; set; }

	public AFragmentManager? SpyFragmentManager { get; set; }

	public override AWindow? Window => SpyWindow;

	public override AFragmentManager SupportFragmentManager => SpyFragmentManager!;

	public override string LocalClassName => "Test.SpyActivity";
}

/// <summary>An IDialogFragmentService that records every call with its arguments.</summary>
public sealed class RecordingDialogFragmentService : IDialogFragmentService
{
	readonly List<(string Name, object?[] Arguments)> calls = [];

	public IReadOnlyList<(string Name, object?[] Arguments)> Calls
	{
		get
		{
			lock (calls)
			{
				return [.. calls];
			}
		}
	}

	public IReadOnlyList<string> CallsFor(AFragment fragment) =>
		[.. Calls.Where(call => call.Arguments.Length > 1 && ReferenceEquals(call.Arguments[1], fragment)).Select(call => call.Name)];

	public void OnFragmentAttached(AFragmentManager fm, AFragment f, Context context) => Record(nameof(OnFragmentAttached), fm, f, context);

	public void OnFragmentCreated(AFragmentManager fm, AFragment f, Bundle? savedInstanceState) => Record(nameof(OnFragmentCreated), fm, f, savedInstanceState);

	public void OnFragmentDestroyed(AFragmentManager fm, AFragment f) => Record(nameof(OnFragmentDestroyed), fm, f);

	public void OnFragmentDetached(AFragmentManager fm, AFragment f) => Record(nameof(OnFragmentDetached), fm, f);

	public void OnFragmentPaused(AFragmentManager fm, AFragment f) => Record(nameof(OnFragmentPaused), fm, f);

	public void OnFragmentPreAttached(AFragmentManager fm, AFragment f, Context context) => Record(nameof(OnFragmentPreAttached), fm, f, context);

	public void OnFragmentPreCreated(AFragmentManager fm, AFragment f, Bundle? savedInstanceState) => Record(nameof(OnFragmentPreCreated), fm, f, savedInstanceState);

	public void OnFragmentResumed(AFragmentManager fm, AFragment f) => Record(nameof(OnFragmentResumed), fm, f);

	public void OnFragmentSaveInstanceState(AFragmentManager fm, AFragment f, Bundle outState) => Record(nameof(OnFragmentSaveInstanceState), fm, f, outState);

	public void OnFragmentStarted(AFragmentManager fm, AFragment f) => Record(nameof(OnFragmentStarted), fm, f);

	public void OnFragmentStopped(AFragmentManager fm, AFragment f) => Record(nameof(OnFragmentStopped), fm, f);

	public void OnFragmentViewCreated(AFragmentManager fm, AFragment f, AView v, Bundle? savedInstanceState) => Record(nameof(OnFragmentViewCreated), fm, f, v, savedInstanceState);

	public void OnFragmentViewDestroyed(AFragmentManager fm, AFragment f) => Record(nameof(OnFragmentViewDestroyed), fm, f);

	void Record(string name, params object?[] arguments)
	{
		lock (calls)
		{
			calls.Add((name, arguments));
		}
	}
}

/// <summary>An IDialogFragmentService whose every member throws, to show that FragmentLifecycleManager does not swallow exceptions.</summary>
public sealed class ThrowingDialogFragmentService : IDialogFragmentService
{
	public void OnFragmentAttached(AFragmentManager fm, AFragment f, Context context) => throw new InvalidOperationException(nameof(OnFragmentAttached));

	public void OnFragmentCreated(AFragmentManager fm, AFragment f, Bundle? savedInstanceState) => throw new InvalidOperationException(nameof(OnFragmentCreated));

	public void OnFragmentDestroyed(AFragmentManager fm, AFragment f) => throw new InvalidOperationException(nameof(OnFragmentDestroyed));

	public void OnFragmentDetached(AFragmentManager fm, AFragment f) => throw new InvalidOperationException(nameof(OnFragmentDetached));

	public void OnFragmentPaused(AFragmentManager fm, AFragment f) => throw new InvalidOperationException(nameof(OnFragmentPaused));

	public void OnFragmentPreAttached(AFragmentManager fm, AFragment f, Context context) => throw new InvalidOperationException(nameof(OnFragmentPreAttached));

	public void OnFragmentPreCreated(AFragmentManager fm, AFragment f, Bundle? savedInstanceState) => throw new InvalidOperationException(nameof(OnFragmentPreCreated));

	public void OnFragmentResumed(AFragmentManager fm, AFragment f) => throw new InvalidOperationException(nameof(OnFragmentResumed));

	public void OnFragmentSaveInstanceState(AFragmentManager fm, AFragment f, Bundle outState) => throw new InvalidOperationException(nameof(OnFragmentSaveInstanceState));

	public void OnFragmentStarted(AFragmentManager fm, AFragment f) => throw new InvalidOperationException(nameof(OnFragmentStarted));

	public void OnFragmentStopped(AFragmentManager fm, AFragment f) => throw new InvalidOperationException(nameof(OnFragmentStopped));

	public void OnFragmentViewCreated(AFragmentManager fm, AFragment f, AView v, Bundle? savedInstanceState) => throw new InvalidOperationException(nameof(OnFragmentViewCreated));

	public void OnFragmentViewDestroyed(AFragmentManager fm, AFragment f) => throw new InvalidOperationException(nameof(OnFragmentViewDestroyed));
}

/// <summary>An IPlatformApplication that exposes the given services, as IPlatformApplication.Current.</summary>
public sealed class FakePlatformApplication(IServiceProvider services) : IPlatformApplication
{
	public IServiceProvider Services { get; } = services;

	public IApplication Application => throw new NotSupportedException("The fake platform application has no IApplication.");
}

/// <summary>Replaces IPlatformApplication.Current for the lifetime of the scope (null is allowed).</summary>
public sealed class PlatformApplicationScope : IDisposable
{
	readonly IPlatformApplication? previous;

	public PlatformApplicationScope(IPlatformApplication? replacement)
	{
		previous = IPlatformApplication.Current;
		IPlatformApplication.Current = replacement;
	}

	public void Dispose() => IPlatformApplication.Current = previous;
}

/// <summary>
/// Replaces Platform.CurrentActivity for the lifetime of the scope. .NET MAUI offers no public way to set the current activity
/// without registering another lifecycle listener (Platform.Init), so the activity of its ActivityLifecycleContextListener is set
/// through reflection and restored on dispose.
/// </summary>
public sealed class CurrentActivityScope : IDisposable
{
	readonly Android.App.Activity? previous = Platform.CurrentActivity;

	public CurrentActivityScope(Android.App.Activity? replacement) => Set(replacement);

	public static void Set(Android.App.Activity? activity)
	{
		var manager = ActivityStateManager.Default;
		var listenerField = manager.GetType().GetField("lifecycleListener", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"{manager.GetType().FullName} has no 'lifecycleListener' field: .NET MAUI changed its ActivityStateManager.");
		var listener = listenerField.GetValue(manager)
			?? throw new InvalidOperationException("The ActivityStateManager has no lifecycle listener: Platform.Init was not called.");
		var activityProperty = listener.GetType().GetProperty("Activity", BindingFlags.Instance | BindingFlags.Public)
			?? throw new InvalidOperationException($"{listener.GetType().FullName} has no 'Activity' property: .NET MAUI changed its ActivityStateManager.");

		activityProperty.SetValue(listener, activity);
	}

	public void Dispose() => Set(previous);
}

/// <summary>
/// Undoes, on dispose, what Platform.Init(Application) did to the runner while the scope was alive. The Essentials registration of every
/// MauiApp calls it when the app's ILifecycleEventService is created: it registers a new ActivityLifecycleContextListener on the
/// Application, makes it the listener of ActivityStateManager.Default (Platform.CurrentActivity, Platform.ActivityStateChanged), and
/// never unregisters the listener it had (decompiled .NET MAUI 10.0.90: ActivityStateManagerImplementation.Init, UseEssentials). Without
/// the scope, every test that creates the service would leave one more listener registered for the rest of the launch, and every later
/// activity event would be raised once more to the subscribers of Platform.ActivityStateChanged. The scope unregisters the listener the
/// ActivityStateManager holds at the end, the last one registered: a test MauiApp has one Essentials registration, so one is registered
/// (measured on Android API 36 and 28: 2 listeners before and after the scope, 3 after the same resolution without it).
/// </summary>
public sealed class ActivityStateListenerScope : IDisposable
{
	readonly IActivityStateManager manager = ActivityStateManager.Default;
	readonly FieldInfo listenerField;
	readonly object? previous;

	public ActivityStateListenerScope()
	{
		listenerField = manager.GetType().GetField("lifecycleListener", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"{manager.GetType().FullName} has no 'lifecycleListener' field: .NET MAUI changed its ActivityStateManager.");
		previous = listenerField.GetValue(manager);
	}

	public void Dispose()
	{
		var current = listenerField.GetValue(manager);
		if (ReferenceEquals(current, previous))
		{
			return;
		}

		if (current is Android.App.Application.IActivityLifecycleCallbacks added)
		{
			((Android.App.Application)Android.App.Application.Context).UnregisterActivityLifecycleCallbacks(added);
		}

		listenerField.SetValue(manager, previous);
	}
}

/// <summary>Collects System.Diagnostics.Trace output while it is alive.</summary>
public sealed class FragmentTraceListener : TraceListener
{
	readonly List<string> messages = [];

	public FragmentTraceListener() => System.Diagnostics.Trace.Listeners.Add(this);

	public IReadOnlyList<string> Messages
	{
		get
		{
			lock (messages)
			{
				return [.. messages];
			}
		}
	}

	public override void Write(string? message) => Add(message);

	public override void WriteLine(string? message) => Add(message);

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			System.Diagnostics.Trace.Listeners.Remove(this);
		}

		base.Dispose(disposing);
	}

	void Add(string? message)
	{
		lock (messages)
		{
			messages.Add(message ?? string.Empty);
		}
	}
}

/// <summary>An IWindowInsetsController that reports a configured appearance and records every SetSystemBarsAppearance call.</summary>
public sealed class SpyInsetsController : Java.Lang.Object, IWindowInsetsController
{
	readonly List<(int Appearance, int Mask)> appearanceCalls = [];

	public int ReportedAppearance { get; set; }

	public IReadOnlyList<(int Appearance, int Mask)> AppearanceCalls
	{
		get
		{
			lock (appearanceCalls)
			{
				return [.. appearanceCalls];
			}
		}
	}

	public int SystemBarsAppearance => ReportedAppearance;

	public int SystemBarsBehavior { get; set; }

	public void SetSystemBarsAppearance(int appearance, int mask)
	{
		lock (appearanceCalls)
		{
			appearanceCalls.Add((appearance, mask));
		}

		ReportedAppearance = (ReportedAppearance & ~mask) | (appearance & mask);
	}

	public void AddOnControllableInsetsChangedListener(IWindowInsetsController.IOnControllableInsetsChangedListener listener) => throw new NotSupportedException();

	public void ControlWindowInsetsAnimation(int types, long durationMillis, Android.Views.Animations.IInterpolator? interpolator, CancellationSignal? cancellationSignal, IWindowInsetsAnimationControlListener listener) => throw new NotSupportedException();

	public void Hide(int types) => throw new NotSupportedException();

	public void RemoveOnControllableInsetsChangedListener(IWindowInsetsController.IOnControllableInsetsChangedListener listener) => throw new NotSupportedException();

	public void Show(int types) => throw new NotSupportedException();
}

/// <summary>
/// A Window that reports a configured status bar color and insets controller and records what the status bar handling writes:
/// SetStatusBarColor, SetDecorFitsSystemWindows, ClearFlags and SetFlags. It lets tests reach states a real window cannot be in
/// (an insets controller that is null, a status bar color that is not ignored on Android 15 and newer). Every other member is unsupported.
/// </summary>
public sealed class SpyWindow : AWindow
{
	readonly List<int> statusBarColorCalls = [];
	readonly List<bool> decorFitsSystemWindowsCalls = [];
	readonly List<WindowManagerFlags> clearFlagsCalls = [];
	readonly List<(WindowManagerFlags Flags, WindowManagerFlags Mask)> setFlagsCalls = [];

	public SpyWindow(Context context) : base(context)
	{
	}

	public int ReportedStatusBarColor { get; set; }

	public IWindowInsetsController? ReportedInsetsController { get; set; }

	public IReadOnlyList<int> StatusBarColorCalls => Snapshot(statusBarColorCalls);

	public IReadOnlyList<bool> DecorFitsSystemWindowsCalls => Snapshot(decorFitsSystemWindowsCalls);

	public IReadOnlyList<WindowManagerFlags> ClearFlagsCalls => Snapshot(clearFlagsCalls);

	public IReadOnlyList<(WindowManagerFlags Flags, WindowManagerFlags Mask)> SetFlagsCalls => Snapshot(setFlagsCalls);

	public override int StatusBarColor => ReportedStatusBarColor;

	public override IWindowInsetsController? InsetsController => ReportedInsetsController;

	public override void SetStatusBarColor(AColor color)
	{
		lock (statusBarColorCalls)
		{
			statusBarColorCalls.Add(color.ToArgb());
		}

		ReportedStatusBarColor = color.ToArgb();
	}

	public override void SetDecorFitsSystemWindows(bool decorFitsSystemWindows)
	{
		lock (decorFitsSystemWindowsCalls)
		{
			decorFitsSystemWindowsCalls.Add(decorFitsSystemWindows);
		}
	}

	public override void ClearFlags(WindowManagerFlags flags)
	{
		lock (clearFlagsCalls)
		{
			clearFlagsCalls.Add(flags);
		}
	}

	public override void SetFlags(WindowManagerFlags flags, WindowManagerFlags mask)
	{
		lock (setFlagsCalls)
		{
			setFlagsCalls.Add((flags, mask));
		}
	}

	public override AView? CurrentFocus => throw new NotSupportedException();

	public override AView DecorView => throw new NotSupportedException();

	public override bool IsFloating => throw new NotSupportedException();

	public override LayoutInflater LayoutInflater => throw new NotSupportedException();

	public override int NavigationBarColor => throw new NotSupportedException();

	public override Android.Media.Stream VolumeControlStream
	{
		get => throw new NotSupportedException();
		set => throw new NotSupportedException();
	}

	public override void AddContentView(AView? view, ViewGroup.LayoutParams? @params) => throw new NotSupportedException();

	public override void CloseAllPanels() => throw new NotSupportedException();

	public override void ClosePanel(int featureId) => throw new NotSupportedException();

	public override void InvalidatePanelMenu(WindowFeatures featureId) => throw new NotSupportedException();

	public override bool IsShortcutKey(Keycode keyCode, KeyEvent? e) => throw new NotSupportedException();

	protected override void OnActive() => throw new NotSupportedException();

	public override void OnConfigurationChanged(Android.Content.Res.Configuration? newConfig) => throw new NotSupportedException();

	public override void OpenPanel(int featureId, KeyEvent? e) => throw new NotSupportedException();

	public override AView? PeekDecorView() => throw new NotSupportedException();

	public override bool PerformContextMenuIdentifierAction(int id, MenuPerformFlags flags) => throw new NotSupportedException();

	public override bool PerformPanelIdentifierAction(int featureId, int id, MenuPerformFlags flags) => throw new NotSupportedException();

	public override bool PerformPanelShortcut(int featureId, Keycode keyCode, KeyEvent? e, MenuPerformFlags flags) => throw new NotSupportedException();

	public override void RestoreHierarchyState(Bundle? savedInstanceState) => throw new NotSupportedException();

	public override Bundle? SaveHierarchyState() => throw new NotSupportedException();

	public override void SetBackgroundDrawable(Android.Graphics.Drawables.Drawable? drawable) => throw new NotSupportedException();

	public override void SetChildDrawable(int featureId, Android.Graphics.Drawables.Drawable? drawable) => throw new NotSupportedException();

	public override void SetChildInt(int featureId, int value) => throw new NotSupportedException();

	public override void SetContentView(AView? view, ViewGroup.LayoutParams? @params) => throw new NotSupportedException();

	public override void SetContentView(AView? view) => throw new NotSupportedException();

	public override void SetContentView(int layoutResID) => throw new NotSupportedException();

	public override void SetDecorCaptionShade(DecorCaptionShade decorCaptionShade) => throw new NotSupportedException();

	public override void SetFeatureDrawable(WindowFeatures featureId, Android.Graphics.Drawables.Drawable? drawable) => throw new NotSupportedException();

	public override void SetFeatureDrawableAlpha(WindowFeatures featureId, int alpha) => throw new NotSupportedException();

	public override void SetFeatureDrawableResource(WindowFeatures featureId, int resId) => throw new NotSupportedException();

	public override void SetFeatureDrawableUri(WindowFeatures featureId, Android.Net.Uri? uri) => throw new NotSupportedException();

	public override void SetFeatureInt(WindowFeatures featureId, int value) => throw new NotSupportedException();

	public override void SetNavigationBarColor(AColor color) => throw new NotSupportedException();

	public override void SetResizingCaptionDrawable(Android.Graphics.Drawables.Drawable? drawable) => throw new NotSupportedException();

	public override void SetTitle(Java.Lang.ICharSequence? title) => throw new NotSupportedException();

	public override void SetTitleColor(AColor textColor) => throw new NotSupportedException();

	public override bool SuperDispatchGenericMotionEvent(MotionEvent? e) => throw new NotSupportedException();

	public override bool SuperDispatchKeyEvent(KeyEvent? e) => throw new NotSupportedException();

	public override bool SuperDispatchKeyShortcutEvent(KeyEvent? e) => throw new NotSupportedException();

	public override bool SuperDispatchTouchEvent(MotionEvent? e) => throw new NotSupportedException();

	public override bool SuperDispatchTrackballEvent(MotionEvent? e) => throw new NotSupportedException();

	public override void TakeInputQueue(InputQueue.ICallback? callback) => throw new NotSupportedException();

	public override void TakeKeyEvents(bool get) => throw new NotSupportedException();

	public override void TakeSurface(ISurfaceHolderCallback2? callback) => throw new NotSupportedException();

	public override void TogglePanel(int featureId, KeyEvent? e) => throw new NotSupportedException();

	static IReadOnlyList<T> Snapshot<T>(List<T> list)
	{
		lock (list)
		{
			return [.. list];
		}
	}
}

/// <summary>Base of the Android fragment tests: hosts fragments in the runner's AppCompatActivity.</summary>
public abstract class FragmentTestBase : CommunityToolkitHandlerTestBase
{
	/// <summary>A color the runner's themes never use, so "was copied" and "was left alone" are distinguishable.</summary>
	protected const int distinctOpaqueColor = unchecked((int)0xFF336699);

	/// <summary>A status bar color a dialog starts with in the tests, so a call that does change it is visible.</summary>
	protected const int dialogMarkerColor = unchecked((int)0xFFAA5500);

	protected static readonly int lightStatusBarsBit = (int)WindowInsetsControllerAppearance.LightStatusBars;

	readonly Android.App.Activity? currentActivityAtStart = Platform.CurrentActivity;

	/// <summary>The runner's activity (headless: TestActivity, visual runner: MainActivity).</summary>
	protected static AppCompatActivity HostActivity =>
		TestWindow.PlatformWindow as AppCompatActivity
		?? throw new InvalidOperationException($"The runner's activity must be an {nameof(AppCompatActivity)}, but it is {TestWindow.PlatformWindow.GetType().FullName}.");

	/// <summary>
	/// Shows <paramref name="fragment"/> on the host activity's FragmentManager (so it runs the whole lifecycle up to RESUMED),
	/// runs <paramref name="body"/> on the UI thread with the host activity as Platform.CurrentActivity, and dismisses the fragment again.
	/// </summary>
	protected static async Task WithShownDialogFragmentAsync(ProbeDialogFragment fragment, Func<AppCompatActivity, ProbeDialogFragment, Task> body)
	{
		var tag = $"test-{Guid.NewGuid():N}";
		await InvokeOnMainThreadAsync(() => fragment.ShowNow(HostActivity.SupportFragmentManager, tag));
		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				using (new CurrentActivityScope(HostActivity))
				{
					return body(HostActivity, fragment);
				}
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				if (fragment.IsAdded)
				{
					fragment.DismissNow();
				}
			});
		}
	}

	protected static Task WithShownDialogFragmentAsync(Func<AppCompatActivity, ProbeDialogFragment, Task> body) =>
		WithShownDialogFragmentAsync(new ProbeDialogFragment(), body);

	/// <summary>Sets the status bar color of the host activity's window for the duration of <paramref name="body"/> (UI thread).</summary>
	protected static async Task WithActivityStatusBarColorAsync(int color, Func<Task> body)
	{
		var original = await InvokeOnMainThreadAsync(() =>
		{
			var window = HostActivity.Window!;
			var previous = window.StatusBarColor;
			window.SetStatusBarColor(new AColor(color));
			return previous;
		});

		try
		{
			await body();
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => HostActivity.Window!.SetStatusBarColor(new AColor(original)));
		}
	}

	/// <summary>The state of a window that the status bar handling reads or writes, as the native window reports it.</summary>
	protected readonly record struct WindowState(int StatusBarColor, WindowManagerFlags Flags, int Appearance)
	{
		public const int AppearanceUnavailable = -1;

		public bool HasFlag(WindowManagerFlags flag) => (Flags & flag) == flag;

		public bool HasLightStatusBars => Appearance != AppearanceUnavailable
			&& (Appearance & (int)WindowInsetsControllerAppearance.LightStatusBars) != 0;
	}

	protected static WindowState Capture(AWindow window) =>
		new(window.StatusBarColor, window.Attributes!.Flags, ReadAppearance(window));

	protected static int ReadAppearance(AWindow window) =>
		OperatingSystem.IsAndroidVersionAtLeast(30) && window.InsetsController is IWindowInsetsController controller
			? controller.SystemBarsAppearance
			: WindowState.AppearanceUnavailable;

	/// <summary>Sets bits of SystemBarsAppearance on a window whose insets controller exists (Android 11+).</summary>
	protected static void SetAppearance(AWindow window, int appearance, int mask) =>
		window.InsetsController!.SetSystemBarsAppearance(appearance, mask);

	// A safety net for the tests that run afterwards: if anything in a test left Platform.CurrentActivity null (the listener of the
	// ActivityStateManager replaced; GetToolkitOnCreateDelegates undoes that itself), put back the activity the test started with.
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			if (Platform.CurrentActivity is null && currentActivityAtStart is not null)
			{
				CurrentActivityScope.Set(currentActivityAtStart);
			}
		}
	}

	// The OnCreate delegates the toolkit added (UseMauiCommunityToolkitCore with the modal status bar option): the lifecycle service also
	// holds MAUI's own (Essentials) OnCreate delegates, which must not be invoked with activities that are not attached. The toolkit's
	// lambdas are compiled into Core.AppBuilderExtensions.
	protected IReadOnlyList<AndroidLifecycle.OnCreate> GetToolkitOnCreateDelegates()
	{
		ILifecycleEventService? lifecycle;

		// Creating the lifecycle service runs every lifecycle registration of the test's MauiApp, MAUI's Essentials one included, which
		// calls Platform.Init(Application) on the runner's Application: the scope unregisters the listener that adds and puts the
		// runner's own listener back (see ActivityStateListenerScope).
		using (new ActivityStateListenerScope())
		{
			lifecycle = ApplicationServices.GetService<ILifecycleEventService>();
		}

		return [.. (lifecycle?.GetEventDelegates<AndroidLifecycle.OnCreate>(nameof(AndroidLifecycle.OnCreate)) ?? [])
			.Where(onCreate => onCreate.Method.DeclaringType?.DeclaringType == typeof(Core.AppBuilderExtensions))];
	}

	// What DialogFragmentService.OnFragmentStarted did to a dialog that ArrangeThatStartedWouldChange prepared, while the activity had the
	// status bar of WithActivityStatusBarAsync(distinctOpaqueColor): its color (read back up to Android 14) and, from Android 11 on, its
	// light status bars.
	protected static void AssertDialogTookOverTheStatusBarOfTheActivity(AWindow dialogWindow)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			Assert.Equal(distinctOpaqueColor, dialogWindow.StatusBarColor);
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			Assert.True(Capture(dialogWindow).HasLightStatusBars, "The dialog should have taken over the light status bar appearance of the activity.");
		}
	}

	// Gives the dialog a state OnFragmentStarted would change on this Android version (see WithActivityStatusBarAsync for the activity side).
	protected static void ArrangeThatStartedWouldChange(AWindow dialogWindow)
	{
		// A window that was never shown has no decor view yet, and its insets controller needs one.
		_ = dialogWindow.DecorView;

		if (!OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			dialogWindow.SetStatusBarColor(new AColor(dialogMarkerColor));
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			SetAppearance(dialogWindow, 0, lightStatusBarsBit);
		}
	}

	// The activity's status bar: the color (ignored from Android 15 on) and, from Android 11 on, light status bar icons.
	protected static async Task WithActivityStatusBarAsync(int color, Func<Task> body)
	{
		var originalLight = await InvokeOnMainThreadAsync(() =>
		{
			var window = HostActivity.Window!;
			var wasLight = OperatingSystem.IsAndroidVersionAtLeast(30) && (ReadAppearance(window) & lightStatusBarsBit) != 0;
			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				SetAppearance(window, lightStatusBarsBit, lightStatusBarsBit);
			}

			return wasLight;
		});

		try
		{
			await WithActivityStatusBarColorAsync(color, body);
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				if (OperatingSystem.IsAndroidVersionAtLeast(30))
				{
					SetAppearance(HostActivity.Window!, originalLight ? lightStatusBarsBit : 0, lightStatusBarsBit);
				}
			});
		}
	}
}