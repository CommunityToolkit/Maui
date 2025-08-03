using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class BarcodeScanningPage : BasePage<BarcodeScanningViewModel>
{
	public BarcodeScanningPage(BarcodeScanningViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}