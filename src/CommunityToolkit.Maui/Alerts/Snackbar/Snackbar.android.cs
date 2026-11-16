using System.Diagnostics.CodeAnalysis;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using CommunityToolkit.Maui.Extensions;
using Google.Android.Material.Shape;
using Google.Android.Material.Snackbar;
using Microsoft.Extensions.DependencyInjection;
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
		if (Application.Current?.Windows is [{ Page: Page mainPage }, ..]
			&& mainPage.Navigation.ModalStack.Count > 0
			&& mainPage.Navigation.ModalStack.Last().Handler?.PlatformView is View platformModalPage)
		{
			modalPage = platformModalPage;
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
		token.ThrowIfCancellationRequested();

		if (PlatformSnackbar is null)
		{
			dismissedTCS = null;
			return;
		}

		if (!PlatformSnackbar.IsPeerDisposed())
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

		PlatformSnackbar = Google.Android.Material.Snackbar.Snackbar.Make(GetParentView(), Text, GetMaterialDuration(Duration));
		var snackbarView = PlatformSnackbar.View;

		if (Anchor is not Page)
		{
			PlatformSnackbar.SetAnchorView(Anchor?.Handler?.PlatformView as View);

			// Material places an anchored snackbar above its anchor when it shows it, and places it again after a layout pass only when its
			// anchor layout listener is enabled. Without the listener the snackbar keeps the bottom margin computed when it was shown: after a
			// rotation to landscape that margin can be taller than the parent, and the snackbar is laid out above the screen with no height
			PlatformSnackbar.AnchorViewLayoutListenerEnabled = true;
		}

		var fontManager = Application.Current?.Handler?.MauiContext?.Services.GetRequiredService<IFontManager>() ?? throw new InvalidOperationException($"{nameof(IFontManager)} Required");
		SetLayoutParametersForView(snackbarView);
		SetContainerForView(snackbarView);
		SetMessageForView(snackbarView, fontManager);
		SetActionForSnackbar(PlatformSnackbar, fontManager);

		PlatformSnackbar.Show();
	}

	// Material reads 0, -1 and -2 as its LENGTH_LONG, LENGTH_SHORT and LENGTH_INDEFINITE constants and only a positive duration as a
	// number of milliseconds: a Duration below 1 ms is shown for 1 ms, so that the snackbar goes away at once, as on the other platforms
	static int GetMaterialDuration(TimeSpan duration) => (int)Math.Max(duration.TotalMilliseconds, 1);

	void SetContainerForView(in View snackbarView)
	{
		var density = snackbarView.Context?.Resources?.DisplayMetrics?.Density ?? 1;
		var topLeft = (float)(VisualOptions.CornerRadius.TopLeft * density);
		var topRight = (float)(VisualOptions.CornerRadius.TopRight * density);
		var bottomRight = (float)(VisualOptions.CornerRadius.BottomRight * density);
		var bottomLeft = (float)(VisualOptions.CornerRadius.BottomLeft * density);

		switch (snackbarView.Background)
		{
			// The background that Material builds for a snackbar under a Material Components theme
			case GradientDrawable gradientDrawable:
				gradientDrawable.SetColor(VisualOptions.BackgroundColor.ToPlatform().ToArgb());

				// GradientDrawable takes an x and a y radius per corner, in the order top left, top right, bottom right, bottom left
				gradientDrawable.SetCornerRadii([topLeft, topLeft, topRight, topRight, bottomRight, bottomRight, bottomLeft, bottomLeft]);

				snackbarView.SetBackground(gradientDrawable);
				break;

			// The background that Material builds under a Material 3 theme. Its snackbar style colors it with a backgroundTint, which
			// replaces the fill color, so the color is applied as the tint (as Material's Snackbar.setBackgroundTint does)
			case MaterialShapeDrawable materialShapeDrawable:
				materialShapeDrawable.ShapeAppearanceModel = materialShapeDrawable.ShapeAppearanceModel.ToBuilder()
					.SetTopLeftCornerSize(topLeft)
					.SetTopRightCornerSize(topRight)
					.SetBottomRightCornerSize(bottomRight)
					.SetBottomLeftCornerSize(bottomLeft)
					.Build();

				snackbarView.BackgroundTintList = ColorStateList.ValueOf(VisualOptions.BackgroundColor.ToPlatform());
				break;
		}
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

			// Material reports a snackbar that is dismissed again while it animates out (for example by the next snackbar, as
			// DISMISS_EVENT_CONSECUTIVE after DISMISS_EVENT_MANUAL) once per dismissal: raise Dismissed only for the first one
			if (dismissedTCS.TrySetResult(true))
			{
				snackbar.OnDismissed();
			}
		}
	}
}