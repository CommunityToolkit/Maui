using System;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// <see cref="UIButton"/> with Left, Top, Right and Bottom Padding
/// </summary>
public class PaddedButton : UIButton
{
	/// <summary>
	/// Initialize <see cref="PaddedButton"/>
	/// </summary>
	/// <param name="leftPadding"></param>
	/// <param name="topPadding"></param>
	/// <param name="rightPadding"></param>
	/// <param name="bottomPadding"></param>
	public PaddedButton(nfloat leftPadding, nfloat topPadding, nfloat rightPadding, nfloat bottomPadding)
	{
		LeftPadding = leftPadding;
		TopPadding = topPadding;
		RightPadding = rightPadding;
		BottomPadding = bottomPadding;

		ContentEdgeInsets = new UIEdgeInsets(topPadding, leftPadding, bottomPadding, rightPadding);
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