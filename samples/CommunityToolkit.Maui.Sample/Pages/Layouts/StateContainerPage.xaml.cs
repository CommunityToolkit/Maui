using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;

namespace CommunityToolkit.Maui.Sample.Pages.Layouts;

public partial class StateContainerPage : BasePage<StateContainerViewModel>
{
	public StateContainerPage(StateContainerViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
		Padding = new Thickness(12, 0);
	}

	async void ChangeStateWithFadeAnimation(object? sender, EventArgs e)
	{
		await StateContainer.ChangeStateWithAnimation(GridWithAnimation, "ReplaceGrid", CancellationToken.None);
	}

	async void ChangeStateWithCustomAnimation(object? sender, EventArgs e)
	{
		await StateContainer.ChangeStateWithAnimation(GridWithAnimation,
														"ReplaceGrid",
														(element, token) => element.ScaleTo(0, 100, Easing.SpringIn).WaitAsync(token),
														(element, token) => element.ScaleTo(1, 250, Easing.SpringOut).WaitAsync(token),
														CancellationToken.None);
	}
}