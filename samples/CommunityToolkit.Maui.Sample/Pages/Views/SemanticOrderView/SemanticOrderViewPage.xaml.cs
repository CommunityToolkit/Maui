using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class SemanticOrderViewPage : BasePage<SemanticOrderViewPageViewModel>
{
	public SemanticOrderViewPage(SemanticOrderViewPageViewModel semanticOrderViewPageViewModel) : base(semanticOrderViewPageViewModel)
	{
		InitializeComponent();
		SemanticOrderView.ViewOrder = new List<View> { First, Second, Third, Fourth, Fifth };
	}
}