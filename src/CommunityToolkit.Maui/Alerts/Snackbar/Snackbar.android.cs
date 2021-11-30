using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
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
using AndroidSnackbar = Google.Android.Material.Snackbar.Snackbar;
using Object = Java.Lang.Object;
using View = Android.Views.View;

namespace CommunityToolkit.Maui.Alerts.Snackbar;

public partial class Snackbar
{
	static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

	AndroidSnackbar? _nativeSnackbar;
	TaskCompletionSource<bool>? _dismissedTCS;

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	public async Task Dismiss()
	{
		if (_nativeSnackbar is null)
		{
			_dismissedTCS = null;
			return;
		}

		await _semaphoreSlim.WaitAsync();

		try
		{
			_nativeSnackbar.Dismiss();
			if (_dismissedTCS is not null)
				await _dismissedTCS.Task;

			OnDismissed();
		}
		finally
		{
			_semaphoreSlim.Release();
		}
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	public async Task Show()
	{
		await Dismiss();

		var rootView = Microsoft.Maui.Essentials.Platform.GetCurrentActivity(true).Window?.DecorView.FindViewById(Android.Resource.Id.Content);
		if (rootView is null)
			throw new NotSupportedException("Unable to retrieve snackbar parent");

		_nativeSnackbar = AndroidSnackbar.Make(rootView, Text, (int)Duration.TotalMilliseconds);
		var snackbarView = _nativeSnackbar.View;

		if (Anchor is not Page)
		{
			_nativeSnackbar.SetAnchorView(Anchor?.Handler?.NativeView as View);
		}

		SetupContainer(VisualOptions, snackbarView);
		SetupMessage(VisualOptions, snackbarView);
		SetupActions(_nativeSnackbar);

		_nativeSnackbar.Show();

		OnShown();
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

	[MemberNotNull(nameof(_dismissedTCS))]
	void SetupActions(AndroidSnackbar nativeSnackbar)
	{
		var snackActionButtonView = nativeSnackbar.View.FindViewById<TextView>(Resource.Id.snackbar_action) ?? throw new InvalidOperationException("Unable to find Snackbar action button");
		snackActionButtonView.SetTypeface(VisualOptions.ActionButtonFont.ToTypeface(), TypefaceStyle.Normal);

		nativeSnackbar.SetAction(ActionButtonText, _ =>
		{
			Action?.Invoke();
		});
		nativeSnackbar.SetActionTextColor(VisualOptions.ActionButtonTextColor.ToAndroid());

		nativeSnackbar.AddCallback(new SnackbarCallback(_dismissedTCS = new()));
	}

	class SnackbarCallback : BaseTransientBottomBar.BaseCallback
	{
		readonly TaskCompletionSource<bool> _dismissedTCS;

		public SnackbarCallback(in TaskCompletionSource<bool> dismissedTCS)
		{
			_dismissedTCS = dismissedTCS;
		}

		public override void OnDismissed(Object transientBottomBar, int e)
		{
			base.OnDismissed(transientBottomBar, e);
			_dismissedTCS.SetResult(true);
		}
	}
}