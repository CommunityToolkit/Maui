using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Maui.Sample.ViewModels.Layouts;

namespace CommunityToolkit.Maui.Sample.Pages.Layouts;

public partial class StateContainerPage : BasePage<StateContainerViewModel>
{
	public StateContainerPage(StateContainerViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}

	async void ChangeStateWithFadeAnimation(object? sender, EventArgs e) 
	{
		await StateContainer.ChangeStateWithAnimation(GridWithAnimation, "ReplaceGrid", CancellationToken.None);
	}

	async void ChangeStateWithCustomAnimation(object? sender, EventArgs e)
	{
		await StateContainer.ChangeStateWithAnimation(GridWithAnimation, "ReplaceGrid", async (l, c)=> await new FadeAnimation(){Length = 1000, Opacity = 0}.Animate(l).WaitAsync(c), async (l, c) => await new FadeAnimation() { Length = 1000, Opacity = 1 }.Animate(l).WaitAsync(c), CancellationToken.None);
	}
}