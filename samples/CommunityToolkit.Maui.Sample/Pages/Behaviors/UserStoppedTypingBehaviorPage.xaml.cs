using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class UserStoppedTypingBehaviorPage : BasePage<UserStoppedTypingBehaviorViewModel>
{
	public UserStoppedTypingBehaviorPage(UserStoppedTypingBehaviorViewModel userStoppedTypingBehaviorViewModel)
		: base(userStoppedTypingBehaviorViewModel)
	{
		InitializeComponent();
	}
}