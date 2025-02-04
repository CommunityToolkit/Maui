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

				case AndroidMaterialButton materialButton when tintColor is not null:
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

		static void SetMaterialButtonTintColor(AndroidMaterialButton button, Color color)
		{
			button.IconTintMode = PorterDuff.Mode.SrcIn;
			button.IconTint = new Android.Content.Res.ColorStateList([[]], [color.ToPlatform()]);
		}

		static void SetWidgetButtonTintColor(AndroidWidgetButton button, Color? color)
		{
			var drawables = button.GetCompoundDrawables().ToList();

			if (color is null)
			{
				foreach (var img in drawables)
				{
					img.ClearColorFilter();
				}
				color = Colors.Transparent;
			}

			foreach (var img in drawables)
			{
				img.SetTint(color.ToPlatform());
			}
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
					foreach (var drawable in button.GetCompoundDrawables())
					{
						drawable.ClearColorFilter();
					}

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