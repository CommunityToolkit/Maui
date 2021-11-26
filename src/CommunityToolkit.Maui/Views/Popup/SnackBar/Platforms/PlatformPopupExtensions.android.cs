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
using AndroidSnackbar = Google.Android.Material.Snackbar.Snackbar;
using Object = Java.Lang.Object;
using View = Android.Views.View;

namespace CommunityToolkit.Maui.Views.Popup.Snackbar.Platforms;

class PlatformPopupExtensions : IPlatformPopupExtensions
{
	public void Dismiss(Snackbar snackbar)
	{
		snackbar.NativeSnackbar?.Dismiss();
	}

	public AndroidSnackbar Show(Snackbar snackbar)
	{
		var rootView = Microsoft.Maui.Essentials.Platform.GetCurrentActivity(true).Window?.DecorView.FindViewById(Android.Resource.Id.Content);
		if (rootView is null)
		{
			throw new NotSupportedException("Unable to retrieve snackbar parent");
		}

		var nativeSnackbar = AndroidSnackbar.Make(
			rootView,
			snackbar.Text,
			(int)snackbar.Duration.TotalMilliseconds);
		var snackbarView = nativeSnackbar.View;

		if (snackbar.Anchor is not Page)
		{
			nativeSnackbar.SetAnchorView(snackbar.Anchor?.Handler?.NativeView as View);
		}

		SetupContainer(snackbar.VisualOptions, snackbarView);
		SetupMessage(snackbar.VisualOptions, snackbarView);
		SetupActions(snackbar, nativeSnackbar);
		nativeSnackbar.Show();
		return nativeSnackbar;
	}

	static void SetupContainer(SnackbarOptions snackbarOptions, View snackbarView)
	{
		if (snackbarView.Background is GradientDrawable shape)
		{
			shape.SetColor(snackbarOptions.BackgroundColor.ToAndroid().ToArgb());

			var density = snackbarView.Context?.Resources?.DisplayMetrics?.Density ?? 1;
			var cornerRadius = new Thickness(
				snackbarOptions.CornerRadius.BottomLeft * density,
				snackbarOptions.CornerRadius.TopLeft * density,
				snackbarOptions.CornerRadius.TopRight * density,
				snackbarOptions.CornerRadius.BottomRight * density);
			shape.SetCornerRadii(new[]
				{
					(float)cornerRadius.Left, (float)cornerRadius.Left,
					(float)cornerRadius.Top, (float)cornerRadius.Top,
					(float)cornerRadius.Right, (float)cornerRadius.Right,
					(float)cornerRadius.Bottom, (float)cornerRadius.Bottom
				});

			snackbarView.SetBackground(shape);
		}
	}

	static void SetupMessage(SnackbarOptions snackbarOptions, View snackbarView)
	{
		var snackTextView = snackbarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new InvalidOperationException("Unable to find Snackbar text view");
		snackTextView.SetMaxLines(10);

		snackTextView.SetTextColor(snackbarOptions.TextColor.ToAndroid());
		if (snackbarOptions.Font.Size > 0)
		{
			snackTextView.SetTextSize(ComplexUnitType.Dip, (float)snackbarOptions.Font.Size);
		}

		snackTextView.LetterSpacing = (float)snackbarOptions.CharacterSpacing;

		snackTextView.SetTypeface(snackbarOptions.Font.ToTypeface(), TypefaceStyle.Normal);
	}

	static void SetupActions(Snackbar snackbar, AndroidSnackbar nativeSnackbar)
	{
		var snackActionButtonView = nativeSnackbar.View.FindViewById<TextView>(Resource.Id.snackbar_action) ?? throw new InvalidOperationException("Unable to find Snackbar action button");
		snackActionButtonView.SetTypeface(snackbar.VisualOptions.ActionButtonFont.ToTypeface(), TypefaceStyle.Normal);

		nativeSnackbar.SetAction(snackbar.ActionButtonText, _ =>
		{
			snackbar.Action();
		});
		nativeSnackbar.SetActionTextColor(snackbar.VisualOptions.ActionButtonTextColor.ToAndroid());

		nativeSnackbar.AddCallback(new SnackbarCallback(snackbar));
	}

	class SnackbarCallback : BaseTransientBottomBar.BaseCallback
	{
		readonly Snackbar snackbar;

		public SnackbarCallback(Snackbar snackbar)
		{
			this.snackbar = snackbar;
		}

		public override void OnDismissed(Object transientBottomBar, int e)
		{
			base.OnDismissed(transientBottomBar, e);
			snackbar.OnDismissed();
		}
	}
}