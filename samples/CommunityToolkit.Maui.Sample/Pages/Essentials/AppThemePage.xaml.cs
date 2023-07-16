using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class AppThemePage : BasePage<AppThemeViewModel>
{
	public AppThemePage(AppThemeViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	void Switch_Toggled(object sender, ToggledEventArgs e)
	{
		if (Application.Current is not null)
		{
			Application.Current.UserAppTheme = Application.Current.RequestedTheme is AppTheme.Dark
												? AppTheme.Light
												: AppTheme.Dark;
		}
	}
}