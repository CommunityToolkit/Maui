#if !NET11_0_OR_GREATER
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// .NET 10: UIKit takes the status bar style of an app whose Info.plist keeps view controller-based status bar appearance, such as the runner, from its
// view controllers, and the view controllers of .NET MAUI 10 choose no style. StatusBar.SetStyle sets the style of the whole app: the first
// LightContent or DarkContent replaces -preferredStatusBarStyle of UIViewController (and of the UIKit containers that have their own), and every view
// controller that does not choose a style returns the requested one until SetStyle(Default). The tests show their page in a .NET MAUI window and read what UIKit shows (AssertStatusBarStyleAsync): the style of the
// view controller that UIKit asks, the style of the scene's status bar and the content on the screen. The base class sets Default after each test.
public partial class StatusBarTests
{
	static readonly TimeSpan navigationTimeout = TimeSpan.FromSeconds(10);

	// The window's appearance (light or dark mode): LightContent and DarkContent win over it, and Default leaves the choice to UIKit
	[Theory]
	[InlineData(StatusBarWindowPage.ContentPage)]
	[InlineData(StatusBarWindowPage.NavigationPage)]
	[InlineData(StatusBarWindowPage.TabbedPage)]
	public async Task SetStyleWinsOverTheAppearanceOfTheWindow(StatusBarWindowPage kind)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(kind, CreateStyleTestPage(), async mauiWindow =>
		{
			foreach (var appearance in new[] { UIUserInterfaceStyle.Dark, UIUserInterfaceStyle.Light })
			{
				mauiWindow.PlatformWindow.OverrideUserInterfaceStyle = appearance;

				await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);
				await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);
				await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.Default, UIStatusBarStyle.Default);
			}
		});
	}

	// The page that a NavigationPage pushes, and the page that it pops back to, show the style, with and without the animations
	[Fact]
	public async Task SetStyleStaysWhenANavigationPagePushesAndPops()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreateStyleTestPage();
		var navigationPage = new NavigationPage(page) { BarBackgroundColor = Colors.Red };

		await RunInMauiWindowAsync(navigationPage, false, page, async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);

			var pushedPage = CreateStyleTestPage();
			await navigationPage.PushAsync(pushedPage, false);
			await pushedPage.OnLoadedAsync(navigationTimeout);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);

			await navigationPage.PopAsync(false);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);

			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);

			await navigationPage.PushAsync(CreateStyleTestPage(), true);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			await navigationPage.PopAsync(true);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);
		});
	}

	[Fact]
	public async Task SetStyleStaysWhenATabbedPageSwitchesTabs()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreateStyleTestPage();
		var secondPage = CreateStyleTestPage();
		var tabbedPage = new TabbedPage { Children = { page, secondPage } };

		await RunInMauiWindowAsync(tabbedPage, false, page, async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);

			tabbedPage.CurrentPage = secondPage;
			await secondPage.OnLoadedAsync(navigationTimeout);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);

			tabbedPage.CurrentPage = page;
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	[Fact]
	public async Task SetStyleStaysWhenShellSwitchesTabs()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreateStyleTestPage();
		var secondPage = CreateStyleTestPage();
		var tabBar = new TabBar { Items = { new ShellContent { Title = "First", Content = page }, new ShellContent { Title = "Second", Content = secondPage } } };
		var shell = new Shell { BackgroundColor = Colors.Red, Items = { tabBar } };

		await RunInMauiWindowAsync(shell, false, page, async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);

			tabBar.CurrentItem = tabBar.Items[1];
			await secondPage.OnLoadedAsync(navigationTimeout);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);

			tabBar.CurrentItem = tabBar.Items[0];
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	[Fact]
	public async Task SetStyleStaysWhenAFlyoutPageShowsItsFlyoutAndChangesItsDetail()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreateStyleTestPage();
		var flyoutPage = new FlyoutPage { Flyout = CreateStyleTestPage(), Detail = page };

		await RunInMauiWindowAsync(flyoutPage, false, page, async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);

			flyoutPage.IsPresented = true;
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);

			flyoutPage.IsPresented = false;
			var detail = CreateStyleTestPage();
			flyoutPage.Detail = detail;
			await detail.OnLoadedAsync(navigationTimeout);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	[Fact]
	public async Task SetStyleStaysWhenAModalPageIsPushedAndPopped()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreateStyleTestPage();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, page, async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);

			var modalPage = CreateStyleTestPage();
			await mauiWindow.Window.Navigation.PushModalAsync(modalPage, false);
			await modalPage.OnLoadedAsync(navigationTimeout);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);

			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);

			await mauiWindow.Window.Navigation.PopModalAsync(false);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);
		});
	}

	// The window gets another page (a new root view controller), and its first page back
	[Fact]
	public async Task SetStyleStaysWhenThePageOfTheWindowIsReplaced()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreateStyleTestPage();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, page, async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);

			var replacement = new NavigationPage(CreateStyleTestPage()) { BarBackgroundColor = Colors.Red };
			mauiWindow.Window.Page = replacement;
			await replacement.CurrentPage.OnLoadedAsync(navigationTimeout);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);

			mauiWindow.Window.Page = page;
			await page.OnLoadedAsync(navigationTimeout);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	// The style belongs to the app: a second window, which opens after the call, shows it, and so does the first window again
	[Fact]
	public async Task SetStyleReachesEveryWindowAlsoOneThatOpensLater()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);

			using var viewController = new RedViewController();
			var secondPlatformWindow = new UIWindow(mauiWindow.Scene) { Frame = mauiWindow.PlatformWindow.Frame, RootViewController = viewController };

			try
			{
				secondPlatformWindow.MakeKeyAndVisible();
				var secondWindow = mauiWindow with { PlatformWindow = secondPlatformWindow };
				await AssertStatusBarStyleAsync(secondWindow, UIStatusBarStyle.LightContent);

				await SetStyleAndAssertAsync(secondWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);

				secondPlatformWindow.Hidden = true;
				mauiWindow.PlatformWindow.MakeKeyAndVisible();
				await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
			}
			finally
			{
				IosStatusBarViews.RemoveWindow(secondPlatformWindow);
				mauiWindow.PlatformWindow.MakeKeyAndVisible();
			}
		});
	}

	// View controllers of the app that choose their own style (override preferredStatusBarStyle) keep it, also after Default, and a view controller
	// presented over one of them shows the requested style
	[Fact]
	public async Task ViewControllersThatChooseTheirOwnStyleKeepIt()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);

			using var ownStyleViewController = new OwnStyleViewController(UIStatusBarStyle.LightContent);
			using var viewController = new RedViewController();
			await RootViewController(mauiWindow).PresentViewControllerAsync(ownStyleViewController, false);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			await ownStyleViewController.PresentViewControllerAsync(viewController, false);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);

			await viewController.DismissViewControllerAsync(false);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.Default, UIStatusBarStyle.LightContent);

			await ownStyleViewController.DismissViewControllerAsync(false);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.Default);
		});
	}

	// An override of preferredStatusBarStyle that returns the style of the base implementation (as the view controllers of .NET MAUI 11 do for
	// StatusBarTheme.Default) returns the requested style: a view controller, and a navigation controller, whose class has its own implementation
	[Fact]
	public async Task OverridesThatReturnTheBaseStyleReturnTheRequestedStyle()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			using var viewController = new BaseStyleViewController();
			await RootViewController(mauiWindow).PresentViewControllerAsync(viewController, false);
			await SetStylesAndAssertAsync(mauiWindow);
			Assert.True(viewController.StyleQueries > 0, "UIKit did not ask the view controller that returns the base style.");
			await viewController.DismissViewControllerAsync(false);

			using var navigationController = new BaseStyleNavigationController(new RedViewController());
			await RootViewController(mauiWindow).PresentViewControllerAsync(navigationController, false);
			await SetStylesAndAssertAsync(mauiWindow);
			Assert.True(navigationController.StyleQueries > 0, "UIKit did not ask the navigation controller that returns the base style.");
			await navigationController.DismissViewControllerAsync(false);
		});

		static async Task SetStylesAndAssertAsync(HostedMauiWindow mauiWindow)
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.Default, UIStatusBarStyle.Default);
		}
	}

	// An alert (UIAlertController, which DisplayAlert shows) is a view controller without a style of its own
	[Fact]
	public async Task SetStyleStaysWhileAnAlertIsShown()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);

			using var alert = UIAlertController.Create("Status bar", "Style", UIAlertControllerStyle.Alert);
			alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
			await RootViewController(mauiWindow).PresentViewControllerAsync(alert, false);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.LightContent);

			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);

			await alert.DismissViewControllerAsync(false);
			await AssertStatusBarStyleAsync(mauiWindow, UIStatusBarStyle.DarkContent);
		});
	}

	// Another library replaces -[UIViewController preferredStatusBarStyle] after the toolkit did, and its implementation calls the one that it replaced:
	// the toolkit's, which returns the requested style
	[Fact]
	public async Task AnotherLibraryThatReplacesTheStyleAfterTheToolkitShowsTheRequestedStyle()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);
			AnotherLibrary.ReplacePreferredStatusBarStyle();

			try
			{
				await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);
				await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.Default, UIStatusBarStyle.Default);
				await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.LightContent, UIStatusBarStyle.LightContent);
				Assert.True(AnotherLibrary.StyleQueries > 0, "UIKit did not call the implementation of the other library.");
			}
			finally
			{
				AnotherLibrary.RestorePreferredStatusBarStyle();
			}
		});
	}

	// Another library replaced -[UIViewController preferredStatusBarStyle] before the toolkit's first style, with a style of its own: the toolkit's
	// implementation returns the requested style, and after Default the style of the implementation that it replaced, the library's
	[Fact]
	public async Task AnotherLibraryThatReplacedTheStyleBeforeTheToolkitGetsItsStyleBackWithDefault()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await RunInMauiWindowAsync(StatusBarWindowPage.ContentPage, CreateStyleTestPage(), async mauiWindow =>
		{
			ToolkitReplacement.Undo();
			AnotherLibrary.ReplacePreferredStatusBarStyle(UIStatusBarStyle.LightContent);

			try
			{
				await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.Default, UIStatusBarStyle.LightContent);
				await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.DarkContent, UIStatusBarStyle.DarkContent);
				Assert.Equal(AnotherLibrary.Implementation, ToolkitReplacement.ReplacedImplementation("UIViewController"));

				await SetStyleAndAssertAsync(mauiWindow, StatusBarStyle.Default, UIStatusBarStyle.LightContent);
			}
			finally
			{
				ToolkitReplacement.Undo();
				AnotherLibrary.RestorePreferredStatusBarStyle();
			}
		});
	}

	// Default requests no style and replaces nothing. The first LightContent or DarkContent replaces the implementation of UIViewController and of each
	// container class that has its own (the others have UIViewController's method), once: the toolkit keeps UIKit's implementations and calls them for
	// Default. The test starts from UIKit's implementations, whatever the tests before it did.
	[Fact]
	public async Task SetStyleReplacesTheImplementationsOfUIKitOnceAndOnlyForAStyle()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			ToolkitReplacement.Undo();
			var uikitImplementations = ToolkitReplacement.Implementations();
			Assert.All(uikitImplementations.Values, static implementation => Assert.Equal("UIKitCore", ToolkitReplacement.ImageName(implementation)));

			StatusBar.SetStyle(StatusBarStyle.Default);

			Assert.Equal(uikitImplementations, ToolkitReplacement.Implementations());
			Assert.False(ToolkitReplacement.IsReplaced);

			StatusBar.SetStyle(StatusBarStyle.LightContent);

			foreach (var className in ToolkitReplacement.ClassNames)
			{
				var hasOwnMethod = ToolkitReplacement.HasOwnMethod(className);
				Assert.Equal(ToolkitReplacement.ToolkitImplementation(hasOwnMethod ? className : "UIViewController"), ToolkitReplacement.Implementation(className));
				Assert.Equal(hasOwnMethod ? uikitImplementations[className] : IntPtr.Zero, ToolkitReplacement.ReplacedImplementation(className));
			}

			var implementations = ToolkitReplacement.Implementations();
			StatusBar.SetStyle(StatusBarStyle.DarkContent);
			StatusBar.SetStyle(StatusBarStyle.Default);
			StatusBar.SetStyle(StatusBarStyle.LightContent);

			Assert.Equal(implementations, ToolkitReplacement.Implementations());
		});
	}

	// The replacement runs once per process. Run again (its guard taken away), it finds the toolkit's implementations in place and keeps the ones that it
	// replaced: Default calls UIKit's implementation, not the toolkit's own again until the stack overflows.
	[Fact]
	public async Task ASecondReplacementKeepsTheReplacedImplementations()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetStyle(StatusBarStyle.LightContent);
			var implementations = ToolkitReplacement.Implementations();
			var replacedImplementations = ToolkitReplacement.ReplacedImplementations();

			ToolkitReplacement.IsReplaced = false;
			ToolkitReplacement.Replace();

			Assert.Equal(implementations, ToolkitReplacement.Implementations());
			Assert.Equal(replacedImplementations, ToolkitReplacement.ReplacedImplementations());

			using var viewController = new UIViewController();
			using var navigationController = new UINavigationController(new UIViewController());
			Assert.Equal(UIStatusBarStyle.LightContent, viewController.PreferredStatusBarStyle());
			Assert.Equal(UIStatusBarStyle.LightContent, navigationController.PreferredStatusBarStyle());

			StatusBar.SetStyle(StatusBarStyle.Default);

			Assert.Equal(UIStatusBarStyle.Default, viewController.PreferredStatusBarStyle());
			Assert.Equal(UIStatusBarStyle.Default, navigationController.PreferredStatusBarStyle());
		});
	}

	// The first replacement, run by several threads at the same time: each class gets the toolkit's implementation once, and the toolkit keeps UIKit's
	[Fact]
	public async Task ReplacementsAtTheSameTimeReplaceEachImplementationOnce()
	{
		const int threadCount = 8;
		SetupBuilder();
		await ResetNativeStateAsync();
		var uikitImplementations = await InvokeOnMainThreadAsync(static () =>
		{
			ToolkitReplacement.Undo();
			return ToolkitReplacement.Implementations();
		});

		using (var start = new Barrier(threadCount))
		{
			await Task.WhenAll(Enumerable.Range(0, threadCount).Select(_ => Task.Factory.StartNew(() =>
			{
				start.SignalAndWait();
				ToolkitReplacement.Replace();
			}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)));
		}

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.True(ToolkitReplacement.IsReplaced);
			foreach (var className in ToolkitReplacement.ClassNames.Where(ToolkitReplacement.HasOwnMethod))
			{
				Assert.Equal(ToolkitReplacement.ToolkitImplementation(className), ToolkitReplacement.Implementation(className));
				Assert.Equal(uikitImplementations[className], ToolkitReplacement.ReplacedImplementation(className));
			}

			using var viewController = new UIViewController();
			Assert.Equal(UIStatusBarStyle.Default, viewController.PreferredStatusBarStyle());

			StatusBar.SetStyle(StatusBarStyle.DarkContent);

			Assert.Equal(UIStatusBarStyle.DarkContent, viewController.PreferredStatusBarStyle());
		});
	}

	// UIKit asks for the style on the main thread. The first calls, from several other threads at the same time, throw before they change anything (no
	// style, no replacement), also when .NET for iOS does not check the thread of UIKit calls, as in a Release build
	// (UIApplication.CheckForIllegalCrossThreadCalls is false).
	[Fact]
	public async Task SetStyleFromOtherThreadsThrowsBeforeItChangesAnything()
	{
		const int threadCount = 4;
		SetupBuilder();
		await ResetNativeStateAsync();
		var uikitImplementations = await InvokeOnMainThreadAsync(static () =>
		{
			ToolkitReplacement.Undo();
			return ToolkitReplacement.Implementations();
		});
		var checkForIllegalCrossThreadCalls = UIApplication.CheckForIllegalCrossThreadCalls;
		Exception?[] exceptions;

		UIApplication.CheckForIllegalCrossThreadCalls = false;
		try
		{
			using var start = new Barrier(threadCount);
			exceptions = await Task.WhenAll(Enumerable.Range(0, threadCount).Select(_ => Task.Factory.StartNew(() =>
			{
				start.SignalAndWait();
				return Record.Exception(static () => StatusBar.SetStyle(StatusBarStyle.LightContent));
			}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)));
		}
		finally
		{
			UIApplication.CheckForIllegalCrossThreadCalls = checkForIllegalCrossThreadCalls;
		}

		Assert.All(exceptions, static exception => Assert.IsType<UIKitThreadAccessException>(exception));
		await InvokeOnMainThreadAsync(() =>
		{
			Assert.False(ToolkitReplacement.IsReplaced);
			Assert.Equal(uikitImplementations, ToolkitReplacement.Implementations());

			using var viewController = new UIViewController();
			Assert.Equal(UIStatusBarStyle.Default, viewController.PreferredStatusBarStyle());
		});
	}

	static Task SetStyleAndAssertAsync(HostedMauiWindow mauiWindow, StatusBarStyle style, UIStatusBarStyle expected)
	{
		StatusBar.SetStyle(style);
		return AssertStatusBarStyleAsync(mauiWindow, expected);
	}

	static UIViewController RootViewController(HostedMauiWindow mauiWindow) =>
		mauiWindow.PlatformWindow.RootViewController ?? throw new InvalidOperationException("The window has no root view controller.");

	// A full-screen red view controller without a style of its own: the status bar shows its white or black content over it
	class RedViewController : UIViewController
	{
		public RedViewController() => ModalPresentationStyle = UIModalPresentationStyle.FullScreen;

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			View?.BackgroundColor = UIColor.Red;
		}
	}

	sealed class OwnStyleViewController(UIStatusBarStyle style) : RedViewController
	{
		public override UIStatusBarStyle PreferredStatusBarStyle() => style;
	}

	sealed class BaseStyleViewController : RedViewController
	{
		public int StyleQueries { get; private set; }

		public override UIStatusBarStyle PreferredStatusBarStyle()
		{
			StyleQueries++;
			return base.PreferredStatusBarStyle();
		}
	}

	sealed class BaseStyleNavigationController : UINavigationController
	{
		public BaseStyleNavigationController(UIViewController rootViewController) : base(rootViewController) => ModalPresentationStyle = UIModalPresentationStyle.FullScreen;

		public int StyleQueries { get; private set; }

		public override UIStatusBarStyle PreferredStatusBarStyle()
		{
			StyleQueries++;
			return base.PreferredStatusBarStyle();
		}
	}
}

