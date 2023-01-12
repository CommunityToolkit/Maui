using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class FileSaverPage : BasePage<FileSaverViewModel>
{
	public FileSaverPage(FileSaverViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}