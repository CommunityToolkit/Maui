using CommunityToolkit.Mvvm.Input;
using ThreadNetwork;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;

public sealed partial class OnScreenSizeExtensionViewModel : BaseViewModel
{
	public OnScreenSizeExtensionViewModel():base()
	{
	}

	[RelayCommand]
	async Task OpenDocumentation()
	{
		var url = "https://github.com/MicrosoftDocs/CommunityToolkit/tree/main/docs/maui/extensions/on-screen-size-extension.md";
		await Launcher.OpenAsync(new System.Uri(url));
	}
}