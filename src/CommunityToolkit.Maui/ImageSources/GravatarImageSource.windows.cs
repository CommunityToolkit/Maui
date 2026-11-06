using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using WButton = Microsoft.UI.Xaml.Controls.Button;
using WImage = Microsoft.UI.Xaml.Controls.Image;
using WTextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

namespace CommunityToolkit.Maui.ImageSources;

public partial class GravatarImageSource
{
	Button? hostButton;
	WImage? hostButtonImage;
	long hostButtonImageSourceCallbackToken;

	static double GetRoomForPicture(WButton platformButton, Button button)
	{
		var room = button.Height
			- platformButton.Padding.Top - platformButton.Padding.Bottom
			- platformButton.BorderThickness.Top - platformButton.BorderThickness.Bottom;

		if (button.ContentLayout.Position is Button.ButtonContentLayout.ImagePosition.Top or Button.ButtonContentLayout.ImagePosition.Bottom
			&& platformButton.GetContent<WTextBlock>() is WTextBlock textBlock
			&& !string.IsNullOrEmpty(textBlock.Text))
		{
			room -= textBlock.DesiredSize.Height + button.ContentLayout.Spacing;
		}

		return Math.Max(0, room);
	}

	partial void PlatformOnParentSet()
	{
		ReleaseHostButton();

		if (Parent is not Button button)
		{
			return;
		}

		hostButton = button;
		button.HandlerChanged += OnHostButtonHandlerChanged;
		button.SizeChanged += OnHostButtonSizeChanged;
		AttachHostButtonImage();
	}

	void AttachHostButtonImage()
	{
		if (hostButton?.Handler?.PlatformView is not WButton platformButton || platformButton.GetContent<WImage>() is not WImage image)
		{
			return;
		}

		hostButtonImage = image;

		// The native Image must not keep this source alive: it can outlive the source's time on this Button.
		var weakSource = new WeakReference<GravatarImageSource>(this);
		hostButtonImageSourceCallbackToken = image.RegisterPropertyChangedCallback(WImage.SourceProperty, (_, _) =>
		{
			if (weakSource.TryGetTarget(out var source))
			{
				source.FitHostButtonImage();
			}
		});

		FitHostButtonImage();
	}

	// .NET MAUI keeps the picture of a Windows Button at its natural size only from BitmapImage.ImageOpened, which a BitmapImage decoded from a
	// stream, like every GravatarImageSource picture, has already raised when the Button subscribes: the picture would fill the Button's width.
	// The picture must also fit the room the Button already has: this source asks for a picture as large as the Button, so a Button that grew
	// for its picture would ask for a larger one each time.
	void FitHostButtonImage()
	{
		if (hostButtonImage is null || hostButton?.Handler?.PlatformView is not WButton platformButton)
		{
			return;
		}

		switch (hostButtonImage.Source)
		{
			case null:
				// MAUI collapses an Image without a source; keeping the height avoids a jump when the next picture arrives.
				return;

			case BitmapSource bitmap when bitmap.PixelHeight > 0:
				var height = bitmap.GetImageSourceSize(platformButton).Height;
				if (hostButton.Height >= 0)
				{
					height = Math.Min(height, GetRoomForPicture(platformButton, hostButton));
				}

				hostButtonImage.Height = height;
				return;

			default:
				hostButtonImage.ClearValue(FrameworkElement.HeightProperty);
				return;
		}
	}

	void OnHostButtonHandlerChanged(object? sender, EventArgs e)
	{
		DetachHostButtonImage();
		AttachHostButtonImage();
	}

	void OnHostButtonSizeChanged(object? sender, EventArgs e) => FitHostButtonImage();

	void DetachHostButtonImage()
	{
		if (hostButtonImage is null)
		{
			return;
		}

		hostButtonImage.UnregisterPropertyChangedCallback(WImage.SourceProperty, hostButtonImageSourceCallbackToken);
		hostButtonImage.ClearValue(FrameworkElement.HeightProperty);
		hostButtonImage = null;
	}

	void ReleaseHostButton()
	{
		DetachHostButtonImage();

		if (hostButton is null)
		{
			return;
		}

		hostButton.HandlerChanged -= OnHostButtonHandlerChanged;
		hostButton.SizeChanged -= OnHostButtonSizeChanged;
		hostButton = null;
	}
}
