#pragma warning disable CA1416, CA1422, CS0618
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using AColor = Android.Graphics.Color;
using AFragment = AndroidX.Fragment.App.Fragment;
using AView = Android.Views.View;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// Where the framework lets the state be observed, it is read back from the real native window (StatusBarColor, window flags and
// WindowInsetsController appearance). From Android 15 on, the status bar color APIs are ignored for apps that target API 35 or newer
// (their getter always returns 0), and a real window always has an insets controller, so the remaining branches are driven through
// SpyWindow, which records what the service writes to the dialog window.
[Category(TestCategory.DialogFragmentService)]
[Collection(TestCollections.DisableParallelization)]
public class DialogFragmentServiceTests : FragmentTestBase
{
	const int transparentColor = 0;

	const string androidStatusBarColorIgnoredReason = "Window.StatusBarColor is ignored for apps that target Android 15 (API 35) or newer: the getter always returns 0.";

	[Theory]
	[InlineData(nameof(IDialogFragmentService.OnFragmentAttached))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentCreated))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentDestroyed))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentDetached))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentPaused))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentPreAttached))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentPreCreated))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentResumed))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentSaveInstanceState))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentStopped))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentViewCreated))]
	[InlineData(nameof(IDialogFragmentService.OnFragmentViewDestroyed))]
	public async Task CallbacksOtherThanStartedLeaveTheDialogWindowUntouched(string callback)
	{
		var service = new DialogFragmentService();

		await WithActivityStatusBarAsync(distinctOpaqueColor, () => WithShownDialogFragmentAsync((activity, fragment) =>
		{
			var dialogWindow = fragment.Dialog!.Window!;
			ArrangeThatStartedWouldChange(dialogWindow);
			var before = Capture(dialogWindow);

			InvokeCallback(service, callback, activity, fragment);

			Assert.Equal(before, Capture(dialogWindow));
			return Task.CompletedTask;
		}));
	}

	[Fact]
	public async Task StartedIgnoresFragmentsThatAreNotDialogFragments()
	{
		var service = new DialogFragmentService();
		var fragment = new PlainFragment();

		// A service that did not return early would fail on the missing dialog.
		await WithActivityStatusBarAsync(distinctOpaqueColor, () => InvokeOnMainThreadAsync(() =>
		{
			using (new CurrentActivityScope(HostActivity))
			{
				var exception = Record.Exception(() => service.OnFragmentStarted(HostActivity.SupportFragmentManager, fragment));

				Assert.Null(exception);
			}
		}));
	}

	[Fact]
	public async Task StartedDoesNothingWhenThereIsNoCurrentActivity()
	{
		var service = new DialogFragmentService();

		await WithActivityStatusBarAsync(distinctOpaqueColor, () => WithShownDialogFragmentAsync((activity, fragment) =>
		{
			var dialogWindow = fragment.Dialog!.Window!;
			ArrangeThatStartedWouldChange(dialogWindow);
			var before = Capture(dialogWindow);

			using (new CurrentActivityScope(null))
			{
				service.OnFragmentStarted(activity.SupportFragmentManager, fragment);
			}

			Assert.Equal(before, Capture(dialogWindow));
			return Task.CompletedTask;
		}));
	}

	[Fact]
	public async Task StartedDoesNothingWhenTheCurrentActivityIsNotAnAppCompatActivity()
	{
		var service = new DialogFragmentService();

		await WithActivityStatusBarAsync(distinctOpaqueColor, () => WithShownDialogFragmentAsync((activity, fragment) =>
		{
			var dialogWindow = fragment.Dialog!.Window!;
			ArrangeThatStartedWouldChange(dialogWindow);
			var before = Capture(dialogWindow);

			using (new CurrentActivityScope(new PlainActivity()))
			{
				service.OnFragmentStarted(activity.SupportFragmentManager, fragment);
			}

			Assert.Equal(before, Capture(dialogWindow));
			return Task.CompletedTask;
		}));
	}

	[Fact]
	public async Task StartedDoesNothingWhenTheCurrentActivityHasNoWindow()
	{
		var service = new DialogFragmentService();

		await WithActivityStatusBarAsync(distinctOpaqueColor, () => WithShownDialogFragmentAsync((activity, fragment) =>
		{
			var dialogWindow = fragment.Dialog!.Window!;
			ArrangeThatStartedWouldChange(dialogWindow);
			var before = Capture(dialogWindow);

			using (new CurrentActivityScope(new SpyActivity { SpyWindow = null }))
			{
				service.OnFragmentStarted(activity.SupportFragmentManager, fragment);
			}

			Assert.Equal(before, Capture(dialogWindow));
			return Task.CompletedTask;
		}));
	}

	[Fact]
	public async Task StartedThrowsWhenTheDialogFragmentHasNoDialog()
	{
		var service = new DialogFragmentService();
		var fragment = new ProbeDialogFragment();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(fragment.Dialog);
			using (new CurrentActivityScope(HostActivity))
			{
				service.OnFragmentStarted(HostActivity.SupportFragmentManager, fragment);
			}
		}));

		Assert.Equal("Dialog window cannot be null", exception.Message);
	}

	[Fact]
	public async Task StartedThrowsWhenTheDialogHasNoWindow()
	{
		var service = new DialogFragmentService();
		var fragment = new ProbeDialogFragment { DialogFactory = context => new SpyDialog(context) };

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeOnMainThreadAsync(() =>
		{
			fragment.ShowNow(HostActivity.SupportFragmentManager, $"test-no-window-{Guid.NewGuid():N}");
			var dialog = (SpyDialog)fragment.Dialog!;

			try
			{
				// The dialog reports no window only while the service looks at it: the framework has to keep seeing the real one.
				dialog.ReportsNoWindow = true;
				using (new CurrentActivityScope(HostActivity))
				{
					service.OnFragmentStarted(HostActivity.SupportFragmentManager, fragment);
				}
			}
			finally
			{
				dialog.ReportsNoWindow = false;
				fragment.DismissNow();
			}
		}));

		Assert.Equal("Dialog window cannot be null", exception.Message);
	}

	[Theory]
	[InlineData(0xFF336699u)]
	[InlineData(0xFFFFFFFFu)]
	[InlineData(0xFF000000u)]
	[InlineData(0x80336699u)]
	public async Task StartedCopiesTheActivityStatusBarColorToTheDialogWindow(uint argb)
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(35), androidStatusBarColorIgnoredReason);
		var service = new DialogFragmentService();
		var activityColor = unchecked((int)argb);

		await WithActivityStatusBarAsync(activityColor, () => WithShownDialogFragmentAsync((activity, fragment) =>
		{
			var dialogWindow = fragment.Dialog!.Window!;
			dialogWindow.SetStatusBarColor(new AColor(dialogMarkerColor));
			Assert.Equal(dialogMarkerColor, dialogWindow.StatusBarColor);

			service.OnFragmentStarted(activity.SupportFragmentManager, fragment);

			Assert.Equal(activityColor, dialogWindow.StatusBarColor);
			return Task.CompletedTask;
		}));
	}

	[Fact]
	public async Task StartedCopiesATransparentActivityStatusBarColorToTheDialogWindow()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(35), androidStatusBarColorIgnoredReason);
		var service = new DialogFragmentService();

		await WithActivityStatusBarAsync(transparentColor, () => WithShownDialogFragmentAsync((activity, fragment) =>
		{
			var dialogWindow = fragment.Dialog!.Window!;
			dialogWindow.SetStatusBarColor(new AColor(dialogMarkerColor));

			service.OnFragmentStarted(activity.SupportFragmentManager, fragment);

			Assert.Equal(transparentColor, dialogWindow.StatusBarColor);
			return Task.CompletedTask;
		}));
	}

	[Theory]
	[InlineData(0xFF336699u)]
	[InlineData(0x80336699u)]
	[InlineData(0x00000000u)]
	[InlineData(0x00FF0000u)]
	[InlineData(0x00FFFFFFu)]
	public async Task StartedLaysTheDialogOutLikeThePageWindowForEveryStatusBarColorOnAndroid10AndOlder(uint argb)
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(30), "Window flags are only used below Android 11 (API 30).");
		var service = new DialogFragmentService();
		var activityColor = unchecked((int)argb);

		await WithActivityStatusBarAsync(activityColor, () => WithShownDialogFragmentAsync((activity, fragment) =>
		{
			// The dialog starts with the layout a transparent status bar used to give it, so that each of the flags is seen changing.
			var dialogWindow = fragment.DialogWindow;
			dialogWindow.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
			dialogWindow.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
			var before = Capture(dialogWindow);
			Assert.False(before.HasFlag(WindowManagerFlags.DrawsSystemBarBackgrounds));
			Assert.True(before.HasFlag(WindowManagerFlags.LayoutNoLimits));
			Assert.Equal(0, AndroidSystemBars.SystemUiVisibility(dialogWindow) & AndroidSystemBars.LayoutSystemUiFlags);

			service.OnFragmentStarted(activity.SupportFragmentManager, fragment);

			var after = Capture(dialogWindow);
			Assert.Equal(activityColor, after.StatusBarColor);
			Assert.True(after.HasFlag(WindowManagerFlags.DrawsSystemBarBackgrounds));
			Assert.False(after.HasFlag(WindowManagerFlags.LayoutNoLimits));
			Assert.Equal(AndroidSystemBars.LayoutSystemUiFlags, AndroidSystemBars.SystemUiVisibility(dialogWindow) & AndroidSystemBars.LayoutSystemUiFlags);
			return Task.CompletedTask;
		}));
	}

	// A page keeps clear of the status bar with the window insets that it receives, and the dialog used to keep them from it: laid out without
	// limits for a transparent status bar on Android 10 and older (#2351), or with a decor view that fit the system windows for the other colors.
	[Theory]
	[InlineData(0xFF336699u)]
	[InlineData(0x80336699u)]
	[InlineData(0x00000000u)]
	[InlineData(0x00FF0000u)]
	[InlineData(0x00FFFFFFu)]
	public async Task ModalPageLaysOutBelowTheStatusBarLikeThePageWindowForEveryStatusBarColor(uint argb)
	{
		var color = Color.FromUint(argb);
		var lifecycleCallbacks = new FragmentLifecycleManager(new DialogFragmentService());
		var modalPage = new ModalPageFragment();

		await InvokeOnMainThreadAsync(async () =>
		{
			var fragmentManager = HostActivity.SupportFragmentManager;
			var pageWindow = HostActivity.Window ?? throw new InvalidOperationException("The runner's activity has no window.");
			AndroidSystemBars.CaptureOriginalState(pageWindow);
			await AndroidSystemBars.FlushPostedActions(pageWindow);
			AndroidSystemBars.Reset(pageWindow);

			try
			{
				StatusBar.SetColor(color);
				await AndroidSystemBars.FlushPostedActions(pageWindow);
				var pageWindowLayout = ReadSystemBarLayout(pageWindow);
				Assert.True(pageWindowLayout.StatusBarInset > 0, "The runner's window reports no status bar, so a page below it cannot be told apart from a page behind it.");

				fragmentManager.RegisterFragmentLifecycleCallbacks(lifecycleCallbacks, false);
				try
				{
					modalPage.ShowNow(fragmentManager, $"test-modal-page-{Guid.NewGuid():N}");
					await AssertHelpers.AssertEventually(() => modalPage.IsPageLaidOut, timeout: 10000, interval: 50, message: "The modal page was never laid out.");

					Assert.Equal(pageWindowLayout.StatusBarInset, modalPage.PageTopInset);
					Assert.Equal(pageWindowLayout.StatusBarInset, modalPage.PageTopOnScreen);
					Assert.Equal(pageWindowLayout, ReadSystemBarLayout(modalPage.DialogWindow));
				}
				finally
				{
					fragmentManager.UnregisterFragmentLifecycleCallbacks(lifecycleCallbacks);
					if (modalPage.IsAdded)
					{
						modalPage.DismissNow();
					}
				}
			}
			finally
			{
				AndroidSystemBars.Reset(pageWindow);
			}
		});
	}

	[Fact]
	public async Task StartedMirrorsTheLightStatusBarsAppearanceOfTheActivityOnAndroid11AndNewer()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(30), "WindowInsetsController appearance needs Android 11 (API 30).");
		var service = new DialogFragmentService();

		await WithActivityStatusBarAsync(distinctOpaqueColor, () => WithShownDialogFragmentAsync((activity, fragment) =>
		{
			var dialogWindow = fragment.Dialog!.Window!;
			SetAppearance(dialogWindow, 0, lightStatusBarsBit);
			Assert.False(Capture(dialogWindow).HasLightStatusBars);
			Assert.True(Capture(activity.Window!).HasLightStatusBars);

			service.OnFragmentStarted(activity.SupportFragmentManager, fragment);

			Assert.True(Capture(dialogWindow).HasLightStatusBars, "The dialog should have taken over the activity's light status bar appearance.");
			return Task.CompletedTask;
		}));
	}

	[Fact]
	public async Task StartedClearsTheLightStatusBarsAppearanceOfTheDialogWhenTheActivityDoesNotHaveIt()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(30), "WindowInsetsController appearance needs Android 11 (API 30).");
		var service = new DialogFragmentService();

		await WithActivityStatusBarAsync(distinctOpaqueColor, () => WithShownDialogFragmentAsync((activity, fragment) =>
		{
			SetAppearance(activity.Window!, 0, lightStatusBarsBit);
			var dialogWindow = fragment.Dialog!.Window!;
			SetAppearance(dialogWindow, lightStatusBarsBit, lightStatusBarsBit);
			Assert.False(Capture(activity.Window!).HasLightStatusBars);
			Assert.True(Capture(dialogWindow).HasLightStatusBars);

			service.OnFragmentStarted(activity.SupportFragmentManager, fragment);

			Assert.False(Capture(dialogWindow).HasLightStatusBars, "The dialog should have taken over the activity's appearance, which does not have light status bars.");
			return Task.CompletedTask;
		}));
	}

	[Fact]
	public async Task StartedKeepsTheDialogUntouchedWhenItsWindowHasNoInsetsController()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(30), "WindowInsetsController needs Android 11 (API 30).");
		var service = new DialogFragmentService();
		using var trace = new FragmentTraceListener();

		await WithSpyScenarioAsync(distinctOpaqueColor, activityAppearance: lightStatusBarsBit, dialogHasInsetsController: false, scenario =>
		{
			service.OnFragmentStarted(HostActivity.SupportFragmentManager, scenario.Fragment);

			Assert.Empty(scenario.DialogWindow.StatusBarColorCalls);
			Assert.Empty(scenario.DialogWindow.DecorFitsSystemWindowsCalls);
			Assert.Empty(scenario.ActivityController!.AppearanceCalls);
			return Task.CompletedTask;
		});

		Assert.Contains("WindowInsetsController is null, cannot set system bars appearance.", trace.Messages);
	}

	// The activity's appearance is a bit mask: 8 = light status bars, 16 = light navigation bars.
	[Theory]
	[InlineData(0, false)]
	[InlineData(0, true)]
	[InlineData(8, false)]
	[InlineData(8, true)]
	[InlineData(16, false)]
	[InlineData(16, true)]
	[InlineData(24, false)]
	[InlineData(24, true)]
	public async Task StartedMirrorsTheLightStatusBarsBitOfTheActivityOnTheDialog(int activityAppearance, bool dialogStartsWithLightStatusBars)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(30), "WindowInsetsController needs Android 11 (API 30).");
		var activityHasLightStatusBars = (activityAppearance & lightStatusBarsBit) != 0;
		var service = new DialogFragmentService();

		await WithSpyScenarioAsync(distinctOpaqueColor, activityAppearance, dialogHasInsetsController: true, scenario =>
		{
			Assert.Equal(dialogStartsWithLightStatusBars, (scenario.DialogController!.ReportedAppearance & lightStatusBarsBit) != 0);

			service.OnFragmentStarted(HostActivity.SupportFragmentManager, scenario.Fragment);

			Assert.Equal(activityHasLightStatusBars, (scenario.DialogController.ReportedAppearance & lightStatusBarsBit) != 0);
			// Every appearance bit of the activity (light navigation bars too) ends up on the dialog.
			Assert.Equal(activityAppearance, scenario.DialogController.ReportedAppearance & activityAppearance);
			return Task.CompletedTask;
		}, dialogAppearance: dialogStartsWithLightStatusBars ? lightStatusBarsBit : 0);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StartedUsesTheDefaultAppearanceWhenTheActivityWindowHasNoInsetsController(bool transparent)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(30), "WindowInsetsController needs Android 11 (API 30).");
		var service = new DialogFragmentService();
		var activityColor = transparent ? transparentColor : distinctOpaqueColor;

		await WithSpyScenarioAsync(activityColor, activityAppearance: null, dialogHasInsetsController: true, scenario =>
		{
			service.OnFragmentStarted(HostActivity.SupportFragmentManager, scenario.Fragment);

			// A transparent status bar clears the light icons, any other color asks for them.
			var call = Assert.Single(scenario.DialogController!.AppearanceCalls);
			Assert.Equal((transparent ? 0 : lightStatusBarsBit, lightStatusBarsBit), call);
			return Task.CompletedTask;
		});
	}

	[Theory]
	[InlineData(0x00FF0000u)]
	[InlineData(0x00FFFFFFu)]
	public async Task StartedTreatsAStatusBarColorWithoutAlphaLikeAnyOtherColorOnAndroid11AndNewer(uint argb)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(30), "WindowInsetsController needs Android 11 (API 30).");
		var service = new DialogFragmentService();
		var activityColor = unchecked((int)argb);

		await WithSpyScenarioAsync(activityColor, activityAppearance: null, dialogHasInsetsController: true, scenario =>
		{
			service.OnFragmentStarted(HostActivity.SupportFragmentManager, scenario.Fragment);

			Assert.Equal([activityColor], scenario.DialogWindow.StatusBarColorCalls);
			Assert.NotNull(scenario.DialogController);
			Assert.Equal((lightStatusBarsBit, lightStatusBarsBit), Assert.Single(scenario.DialogController.AppearanceCalls));
			Assert.Equal([false], scenario.DialogWindow.DecorFitsSystemWindowsCalls);
			return Task.CompletedTask;
		});
	}

	[Theory]
	[InlineData(0xFF336699u)]
	[InlineData(0x80336699u)]
	[InlineData(0x00FF0000u)]
	[InlineData(0u)]
	public async Task StartedSetsTheStatusBarColorOfTheActivityOnTheDialogWindow(uint argb)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(30), "This arm of the service is used on Android 11 (API 30) and newer.");
		var service = new DialogFragmentService();
		var activityColor = unchecked((int)argb);

		await WithSpyScenarioAsync(activityColor, activityAppearance: lightStatusBarsBit, dialogHasInsetsController: true, scenario =>
		{
			service.OnFragmentStarted(HostActivity.SupportFragmentManager, scenario.Fragment);

			Assert.Equal([activityColor], scenario.DialogWindow.StatusBarColorCalls);
			Assert.Empty(scenario.DialogWindow.ClearFlagsCalls);
			Assert.Empty(scenario.DialogWindow.SetFlagsCalls);
			return Task.CompletedTask;
		});
	}

	// From Android 11 on, WindowCompat.SetDecorFitsSystemWindows ends in Window.SetDecorFitsSystemWindows of the dialog window, which is what the
	// spy window records. Below Android 15 it also sets the stable layout flag on the decor view, so Android 11-14 have a test of their own, which
	// the API 28 and API 36 device-test emulators report as skipped.
	[Theory]
	[InlineData(0xFF336699u)]
	[InlineData(0x80336699u)]
	[InlineData(0x00000000u)]
	[InlineData(0x00FF0000u)]
	public async Task StartedKeepsTheDecorFromFittingTheSystemWindowsForEveryStatusBarColorOnAndroid15AndNewer(uint argb)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "WindowCompat only calls Window.SetDecorFitsSystemWindows from Android 15 (API 35) on.");
		await AssertDecorDoesNotFitSystemWindowsAsync(unchecked((int)argb));
	}

	[Theory]
	[InlineData(0xFF336699u)]
	[InlineData(0x80336699u)]
	[InlineData(0x00000000u)]
	[InlineData(0x00FF0000u)]
	public async Task StartedKeepsTheDecorFromFittingTheSystemWindowsForEveryStatusBarColorOnAndroid11To14(uint argb)
	{
		Skip.IfNot(
			OperatingSystem.IsAndroidVersionAtLeast(30) && !OperatingSystem.IsAndroidVersionAtLeast(35),
			"Only on Android 11-14 (API 30-34) does WindowCompat set the stable layout flag on the decor view next to calling Window.SetDecorFitsSystemWindows. The device-test emulators are API 28 and API 36, so this test needs an API 30-34 device or emulator.");
		await AssertDecorDoesNotFitSystemWindowsAsync(unchecked((int)argb));
	}

	[Theory]
	[InlineData(0xFF336699u)]
	[InlineData(0x00000000u)]
	[InlineData(0x00FF0000u)]
	public async Task StartedWritesOnlyTheLegacyColorAndLayoutToTheDialogWindowOnAndroid10AndOlder(uint argb)
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(30), "Window flags are only used below Android 11 (API 30).");
		var service = new DialogFragmentService();
		var activityColor = unchecked((int)argb);

		await WithSpyScenarioAsync(activityColor, activityAppearance: null, dialogHasInsetsController: false, scenario =>
		{
			service.OnFragmentStarted(HostActivity.SupportFragmentManager, scenario.Fragment);

			var dialogWindow = scenario.DialogWindow;
			Assert.Equal([activityColor], dialogWindow.StatusBarColorCalls);
			Assert.Equal([WindowManagerFlags.LayoutNoLimits], dialogWindow.ClearFlagsCalls);
			Assert.Equal([(WindowManagerFlags.DrawsSystemBarBackgrounds, WindowManagerFlags.DrawsSystemBarBackgrounds)], dialogWindow.SetFlagsCalls);

			// Window.SetDecorFitsSystemWindows needs Android 11: WindowCompat lays the decor view out behind the system bars with system UI flags instead.
			Assert.Empty(dialogWindow.DecorFitsSystemWindowsCalls);
			Assert.Equal(AndroidSystemBars.LayoutSystemUiFlags, AndroidSystemBars.SystemUiVisibility(dialogWindow) & AndroidSystemBars.LayoutSystemUiFlags);
			return Task.CompletedTask;
		});
	}

	// The toolkit registers a FragmentLifecycleManager, which holds the service, on the FragmentManager of every activity it creates
	// (OnCreate of UseMauiCommunityToolkitCore) and never unregisters it: that registration has to go away with the FragmentManager, and
	// keep neither the service, nor the dialog fragment it handled, nor the activity alive. The child FragmentManager of a host fragment
	// stands in for the FragmentManager of an activity, so that the test can throw it away.
	[Fact]
	public async Task ServiceRegisteredByTheToolkitIsCollectedWithTheFragmentManagerItWasRegisteredOn()
	{
		SetupBuilder(configureToolkitOptions: options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		var onCreate = GetToolkitOnCreateDelegates().Single();
		WeakReference serviceReference = null!, dialogFragmentReference = null!, activityReference = null!, hostReference = null!;

		await WithActivityStatusBarAsync(distinctOpaqueColor, () => InvokeOnMainThreadAsync(() =>
		{
			var services = new ServiceCollection().AddSingleton<IDialogFragmentService, DialogFragmentService>().BuildServiceProvider();
			var service = services.GetRequiredService<IDialogFragmentService>();
			var host = new PlainFragment();
			var activity = new SpyActivity();
			var dialogFragment = new ProbeDialogFragment
			{
				DialogFactory = context =>
				{
					var dialog = new AppCompatDialog(context);
					ArrangeThatStartedWouldChange(dialog.Window!);
					return dialog;
				}
			};

			HostActivity.SupportFragmentManager.BeginTransaction().Add(host, $"test-leak-host-{Guid.NewGuid():N}").CommitNow();
			try
			{
				activity.SpyFragmentManager = host.ChildFragmentManager;
				using (new PlatformApplicationScope(new FakePlatformApplication(services)))
				{
					onCreate(activity, null);
				}

				dialogFragment.ShowNow(host.ChildFragmentManager, "test-leak-dialog");
				AssertDialogTookOverTheStatusBarOfTheActivity(dialogFragment.Dialog!.Window!);
				dialogFragment.DismissNow();
			}
			finally
			{
				activity.SpyFragmentManager = null;
				HostActivity.SupportFragmentManager.BeginTransaction().Remove(host).CommitNow();
			}

			services.Dispose();
			serviceReference = new WeakReference(service);
			dialogFragmentReference = new WeakReference(dialogFragment);
			activityReference = new WeakReference(activity);
			hostReference = new WeakReference(host);
		}));

		await AssertionExtensions.WaitForGC(serviceReference, dialogFragmentReference, activityReference, hostReference);
	}

	readonly record struct SystemBarLayout(bool LayoutNoLimits, bool DrawsSystemBarBackgrounds, int SystemUiLayoutFlags, int StatusBarInset);

	sealed record SpyScenario(SpyActivity Activity, SpyWindow ActivityWindow, SpyInsetsController? ActivityController, ProbeDialogFragment Fragment, SpyWindow DialogWindow, SpyInsetsController? DialogController);

	static Task WithSpyScenarioAsync(int activityStatusBarColor, int? activityAppearance, bool dialogHasInsetsController, Func<SpyScenario, Task> body, int dialogAppearance = 0) =>
		InvokeOnMainThreadAsync(async () =>
		{
			var host = HostActivity;
			var activityController = activityAppearance is int appearance ? new SpyInsetsController { ReportedAppearance = appearance } : null;
			var dialogController = dialogHasInsetsController ? new SpyInsetsController { ReportedAppearance = dialogAppearance } : null;
			var activityWindow = new SpyWindow(host) { ReportedStatusBarColor = activityStatusBarColor, ReportedInsetsController = activityController };
			var dialogWindow = new SpyWindow(host) { ReportedInsetsController = dialogController, ReportedDecorView = new Android.Widget.FrameLayout(host) };
			var spyActivity = new SpyActivity { SpyWindow = activityWindow };
			var fragment = new ProbeDialogFragment { DialogFactory = context => new SpyDialog(context) };

			fragment.ShowNow(host.SupportFragmentManager, $"test-spy-{Guid.NewGuid():N}");
			var dialog = (SpyDialog)fragment.Dialog!;
			try
			{
				// The dialog reports its spy window only while the body runs: framework (Java) code that asks the dialog for its window while the dialog
				// is created, shown or dismissed must get the real one.
				dialog.SpyWindow = dialogWindow;
				using (new CurrentActivityScope(spyActivity))
				{
					await body(new SpyScenario(spyActivity, activityWindow, activityController, fragment, dialogWindow, dialogController));
				}
			}
			finally
			{
				dialog.SpyWindow = null;
				if (fragment.IsAdded)
				{
					fragment.DismissNow();
				}
			}
		});

	static SystemBarLayout ReadSystemBarLayout(AWindow window) => new(
		AndroidSystemBars.HasFlag(window, WindowManagerFlags.LayoutNoLimits),
		AndroidSystemBars.HasFlag(window, WindowManagerFlags.DrawsSystemBarBackgrounds),
		AndroidSystemBars.SystemUiVisibility(window) & AndroidSystemBars.LayoutSystemUiFlags,
		ViewCompat.GetRootWindowInsets(window.DecorView)?.GetInsets(WindowInsetsCompat.Type.SystemBars())?.Top ?? 0);

	static async Task AssertDecorDoesNotFitSystemWindowsAsync(int activityColor)
	{
		var service = new DialogFragmentService();

		await WithSpyScenarioAsync(activityColor, activityAppearance: 0, dialogHasInsetsController: true, scenario =>
		{
			service.OnFragmentStarted(HostActivity.SupportFragmentManager, scenario.Fragment);

			Assert.Equal([false], scenario.DialogWindow.DecorFitsSystemWindowsCalls);
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				Assert.Equal(AndroidSystemBars.LayoutStableSystemUiFlag, AndroidSystemBars.SystemUiVisibility(scenario.DialogWindow) & AndroidSystemBars.LayoutStableSystemUiFlag);
			}

			return Task.CompletedTask;
		});
	}

	static void InvokeCallback(IDialogFragmentService service, string callback, AppCompatActivity activity, AFragment fragment)
	{
		var fragmentManager = activity.SupportFragmentManager;
		switch (callback)
		{
			case nameof(IDialogFragmentService.OnFragmentAttached):
				service.OnFragmentAttached(fragmentManager, fragment, activity);
				break;
			case nameof(IDialogFragmentService.OnFragmentCreated):
				service.OnFragmentCreated(fragmentManager, fragment, new Bundle());
				break;
			case nameof(IDialogFragmentService.OnFragmentDestroyed):
				service.OnFragmentDestroyed(fragmentManager, fragment);
				break;
			case nameof(IDialogFragmentService.OnFragmentDetached):
				service.OnFragmentDetached(fragmentManager, fragment);
				break;
			case nameof(IDialogFragmentService.OnFragmentPaused):
				service.OnFragmentPaused(fragmentManager, fragment);
				break;
			case nameof(IDialogFragmentService.OnFragmentPreAttached):
				service.OnFragmentPreAttached(fragmentManager, fragment, activity);
				break;
			case nameof(IDialogFragmentService.OnFragmentPreCreated):
				service.OnFragmentPreCreated(fragmentManager, fragment, null);
				break;
			case nameof(IDialogFragmentService.OnFragmentResumed):
				service.OnFragmentResumed(fragmentManager, fragment);
				break;
			case nameof(IDialogFragmentService.OnFragmentSaveInstanceState):
				service.OnFragmentSaveInstanceState(fragmentManager, fragment, new Bundle());
				break;
			case nameof(IDialogFragmentService.OnFragmentStopped):
				service.OnFragmentStopped(fragmentManager, fragment);
				break;
			case nameof(IDialogFragmentService.OnFragmentViewCreated):
				service.OnFragmentViewCreated(fragmentManager, fragment, new AView(activity), new Bundle());
				break;
			case nameof(IDialogFragmentService.OnFragmentViewDestroyed):
				service.OnFragmentViewDestroyed(fragmentManager, fragment);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(callback), callback, "Not a callback of IDialogFragmentService.");
		}
	}
}