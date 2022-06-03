using Android.Graphics;
using Android.Widget;
using Microsoft.Maui.Platform;
using AButton = Android.Widget.Button;
using Color = Microsoft.Maui.Graphics.Color;
using AView = Android.Views.View;
using System.ComponentModel;
using ImageButton = Microsoft.Maui.Controls.ImageButton;

namespace CommunityToolkit.Maui.Behaviors;

public partial class IconTintColorBehavior : PlatformBehavior<Image>
{

	/// <inheritdoc/>
	protected override void OnAttachedTo(Image bindable, AView platformView) =>
		ApplyTintColor(bindable, platformView);

	/// <inheritdoc/>
	protected override void OnDetachedFrom(Image bindable, AView platformView) =>
		ClearTintColor(bindable, platformView);

	void ApplyTintColor(Image element, AView control)
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
				throw new InvalidOperationException($"The control must be of type {nameof(AButton)} or {nameof(ImageView)}.");
		}


		static void SetImageViewTintColor(ImageView image, Color? color)
		{
			if (color is null)
			{
				image.ClearColorFilter();
				color = Colors.Transparent;
			}

			image.SetColorFilter(new PorterDuffColorFilter(color.ToPlatform(), PorterDuff.Mode.SrcIn ?? throw new NullReferenceException()));
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
			|| sender is not Image bindable
			|| bindable.Handler?.PlatformView is not AView platformView)
		{
			return;
		}

		if (!propertyName.Equals(TintColorProperty.PropertyName)
			&& !propertyName.Equals(Image.SourceProperty.PropertyName)
			&& !propertyName.Equals(ImageButton.SourceProperty.PropertyName))
		{
			return;
		}

		ApplyTintColor(bindable, platformView);
	}

	void ClearTintColor(Image element, AView control)
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
