using System.Diagnostics.CodeAnalysis;
using Android.Graphics;
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

	static Google.Android.Material.Snackbar.Snackbar? PlatformSnackbar { get; set; }

	/// <summary>
	/// Dispose Snackbar
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (isDisposing)
		{
			PlatformSnackbar?.Dispose();
		}

		isDisposed = true;
	}

	static bool IsModalPageActive() => Application.Current?.MainPage is not null &&
										Application.Current.MainPage.Navigation.ModalStack.Count > 0;

	static View GetParentView()
	{
		var parentView = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.DecorView.FindViewById(Android.Resource.Id.Content);

		if (IsModalPageActive())
		{
			parentView = parentView?.RootView;
		}

		return parentView ?? throw new NotSupportedException("Unable to retrieve Snackbar parent");
	}

	static void SetLayoutParametersForView(in View snackbarView)
	{
		if (IsModalPageActive() && snackbarView.Context?.Resources is not null)
		{
			var resourceId = snackbarView.Context.Resources.GetIdentifier(
				"navigation_bar_height",
				"dimen",
				"android"
			);
			var navBarHeight = snackbarView.Context.Resources.GetDimensionPixelSize(resourceId);
			var layoutParameters = (FrameLayout.LayoutParams?)snackbarView.LayoutParameters;
			if (layoutParameters is not null)
			{
				layoutParameters.SetMargins(layoutParameters.LeftMargin, layoutParameters.TopMargin, layoutParameters.RightMargin, layoutParameters.BottomMargin + navBarHeight);
				snackbarView.LayoutParameters = layoutParameters;
			}
		}
	}

	async Task DismissPlatform(CancellationToken token)
	{
		if (PlatformSnackbar is null)
		{
			dismissedTCS = null;
			return;
		}

		token.ThrowIfCancellationRequested();

		if (!PlatformSnackbar.IsDisposed())
		{
			PlatformSnackbar.Dismiss();
		}

		if (dismissedTCS is not null)
		{
			await dismissedTCS.Task;
		}
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	async Task ShowPlatform(CancellationToken token)
	{
		await DismissPlatform(token);
		token.ThrowIfCancellationRequested();

		PlatformSnackbar = Google.Android.Material.Snackbar.Snackbar.Make(GetParentView(), Text, (int)Duration.TotalMilliseconds);
		var snackbarView = PlatformSnackbar.View;

		if (Anchor is not Page)
		{
			PlatformSnackbar.SetAnchorView(Anchor?.Handler?.PlatformView as View);
		}

		var fontManager = Application.Current?.RequireFontManager() ?? throw new InvalidOperationException($"{nameof(IFontManager)} Required");
		SetLayoutParametersForView(snackbarView);
		SetContainerForView(snackbarView);
		SetMessageForView(snackbarView, fontManager);
		SetActionForSnackbar(PlatformSnackbar, fontManager);

		PlatformSnackbar.Show();
	}

	void SetContainerForView(in View snackbarView)
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

	void SetMessageForView(in View snackbarView, IFontManager fontManager)
	{
		var snackTextView = snackbarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new InvalidOperationException("Unable to find Snackbar text view");
		snackTextView.SetMaxLines(10);

		snackTextView.SetTextColor(VisualOptions.TextColor.ToAndroid());
		if (VisualOptions.Font.Size > 0)
		{
			snackTextView.SetTextSize(ComplexUnitType.Dip, (float)VisualOptions.Font.Size);
		}

		snackTextView.SetTypeface(fontManager.GetTypeface(VisualOptions.Font), TypefaceStyle.Normal);

		snackTextView.LetterSpacing = (float)VisualOptions.CharacterSpacing;
	}

	[MemberNotNull(nameof(dismissedTCS))]
	void SetActionForSnackbar(in Google.Android.Material.Snackbar.Snackbar platformSnackbar, IFontManager fontManager)
	{
		var snackActionButtonView = platformSnackbar.View.FindViewById<TextView>(Resource.Id.snackbar_action) ?? throw new InvalidOperationException("Unable to find Snackbar action button");

		platformSnackbar.SetActionTextColor(VisualOptions.ActionButtonTextColor.ToAndroid());
		if (VisualOptions.ActionButtonFont.Size > 0)
		{
			snackActionButtonView.SetTextSize(ComplexUnitType.Dip, (float)VisualOptions.ActionButtonFont.Size);
		}

		snackActionButtonView.SetTypeface(fontManager.GetTypeface(VisualOptions.ActionButtonFont), TypefaceStyle.Normal);

		platformSnackbar.SetAction(ActionButtonText, _ => Action?.Invoke());

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

		public override void OnShown(Object? transientBottomBar)
		{
			base.OnShown(transientBottomBar);
			snackbar.OnShown();
		}

		public override void OnDismissed(Object? transientBottomBar, int e)
		{
			base.OnDismissed(transientBottomBar, e);

			dismissedTCS.TrySetResult(true);
			snackbar.OnDismissed();
		}
	}
}