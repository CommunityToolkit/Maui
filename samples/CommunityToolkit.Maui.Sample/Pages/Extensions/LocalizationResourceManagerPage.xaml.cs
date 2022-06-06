using CommunityToolkit.Maui.Sample.ViewModels.Extensions;

namespace CommunityToolkit.Maui.Sample.Pages.Extensions;

public partial class LocalizationResourceManagerPage : BasePage<LocalizationResourceManagerViewModel>
{
	public LocalizationResourceManagerPage(LocalizationResourceManagerViewModel localizationResourceManagerViewModel)
		: base(localizationResourceManagerViewModel)
	{
		InitializeComponent();
	}
}