/// <summary>
/// The toolkit's replacement of -preferredStatusBarStyle (StatusBar.ios.cs, .NET 10), read through the Objective-C runtime and its private fields.
/// <see cref="Undo"/> takes it out, so that a test can start from UIKit's implementations whatever the tests before it did. Call it on the UI thread
/// (<see cref="Replace"/> on any).
/// </summary>
static class ToolkitReplacement
{
	const BindingFlags privateStatic = BindingFlags.NonPublic | BindingFlags.Static;

	/// <summary>The classes whose implementation the toolkit replaces when they have their own; their StatusBar fields are named after them without "UI".</summary>
	public static readonly string[] ClassNames = ["UIViewController", "UINavigationController", "UITabBarController", "UISplitViewController"];

	static readonly IntPtr preferredStatusBarStyleSelector = Selector.GetHandle("preferredStatusBarStyle");

	public static bool IsReplaced
	{
		get => Field("isPreferredStatusBarStyleReplaced").GetValue(null) is true;
		set => Field("isPreferredStatusBarStyleReplaced").SetValue(null, value);
	}

	public static IntPtr Method(string className) => class_getInstanceMethod(Class.GetHandle(className), preferredStatusBarStyleSelector);

	public static IntPtr Implementation(string className) => method_getImplementation(Method(className));

