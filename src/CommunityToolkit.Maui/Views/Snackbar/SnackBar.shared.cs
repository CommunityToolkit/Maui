using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;


namespace CommunityToolkit.Maui.Controls.Snackbar;

public class Snackbar : BasePopupView, IText
{
#if NET6_0_ANDROID
	internal Google.Android.Material.Snackbar.Snackbar? nativeSnackbar;
#endif

	public Snackbar()
	{
		this.Parent = Application.Current?.MainPage;
		BackgroundColor = Colors.LightGray;
	}

	public static readonly BindableProperty ActionProperty = BindableProperty.Create(nameof(Action), typeof(Action), typeof(Snackbar), () => { }, BindingMode.TwoWay);
	public static readonly BindableProperty ActionTextColorProperty = BindableProperty.Create(nameof(ActionTextColor), typeof(Color), typeof(Snackbar), Colors.Black, BindingMode.TwoWay);
	public static readonly BindableProperty ActionButtonTextProperty = BindableProperty.Create(nameof(ActionButtonText), typeof(string), typeof(Snackbar), "OK", BindingMode.TwoWay);
	public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(TimeSpan), typeof(Snackbar), TimeSpan.FromMilliseconds(3000), BindingMode.TwoWay);
	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Snackbar), string.Empty, BindingMode.TwoWay);
	public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(Snackbar), Colors.Black, BindingMode.TwoWay);
	public static readonly BindableProperty FontProperty = BindableProperty.Create(nameof(Font), typeof(Font), typeof(Snackbar), Font.SystemFontOfSize(14), BindingMode.TwoWay);
	public static readonly BindableProperty CharacterSpacingProperty = BindableProperty.Create(nameof(CharacterSpacing), typeof(double), typeof(Snackbar), default, BindingMode.TwoWay);


	public double CharacterSpacing
	{
		get { return (double)GetValue(CharacterSpacingProperty); }
		set { SetValue(CharacterSpacingProperty, value); }
	}

	public Font Font
	{
		get { return (Font)GetValue(FontProperty); }
		set { SetValue(FontProperty, value); }
	}

	public string Text
	{
		get { return (string)GetValue(TextProperty); }
		set { SetValue(TextProperty, value); }
	}

	public Color TextColor
	{
		get { return (Color)GetValue(TextColorProperty); }
		set { SetValue(TextColorProperty, value); }
	}

	public TimeSpan Duration
	{
		get { return (TimeSpan)GetValue(DurationProperty); }
		set { SetValue(DurationProperty, value); }
	}

	public Action Action
	{
		get { return (Action)GetValue(ActionProperty); }
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

	public static Snackbar Make(string text, TimeSpan? duration, Action action, IView? anchor = null)
	{
		var snackbar = new Snackbar
		{
			Text = text,
			Anchor = anchor,
			Action = action
		};
		if (duration is not null)
		{
			snackbar.Duration = duration.Value;
		}

		return snackbar;
	}

	public override Task Show()
	{
		if (Parent is null)
		{
			throw new ArgumentNullException(nameof(Parent));
		}

#if NET6_0_ANDROID
		nativeSnackbar = PlatformPopupExtensions.Show(this);
		IsShown = true;		
#else
		throw new PlatformNotSupportedException();
#endif
		return Task.CompletedTask;
	}

	internal override Task DismissInternal()
	{
#if NET6_0_ANDROID
		PlatformPopupExtensions.Dismiss(this);
		IsShown = false;
#else
		throw new PlatformNotSupportedException();
#endif

		return Task.CompletedTask;
	}
}