using CommunityToolkit.Maui.Sample.ViewModels.PlatformSpecific;

namespace CommunityToolkit.Maui.Sample.Pages.PlatformSpecific;

public partial class NavigationBarPage : BasePage<NavigationBarAndroidViewModel>
{
	public NavigationBarPage(NavigationBarAndroidViewModel vm) : base(vm)
	{
		InitializeComponent();
	}
}