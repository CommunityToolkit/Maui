using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class AvatarViewGesturesPage : BasePage<AvatarViewGesturesViewModel>
{
	public AvatarViewGesturesPage(AvatarViewGesturesViewModel avatarViewGesturesViewModel) : base(avatarViewGesturesViewModel) => InitializeComponent();

	async void DragGestureRecognizer_DragStarting(object sender, DragStartingEventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await ShowToastGestureMessage("AvatarView drag gesture recognizer, drag starting.", cts.Token);
	}

	async void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await ShowToastGestureMessage("AvatarView pan gesture recognizer, pan updated.", cts.Token);
	}

	async void PinchGestureRecognizer_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await ShowToastGestureMessage("AvatarView pinch gesture recognizer, pinch updated.", cts.Token);
	}

	async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await ShowToastGestureMessage("AvatarView swipe gesture recognizer, swiped.", cts.Token);
	}

	async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await ShowToastGestureMessage("AvatarView Tap Gesture Recognizer, tapped.", cts.Token);
	}

	static Task ShowToastGestureMessage(string message, CancellationToken token)
	{
		Core.IToast toast = Toast.Make(message, Core.ToastDuration.Short);
		return toast.Show(token);
	}
}