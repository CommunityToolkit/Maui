using System.ComponentModel;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Behaviors;

public partial class IconTintColorBehavior
{
	// The states whose title color MAUI sets on a UIButton (ButtonHandler). A title without a color of its own follows the tint color.
	static readonly UIControlState[] titleColorStates = [UIControlState.Normal, UIControlState.Highlighted, UIControlState.Disabled];

	// Null while the view is not tinted.
	OriginalTint? originalTint;

	// MAUI gives the button the image of a new ImageSource once it has loaded it, as AlwaysOriginal, and unlike Image and ImageButton, a Button
	// has no IsLoading that tells when.
	IDisposable? buttonImageObserver;

	// The observer only holds a weak reference to the managed UIImageView, and cannot remove the observation once that is collected: the image
	// view is kept for as long as the observation.
	UIImageView? observedButtonImageView;

	/// <inheritdoc/>
	protected override void OnViewPropertyChanged(View sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);

		// MAUI maps Button.TextColor to the tint color and the title colors of the UIButton (ButtonExtensions.UpdateTextColor) after this
		// notification, which recolors the icon: it is tinted again once MAUI is done.
		if (e.PropertyName == Button.TextColorProperty.PropertyName && sender is Button && originalTint is not null)
		{
			sender.Dispatcher.Dispatch(OnButtonTextColorMapped);
			return;
		}

		if ((e.PropertyName != ImageButton.IsLoadingProperty.PropertyName
			&& e.PropertyName != Image.SourceProperty.PropertyName
			&& e.PropertyName != ImageButton.SourceProperty.PropertyName)
			|| sender.Handler?.PlatformView is not UIView platformView)
		{
			return;
		}

