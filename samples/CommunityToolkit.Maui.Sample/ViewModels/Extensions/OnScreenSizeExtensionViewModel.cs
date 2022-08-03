using System.Windows.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Extensions;

public sealed partial class OnScreenSizeExtensionViewModel : BaseViewModel
{
	 readonly ILauncher launcher;

	public OnScreenSizeExtensionViewModel(ILauncher launcher)
	{
		this.launcher = launcher;
		OpenDocumentationCommand = new Command(async () => await OpenDocumentation());
	}

	public ICommand OpenDocumentationCommand { get; }
	
	async Task OpenDocumentation()
	{
		var url = "https://docs.microsoft.com/dotnet/communitytoolkit/maui/extensions/on-screen-size-extension";
		await launcher.OpenAsync(new Uri(url));
	}
}