using System;
using System.Diagnostics;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class BrokenTitleViewShell : Shell
{
	// The same instance, not a new AppShell: its navigation stack and its pages, with their view models, stay alive while this Shell is shown,
	// so the user returns to the page as they left it
	readonly Page previousPage;

	bool hasNavigatedToTabbar;

#if ANDROID
	Action? removeOnBackPressedCallback;
#endif

	public BrokenTitleViewShell(Page previousPage)
	{
		this.previousPage = previousPage;

		InitializeComponent();
	}

	public void ReturnToPreviousPage()
	{
		Window?.Page = previousPage;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

#if ANDROID
		AddOnBackPressedCallback();
#endif

		if (hasNavigatedToTabbar)
		{
			return;
		}

		hasNavigatedToTabbar = true;

		try
		{
			await GoToAsync("//Tabbar");
		}
		catch (Exception exception)
		{
			hasNavigatedToTabbar = false;
			Trace.WriteLine(exception);
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

#if ANDROID
		removeOnBackPressedCallback?.Invoke();
		removeOnBackPressedCallback = null;
#endif
	}

#if ANDROID
	// .NET MAUI handles the back key only while the window can go back (a modal page, a pushed page or an open flyout), which this Shell never can:
	// on Android 13 and later (android:enableOnBackInvokedCallback) the back key would close the app without calling OnBackButtonPressed
	void AddOnBackPressedCallback()
	{
		if (Window?.Handler?.PlatformView is not AndroidX.Activity.ComponentActivity activity)
		{
			return;
		}

		var onBackPressedCallback = new ReturnToPreviousPageOnBackPressedCallback(this);
		activity.OnBackPressedDispatcher.AddCallback(onBackPressedCallback);
		removeOnBackPressedCallback = onBackPressedCallback.Remove;
	}

	sealed class ReturnToPreviousPageOnBackPressedCallback(BrokenTitleViewShell brokenTitleViewShell) : AndroidX.Activity.OnBackPressedCallback(true)
	{
		public override void HandleOnBackPressed() => brokenTitleViewShell.ReturnToPreviousPage();
	}
#endif
}