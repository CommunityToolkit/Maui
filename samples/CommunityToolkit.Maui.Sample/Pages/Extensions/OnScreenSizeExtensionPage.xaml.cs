using CommunityToolkit.Maui.Helpers;
using CommunityToolkit.Maui.Sample.ViewModels.Extensions;

namespace CommunityToolkit.Maui.Sample.Pages.Extensions;

public partial class OnScreenSizeExtensionPage: BasePage<OnScreenSizeExtensionViewModel>
{
	public OnScreenSizeExtensionPage(OnScreenSizeExtensionViewModel viewModel)
	: base(viewModel)
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		mappings.ItemsSource = OnScreenSizeManager.Current.Mappings;
        
		base.OnAppearing();
	}
}
