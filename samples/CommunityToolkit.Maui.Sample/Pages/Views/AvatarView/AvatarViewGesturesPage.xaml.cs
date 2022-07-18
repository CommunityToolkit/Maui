using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class AvatarViewGesturesPage : BasePage<AvatarViewGesturesViewModel>
{
	public AvatarViewGesturesPage(AvatarViewGesturesViewModel avatarViewGesturesViewModel) : base(avatarViewGesturesViewModel) => InitializeComponent();

	async void DragGestureRecognizer_DragStarting(object sender, DragStartingEventArgs e) => await ShowToastGestureMessage("AvatarView drag gesture recognizer, drag starting.");

	async void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e) => await ShowToastGestureMessage("AvatarView pan gesture recognizer, pan updated.");

	async void PinchGestureRecognizer_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e) => await ShowToastGestureMessage("AvatarView pinch gesture recognizer, pinch updated.");

	async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e) => await ShowToastGestureMessage("AvatarView swipe gesture recognizer, swiped.");

	async void TapGestureRecognizer_Tapped(object sender, EventArgs e) => await ShowToastGestureMessage("AvatarView Tap Gesture Recognizer, tapped.");

	static async Task ShowToastGestureMessage(string message)
	{
		Core.IToast toast = Toast.Make(message, Core.ToastDuration.Short);
		await toast.Show();
	}
}