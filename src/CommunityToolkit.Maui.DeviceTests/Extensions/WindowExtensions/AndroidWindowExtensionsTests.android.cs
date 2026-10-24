using Android.Runtime;
using Android.Views;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.AndroidWindowExtensions)]
[Collection(TestCollections.DisableParallelization)]
public class AndroidWindowExtensionsTests : StatusBarTestBase
{
	[Fact]
	public async Task GetCurrentWindowReturnsTheActivityWindowWhenThereAreNoDialogs()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () => AssertIsActivityWindow(AndroidSystemBars.Activity.GetCurrentWindow()));
	}

	[Fact]
	public async Task GetCurrentWindowReturnsTheWindowOfTheVisibleDialog()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await WithDialogsAsync([nameof(GetCurrentWindowReturnsTheWindowOfTheVisibleDialog)], static async dialogs =>
		{
			var dialog = dialogs[0];
			await InvokeOnMainThreadAsync(() =>
			{
				Assert.True(dialog.IsShowingAndVisible);

				var window = AndroidSystemBars.Activity.GetCurrentWindow();

				AssertIsWindow(dialog.DialogWindow, window);
				Assert.False(IsSameWindow(AndroidSystemBars.ActivityWindow, window));
			});
		});
	}

	[Fact]
	public async Task GetCurrentWindowReturnsTheActivityWindowAgainAfterTheDialogWasDismissed()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var dialog = await InvokeOnMainThreadAsync(static () => TestDialogFragment.Show(nameof(GetCurrentWindowReturnsTheActivityWindowAgainAfterTheDialogWasDismissed)));

		try
		{
			await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, "The dialog fragment did not become visible.");
			await InvokeOnMainThreadAsync(() => AssertIsWindow(dialog.DialogWindow, AndroidSystemBars.Activity.GetCurrentWindow()));
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}

		await InvokeOnMainThreadAsync(static () => AssertIsActivityWindow(AndroidSystemBars.Activity.GetCurrentWindow()));
	}

	[Fact]
	public async Task GetCurrentWindowReturnsTheTopmostOfSeveralVisibleDialogs()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await WithDialogsAsync(["bottom", "middle", "top"], static async dialogs =>
		{
			await InvokeOnMainThreadAsync(() =>
			{
				AssertIsWindow(dialogs[2].DialogWindow, AndroidSystemBars.Activity.GetCurrentWindow());

				dialogs[2].Remove();
				AssertIsWindow(dialogs[1].DialogWindow, AndroidSystemBars.Activity.GetCurrentWindow());

				dialogs[1].Remove();
				AssertIsWindow(dialogs[0].DialogWindow, AndroidSystemBars.Activity.GetCurrentWindow());
			});
		});
	}

	[Fact]
	public async Task GetCurrentWindowSkipsATopDialogWhoseWindowIsHidden()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var isWindowHidden = false;

		await WithDialogsAsync(["bottom", "top"], async dialogs =>
		{
			await InvokeOnMainThreadAsync(() =>
			{
				dialogs[1].HideDialog();
				isWindowHidden = dialogs[1].Dialog?.IsShowing is false;
				if (isWindowHidden)
				{
					Assert.True(dialogs[1].IsVisible);

					AssertIsWindow(dialogs[0].DialogWindow, AndroidSystemBars.Activity.GetCurrentWindow());
				}
			});
		});

		Skip.IfNot(isWindowHidden, hideDoesNotClearIsShowing);
	}

	[Fact]
	public async Task GetCurrentWindowSkipsATopDialogWhoseFragmentIsHidden()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await WithDialogsAsync(["bottom", "top"], static async dialogs =>
		{
			await InvokeOnMainThreadAsync(() =>
			{
				dialogs[1].HideFragment();
				Assert.True(dialogs[1].Dialog?.IsShowing);
				Assert.False(dialogs[1].IsVisible);

				AssertIsWindow(dialogs[0].DialogWindow, AndroidSystemBars.Activity.GetCurrentWindow());
			});
		});
	}

	[Fact]
	public async Task GetCurrentWindowReturnsTheActivityWindowWhenTheOnlyDialogWindowIsHidden()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var isWindowHidden = false;

		await WithDialogsAsync([nameof(GetCurrentWindowReturnsTheActivityWindowWhenTheOnlyDialogWindowIsHidden)], async dialogs =>
		{
			await InvokeOnMainThreadAsync(() =>
			{
				dialogs[0].HideDialog();
				isWindowHidden = dialogs[0].Dialog?.IsShowing is false;
				if (isWindowHidden)
				{
					Assert.True(dialogs[0].IsVisible);

					AssertIsActivityWindow(AndroidSystemBars.Activity.GetCurrentWindow());
				}
			});
		});

		Skip.IfNot(isWindowHidden, hideDoesNotClearIsShowing);
	}

	[Fact]
	public async Task GetCurrentWindowReturnsTheActivityWindowWhenTheOnlyDialogFragmentIsHidden()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await WithDialogsAsync([nameof(GetCurrentWindowReturnsTheActivityWindowWhenTheOnlyDialogFragmentIsHidden)], static async dialogs =>
		{
			await InvokeOnMainThreadAsync(() =>
			{
				dialogs[0].HideFragment();
				Assert.True(dialogs[0].Dialog?.IsShowing);
				Assert.False(dialogs[0].IsVisible);

				AssertIsActivityWindow(AndroidSystemBars.Activity.GetCurrentWindow());
			});
		});
	}

	[Fact]
	public async Task GetCurrentWindowReturnsTheActivityWindowForADialogFragmentWithoutDialog()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var embedded = await InvokeOnMainThreadAsync(static () => TestDialogFragment.Embed(nameof(GetCurrentWindowReturnsTheActivityWindowForADialogFragmentWithoutDialog)));

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				Assert.True(embedded.IsAdded);
				Assert.Null(embedded.Dialog); // added to a container, a dialog fragment does not create its dialog

				AssertIsActivityWindow(AndroidSystemBars.Activity.GetCurrentWindow());
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(embedded.Remove);
		}
	}

	[Fact]
	public async Task GetCurrentWindowIgnoresFragmentsThatAreNotDialogFragments()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var plainFragment = await InvokeOnMainThreadAsync(static () => TestPlainFragment.Add(nameof(GetCurrentWindowIgnoresFragmentsThatAreNotDialogFragments)));

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				Assert.True(plainFragment.IsAdded);

				AssertIsActivityWindow(AndroidSystemBars.Activity.GetCurrentWindow());
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(plainFragment.Remove);
		}
	}

	[Fact]
	public async Task GetCurrentWindowStillFindsTheDialogWhenAFragmentThatIsNotADialogIsOnTopOfIt()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await WithDialogsAsync([nameof(GetCurrentWindowStillFindsTheDialogWhenAFragmentThatIsNotADialogIsOnTopOfIt)], static async dialogs =>
		{
			var plainFragment = await InvokeOnMainThreadAsync(static () => TestPlainFragment.Add("plain-on-top"));

			try
			{
				await InvokeOnMainThreadAsync(() => AssertIsWindow(dialogs[0].DialogWindow, AndroidSystemBars.Activity.GetCurrentWindow()));
			}
			finally
			{
				await InvokeOnMainThreadAsync(plainFragment.Remove);
			}
		});
	}

	[Fact]
	public async Task GetCurrentWindowPreparesTheActivityWindowForTheSystemBarColors()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			window.AddFlags(WindowManagerFlags.TranslucentStatus);
			window.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
			Assert.True(AndroidSystemBars.HasFlag(window, WindowManagerFlags.TranslucentStatus));
			Assert.False(AndroidSystemBars.HasFlag(window, WindowManagerFlags.DrawsSystemBarBackgrounds));

			var current = AndroidSystemBars.Activity.GetCurrentWindow();

			AssertIsActivityWindow(current);
			Assert.False(AndroidSystemBars.HasFlag(window, WindowManagerFlags.TranslucentStatus));
			Assert.True(AndroidSystemBars.HasFlag(window, WindowManagerFlags.DrawsSystemBarBackgrounds));
		});
	}

	[Fact]
	public async Task GetCurrentWindowPreparesOnlyTheWindowOfTheVisibleDialog()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await WithDialogsAsync([nameof(GetCurrentWindowPreparesOnlyTheWindowOfTheVisibleDialog)], static async dialogs =>
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var activityWindow = AndroidSystemBars.ActivityWindow;
				var dialogWindow = dialogs[0].DialogWindow;
				foreach (var window in new[] { activityWindow, dialogWindow })
				{
					window.AddFlags(WindowManagerFlags.TranslucentStatus);
					window.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
					Assert.True(AndroidSystemBars.HasFlag(window, WindowManagerFlags.TranslucentStatus));
					Assert.False(AndroidSystemBars.HasFlag(window, WindowManagerFlags.DrawsSystemBarBackgrounds));
				}

				var current = AndroidSystemBars.Activity.GetCurrentWindow();

				AssertIsWindow(dialogWindow, current);
				Assert.False(AndroidSystemBars.HasFlag(dialogWindow, WindowManagerFlags.TranslucentStatus));
				Assert.True(AndroidSystemBars.HasFlag(dialogWindow, WindowManagerFlags.DrawsSystemBarBackgrounds));
				Assert.True(AndroidSystemBars.HasFlag(activityWindow, WindowManagerFlags.TranslucentStatus));
				Assert.False(AndroidSystemBars.HasFlag(activityWindow, WindowManagerFlags.DrawsSystemBarBackgrounds));
			});
		});
	}

	[Fact]
	public async Task GetCurrentWindowOfAnActivityWithoutFragmentManagerReturnsItsWindow()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			// An activity that is not a FragmentActivity has no fragment manager.
			using var unshown = new UnshownWindow();
			unshown.Window.AddFlags(WindowManagerFlags.TranslucentStatus);
			unshown.Window.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
			Assert.Null(unshown.Activity.GetFragmentManager());

			var current = unshown.Activity.GetCurrentWindow();

			AssertIsWindow(unshown.Window, current);
			Assert.False(AndroidSystemBars.HasFlag(unshown.Window, WindowManagerFlags.TranslucentStatus));
			Assert.True(AndroidSystemBars.HasFlag(unshown.Window, WindowManagerFlags.DrawsSystemBarBackgrounds));
		});
	}

	[Fact]
	public async Task GetCurrentWindowOfAnActivityWithoutWindowThrowsInvalidOperationException()
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(static () =>
		{
			// A plain Activity that the system never attached: it has no window and, not being a FragmentActivity, no fragment manager.
			using var activity = new Android.App.Activity();
			Assert.Null(activity.Window);

			return Assert.Throws<InvalidOperationException>(activity.GetCurrentWindow);
		});

		Assert.Equal("Window cannot be null", exception.Message);
	}

	async Task WithDialogsAsync(string[] tags, Func<TestDialogFragment[], Task> action)
	{
		var dialogs = new List<TestDialogFragment>();

		try
		{
			foreach (var tag in tags)
			{
				var dialog = await InvokeOnMainThreadAsync(() => TestDialogFragment.Show(tag));
				dialogs.Add(dialog);
				await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, $"The dialog fragment {tag} did not become visible.");
			}

			await action([.. dialogs]);
		}
		finally
		{
			await InvokeOnMainThreadAsync(() =>
			{
				foreach (var dialog in Enumerable.Reverse(dialogs))
				{
					dialog.Remove();
				}
			});
		}
	}

	// Dialog.Hide() hides the dialog's window on every Android version, but only newer versions (API 36) also report IsShowing = false for it.
	const string hideDoesNotClearIsShowing = "Dialog.Hide() does not clear Dialog.IsShowing on this Android version, so GetCurrentWindow's IsShowing check cannot be reached with a hidden window here (API 36 can).";

	static bool IsSameWindow(AWindow expected, AWindow actual) => JNIEnv.IsSameObject(expected.Handle, actual.Handle);

	static void AssertIsWindow(AWindow expected, AWindow actual) =>
		Assert.True(IsSameWindow(expected, actual), "GetCurrentWindow returned another window than expected.");

	static void AssertIsActivityWindow(AWindow actual) =>
		Assert.True(IsSameWindow(AndroidSystemBars.ActivityWindow, actual), "GetCurrentWindow did not return the activity's window.");
}