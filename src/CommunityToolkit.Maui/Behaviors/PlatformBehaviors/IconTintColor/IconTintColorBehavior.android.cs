using System.ComponentModel;
using Android.Graphics;
using Android.Widget;
using Microsoft.Maui.Platform;
using AndroidMaterialButton = Google.Android.Material.Button.MaterialButton;
using AndroidView = Android.Views.View;
using AndroidWidgetButton = Android.Widget.Button;
using Color = Microsoft.Maui.Graphics.Color;
using ImageButton = Microsoft.Maui.Controls.ImageButton;

namespace CommunityToolkit.Maui.Behaviors;

public partial class IconTintColorBehavior
{
	AndroidView? nativeView;

	/// <inheritdoc/>
	protected override void OnAttachedTo(View bindable, AndroidView platformView)
	{
		base.OnAttachedTo(bindable, platformView);
		nativeView = platformView;

		ApplyTintColor(nativeView, TintColor);

		bindable.PropertyChanged += OnElementPropertyChanged;
		PropertyChanged += OnTintedImagePropertyChanged;
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(View bindable, AndroidView platformView)
	{
		base.OnDetachedFrom(bindable, platformView);

		ClearTintColor(platformView);

		bindable.PropertyChanged -= OnElementPropertyChanged;
		PropertyChanged -= OnTintedImagePropertyChanged;

		// A detached behavior that is still referenced must not keep the platform view (and its Activity) alive.
		nativeView = null;
	}

	static void ApplyTintColor(AndroidView? nativeView, Color? tintColor)
	{
		if (nativeView is null)
		{
			return;
		}

		try
		{
			switch (nativeView)
			{
				case ImageView image:
					SetImageViewTintColor(image, tintColor);
					break;

				case AndroidMaterialButton materialButton:
					SetMaterialButtonTintColor(materialButton, tintColor);
					break;

				case AndroidWidgetButton widgetButton:
					SetWidgetButtonTintColor(widgetButton, tintColor);
					break;

				default:
					throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports Android.Widget.Button and {nameof(ImageView)}.");
			}
		}
		catch (ObjectDisposedException)
		{
		}

		static void SetImageViewTintColor(ImageView image, Color? color)
		{
			if (color is null)
			{
				image.ClearColorFilter();
				return;
			}

			image.SetColorFilter(new PorterDuffColorFilter(color.ToPlatform(), PorterDuff.Mode.SrcIn ?? throw new InvalidOperationException("PorterDuff.Mode.SrcIn should not be null at runtime.")));
		}

		static void SetMaterialButtonTintColor(AndroidMaterialButton button, Color? color)
		{
			// A MaterialButton is also an Android.Widget.Button. Its icon is one of its compound drawables, but MaterialButton tints it with
			// IconTint, which it applies again whenever it updates the icon.
			if (color is null)
			{
				button.IconTint = null;
				return;
			}

			button.IconTintMode = PorterDuff.Mode.SrcIn;
			button.IconTint = new Android.Content.Res.ColorStateList([[]], [color.ToPlatform()]);
		}

		static void SetWidgetButtonTintColor(AndroidWidgetButton button, Color? color)
		{
			if (color is null)
			{
				ClearWidgetButtonTintColor(button);
				return;
			}

			// getCompoundDrawables() returns null for each side that has no drawable.
			foreach (var img in button.GetCompoundDrawables())
			{
				img?.SetTint(color.ToPlatform());
			}
		}
	}

	static void ClearWidgetButtonTintColor(AndroidWidgetButton button)
	{
		// getCompoundDrawables() returns null for each side that has no drawable.
		foreach (var drawable in button.GetCompoundDrawables())
		{
			if (drawable is null)
			{
				continue;
			}

			drawable.ClearColorFilter();
			drawable.SetTintList(null);
		}
	}

	static void ClearTintColor(AndroidView? nativeView)
	{
		if (nativeView is null)
		{
			return;
		}

		try
		{
			switch (nativeView)
			{
				case ImageView image:
					image.ClearColorFilter();
					break;

				case AndroidMaterialButton mButton:
					mButton.IconTint = null;
					break;

				case AndroidWidgetButton button:
					ClearWidgetButtonTintColor(button);
					break;
			}
		}
		catch (ObjectDisposedException)
		{
		}
	}

	void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs args)
	{
		if (args.PropertyName is not string propertyName
			|| sender is not View bindable
			|| bindable.Handler?.PlatformView is not AndroidView)
		{
			return;
		}

		if (!propertyName.Equals(Image.SourceProperty.PropertyName, StringComparison.Ordinal)
			&& !propertyName.Equals(ImageButton.SourceProperty.PropertyName, StringComparison.Ordinal))
		{
			return;
		}

		ApplyTintColor(nativeView, TintColor);
	}

	void OnTintedImagePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == TintColorProperty.PropertyName)
		{
			ApplyTintColor(nativeView, TintColor);
		}
	}
}