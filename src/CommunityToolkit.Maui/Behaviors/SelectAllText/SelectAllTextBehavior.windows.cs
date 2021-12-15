using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.Behaviors;
public partial class SelectAllTextBehavior
{
	TextBox? nativeView;

	partial void OnPlatformkAttachedBehavior(InputView view)
	{
		nativeView = view.ToNative(view.Handler.MauiContext!) as TextBox;

		nativeView!.GotFocus += OnGotFocus;
	}

	void OnGotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		nativeView?.SelectAll();
	}

	partial void OnPlatformDeattachedBehavior(InputView view)
	{
		if (view is null || nativeView is null)
			return;

		nativeView.GotFocus -= OnGotFocus;
	}
}
