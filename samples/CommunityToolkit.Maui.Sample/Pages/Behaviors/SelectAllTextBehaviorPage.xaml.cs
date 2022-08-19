using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class SelectAllTextBehaviorPage : BasePage<SelectAllTextBehaviorViewModel>
{
	public SelectAllTextBehaviorPage(SelectAllTextBehaviorViewModel selectAllTextBehaviorViewModel)
		: base(selectAllTextBehaviorViewModel)
	{
		InitializeComponent();
	}
}