		if (sender is Image { IsLoading: false } or ImageButton { IsLoading: false })
		{
			ApplyTintColor(platformView, sender, TintColor);
		}
	}

	/// <inheritdoc/>
	protected override void OnAttachedTo(View bindable, UIView platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		ApplyTintColor(platformView, bindable, TintColor);

		PropertyChanged += OnIconTintColorBehaviorPropertyChanged;

		if (bindable is Button { Handler.PlatformView: UIButton button })
		{
			StopObservingButtonImage();
			observedButtonImageView = button.ImageView;
			buttonImageObserver = observedButtonImageView.AddObserver("image", NSKeyValueObservingOptions.New, OnButtonImageChanged);
		}
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(View bindable, UIView platformView)
	{
		base.OnDetachedFrom(bindable, platformView);

		PropertyChanged -= OnIconTintColorBehaviorPropertyChanged;

		StopObservingButtonImage();

		ClearTintColor(platformView, bindable);
	}

	// UIKit dims the tint color of the views behind an alert, an action sheet or a popover (TintAdjustmentMode Dimmed), and a button reports
	// its tint color, and the title colors that follow it, dimmed meanwhile. The colors the behavior keeps, compares and puts back are the
	// ones without the dimming.
	static void RunUndimmed(UIView view, Action action)
	{
		if (view.TintAdjustmentMode is not UIViewTintAdjustmentMode.Dimmed)
		{
			action();
			return;
		}

		// Dimmed by its superview, the view follows it again (MAUI leaves TintAdjustmentMode Automatic); otherwise it is dimmed of its own.
		var tintAdjustmentMode = view.Superview?.TintAdjustmentMode is UIViewTintAdjustmentMode.Dimmed ? UIViewTintAdjustmentMode.Automatic : UIViewTintAdjustmentMode.Dimmed;
		view.TintAdjustmentMode = UIViewTintAdjustmentMode.Normal;

		try
		{
			action();
		}
		finally
		{
			view.TintAdjustmentMode = tintAdjustmentMode;
		}
	}

	void StopObservingButtonImage()
	{
		buttonImageObserver?.Dispose();
		buttonImageObserver = null;
		observedButtonImageView = null;
	}

	void OnIconTintColorBehaviorPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == TintColorProperty.PropertyName && View?.Handler?.PlatformView is UIView platformView)
		{
			ApplyTintColor(platformView, View, TintColor);
		}
	}

	// UIKit reports the new image while the button is still updating it: the image is tinted once the button is done.
	void OnButtonImageChanged(NSObservedChange change)
	{
		if (GetButtonWithAnUntintedImage() is not null)
		{
			View?.Dispatcher.Dispatch(() =>
			{
				if (GetButtonWithAnUntintedImage() is UIButton button && View is View view && TintColor is Color tintColor)
				{
					SetUIButtonTintColor(button, view, tintColor);
				}
			});
		}
	}

	// MAUI has given the button the tint color and the title colors of its TextColor: they are the colors to put back from now on. MAUI leaves
	// the tint color alone when it styles the button through its UIButtonConfiguration (Mac idiom).
	void OnButtonTextColorMapped()
	{
		if (originalTint is null || TintColor is not Color tintColor || View is not Button view || view.Handler?.PlatformView is not UIButton button)
		{
			return;
		}

		RunUndimmed(button, () =>
		{
			if (!tintColor.ToPlatform().Equals(button.TintColor))
			{
				originalTint = null;
				SetUIButtonTintColor(button, view, tintColor);
			}
		});
	}

	UIButton? GetButtonWithAnUntintedImage() =>
		TintColor is not null && View?.Handler?.PlatformView is UIButton { CurrentImage.RenderingMode: not UIImageRenderingMode.AlwaysTemplate } button ? button : null;

	void ClearTintColor(UIView platformView, View element)
	{
		switch (platformView)
		{
			case UIImageView imageView:
				if (imageView.Image is not null)
				{
					imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
				}

				// MAUI never sets the tint color of an image view: it follows the one of its superview again.
				if (originalTint is not null)
				{
					imageView.TintColor = null;
				}

				break;
			case UIButton button:
				RunUndimmed(button, () => ClearUIButtonTintColor(button));
				break;

			default:
				throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports {nameof(UIButton)} and {nameof(UIImageView)}.");
		}

		originalTint = null;
	}

	void ClearUIButtonTintColor(UIButton button)
	{
		// CurrentImage is null for an image that comes from a UIButtonConfiguration, which the behavior does not tint (SetUIButtonTintColor).
		if (button.CurrentImage is UIImage currentImage)
		{
			var originalImage = currentImage.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
			button.SetImage(originalImage, UIControlState.Normal);
		}

		if (originalTint is null)
		{
			return;
		}

		button.TintColor = originalTint.TintColor;

		// MAUI never sets the tint color of the button's image view: it follows the button's again.
		button.ImageView.TintColor = null;

		// SetUIButtonTintColor set these title colors to keep the title out of the tint; a title color the button got in the meantime stays.
		foreach (var (state, titleColor) in originalTint.TitleColors)
		{
			if (titleColor.Equals(button.TitleColor(state)))
			{
				button.SetTitleColor(null, state);
			}
		}
	}

	void ApplyTintColor(UIView platformView, View element, Color? color)
	{
		if (color is null)
		{
			ClearTintColor(platformView, element);
			return;
		}

		switch (platformView)
		{
			case UIImageView imageView:
				SetUIImageViewTintColor(imageView, element, color);
				break;
			case UIButton button:
				SetUIButtonTintColor(button, element, color);
				break;
			default:
				throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports {nameof(UIButton)} and {nameof(UIImageView)}.");
		}
	}

	void SetUIButtonTintColor(UIButton button, View element, Color color)
	{
		if (button.ImageView.Image is null || button.CurrentImage is not UIImage currentImage)
		{
			return;
		}

		RunUndimmed(button, () =>
		{
			var templatedImage = currentImage.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
			var titleColors = Array.ConvertAll(titleColorStates, button.TitleColor);
			var tint = originalTint ??= new(button.TintColor);

			button.SetImage(null, UIControlState.Normal);
			var platformColor = color.ToPlatform();
			button.TintColor = platformColor;
			button.ImageView.TintColor = platformColor;
			button.SetImage(templatedImage, UIControlState.Normal);

			// The tint color is for the icon: the title keeps the color it had in each state. A state's title color can follow the tint color, or
			// the Normal one once that is set (Disabled does), so each state is compared after the previous ones were set back.
			for (var i = 0; i < titleColorStates.Length; i++)
			{
				if (titleColors[i] is UIColor titleColor && !titleColor.Equals(button.TitleColor(titleColorStates[i])))
				{
					button.SetTitleColor(titleColor, titleColorStates[i]);
					tint.TitleColors.Add((titleColorStates[i], titleColor));
				}
			}
		});
	}

	void SetUIImageViewTintColor(UIImageView imageView, View element, Color color)
	{
		if (imageView.Image is null)
		{
			return;
		}

		// MAUI never sets the tint color of an image view: ClearTintColor gives it back the one of its superview.
		originalTint ??= new(null);

		imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
		imageView.TintColor = color.ToPlatform();
	}

	sealed class OriginalTint(UIColor? tintColor)
	{
		public UIColor? TintColor { get; } = tintColor;

		public List<(UIControlState State, UIColor Color)> TitleColors { get; } = [];
	}
}