	public static Dictionary<string, IntPtr> Implementations() => ClassNames.ToDictionary(static className => className, Implementation);

	/// <summary>Whether the class has a method of its own, not UIViewController's (UIViewController has its own).</summary>
	public static bool HasOwnMethod(string className) => className is "UIViewController" || Method(className) != Method("UIViewController");

	/// <summary>The toolkit's implementation for the class.</summary>
	public static IntPtr ToolkitImplementation(string className) => ImplementationField($"toolkit{className[2..]}Implementation");

	/// <summary>The implementation that the toolkit replaced in the class (IntPtr.Zero if none).</summary>
	public static IntPtr ReplacedImplementation(string className) => ImplementationField($"replaced{className[2..]}Implementation");

	public static Dictionary<string, IntPtr> ReplacedImplementations() => ClassNames.ToDictionary(static className => className, ReplacedImplementation);

	/// <summary>Runs the toolkit's replacement (StatusBar.ReplacePreferredStatusBarStyle), which the first LightContent or DarkContent runs.</summary>
	public static void Replace() =>
		(typeof(StatusBar).GetMethod("ReplacePreferredStatusBarStyle", privateStatic, Type.EmptyTypes) ?? throw new InvalidOperationException("StatusBar has no private static ReplacePreferredStatusBarStyle()."))
			.Invoke(null, null);

