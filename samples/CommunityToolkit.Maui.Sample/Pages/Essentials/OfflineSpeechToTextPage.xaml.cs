using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class OfflineSpeechToTextPage : BasePage<OfflineSpeechToTextViewModel>
{
	public OfflineSpeechToTextPage(OfflineSpeechToTextViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}