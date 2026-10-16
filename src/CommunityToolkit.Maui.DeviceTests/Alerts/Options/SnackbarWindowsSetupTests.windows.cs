using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.LifecycleEvents;
using Application = Microsoft.Maui.Controls.Application;
using Window = Microsoft.Maui.Controls.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// The Windows block of Options.SetShouldEnableSnackbarOnWindows registers two lifecycle delegates on the builder: OnLaunched registers
// the app notification manager (so that the buttons of a Snackbar reach the toolkit) and OnClosed unregisters it. The delegates are
// taken from the test's MauiApp and invoked directly, except for the two arms that call the process-wide AppNotificationManager
// Register() and Unregister(): the runner's own launch ran the first one (RunnerLaunchRanTheRegistrationArmOfThisConfiguration), the
// second one would stop the runner's registration. A person checks it in the Sample (Alerts pages) by closing the last window, and the
// button of a notification by pressing it (SnackbarTests.PressingTheButtonOfTheNotificationRunsTheActionOnTheUiThreadAndDismissesTheSnackbar).
public partial class SnackbarWindowsSetupTests
{
	const string documentationUrl = "https://learn.microsoft.com/dotnet/communitytoolkit/maui/alerts/snackbar";
	const string insightsResourceDll = "Microsoft.WindowsAppRuntime.Insights.Resource.dll";