	/// <summary>
	/// Gives each class whose implementation the toolkit replaced that implementation back, and lets the toolkit forget it: the next LightContent or
	/// DarkContent replaces the implementations again, as the first one after the app started does.
	/// </summary>
	public static void Undo()
	{
		StatusBar.SetStyle(StatusBarStyle.Default);

		foreach (var className in ClassNames)
		{
			var replacedImplementation = ReplacedImplementation(className);
			if (replacedImplementation != IntPtr.Zero && Implementation(className) == ToolkitImplementation(className))
			{
				method_setImplementation(Method(className), replacedImplementation);
			}

			Field($"replaced{className[2..]}Implementation").SetValue(null, IntPtr.Zero);
		}

		IsReplaced = false;
	}

	/// <summary>The file name of the image (library or executable) that has the code at <paramref name="address"/>.</summary>
	public static string ImageName(IntPtr address) =>
		dladdr(address, out var info) != 0 && info.FileName != IntPtr.Zero ? Path.GetFileName(Marshal.PtrToStringUTF8(info.FileName) ?? string.Empty) : string.Empty;

	public static IntPtr SetImplementation(IntPtr method, IntPtr implementation) => method_setImplementation(method, implementation);

	public static IntPtr GetImplementation(IntPtr method) => method_getImplementation(method);

