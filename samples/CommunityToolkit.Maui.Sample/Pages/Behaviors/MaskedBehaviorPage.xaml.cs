using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class MaskedBehaviorPage : BasePage<MaskedBehaviorViewModel>
{
	public MaskedBehaviorPage(MaskedBehaviorViewModel maskedBehaviorViewModel)
		: base(maskedBehaviorViewModel)
	{
		InitializeComponent();
	}
}