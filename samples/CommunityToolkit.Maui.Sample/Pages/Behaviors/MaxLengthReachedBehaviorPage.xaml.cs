using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

partial class MaxLengthReachedBehaviorPage : BasePage<MaxLengthReachedBehaviorViewModel>
{
	public MaxLengthReachedBehaviorPage(MaxLengthReachedBehaviorViewModel maxLengthReachedBehaviorViewModel)
		: base(maxLengthReachedBehaviorViewModel)
	{
		InitializeComponent();
	}

	void MaxLengthReachedBehavior_MaxLengthReached(object? sender, MaxLengthReachedEventArgs e)
	{
		NextEntry.Focus();
	}
}