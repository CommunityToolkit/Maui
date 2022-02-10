using System.Diagnostics.CodeAnalysis;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using CommunityToolkit.Maui.Core;
using Google.Android.Material.Snackbar;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using AndroidSnackbar = Google.Android.Material.Snackbar.Snackbar;
using Object = Java.Lang.Object;
using View = Android.Views.View;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

	static AndroidSnackbar? nativeSnackbar;
	TaskCompletionSource<bool>? dismissedTCS;

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	public virtual async partial Task Dismiss(CancellationToken token)
	{
		if (nativeSnackbar is null)
		{
			dismissedTCS = null;
			return;
		}

		await semaphoreSlim.WaitAsync(token);

		try
		{
			token.ThrowIfCancellationRequested();

			nativeSnackbar.Dismiss();

			if (dismissedTCS is not null)
			{
				await dismissedTCS.Task;
			}
		}
		finally
		{
			semaphoreSlim.Release();
		}
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);
		token.ThrowIfCancellationRequested();

		var rootView = Microsoft.Maui.Essentials.Platform.GetCurrentActivity(true).Window?.DecorView.FindViewById(Android.Resource.Id.Content)
			?? throw new NotSupportedException("Unable to retrieve snackbar parent");

		nativeSnackbar = AndroidSnackbar.Make(rootView, Text, (int)Duration.TotalMilliseconds);
		var snackbarView = nativeSnackbar.View;

		if (Anchor is not Page)
		{
			nativeSnackbar.SetAnchorView(Anchor?.Handler?.NativeView as View);
		}

		SetupContainer(VisualOptions, snackbarView);
		SetupMessage(VisualOptions, snackbarView);
		SetupActions(nativeSnackbar);

		nativeSnackbar.Show();
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

	[MemberNotNull(nameof(dismissedTCS))]
	void SetupActions(AndroidSnackbar nativeSnackbar)
	{
		var snackActionButtonView = nativeSnackbar.View.FindViewById<TextView>(Resource.Id.snackbar_action) ?? throw new InvalidOperationException("Unable to find Snackbar action button");
		snackActionButtonView.SetTypeface(VisualOptions.ActionButtonFont.ToTypeface(), TypefaceStyle.Normal);

		nativeSnackbar.SetAction(ActionButtonText, _ =>
		{
			Action?.Invoke();
		});
		nativeSnackbar.SetActionTextColor(VisualOptions.ActionButtonTextColor.ToAndroid());

		nativeSnackbar.AddCallback(new SnackbarCallback(this, dismissedTCS = new()));
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