using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Controls.Snackbar;

public class Toast : BasePopupView
{
	public Toast()
	{
		this.Parent = Application.Current?.MainPage;
	}

	public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Toast), string.Empty, BindingMode.TwoWay);
	public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(TimeSpan), typeof(Toast), TimeSpan.FromMilliseconds(3000), BindingMode.TwoWay);
	public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(Toast), Colors.Black, BindingMode.TwoWay);

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

	public static Toast Make(string text, TimeSpan duration)
	{
		return new Toast();
	}

	public void SetText(string text) => Text = text;

	public void SetTextColor(Color textColor) => TextColor = textColor;

	public void SetDuration(TimeSpan duration) => Duration = duration;

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
