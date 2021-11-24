using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using Google.Android.Material.Snackbar;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using AndroidSnackBar = Google.Android.Material.Snackbar.Snackbar;
using Object = Java.Lang.Object;
using View = Android.Views.View;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms;

static partial class PlatformPopupExtensions
{
	public static void Dismiss(AndroidSnackBar? snackbar)
	{
		snackbar?.Dismiss();
	}

	public static AndroidSnackBar Show(ISnackbar snackBar)
	{
		var rootView = Microsoft.Maui.Essentials.Platform.GetCurrentActivity(true).Window?.DecorView.FindViewById(Android.Resource.Id.Content);
		if (rootView is null)
		{
			throw new NotSupportedException("Unable to retrieve snackbar parent");
		}

		var nativeSnackBar = AndroidSnackBar.Make(
			rootView,
			snackBar.Text,
			(int)snackBar.Duration.TotalMilliseconds);
		var snackBarView = nativeSnackBar.View;

		if (snackBar.Anchor is not Page)
		{
			nativeSnackBar.SetAnchorView(snackBar.Anchor?.Handler?.NativeView as View);
		}

		SetupContainer(snackBar.VisualOptions, snackBarView);
		SetupMessage(snackBar.VisualOptions, snackBarView);
		SetupActions(snackBar, nativeSnackBar);
		nativeSnackBar.Show();
		return nativeSnackBar;
	}

	static void SetupContainer(SnackbarOptions snackBarOptions, View snackBarView)
	{
		if (snackBarView.Background is GradientDrawable shape)
		{
			shape.SetColor(snackBarOptions.BackgroundColor.ToAndroid().ToArgb());

			var density = snackBarView.Context?.Resources?.DisplayMetrics?.Density ?? 1;
			var cornerRadius = new Thickness(
				snackBarOptions.CornerRadius.BottomLeft * density,
				snackBarOptions.CornerRadius.TopLeft * density,
				snackBarOptions.CornerRadius.TopRight * density,
				snackBarOptions.CornerRadius.BottomRight * density);
			shape.SetCornerRadii(new[]
				{
					(float)cornerRadius.Left, (float)cornerRadius.Left,
					(float)cornerRadius.Top, (float)cornerRadius.Top,
					(float)cornerRadius.Right, (float)cornerRadius.Right,
					(float)cornerRadius.Bottom, (float)cornerRadius.Bottom
				});

			snackBarView.SetBackground(shape);
		}
	}

	static void SetupMessage(SnackbarOptions snackBarOptions, View snackBarView)
	{
		var snackTextView = snackBarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new InvalidOperationException("Unable to find SnackBar text view");
		snackTextView.SetMaxLines(10);

		snackTextView.SetTextColor(snackBarOptions.TextColor.ToAndroid());
		if (snackBarOptions.Font.Size > 0)
		{
			snackTextView.SetTextSize(ComplexUnitType.Dip, (float)snackBarOptions.Font.Size);
		}

		snackTextView.LetterSpacing = (float)snackBarOptions.CharacterSpacing;

		snackTextView.SetTypeface(snackBarOptions.Font.ToTypeface(), TypefaceStyle.Normal);
	}

	static void SetupActions(ISnackbar snackBar, AndroidSnackBar nativeSnackBar)
	{
		var snackActionButtonView = nativeSnackBar.View.FindViewById<TextView>(Resource.Id.snackbar_action) ?? throw new InvalidOperationException("Unable to find SnackBar action button");
		snackActionButtonView.SetTypeface(snackBar.VisualOptions.ActionButtonFont.ToTypeface(), TypefaceStyle.Normal);

		nativeSnackBar.SetAction(snackBar.ActionButtonText, _ =>
		{
			snackBar.Action();
		});
		nativeSnackBar.SetActionTextColor(snackBar.VisualOptions.ActionButtonTextColor.ToAndroid());

		nativeSnackBar.AddCallback(new SnackBarCallback(snackBar));
	}

	class SnackBarCallback : BaseTransientBottomBar.BaseCallback
	{
		readonly ISnackbar snackbar;

		public SnackBarCallback(ISnackbar snackbar)
		{
			this.snackbar = snackbar;
		}

		public override void OnDismissed(Object transientBottomBar, int e)
		{
			base.OnDismissed(transientBottomBar, e);
		}
	}
}