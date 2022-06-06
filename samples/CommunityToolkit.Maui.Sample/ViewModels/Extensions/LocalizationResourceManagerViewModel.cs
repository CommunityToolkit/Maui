using System.Globalization;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Resources.Localization;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;

public partial class LocalizationResourceManagerViewModel : BaseViewModel
{
	public LocalizationResourceManagerViewModel()
	{
		LocalizationResourceManager.Current.Init(AppResources.ResourceManager);
	}

	public LocalizationResourceManager LocalizationResourceManager => LocalizationResourceManager.Current;

	[ICommand]
	void SetLanguage()
	{
		var currentCulture = LocalizationResourceManager.CurrentCulture.TwoLetterISOLanguageName == "en"
			? new CultureInfo("uk-UA")
			: new CultureInfo("en-US");
		LocalizationResourceManager.Current.CurrentCulture = currentCulture;
	}
}