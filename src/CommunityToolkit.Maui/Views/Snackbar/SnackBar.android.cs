#if ANDROID
using System;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Widget;
using CommunityToolkit.Maui.UI.Views.Options;
using Android.Util;
using Android.Graphics.Drawables;
using AndroidSnackBar = Google.Android.Material.Snackbar.Snackbar;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.UI.Views
{
	class SnackBar
	{
		internal async ValueTask Show(VisualElement sender, SnackBarOptions arguments)
		{
			var view = (sender.Handler.NativeView as Android.Views.View) ?? throw new InvalidOperationException("NativeView is null");
			var snackBar = AndroidSnackBar.Make(view, arguments.MessageOptions.Message, (int)arguments.Duration.TotalMilliseconds);
			var snackBarView = snackBar.View;

			if (sender is not Page)
			{
				snackBar.SetAnchorView(view);
			}

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
			}

			var snackTextView = snackBarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new NullReferenceException();
			snackTextView.SetMaxLines(10);

			if (arguments.MessageOptions.Padding != MessageOptions.DefaultPadding)
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
				if (arguments.MessageOptions.Font.FontSize > 0)
				{
					snackTextView.SetTextSize(ComplexUnitType.Dip, (float)arguments.MessageOptions.Font.FontSize);
				}

				snackTextView.SetTypeface(arguments.MessageOptions.Font.ToTypeface(), TypefaceStyle.Normal);
			}

			snackTextView.LayoutDirection = arguments.IsRtl
				? global::Android.Views.LayoutDirection.Rtl
				: global::Android.Views.LayoutDirection.Inherit;

			foreach (var action in arguments.Actions)
			{
				snackBar.SetAction(action.Text, async v =>
				{
					if (action.Action != null)
						await action.Action();
				});
				if (action.ForegroundColor != Colors.White)
				{
					snackBar.SetActionTextColor(action.ForegroundColor.ToAndroid());
				}

				var snackActionButtonView = snackBarView.FindViewById<TextView>(Resource.Id.snackbar_action) ?? throw new NullReferenceException();
				if (arguments.BackgroundColor != Colors.Black)
				{
					snackActionButtonView.SetBackgroundColor(action.BackgroundColor.ToAndroid());
				}

				if (action.Padding != SnackBarActionOptions.DefaultPadding)
				{
					snackActionButtonView.SetPadding((int)action.Padding.Left,
						(int)action.Padding.Top,
						(int)action.Padding.Right,
						(int)action.Padding.Bottom);
				}

				if (action.Font != Font.Default)
				{
					if (action.Font.FontSize > 0)
					{
						snackTextView.SetTextSize(ComplexUnitType.Dip, (float)action.Font.FontSize);
					}

					snackActionButtonView.SetTypeface(action.Font.ToTypeface(), TypefaceStyle.Normal);
				}

				snackActionButtonView.LayoutDirection = arguments.IsRtl
					? global::Android.Views.LayoutDirection.Rtl
					: global::Android.Views.LayoutDirection.Inherit;
			}

			snackBar.AddCallback(new SnackBarCallback(arguments));
			snackBar.Show();
		}

		class SnackBarCallback : AndroidSnackBar.BaseCallback
		{
			readonly SnackBarOptions arguments;

			public SnackBarCallback(SnackBarOptions arguments) => this.arguments = arguments;

			public override void OnDismissed(Java.Lang.Object transientBottomBar, int e)
			{
				base.OnDismissed(transientBottomBar, e);

				switch (e)
				{
					case DismissEventTimeout:
						arguments.SetResult(false);
						break;
					case DismissEventAction:
						arguments.SetResult(true);
						break;
				}
			}
		}
	}
}
#endif