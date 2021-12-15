using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// 
/// </summary>
public class SelectAllTextAP
{

	static int count;
	/// <summary>
	/// 
	/// </summary>
	public static BindableProperty ShouldSelectAllTextProperty = BindableProperty.CreateAttached("ShouldSelectAllText", typeof(bool), typeof(InputView), default, propertyChanged: OnSelectAllTextAPPropetyChanged);

	async static void OnSelectAllTextAPPropetyChanged(BindableObject bindable, object oldValue, object newValue)
	{

		await Task.Delay(2500);
		if (newValue == oldValue || bindable is not InputView view)
			return;
#if WINDOWS
		var nativeView = view.ToNative(view.Handler.MauiContext!) as MauiTextBox;
		if (nativeView is null)
			return;

		nativeView.GotFocus -= NativeView_GotFocus;

		if ((bool)newValue)
			nativeView.GotFocus += NativeView_GotFocus;


		static void NativeView_GotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			((Microsoft.UI.Xaml.Controls.TextBox)sender).SelectAll();
			Debug.WriteLine("#########");
			Debug.WriteLine("#########");
			Debug.WriteLine($"tô aqui caraí {count}!!!!");
			Debug.WriteLine("#########");
			Debug.WriteLine("#########");
			Debug.WriteLine("#########");
		}

#elif ANDROID
		var nativeView = view.ToNative(view.Handler.MauiContext!) as Android.Widget.EditText;

		if (nativeView is null)
			return;

		nativeView.SetSelectAllOnFocus((bool)newValue);
#endif
	}


	/// <summary>
	/// Gets the value
	/// </summary>
	/// <param name="b"></param>
	/// <returns></returns>
	public static bool GetShouldSelectAllText(BindableObject b) => (bool)b.GetValue(ShouldSelectAllTextProperty);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="b"></param>
	/// <param name="value"></param>
	public static void SetShouldSelectAllText(BindableObject b, bool value) => b.SetValue(ShouldSelectAllTextProperty, value);
}
