using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;

public sealed partial class OnScreenSizeExtensionViewModel : BaseViewModel
{
	 readonly ILauncher launcher;

	public OnScreenSizeExtensionViewModel(ILauncher launcher)
	{
		this.launcher = launcher;
	}

	[RelayCommand]
	async Task OpenDocumentation()
	{
		var url = "https://docs.microsoft.com/dotnet/communitytoolkit/maui/extensions/on-screen-size-extension";
		await launcher.OpenAsync(new Uri(url));
	}
}