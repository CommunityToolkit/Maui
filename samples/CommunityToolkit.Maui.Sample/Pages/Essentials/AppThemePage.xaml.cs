using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class AppThemePage : BasePage<AppThemeViewModel>
{
	public AppThemePage(AppThemeViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}