using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Views;

public abstract class BasePopupView : VisualElement
{
	public static readonly BindableProperty IsShownProperty = BindableProperty.Create(nameof(IsShown), typeof(bool), typeof(BasePopupView), false, BindingMode.OneWay);
	public static readonly BindableProperty AnchorProperty = BindableProperty.Create(nameof(Anchor), typeof(IView), typeof(BasePopupView), null, BindingMode.TwoWay);
	
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

	public event EventHandler? Shown;
	public event EventHandler? Dismissed;

	public abstract Task Show();
	public abstract Task Dismiss();
}

