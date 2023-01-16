using AVKit;
using UIKit;

namespace CommunityToolkit.Maui.MediaPlayer.PlatformView;

/// <summary>
/// The user-interface element that represents the <see cref="MediaPlayer"/> on iOS and macOS.
/// </summary>
public class MauiMediaPlayer : UIView
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaPlayer"/> class.
	/// </summary>
	/// <param name="playerViewController">The <see cref="AVPlayerViewController"/> that acts as the platform media player.</param>
	/// <exception cref="NullReferenceException">Thrown when <paramref name="playerViewController"/><c>.View</c> is <see langword="null"/>.</exception>
	public MauiMediaPlayer(AVPlayerViewController playerViewController)
	{
		ArgumentNullException.ThrowIfNull(playerViewController.View);
		playerViewController.View.Frame = Bounds;

#if IOS16_0_OR_GREATER || MACCATALYST16_1_OR_GREATER
		// On iOS 16+ and macOS 13+ the AVPlayerViewController has to be added to the parent ViewController, otherwise the transport controls won't be displayed.
		var viewController = WindowStateManager.Default.GetCurrentUIViewController() ?? throw new InvalidOperationException("ViewController can't be null.");

		if (viewController.View is null)
		{
			throw new NullReferenceException($"{nameof(viewController.View)} cannot be null");
		}

		// Zero out the safe area insets of the AVPlayerViewController
		UIEdgeInsets insets = viewController.View.SafeAreaInsets;
		playerViewController.AdditionalSafeAreaInsets =
			new UIEdgeInsets(insets.Top * -1, insets.Left, insets.Bottom * -1, insets.Right);

		// Add the View from the AVPlayerViewController to the parent ViewController
		viewController.View.AddSubview(playerViewController.View);
#endif

		AddSubview(playerViewController.View);
	}
}