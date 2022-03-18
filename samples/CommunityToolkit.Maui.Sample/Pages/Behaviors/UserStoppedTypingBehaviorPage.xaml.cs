using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class UserStoppedTypingBehaviorPage : BasePage<UserStoppedTypingBehaviorViewModel>
{
	public UserStoppedTypingBehaviorPage(IDeviceInfo deviceInfo, UserStoppedTypingBehaviorViewModel userStoppedTypingBehaviorViewModel)
		: base(deviceInfo, userStoppedTypingBehaviorViewModel)
	{
		InitializeComponent();

		TimeThresholdSetting ??= new();
		AutoDismissKeyboardSetting ??= new();
		MinimumLengthThresholdSetting ??= new();
	}
}