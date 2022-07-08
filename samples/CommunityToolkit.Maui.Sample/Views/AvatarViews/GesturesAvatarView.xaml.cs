namespace CommunityToolkit.Maui.Sample;

using System.Diagnostics;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample AvatarView gestures.</summary>
public partial class GesturesAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="GesturesAvatarView"/> class.</summary>
	public GesturesAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}

	void DragGestureRecognizer_DragStarting(object sender, DragStartingEventArgs e) => Debug.WriteLine("AvatarView drag gesture recognizer, drag starting.");

	void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e) => Debug.WriteLine("AvatarView pan gesture recognizer, pan updated.");

	void PinchGestureRecognizer_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e) => Debug.WriteLine("AvatarView pinch gesture recognizer, pinch updated.");

	void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e) => Debug.WriteLine("AvatarView swipe gesture recognizer, swiped.");

	void TapGestureRecognizer_Tapped(object sender, EventArgs e) => Debug.WriteLine("AvatarView Tap Gesture Recognizer, tapped.");
}