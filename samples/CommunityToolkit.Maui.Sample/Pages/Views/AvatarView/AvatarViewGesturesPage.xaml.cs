using System.Diagnostics;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class AvatarViewGesturesPage : BasePage<AvatarViewGesturesViewModel>
{
	public AvatarViewGesturesPage(AvatarViewGesturesViewModel avatarViewGesturesViewModel) : base(avatarViewGesturesViewModel)
	{
		InitializeComponent();
	}

	void DragGestureRecognizer_DragStarting(object sender, DragStartingEventArgs e) => Debug.WriteLine("AvatarView drag gesture recognizer, drag starting.");

	void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e) => Debug.WriteLine("AvatarView pan gesture recognizer, pan updated.");

	void PinchGestureRecognizer_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e) => Debug.WriteLine("AvatarView pinch gesture recognizer, pinch updated.");

	void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e) => Debug.WriteLine("AvatarView swipe gesture recognizer, swiped.");

	void TapGestureRecognizer_Tapped(object sender, EventArgs e) => Debug.WriteLine("AvatarView Tap Gesture Recognizer, tapped.");
}