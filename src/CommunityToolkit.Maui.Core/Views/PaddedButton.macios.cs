using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// <see cref="UIButton"/> with Left, Top, Right and Bottom Padding
/// </summary>
public sealed class PaddedButton : UIButton
{
	/// <summary>
	/// Initialize <see cref="PaddedButton"/>
	/// </summary>
	public PaddedButton(nfloat leftPadding, nfloat topPadding, nfloat rightPadding, nfloat bottomPadding)
	{
		LeftPadding = leftPadding;
		TopPadding = topPadding;
		RightPadding = rightPadding;
		BottomPadding = bottomPadding;

#if IOS15_0_OR_GREATER || MACCATALYST15_0_OR_GREATER
		if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
		{
#pragma warning disable CA1416 // Validate platform compatibility
			var filled = UIButtonConfiguration.FilledButtonConfiguration;
			filled.ContentInsets = new NSDirectionalEdgeInsets(topPadding, leftPadding, bottomPadding, rightPadding);
			Configuration = filled;
#pragma warning restore CA1416 // Validate platform compatibility
		}
#else
		ContentEdgeInsets = new UIEdgeInsets(topPadding, leftPadding, bottomPadding, rightPadding);
#endif
	}

	/// <summary>
	/// Left Padding
	/// </summary>
	public nfloat LeftPadding { get; }

	/// <summary>
	/// Top Padding
	/// </summary>
	public nfloat TopPadding { get; }

	/// <summary>
	/// Right Padding
	/// </summary>
	public nfloat RightPadding { get; }

	/// <summary>
	/// Bottom Padding
	/// </summary>
	public nfloat BottomPadding { get; }
}