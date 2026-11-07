using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class BehaviorsGalleryPage : BaseGalleryPage<BehaviorsGalleryViewModel>
{
#if ANDROID || IOS
	readonly CommunityToolkit.Maui.Behaviors.StatusBarBehavior statusBarBehavior = new()
	{
		StatusBarStyle = Core.StatusBarStyle.LightContent,
		ApplyOn = CommunityToolkit.Maui.Behaviors.StatusBarApplyOn.OnPageNavigatedTo,
	};
#endif

	public BehaviorsGalleryPage(IDeviceInfo deviceInfo, BehaviorsGalleryViewModel behaviorsGalleryViewModel)
		: base("Behaviors", deviceInfo, behaviorsGalleryViewModel)
	{
#if ANDROID || IOS
		AddStatusBarBehavior();
		SafeAreaEdges = SafeAreaEdges.Default;
#endif
	}

#if ANDROID || IOS
	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		// The app theme may have changed while a page with a StatusBarBehavior of its own was shown (see HandleRequestedThemeChanged)
		SetStatusBarColor(Application.Current?.RequestedTheme ?? AppTheme.Unspecified);
	}

	// An AppThemeBinding does not follow theme changes on a Behavior, which is not an Element, so the page updates the color when the theme changes:
	// otherwise a band of the other theme's color stays above the navigation bar, also on the pages that are opened after this one.
	void AddStatusBarBehavior()
	{
		var application = Application.Current ?? throw new InvalidOperationException($"{nameof(Application)}.{nameof(Application.Current)} cannot be null");

		SetStatusBarColor(application.RequestedTheme);
		application.RequestedThemeChanged += HandleRequestedThemeChanged;

		Behaviors.Add(statusBarBehavior);
	}

	// The Shell style in Styles.xaml gives the navigation bar NavBarColor in the Light theme and Gray950 in the Dark theme
	void SetStatusBarColor(AppTheme appTheme)
	{
		var resources = Application.Current?.Resources ?? throw new InvalidOperationException($"{nameof(Application)}.{nameof(Application.Current)} cannot be null");
		statusBarBehavior.StatusBarColor = (Color)resources[appTheme is AppTheme.Dark ? "Gray950" : "NavBarColor"];
	}

	void HandleRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
	{
		// StatusBarBehavior applies a new color at once. A page with a StatusBarBehavior of its own, such as StatusBarBehaviorPage, keeps the color it
		// gave the status bar: this page gets the color of the new theme when it is shown again
		if (Shell.Current?.CurrentPage is Page currentPage
			&& currentPage != this
			&& currentPage.Behaviors.Any(static behavior => behavior is CommunityToolkit.Maui.Behaviors.StatusBarBehavior))
		{
			return;
		}

		SetStatusBarColor(e.RequestedTheme);
	}
#endif
}