	[Fact]
	public void EnablingTheOptionWithoutABuilderThrows()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Options)).Capture(typeof(Core.Options));
		try
		{
			var before = Options.ShouldEnableSnackbarOnWindows;

			var exception = Assert.Throws<InvalidOperationException>(() => new Options().SetShouldEnableSnackbarOnWindows(true));

			Assert.Contains(nameof(AppBuilderExtensions.UseMauiCommunityToolkit), exception.Message);
			Assert.Equal(documentationUrl, exception.HelpLink);
			Assert.Equal(before, Options.ShouldEnableSnackbarOnWindows);

			// Disabling needs no builder.
			new Options().SetShouldEnableSnackbarOnWindows(false);
			Assert.False(Options.ShouldEnableSnackbarOnWindows);
		}
		finally
		{
			snapshot.Restore();
		}
	}

	[Fact]
	public async Task DisablingTheOptionRegistersNoLifecycleDelegates()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(false));

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.False(Options.ShouldEnableSnackbarOnWindows);
			Assert.Empty(GetToolkitDelegates<WindowsLifecycle.OnLaunched>(nameof(WindowsLifecycle.OnLaunched)));
			Assert.Empty(GetToolkitDelegates<WindowsLifecycle.OnClosed>(nameof(WindowsLifecycle.OnClosed)));
		});
	}

	[Fact]
	public async Task EnablingTheOptionRegistersTheLaunchedAndClosedDelegates()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(true));

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.True(Options.ShouldEnableSnackbarOnWindows);
			Assert.Single(GetToolkitDelegates<WindowsLifecycle.OnLaunched>(nameof(WindowsLifecycle.OnLaunched)));
			Assert.Single(GetToolkitDelegates<WindowsLifecycle.OnClosed>(nameof(WindowsLifecycle.OnClosed)));
		});
	}

	[Fact]
	public async Task LaunchedDelegateThrowsWithoutAnApplication()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(true));
		var launched = Assert.Single(GetToolkitDelegates<WindowsLifecycle.OnLaunched>(nameof(WindowsLifecycle.OnLaunched)));

		await InvokeOnMainThreadAsync(() =>
		{
			var previousApplication = Application.Current;
			Application.Current = null;
			try
			{
				var exception = Assert.Throws<InvalidOperationException>(() => launched(null!, null!));

				Assert.Contains("cannot be null when Windows are launched", exception.Message);
			}
			finally
			{
				Application.Current = previousApplication;
			}
		});
	}

	[Theory]
	[InlineData(0)]
	[InlineData(2)]
	public async Task LaunchedDelegateRegistersNothingUnlessExactlyOneWindowIsOpen(int windowCount)
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(true));
		var launched = Assert.Single(GetToolkitDelegates<WindowsLifecycle.OnLaunched>(nameof(WindowsLifecycle.OnLaunched)));

		await InvokeOnMainThreadAsync(() =>
		{
			var previousApplication = Application.Current;
			var wasRegistered = IsNotificationManagerRegistered;
			IsNotificationManagerRegistered = false;
			try
			{
				_ = new WindowsTestApplication(windowCount);

				var exception = Record.Exception(() => launched(null!, null!));

				Assert.Null(exception);

				// Register() would have set the flag.
				Assert.False(IsNotificationManagerRegistered);
			}
			finally
			{
				IsNotificationManagerRegistered = wasRegistered;
				Application.Current = previousApplication;
			}
		});
	}

	// The runner's MauiProgram enables the Windows Snackbar and its first window is open when OnLaunched runs, so the runner's launch
	// ran the arm of the launched delegate that registers the app notification manager. Which way that arm ended depends on how the app
	// was built: Register() succeeds in a packaged app and in an unpackaged app that finds the Windows App Runtime Insights resource
	// module, and sets the flag. A self-contained unpackaged app without that module gets the COMException of WindowsAppSDK#6071, which
	// the toolkit tolerates (the catch arm): the finally block unsubscribes the handler, the flag stays false and the Snackbar stays
	// enabled, without action callbacks. CI builds both: packaged (MSIX) and unpackaged with the Windows App SDK bundled.
	[Fact]
	public void RunnerLaunchRanTheRegistrationArmOfThisConfiguration()
	{
		var isPackaged = AppInfo.Current.PackagingModel is AppPackagingModel.Packaged;
		var bundlesTheWindowsAppRuntime = File.Exists(Path.Combine(AppContext.BaseDirectory, "Microsoft.WindowsAppRuntime.dll"));
		var insightsModuleIsMissing = !File.Exists(Path.Combine(AppContext.BaseDirectory, insightsResourceDll));

		Assert.True(Options.ShouldEnableSnackbarOnWindows);
		if (!isPackaged && bundlesTheWindowsAppRuntime && insightsModuleIsMissing)
		{
			Assert.False(IsNotificationManagerRegistered, $"Register() succeeded although this self-contained unpackaged app has no {insightsResourceDll}.");
		}
		else
		{
			Assert.True(IsNotificationManagerRegistered, $"The runner's launch did not register the app notification manager (packaged: {isPackaged}, Windows App Runtime bundled: {bundlesTheWindowsAppRuntime}, {insightsResourceDll} missing: {insightsModuleIsMissing}).");
		}
	}

	[Fact]
	public async Task ClosedDelegateThrowsWithoutAnApplication()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(true));
		var closed = Assert.Single(GetToolkitDelegates<WindowsLifecycle.OnClosed>(nameof(WindowsLifecycle.OnClosed)));

		await InvokeOnMainThreadAsync(() =>
		{
			var previousApplication = Application.Current;
			Application.Current = null;
			try
			{
				var exception = Assert.Throws<InvalidOperationException>(() => closed(null!, null!));

				Assert.Contains("cannot be null when Windows are closed", exception.Message);
			}
			finally
			{
				Application.Current = previousApplication;
			}
		});
	}

	[Theory]
	[InlineData(1, true)]
	[InlineData(0, false)]
	public async Task ClosedDelegateUnregistersNothingWhileAWindowIsOpenOrWhenNothingIsRegistered(int windowCount, bool isRegistered)
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(true));
		var closed = Assert.Single(GetToolkitDelegates<WindowsLifecycle.OnClosed>(nameof(WindowsLifecycle.OnClosed)));

		await InvokeOnMainThreadAsync(() =>
		{
			var previousApplication = Application.Current;
			var wasRegistered = IsNotificationManagerRegistered;
			IsNotificationManagerRegistered = isRegistered;
			try
			{
				_ = new WindowsTestApplication(windowCount);

				var exception = Record.Exception(() => closed(null!, null!));

				Assert.Null(exception);

				// Unregister() would have cleared the flag.
				Assert.Equal(isRegistered, IsNotificationManagerRegistered);
			}
			finally
			{
				IsNotificationManagerRegistered = wasRegistered;
				Application.Current = previousApplication;
			}
		});
	}

	// Options.isSnackbarNotificationManagerRegistered: set after Register() returned, cleared after Unregister().
	static bool IsNotificationManagerRegistered
	{
		get => (bool)NotificationManagerRegisteredField.GetValue(null)!;
		set => NotificationManagerRegisteredField.SetValue(null, value);
	}

	static FieldInfo NotificationManagerRegisteredField =>
		typeof(Options).GetField("isSnackbarNotificationManagerRegistered", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("Options.isSnackbarNotificationManagerRegistered was not found.");

	// The lifecycle delegates that Options.SetShouldEnableSnackbarOnWindows added to the test's MauiApp (its lambdas are nested in Options).
	List<TDelegate> GetToolkitDelegates<TDelegate>(string eventName)
		where TDelegate : Delegate =>
		[.. ApplicationServices.GetService<ILifecycleEventService>()?.GetEventDelegates<TDelegate>(eventName).Where(del => del.Method.DeclaringType?.DeclaringType == typeof(Options)) ?? []];

	// An application with the given number of windows (Application.Current is set by its constructor, the tests put the runner's back).
	sealed class WindowsTestApplication : Application
	{
		public WindowsTestApplication(int windowCount)
		{
			for (var i = 0; i < windowCount; i++)
			{
				((IApplication)this).CreateWindow(null);
			}
		}

		protected override Window CreateWindow(IActivationState? activationState) => new();
	}
}