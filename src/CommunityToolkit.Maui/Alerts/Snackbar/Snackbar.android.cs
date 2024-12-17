using System.Diagnostics.CodeAnalysis;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using Google.Android.Material.Snackbar;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
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

	static bool TryGetPageActiveModalPage([NotNullWhen(true)] out View? modalPage)
	{
		if (Application.Current?.Windows[0].Page is Page mainPage && mainPage.Navigation.ModalStack.Count > 0)
		{
			modalPage = mainPage.Navigation.ModalStack.Last().ToPlatform();
			return true;
		}

		modalPage = null;
		return false;
	}

	static View GetParentView()
	{
		if (TryGetPageActiveModalPage(out var modalPage))
		{
			return modalPage;
		}

		return Platform.CurrentActivity?.Window?.DecorView.FindViewById(Android.Resource.Id.Content) ?? throw new NotSupportedException("Unable to retrieve Snackbar parent");
	}

	static void SetLayoutParametersForView(in View snackbarView)
	{
		if (!TryGetPageActiveModalPage(out _) || snackbarView.Context?.Resources is null)
		{
			return;
		}

		var resourceId = snackbarView.Context.Resources.GetIdentifier(
			"navigation_bar_height",
			"dimen",
			"android"
		);
		var navBarHeight = snackbarView.Context.Resources.GetDimensionPixelSize(resourceId);
		if (snackbarView.LayoutParameters is FrameLayout.LayoutParams layoutParameters)
		{
			layoutParameters.SetMargins(layoutParameters.LeftMargin, layoutParameters.TopMargin, layoutParameters.RightMargin, layoutParameters.BottomMargin + navBarHeight);
			snackbarView.LayoutParameters = layoutParameters;
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
			await dismissedTCS.Task.WaitAsync(token);
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
		if (snackbarView.Background is not GradientDrawable shape)
		{
			return;
		}

		shape.SetColor(VisualOptions.BackgroundColor.ToPlatform().ToArgb());

		var density = snackbarView.Context?.Resources?.DisplayMetrics?.Density ?? 1;
		var cornerRadius = new Thickness(
			VisualOptions.CornerRadius.BottomLeft * density,
			VisualOptions.CornerRadius.TopLeft * density,
			VisualOptions.CornerRadius.TopRight * density,
			VisualOptions.CornerRadius.BottomRight * density);

		shape.SetCornerRadii(
		[
			(float)cornerRadius.Left,
			(float)cornerRadius.Left,
			(float)cornerRadius.Top,
			(float)cornerRadius.Top,
			(float)cornerRadius.Right,
			(float)cornerRadius.Right,
			(float)cornerRadius.Bottom,
			(float)cornerRadius.Bottom
		]);

		snackbarView.SetBackground(shape);
	}

	void SetMessageForView(in View snackbarView, IFontManager fontManager)
	{
		var snackTextView = snackbarView.FindViewById<TextView>(Resource.Id.snackbar_text) ?? throw new InvalidOperationException("Unable to find Snackbar text view");
		snackTextView.SetMaxLines(10);

		snackTextView.SetTextColor(VisualOptions.TextColor.ToPlatform());
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

		platformSnackbar.SetActionTextColor(VisualOptions.ActionButtonTextColor.ToPlatform());
		if (VisualOptions.ActionButtonFont.Size > 0)
		{
			snackActionButtonView.SetTextSize(ComplexUnitType.Dip, (float)VisualOptions.ActionButtonFont.Size);
		}

		snackActionButtonView.SetTypeface(fontManager.GetTypeface(VisualOptions.ActionButtonFont), TypefaceStyle.Normal);

		platformSnackbar.SetAction(ActionButtonText, _ => Action?.Invoke());

		platformSnackbar.AddCallback(new SnackbarCallback(this, dismissedTCS = new()));
	}

	sealed class SnackbarCallback(in Snackbar snackbar, in TaskCompletionSource<bool> dismissedTcs) : BaseTransientBottomBar.BaseCallback
	{
		readonly Snackbar snackbar = snackbar;
		readonly TaskCompletionSource<bool> dismissedTCS = dismissedTcs;

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