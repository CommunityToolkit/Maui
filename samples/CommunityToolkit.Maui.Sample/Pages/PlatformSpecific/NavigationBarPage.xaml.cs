using CommunityToolkit.Maui.Sample.ViewModels.PlatformSpecific;
#if ANDROID
using System.ComponentModel;
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
#endif

namespace CommunityToolkit.Maui.Sample.Pages.PlatformSpecific;

public partial class NavigationBarPage : BasePage<NavigationBarAndroidViewModel>
{
	public NavigationBarPage(NavigationBarAndroidViewModel vm) : base(vm)
	{
		InitializeComponent();
	}

#if ANDROID
	// Once the page clears NavigationBar.Color and NavigationBar.Style, the window gets back the navigation bar it had before the page changed it.
	// The page sets the values instead of binding them: .NET MAUI keeps the value that a binding set after the binding is removed, so a bound value
	// cannot be cleared.
	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		BindingContext.PropertyChanged += HandleViewModelPropertyChanged;
		UpdateNavigationBar();

		// NavigationBar applies a value to the window when the value changes, and Shell raises NavigatedTo once the page is loaded. The Default style
		// is also the value of a page that has none (or cleared it), so setting it changes nothing: apply it to the window now
		Handler?.UpdateValue(NavigationBar.StyleProperty.PropertyName);
	}

	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		base.OnNavigatedFrom(args);

		BindingContext.PropertyChanged -= HandleViewModelPropertyChanged;

		ClearValue(NavigationBar.ColorProperty);
		ClearValue(NavigationBar.StyleProperty);
	}

	void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(NavigationBarAndroidViewModel.NavigationBarColor) or nameof(NavigationBarAndroidViewModel.NavigationBarStyle))
		{
			UpdateNavigationBar();
		}
	}

	void UpdateNavigationBar()
	{
		NavigationBar.SetColor(this, BindingContext.NavigationBarColor);
		NavigationBar.SetStyle(this, BindingContext.NavigationBarStyle);
	}
#endif
}