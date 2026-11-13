using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RangeSliderPage : BasePage<RangeSliderViewModel>
{
	public RangeSliderPage(RangeSliderViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}