using System.Diagnostics.CodeAnalysis;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using Google.Android.Material.Snackbar;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using Object = Java.Lang.Object;
using View = Android.Views.View;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	TaskCompletionSource<bool>? dismissedTCS;

	private async partial Task DismissPlatform(CancellationToken token)
	{
		if (PlatformSnackbar is null)
		{
			dismissedTCS = null;
			return;
		}
		token.ThrowIfCancellationRequested();

		PlatformSnackbar.Dismiss();

		if (dismissedTCS is not null)
		{
			await dismissedTCS.Task;
		}
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	private async partial Task ShowPlatform(CancellationToken token)
	{
		await DismissPlatform(token);
		token.ThrowIfCancellationRequested();

		var rootView = Microsoft.Maui.Essentials.Platform.CurrentActivity.Window?.DecorView.FindViewById(Android.Resource.Id.Content)?.RootView
			?? throw new NotSupportedException("Unable to retrieve snackbar parent");

		PlatformSnackbar = Google.Android.Material.Snackbar.Snackbar.Make(rootView, Text, (int)Duration.TotalMilliseconds);
		var snackbarView = PlatformSnackbar.View;

		if (Anchor is not Page)
		{
			PlatformSnackbar.SetAnchorView(Anchor?.Handler?.PlatformView as View);
		}

		SetupContainer(snackbarView);
		SetupMessage(snackbarView);
		SetupActions(PlatformSnackbar);

		PlatformSnackbar.Show();
	}

	void SetupContainer(View snackbarView)
	{
		if (snackbarView.Background is GradientDrawable shape)
		{
			shape.SetColor(VisualOptions.BackgroundColor.ToAndroid().ToArgb());

			var density = snackbarView.Context?.Resources?.DisplayMetrics?.Density ?? 1;
			var cornerRadius = new Thickness(
				VisualOptions.CornerRadius.BottomLeft * density,
				VisualOptions.CornerRadius.TopLeft * density,
				VisualOptions.CornerRadius.TopRight * density,
				VisualOptions.CornerRadius.BottomRight * density);
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

	void SetupMessage(View snackbarView)
	{
		var snackTextView = snackbarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new InvalidOperationException("Unable to find Snackbar text view");
		snackTextView.SetMaxLines(10);

		snackTextView.SetTextColor(VisualOptions.TextColor.ToAndroid());
		if (VisualOptions.Font.Size > 0)
		{
			snackTextView.SetTextSize(ComplexUnitType.Dip, (float)VisualOptions.Font.Size);
		}

		snackTextView.LetterSpacing = (float)VisualOptions.CharacterSpacing;
	}

	[MemberNotNull(nameof(dismissedTCS))]
	void SetupActions(Google.Android.Material.Snackbar.Snackbar platformSnackbar)
	{
		var snackActionButtonView = platformSnackbar.View.FindViewById<TextView>(Resource.Id.snackbar_action) ?? throw new InvalidOperationException("Unable to find Snackbar action button");

		platformSnackbar.SetActionTextColor(VisualOptions.ActionButtonTextColor.ToAndroid());
		if (VisualOptions.ActionButtonFont.Size > 0)
		{
			snackActionButtonView.SetTextSize(ComplexUnitType.Dip, (float)VisualOptions.ActionButtonFont.Size);
		}

		platformSnackbar.SetAction(ActionButtonText, _ =>
		{
			Action?.Invoke();
		});

		platformSnackbar.AddCallback(new SnackbarCallback(this, dismissedTCS = new()));
	}

	class SnackbarCallback : BaseTransientBottomBar.BaseCallback
	{
		readonly Snackbar snackbar;
		readonly TaskCompletionSource<bool> dismissedTCS;

		public SnackbarCallback(in Snackbar snackbar, in TaskCompletionSource<bool> dismissedTCS)
		{
			this.snackbar = snackbar;
			this.dismissedTCS = dismissedTCS;
		}

		public override void OnShown(Object transientBottomBar)
		{
			base.OnShown(transientBottomBar);
			snackbar.OnShown();
		}

		public override void OnDismissed(Object transientBottomBar, int e)
		{
			base.OnDismissed(transientBottomBar, e);

			dismissedTCS.TrySetResult(true);
			snackbar.OnDismissed();
		}
	}
}