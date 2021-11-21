using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Controls.Snackbar;

public class Snackbar : Toast
{
	public Snackbar():base()
	{

	}
	public static readonly BindableProperty ActionProperty = BindableProperty.Create(nameof(Action), typeof(string), typeof(Snackbar), string.Empty, BindingMode.TwoWay);
	public static readonly BindableProperty ActionTextColorProperty = BindableProperty.Create(nameof(ActionTextColor), typeof(Color), typeof(Snackbar), Colors.Black, BindingMode.TwoWay);
	public static readonly BindableProperty ActionButtonTextProperty = BindableProperty.Create(nameof(ActionButtonText), typeof(string), typeof(Snackbar), "OK", BindingMode.TwoWay);

	public void SetAction(string actionButtonText, Action<IView> action) => (ActionButtonText, Action) = (actionButtonText, action);


	public void SetActionTextColor(Color actionTextColor) => ActionTextColor = actionTextColor;
	public Action<IView>? Action
	{
		get { return (Action<IView>?)GetValue(ActionProperty); }
		set { SetValue(ActionProperty, value); }
	}

	public Color ActionTextColor
	{
		get { return (Color)GetValue(ActionTextColorProperty); }
		set { SetValue(ActionTextColorProperty, value); }
	}

	public string ActionButtonText
	{
		get { return (string)GetValue(ActionButtonTextProperty); }
		set { SetValue(ActionButtonTextProperty, value); }
	}

	public static Snackbar Make(string text, TimeSpan duration, IView? anchor = null)
	{
		return new Snackbar();
	}

	public override Task Show()
	{
		if (Parent is null)
		{
			throw new ArgumentNullException(nameof(Parent));
		}

#if NET6_0_ANDROID || NET6_0_IOS || NET6_0_MACCATALYST
		PlatformPopupExtensions.Show(this);
#else
		throw new PlatformNotSupportedException();
#endif
		return Task.CompletedTask;
	}

	public override Task Dismiss()
	{
		throw new NotImplementedException();
	}
}