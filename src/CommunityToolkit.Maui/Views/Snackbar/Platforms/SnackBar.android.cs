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

		if (snackBarView.Background is GradientDrawable shape)
		{
			shape.SetColor(snackBar.BackgroundColor.ToAndroid().ToArgb());

			var density = view.Context?.Resources?.DisplayMetrics?.Density ?? 1;
			var defaultAndroidCornerRadius = 4 * density;
			var cornerRadius = new Thickness(
				snackBar.CornerRadius.BottomLeft * density,
				snackBar.CornerRadius.TopLeft * density,
				snackBar.CornerRadius.TopRight * density,
				snackBar.CornerRadius.BottomRight * density);
			shape.SetCornerRadii(new[]
				{
					(float)cornerRadius.Left, (float)cornerRadius.Left,
					(float)cornerRadius.Top, (float)cornerRadius.Top,
					(float)cornerRadius.Right, (float)cornerRadius.Right,
					(float)cornerRadius.Bottom, (float)cornerRadius.Bottom
				});

			snackBarView.SetBackground(shape);
		}

		SetupMessage(snackBar, snackBarView);
		SetupActions(snackBar, nativeSnackBar);
		nativeSnackBar.Show();
		return nativeSnackBar;
	}

	static void SetupMessage(Snackbar snackBar, View snackBarView)
	{
		var snackTextView = snackBarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new InvalidOperationException("Unable to find SnackBar text view");
		snackTextView.SetMaxLines(10);
		snackBarView.SetPadding((int)snackBar.Padding.Left,
			(int)snackBar.Padding.Top,
			(int)snackBar.Padding.Right,
			(int)snackBar.Padding.Bottom);

		snackTextView.SetTextColor(snackBar.TextColor.ToAndroid());
		if (snackBar.Font.Size > 0)
		{
			snackTextView.SetTextSize(ComplexUnitType.Dip, (float)snackBar.Font.Size);
		}

		snackTextView.LetterSpacing = (float)snackBar.CharacterSpacing;

		snackTextView.SetTypeface(snackBar.Font.ToTypeface(), TypefaceStyle.Normal);
	}

	static void SetupActions(Snackbar snackBar, AndroidSnackBar nativeSnackBar)
	{
		nativeSnackBar.SetAction(snackBar.ActionButtonText, _ =>
		{
			snackBar.Action();
		});
		nativeSnackBar.SetActionTextColor(snackBar.ActionTextColor.ToAndroid());

		nativeSnackBar.AddCallback(new SnackBarCallback(snackBar));
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
			snackbar.IsShown = false;
		}
	}
}