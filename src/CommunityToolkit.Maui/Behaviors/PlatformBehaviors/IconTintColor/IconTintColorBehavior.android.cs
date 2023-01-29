﻿using System.ComponentModel;
using Android.Graphics;
using Android.Widget;
using Microsoft.Maui.Platform;
using AButton = Android.Widget.Button;
using AView = Android.Views.View;
using Color = Microsoft.Maui.Graphics.Color;
using ImageButton = Microsoft.Maui.Controls.ImageButton;

namespace CommunityToolkit.Maui.Behaviors;

public partial class IconTintColorBehavior
{
	/// <inheritdoc/>
	protected override void OnAttachedTo(View bindable, AView platformView)
	{
		ApplyTintColor(bindable, platformView);

		this.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == TintColorProperty.PropertyName)
			{
				ApplyTintColor(bindable, platformView);
			}
		};
	}

	/// <inheritdoc/>
	protected override void OnDetachedFrom(View bindable, AView platformView) =>
		ClearTintColor(bindable, platformView);

	void ApplyTintColor(View element, AView control)
	{
		var color = TintColor;
		element.PropertyChanged += OnElementPropertyChanged;

		switch (control)
		{
			case ImageView image:
				SetImageViewTintColor(image, color);
				break;
			case AButton button:
				SetButtonTintColor(button, color);
				break;
			default:
				throw new NotSupportedException($"{nameof(IconTintColorBehavior)} only currently supports Android.Widget.Button and {nameof(ImageView)}.");
		}


		static void SetImageViewTintColor(ImageView image, Color? color)
		{
			if (color is null)
			{
				image.ClearColorFilter();
				color = Colors.Transparent;
			}

			image.SetColorFilter(new PorterDuffColorFilter(color.ToPlatform(), PorterDuff.Mode.SrcIn ?? throw new InvalidOperationException("PorterDuff.Mode.SrcIn should not be null at runtime.")));
		}

		static void SetButtonTintColor(AButton button, Color? color)
		{
			var drawables = button.GetCompoundDrawables().Where(d => d is not null);

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

	void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs args)
	{
		if (args.PropertyName is not string propertyName
			|| sender is not View bindable
			|| bindable.Handler?.PlatformView is not AView platformView)
		{
			return;
		}

		if (!propertyName.Equals(Image.SourceProperty.PropertyName, StringComparison.Ordinal)
			&& !propertyName.Equals(ImageButton.SourceProperty.PropertyName, StringComparison.Ordinal))
		{
			return;
		}

		ApplyTintColor(bindable, platformView);
	}

	void ClearTintColor(View element, AView control)
	{
		element.PropertyChanged -= OnElementPropertyChanged;
		switch (control)
		{
			case ImageView image:
				image.ClearColorFilter();
				break;
			case AButton button:
				foreach (var drawable in button.GetCompoundDrawables())
				{
					drawable?.ClearColorFilter();
				}
				break;
		}
	}
}