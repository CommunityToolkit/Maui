using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Android.Widget;

namespace CommunityToolkit.Maui.Behaviors;
public partial class SelectAllTextBehavior
{
	EditText? nativeControl;
	partial void OnPlatformkAttachedBehavior(InputView view)
	{
		nativeControl = view.ToNative(view.Handler.MauiContext!) as EditText;

		nativeControl?.SetSelectAllOnFocus(true);
	}
	partial void OnPlatformDeattachedBehavior(InputView view)
	{
		nativeControl?.SetSelectAllOnFocus(false);
	}
}
