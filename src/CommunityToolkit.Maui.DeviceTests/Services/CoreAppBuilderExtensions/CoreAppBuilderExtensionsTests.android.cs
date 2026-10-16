#pragma warning disable CA1416, CA1422, CS0618
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityToolkit.Maui.DeviceTests;

// The `#if ANDROID` block of UseMauiCommunityToolkitCore: with the option on, it registers IDialogFragmentService and an Android
// OnCreate lifecycle event that registers a FragmentLifecycleManager on the activity's FragmentManager. The OnCreate delegate is
// invoked the way MauiAppCompatActivity would (the headless runner's activity is not a MauiAppCompatActivity), with activities
// that are never attached, and with a host fragment's child FragmentManager so that nothing is left registered on the runner.
public partial class CoreAppBuilderExtensionsTests
{
	// ---- the completion event, which UseMauiCommunityToolkitCore raises after the platform block

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CompletionEventIsRaisedOnlyWhenTheStatusBarOptionIsEnabled(bool enabled)
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Core.Options));
		var raisedCount = 0;
		void OnCompleted(object? sender, EventArgs e) => raisedCount++;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += OnCompleted;

		try
		{
			MauiApp.CreateBuilder().UseMauiCommunityToolkitCore(options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(enabled));
		}
		finally
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= OnCompleted;
			snapshot.Restore();
		}

		Assert.Equal(enabled ? 1 : 0, raisedCount);
	}

	[Fact]
	public void CompletionEventNotifiesEverySubscribedHandlerOnce()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Core.Options));
		var firstCount = 0;
		var secondCount = 0;
		void OnFirstCompleted(object? sender, EventArgs e) => firstCount++;
		void OnSecondCompleted(object? sender, EventArgs e) => secondCount++;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += OnFirstCompleted;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += OnSecondCompleted;

		try
		{
			MauiApp.CreateBuilder().UseMauiCommunityToolkitCore(options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		}
		finally
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= OnFirstCompleted;
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= OnSecondCompleted;
			snapshot.Restore();
		}

		Assert.Equal(1, firstCount);
		Assert.Equal(1, secondCount);
	}

	[Fact]
	public void CompletionEventDoesNotNotifyAHandlerThatWasRemoved()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Core.Options));
		var removedCount = 0;
		var keptCount = 0;
		void OnRemovedCompleted(object? sender, EventArgs e) => removedCount++;
		void OnKeptCompleted(object? sender, EventArgs e) => keptCount++;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += OnRemovedCompleted;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += OnKeptCompleted;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= OnRemovedCompleted;

		try
		{
			MauiApp.CreateBuilder().UseMauiCommunityToolkitCore(options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		}
		finally
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= OnKeptCompleted;
			snapshot.Restore();
		}

		Assert.Equal(0, removedCount);
		Assert.Equal(1, keptCount);
	}

	[Fact]
	public void UseMauiCommunityToolkitCoreInvokesTheOptionsCallbackOnceAndReturnsTheSameBuilder()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Core.Options));
		var callbackCount = 0;
		var builder = MauiApp.CreateBuilder();

		try
		{
			var returned = builder.UseMauiCommunityToolkitCore(_ => callbackCount++);

			Assert.Same(builder, returned);
			Assert.Equal(1, callbackCount);
		}
		finally
		{
			snapshot.Restore();
		}
	}

	[Fact]
	public void UseMauiCommunityToolkitCoreWithoutOptionsKeepsTheCurrentOptionValue()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Core.Options));
		var raisedCount = 0;
		void OnCompleted(object? sender, EventArgs e) => raisedCount++;
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += OnCompleted;

		try
		{
			var enableBuilder = MauiApp.CreateBuilder();
			enableBuilder.UseMauiCommunityToolkitCore(options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
			Assert.Equal(1, raisedCount);

			// A second call without an options callback must not reset the option: the platform block runs again and the event is raised again.
			var builder = MauiApp.CreateBuilder();
			var returned = builder.UseMauiCommunityToolkitCore(null);

			Assert.Same(builder, returned);
			Assert.Equal(2, raisedCount);
			Assert.Contains(builder.Services, service => service.ServiceType == typeof(IDialogFragmentService));
		}
		finally
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= OnCompleted;
			snapshot.Restore();
		}
	}

	// ---- the registrations of the platform block

	[Fact]
	public void EnabledOptionRegistersTheDialogFragmentServiceAsSingleton()
	{
		var descriptors = new List<ServiceDescriptor>();
		SetupBuilder(
			builder => descriptors.AddRange(builder.Services.Where(service => service.ServiceType == typeof(IDialogFragmentService))),
			options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));

		var descriptor = Assert.Single(descriptors);
		Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
		Assert.Equal(typeof(DialogFragmentService), descriptor.ImplementationType);
		var service = ApplicationServices.GetRequiredService<IDialogFragmentService>();
		Assert.IsType<DialogFragmentService>(service);
		Assert.Same(service, ApplicationServices.GetRequiredService<IDialogFragmentService>());
	}

	[Fact]
	public void DisabledOptionRegistersNeitherTheServiceNorTheOnCreateEvent()
	{
		var serviceRegistered = true;
		SetupBuilder(
			builder => serviceRegistered = builder.Services.Any(service => service.ServiceType == typeof(IDialogFragmentService)),
			options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(false));

		Assert.False(serviceRegistered);
		Assert.Null(ApplicationServices.GetService<IDialogFragmentService>());
		Assert.Empty(GetToolkitOnCreateDelegates());
	}

	[Fact]
	public void EnabledOptionRegistersExactlyOneToolkitOnCreateEvent()
	{
		SetupBuilder(configureToolkitOptions: options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));

		Assert.Single(GetToolkitOnCreateDelegates());
	}

	[Fact]
	public void ServiceIsRegisteredBeforeTheCompletionEventIsRaised()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Core.Options));
		var builder = MauiApp.CreateBuilder();
		var registeredWhenRaised = false;
		void OnCompleted(object? sender, EventArgs e) =>
			registeredWhenRaised = builder.Services.Any(service => service.ServiceType == typeof(IDialogFragmentService));
		Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted += OnCompleted;

		try
		{
			builder.UseMauiCommunityToolkitCore(options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		}
		finally
		{
			Core.AppBuilderExtensions.ShouldUseStatusBarBehaviorOnAndroidModalPageOptionCompleted -= OnCompleted;
			snapshot.Restore();
		}

		Assert.True(registeredWhenRaised);
	}

	[Fact]
	public async Task OnCreateWithAnActivityThatIsNotAnAppCompatActivityTracesAndRegistersNothing()
	{
		SetupBuilder(configureToolkitOptions: options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		var onCreate = GetToolkitOnCreateDelegates().Single();
		using var trace = new FragmentTraceListener();

		await InvokeOnMainThreadAsync(() => onCreate(new PlainActivity(), null));

		Assert.Contains("Unable to Modify Android StatusBar On ModalPage: Activity Test.PlainActivity must be an AppCompatActivity", trace.Messages);
	}

	[Fact]
	public async Task OnCreateWithoutAFragmentManagerTracesAndRegistersNothing()
	{
		SetupBuilder(configureToolkitOptions: options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		var onCreate = GetToolkitOnCreateDelegates().Single();
		using var trace = new FragmentTraceListener();

		await InvokeOnMainThreadAsync(() =>
		{
			var spy = new SpyActivity { SpyFragmentManager = null };
			onCreate(spy, null);
		});

		Assert.Contains("Unable to Modify Android StatusBar On ModalPage: Unable to retrieve fragment manager from AppCompatActivity", trace.Messages);
	}

	[Fact]
	public async Task OnCreateWithoutAPlatformApplicationThrowsInvalidOperationException()
	{
		SetupBuilder(configureToolkitOptions: options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		var onCreate = GetToolkitOnCreateDelegates().Single();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeOnMainThreadAsync(() =>
		{
			using (new PlatformApplicationScope(null))
			{
				onCreate(new SpyActivity { SpyFragmentManager = HostActivity.SupportFragmentManager }, null);
			}
		}));

		Assert.Equal($"Unable to retrieve {nameof(IDialogFragmentService)}", exception.Message);
	}

	[Fact]
	public async Task OnCreateThrowsWhenTheRunningAppHasNoDialogFragmentService()
	{
		SetupBuilder(configureToolkitOptions: options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		var onCreate = GetToolkitOnCreateDelegates().Single();
		using var servicesWithoutTheDialogFragmentService = new ServiceCollection().BuildServiceProvider();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeOnMainThreadAsync(() =>
		{
			using (new PlatformApplicationScope(new FakePlatformApplication(servicesWithoutTheDialogFragmentService)))
			{
				onCreate(new SpyActivity { SpyFragmentManager = HostActivity.SupportFragmentManager }, null);
			}
		}));

		Assert.Contains(nameof(IDialogFragmentService), exception.Message);
	}

	[Fact]
	public async Task OnCreateRegistersCallbacksThatForwardToTheServiceOfTheRunningApp()
	{
		SetupBuilder(configureToolkitOptions: options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		var onCreate = GetToolkitOnCreateDelegates().Single();
		var runningAppService = new RecordingDialogFragmentService();
		using var runningAppServices = new ServiceCollection().AddSingleton<IDialogFragmentService>(runningAppService).BuildServiceProvider();
		var host = new PlainFragment();
		var child = new PlainFragment();
		var grandchild = new PlainFragment();

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				HostActivity.SupportFragmentManager.BeginTransaction().Add(host, $"test-host-{Guid.NewGuid():N}").CommitNow();
				var childManager = host.ChildFragmentManager;

				using (new PlatformApplicationScope(new FakePlatformApplication(runningAppServices)))
				{
					onCreate(new SpyActivity { SpyFragmentManager = childManager }, null);
				}

				// The registered FragmentLifecycleManager forwards to the service of IPlatformApplication.Current (not the builder's)...
				childManager.BeginTransaction().Add(child, "test-child").CommitNow();
				Assert.Contains(nameof(IDialogFragmentService.OnFragmentStarted), runningAppService.CallsFor(child));
				Assert.Contains(nameof(IDialogFragmentService.OnFragmentResumed), runningAppService.CallsFor(child));

				// ...and it is registered without recursion: fragments nested one level deeper are not reported.
				child.ChildFragmentManager.BeginTransaction().Add(grandchild, "test-grandchild").CommitNow();
				Assert.Empty(runningAppService.CallsFor(grandchild));

				childManager.BeginTransaction().Remove(child).CommitNow();
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => HostActivity.SupportFragmentManager.BeginTransaction().Remove(host).CommitNow());
		}
	}

	[Fact]
	public async Task OnCreateWiresTheRealServiceSoDialogFragmentsTakeOverTheStatusBarOfTheActivity()
	{
		SetupBuilder(configureToolkitOptions: options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		var onCreate = GetToolkitOnCreateDelegates().Single();
		var host = new PlainFragment();
		// A real dialog that starts out different from the activity: status bar color is read back before Android 15,
		// light status bar appearance from Android 11 on (see DialogFragmentServiceTests for why).
		var dialogFragment = new ProbeDialogFragment
		{
			DialogFactory = context =>
			{
				var dialog = new AndroidX.AppCompat.App.AppCompatDialog(context);
				ArrangeThatStartedWouldChange(dialog.Window!);
				return dialog;
			}
		};

		await WithActivityStatusBarAsync(distinctOpaqueColor, async () =>
		{
			try
			{
				await InvokeOnMainThreadAsync(() =>
				{
					HostActivity.SupportFragmentManager.BeginTransaction().Add(host, $"test-host-{Guid.NewGuid():N}").CommitNow();
					var childManager = host.ChildFragmentManager;

					using (new PlatformApplicationScope(new FakePlatformApplication(ApplicationServices)))
					{
						onCreate(new SpyActivity { SpyFragmentManager = childManager }, null);
					}

					// Starting the dialog runs the registered FragmentLifecycleManager -> DialogFragmentService.OnFragmentStarted.
					dialogFragment.ShowNow(childManager, "test-dialog");
					AssertDialogTookOverTheStatusBarOfTheActivity(dialogFragment.Dialog!.Window!);
				});
			}
			finally
			{
				await InvokeOnMainThreadAsync(() =>
				{
					if (dialogFragment.IsAdded)
					{
						dialogFragment.DismissNow();
					}

					HostActivity.SupportFragmentManager.BeginTransaction().Remove(host).CommitNow();
				});
			}
		});
	}

	// The dialog of a modal page in .NET MAUI asks for light status and navigation bar icons whenever the app is not in night mode
	// (ModalFragment.OnCreateDialog calls WindowExtensions.ConfigureTranslucentSystemBars from Android 11 on: Microsoft.Maui.Controls and
	// Microsoft.Maui 10.0.90, decompiled). A page whose StatusBarBehavior shows light content (light icons on a dark status bar) has
	// cleared that appearance on the activity, so the dialog has to lose it too: otherwise its status bar shows dark icons on the dark
	// color it copied from the activity. That is every day-mode modal page of such an app.
	[Fact]
	public async Task OnCreateWiresTheRealServiceSoTheLightStatusBarsMauiGaveAModalDialogAreClearedWhenTheActivityHasNone()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(30), "WindowInsetsController appearance needs Android 11 (API 30), and .NET MAUI only gives its modal dialog light system bars from Android 11 on.");
		SetupBuilder(configureToolkitOptions: options => options.SetShouldUseStatusBarBehaviorOnAndroidModalPage(true));
		var onCreate = GetToolkitOnCreateDelegates().Single();
		var host = new PlainFragment();
		var dialogFragment = new ProbeDialogFragment
		{
			DialogFactory = context =>
			{
				// What ConfigureTranslucentSystemBars does to the dialog window of a modal page in day mode.
				var dialog = new AppCompatDialog(context);
				var window = dialog.Window!;
				var controller = WindowCompat.GetInsetsController(window, window.DecorView) ?? throw new InvalidOperationException("The dialog window has no insets controller.");
				controller.AppearanceLightStatusBars = true;
				controller.AppearanceLightNavigationBars = true;
				return dialog;
			}
		};

		await WithActivityStatusBarAsync(distinctOpaqueColor, async () =>
		{
			try
			{
				await InvokeOnMainThreadAsync(() =>
				{
					// StatusBarBehavior with StatusBarStyle.LightContent: light icons, so the activity has no light status bars.
					SetAppearance(HostActivity.Window!, 0, lightStatusBarsBit);
					Assert.False(Capture(HostActivity.Window!).HasLightStatusBars);

					HostActivity.SupportFragmentManager.BeginTransaction().Add(host, $"test-host-{Guid.NewGuid():N}").CommitNow();
					var childManager = host.ChildFragmentManager;

					using (new PlatformApplicationScope(new FakePlatformApplication(ApplicationServices)))
					{
						onCreate(new SpyActivity { SpyFragmentManager = childManager }, null);
					}

					dialogFragment.ShowNow(childManager, "test-modal-dialog");

					Assert.False(Capture(dialogFragment.Dialog!.Window!).HasLightStatusBars, "The dialog should have taken over the activity's appearance, which has no light status bars.");
				});
			}
			finally
			{
				await InvokeOnMainThreadAsync(() =>
				{
					if (dialogFragment.IsAdded)
					{
						dialogFragment.DismissNow();
					}

					if (host.IsAdded)
					{
						HostActivity.SupportFragmentManager.BeginTransaction().Remove(host).CommitNow();
					}
				});
			}
		});
	}
}