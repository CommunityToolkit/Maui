using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class FolderPickerPage : BasePage<FolderPickerViewModel>
{
	public FolderPickerPage(FolderPickerViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}