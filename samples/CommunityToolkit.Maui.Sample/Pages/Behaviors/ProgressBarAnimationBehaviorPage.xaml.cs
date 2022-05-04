using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class ProgressBarAnimationBehaviorPage : BasePage<ProgressBarAnimationBehaviorViewModel>
{
	public ProgressBarAnimationBehaviorPage(ProgressBarAnimationBehaviorViewModel progressBarAnimationBehaviorViewModel)
		: base(progressBarAnimationBehaviorViewModel)
	{
		InitializeComponent();

		// Work-around for Error XFC0009 when assigning Easing in XAML
		// No property, BindableProperty, or event found for "Easing", or mismatching type between value and property. (XFC0009)
		ProgressBarAnimationBehavior.Easing = Easing.CubicOut;
	}
}