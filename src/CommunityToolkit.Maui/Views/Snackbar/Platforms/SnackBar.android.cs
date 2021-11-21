using System;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using Google.Android.Material.Snackbar;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;
using AndroidSnackBar = Google.Android.Material.Snackbar.Snackbar;
using LayoutDirection = Android.Views.LayoutDirection;
using Object = Java.Lang.Object;
using View = Android.Views.View;

namespace CommunityToolkit.Maui.Controls.Snackbar;

public static partial class PlatformPopupExtensions
{
	public static void Dismiss(Snackbar snackbar)
	{
		snackbar.nativeSnackbar?.Dismiss();
	}

	public static AndroidSnackBar Show(Snackbar snackBar)
	{
		var view = (snackBar.Parent.Handler?.NativeView as View) ?? throw new InvalidOperationException("NativeView is null");
		var nativeSnackBar = AndroidSnackBar.Make(view, snackBar.Text, (int)snackBar.Duration.TotalMilliseconds);
		var snackBarView = nativeSnackBar.View;

		if (snackBar.Anchor is not Page)
		{
			nativeSnackBar.SetAnchorView(snackBar.Anchor?.Handler?.NativeView as View);
		}


		/*
		if (snackBar.View.Background is GradientDrawable shape)
		{
			if (arguments.BackgroundColor != Colors.Black)
			{
				shape?.SetColor(arguments.BackgroundColor.ToAndroid().ToArgb());
			}

			var density = view.Context?.Resources?.DisplayMetrics?.Density ?? 1;
			var defaultAndroidCornerRadius = 4 * density;
			arguments.CornerRadius = new Thickness(arguments.CornerRadius.Left * density,
				arguments.CornerRadius.Top * density,
				arguments.CornerRadius.Right * density,
				arguments.CornerRadius.Bottom * density);
			if (arguments.CornerRadius != new Thickness(defaultAndroidCornerRadius, defaultAndroidCornerRadius, defaultAndroidCornerRadius, defaultAndroidCornerRadius))
			{
				shape?.SetCornerRadii(new[]
				{
					(float)arguments.CornerRadius.Left, (float)arguments.CornerRadius.Left,
					(float)arguments.CornerRadius.Top, (float)arguments.CornerRadius.Top,
					(float)arguments.CornerRadius.Right, (float)arguments.CornerRadius.Right,
					(float)arguments.CornerRadius.Bottom, (float)arguments.CornerRadius.Bottom
				});
			}

			snackBarView.SetBackground(shape);
		}*/

		var snackTextView = snackBarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new InvalidOperationException("Unable to find SnackBar text view");
		snackTextView.SetMaxLines(10);

		/*if (arguments.MessageOptions.Padding != MessageOptions.DefaultPadding)
		{
			snackBarView.SetPadding((int)arguments.MessageOptions.Padding.Left,
				(int)arguments.MessageOptions.Padding.Top,
				(int)arguments.MessageOptions.Padding.Right,
				(int)arguments.MessageOptions.Padding.Bottom);
		}

		if (arguments.MessageOptions.Foreground != Colors.White)
		{
			snackTextView.SetTextColor(arguments.MessageOptions.Foreground.ToAndroid());
		}

		if (arguments.MessageOptions.Font != Font.Default)
		{
			if (arguments.MessageOptions.Font.Size > 0)
			{
				snackTextView.SetTextSize(ComplexUnitType.Dip, (float)arguments.MessageOptions.Font.Size);
			}

			snackTextView.SetTypeface(arguments.MessageOptions.Font.ToTypeface(), TypefaceStyle.Normal);
		}*/
			nativeSnackBar.SetAction(snackBar.ActionButtonText, _ =>
			{
				snackBar.Action();
			});
			nativeSnackBar.SetActionTextColor(snackBar.ActionTextColor.ToAndroid());

		nativeSnackBar.AddCallback(new SnackBarCallback(snackBar));
		nativeSnackBar.Show();
		return nativeSnackBar;
	}

	class SnackBarCallback : BaseTransientBottomBar.BaseCallback
	{
		private readonly Snackbar snackbar;

		public SnackBarCallback(Snackbar snackbar)
		{
			this.snackbar = snackbar;
		}

		public override void OnDismissed(Object transientBottomBar, int e)
		{
			base.OnDismissed(transientBottomBar, e);

			if (e == DismissEventAction)
				return;

			snackbar.IsShown = false;
		}
	}
}