using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Views;

public abstract class BasePopupView : BoxView, IPadding
{
	public static readonly BindableProperty IsShownProperty = BindableProperty.Create(nameof(IsShown), typeof(bool), typeof(BasePopupView), false, BindingMode.OneWay, propertyChanged: OnIsShownChanged);
	public static readonly BindableProperty AnchorProperty = BindableProperty.Create(nameof(Anchor), typeof(IView), typeof(BasePopupView), null, BindingMode.TwoWay);
	public static readonly BindableProperty PaddingProperty = BindableProperty.Create(nameof(Padding), typeof(Thickness), typeof(BasePopupView), new Thickness(0,0,0,0), BindingMode.TwoWay);

	public bool IsShown
	{
		get { return (bool)GetValue(IsShownProperty); }
		set { SetValue(IsShownProperty, value); }
	}

	public IView? Anchor
	{
		get { return (IView?)GetValue(AnchorProperty); }
		set { SetValue(AnchorProperty, value); }
	}

	public Thickness Padding
	{
		get { return (Thickness)GetValue(PaddingProperty); }
		set { SetValue(PaddingProperty, value); }
	}

	public event EventHandler<ShownEventArgs>? Shown;
	public event EventHandler? Dismissed;

	public abstract Task Show();
	public async Task Dismiss()
	{
		await DismissInternal();
		Dismissed?.Invoke(this, new EventArgs());
	}
	internal abstract Task DismissInternal();

	private static void OnIsShownChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var popup = (BasePopupView)bindable;
		popup.Shown?.Invoke(popup, new ShownEventArgs(popup.IsShown));
	}
}

public class ShownEventArgs : EventArgs
{
	public ShownEventArgs(bool isShown)
	{
		IsShown = isShown;
	}

	public bool IsShown { get; }
}