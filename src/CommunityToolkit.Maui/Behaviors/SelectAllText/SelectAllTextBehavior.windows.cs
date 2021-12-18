using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.Behaviors;
public partial class SelectAllTextBehavior : BasePlatformBehavior<InputView, TextBox>
{
	/// <inheritdoc />

	protected override void OnPlatformAttachedBehavior(InputView view)
	{
		NativeView!.GotFocus += OnGotFocus;
	}
	/// <inheritdoc />

	protected override void OnPlatformDeattachedBehavior(InputView view)
	{
		NativeView!.GotFocus -= OnGotFocus;
	}

	//partial void OnPlatformkAttachedBehavior(InputView view)
	//{
	//	nativeView = view.ToNative(view.Handler.MauiContext!) as TextBox;

	//	nativeView!.GotFocus += OnGotFocus;
	//}


	//partial void OnPlatformDeattachedBehavior(InputView view)
	//{
	//	if (view is null || nativeView is null)
	//		return;

	//	nativeView.GotFocus -= OnGotFocus;
	//}
	void OnGotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		NativeView?.SelectAll();
	}
}
