using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class SemanticOrderViewPage : BasePage<SemanticOrderViewViewModel>
{
	public SemanticOrderViewPage(SemanticOrderViewViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
		acv.ViewOrder = new List<View> { first, second, third, fourth, fifth };
	}
}