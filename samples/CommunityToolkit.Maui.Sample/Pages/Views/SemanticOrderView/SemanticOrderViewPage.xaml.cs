using System.Collections.Generic;

using CommunityToolkit.Maui.Sample.ViewModels.Views;
namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class SemanticOrderViewPage : BasePage<SemanticOrderViewPageViewModel>
{
	public SemanticOrderViewPage(SemanticOrderViewPageViewModel vm) : base(vm)
	{
		InitializeComponent();
		acv.ViewOrder = new List<View> { first, second, third, fourth, fifth };
	}
}