	static FieldInfo Field(string name) =>
		typeof(StatusBar).GetField(name, privateStatic) ?? throw new InvalidOperationException($"StatusBar has no private static field {name}.");

	static IntPtr ImplementationField(string name) =>
		Field(name).GetValue(null) is IntPtr implementation ? implementation : throw new InvalidOperationException($"StatusBar.{name} is not an IntPtr.");

	[DllImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
	static extern IntPtr class_getInstanceMethod(IntPtr cls, IntPtr selector);

	[DllImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
	static extern IntPtr method_getImplementation(IntPtr method);

	[DllImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
	static extern IntPtr method_setImplementation(IntPtr method, IntPtr implementation);

	[DllImport("/usr/lib/libSystem.dylib")]
	static extern int dladdr(IntPtr address, out DlInfo info);

	[StructLayout(LayoutKind.Sequential)]
	struct DlInfo
	{
		public IntPtr FileName;
		public IntPtr FileBase;
		public IntPtr SymbolName;
		public IntPtr SymbolAddress;
	}
}

/// <summary>
/// What another library does that replaces -[UIViewController preferredStatusBarStyle]: its implementation returns a style of its own, or without one the
/// style of the implementation that it replaced. Call it on the UI thread.
/// </summary>
static class AnotherLibrary
{
	static readonly PreferredStatusBarStyleImplementation implementation = PreferredStatusBarStyle;
	static PreferredStatusBarStyleImplementation? replacedImplementation;
	static IntPtr replacedImplementationPointer;
	static UIStatusBarStyle? ownStyle;

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate nint PreferredStatusBarStyleImplementation(IntPtr self, IntPtr selector);

	/// <summary>The library's implementation.</summary>
	public static IntPtr Implementation { get; } = Marshal.GetFunctionPointerForDelegate(implementation);

	/// <summary>How often UIKit called the library's implementation since <see cref="ReplacePreferredStatusBarStyle"/>.</summary>
	public static int StyleQueries { get; private set; }

	public static void ReplacePreferredStatusBarStyle(UIStatusBarStyle? style = null)
	{
		var method = ToolkitReplacement.Method("UIViewController");
		ownStyle = style;
		StyleQueries = 0;
		replacedImplementationPointer = ToolkitReplacement.GetImplementation(method);
		replacedImplementation = Marshal.GetDelegateForFunctionPointer<PreferredStatusBarStyleImplementation>(replacedImplementationPointer);
		ToolkitReplacement.SetImplementation(method, Implementation);
	}

	/// <summary>Gives the method the implementation back that the library replaced.</summary>
	public static void RestorePreferredStatusBarStyle() => ToolkitReplacement.SetImplementation(ToolkitReplacement.Method("UIViewController"), replacedImplementationPointer);

	[MonoPInvokeCallback(typeof(PreferredStatusBarStyleImplementation))]
	static nint PreferredStatusBarStyle(IntPtr self, IntPtr selector)
	{
		StyleQueries++;
		return ownStyle is UIStatusBarStyle style ? (nint)style : replacedImplementation?.Invoke(self, selector) ?? (nint)UIStatusBarStyle.Default;
	}
}
#endif