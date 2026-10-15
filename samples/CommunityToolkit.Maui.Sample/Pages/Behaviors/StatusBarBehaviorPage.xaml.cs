using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class StatusBarBehaviorPage : BasePage<StatusBarBehaviorViewModel>
{
	const string tryStatusBarOnModalPageButtonText = "Try StatusBarBehavior on Modal Page";

	const int maximumRestoreAttempts = 20;
	const double scrollRestoreTolerance = 1;

	static readonly TimeSpan restoreAttemptInterval = TimeSpan.FromMilliseconds(50);

	double? scrollYToRestore;

	public StatusBarBehaviorPage(StatusBarBehaviorViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();

		ModalPageButton.Text = tryStatusBarOnModalPageButtonText;
		ModalPageButton.Clicked += HandleModalButtonClicked;

		Loaded += HandleLoaded;
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		// StatusBar supports Android and iOS only: on Mac Catalyst, where OperatingSystem.IsIOS() is also true, and on Windows, StatusBar.SetColor and
		// StatusBar.SetStyle throw NotSupportedException
		if (OperatingSystem.IsAndroid() || (OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()))
		{
			Core.Platform.StatusBar.SetColor(BindingContext.StatusBarColor);
			Core.Platform.StatusBar.SetStyle(BindingContext.StatusBarStyle);
		}
	}

	static async void HandleModalButtonClicked(object? sender, EventArgs e)
	{
		var statusBarBehaviorModelPage = new StatusBarBehaviorPage(new StatusBarBehaviorViewModel());

		// Get Modal Page Button
		if (!TryGetModalPageButton((Microsoft.Maui.ILayout)((ScrollView)statusBarBehaviorModelPage.Content).Content, out var modalPageButton))
		{
			throw new InvalidOperationException("Unable To Find Modal Page Button");
		}

		// Change Text on Modal Page Button
		modalPageButton.Text = "Pop Modal Page";

		// Modify Behavior of Modal Page Button
		modalPageButton.Clicked -= HandleModalButtonClicked;
		modalPageButton.Clicked += async (_, _) => await Shell.Current.Navigation.PopModalAsync();

		// The implicit Page style makes every Page Transparent on Windows, so that the Mica backdrop of the window shows through it. On Windows, .NET MAUI keeps
		// the Page below a modal Page that has a BackgroundColor on screen, where it shows through the Transparent modal Page: without one, the modal Page replaces it
		if (OperatingSystem.IsWindows())
		{
			statusBarBehaviorModelPage.SetValue(BackgroundColorProperty, null);
		}

		await Shell.Current.Navigation.PushModalAsync(statusBarBehaviorModelPage);
	}

	static bool TryGetModalPageButton(Microsoft.Maui.ILayout layout, [NotNullWhen(true)] out Button? button)
	{
		button = null;

		foreach (var view in layout)
		{
			switch (view)
			{
				case Button { Text: tryStatusBarOnModalPageButtonText } modalPageButton:
					button = modalPageButton;
					return true;

				case Microsoft.Maui.ILayout nestedLayout:
					if (TryGetModalPageButton(nestedLayout, out var modalPageButtonInLayout))
					{
						button = modalPageButtonInLayout;
						return true;
					}
					break;
			}
		}

		return false;
	}

	// On Android, setting the app's Shell back gives this page a new ScrollView, which starts at the top; iOS and Windows keep theirs
	async void HandleLoaded(object? sender, EventArgs e)
	{
		if (scrollYToRestore is not double scrollY)
		{
			return;
		}

		scrollYToRestore = null;

		// On Android, Loaded fires when the new ScrollView is attached but before it is laid out, so the scroll waits for the dispatcher
		await Dispatcher.DispatchAsync(async () =>
		{
			var scrollView = (ScrollView)Content;

			// On Android, ScrollY keeps the old ScrollView's offset until the new one scrolls, so a scroll that the new one cannot make yet would look
			// done: start ScrollY from the new ScrollView's top
			if (OperatingSystem.IsAndroid())
			{
				((Microsoft.Maui.IScrollView)scrollView).VerticalOffset = 0;
			}

			for (var attempt = 0; attempt < maximumRestoreAttempts; attempt++)
			{
				await scrollView.ScrollToAsync(0, scrollY, false);

				// On Android, the new ScrollView can scroll less far than the old one until the window insets give it its bottom padding, after its first layout, and at
				// the default font size not at all: a scroll beyond its end stops there, so scroll again once it can
				if (!OperatingSystem.IsAndroid() || scrollView.ScrollY >= scrollY - scrollRestoreTolerance)
				{
					return;
				}

				await Task.Delay(restoreAttemptInterval);
			}
		});
	}

	void OnEdgeToEdgeShellButtonClicked(object? sender, EventArgs e)
	{
		var window = Application.Current?.Windows[0];
		if (window is null)
		{
			return;
		}

		// The Edge-to-Edge Shell.TitleView demo returns to this page the same way main's removed TitleViewShell navigation did (a window.Page swap),
		// so record the scroll position here, as main's removed OnTitleViewShellButtonClicked did, to keep the return-as-left feature working
		scrollYToRestore = ((ScrollView)Content).ScrollY;

		EdgeToEdgeTitleViewPage.PreviousPage = window.Page;
		window.Page = new EdgeToEdgeTitleViewShell();
	}
}