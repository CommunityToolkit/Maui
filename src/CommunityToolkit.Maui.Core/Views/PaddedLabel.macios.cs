using System;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// <see cref="UILabel"/> with Left, Top, Right and Bottom Padding
/// </summary>
public class PaddedLabel : UILabel
{
	/// <summary>
	/// Initialize <see cref="PaddedLabel"/>
	/// </summary>
	/// <param name="leftPadding"></param>
	/// <param name="topPadding"></param>
	/// <param name="rightPadding"></param>
	/// <param name="bottomPadding"></param>
	public PaddedLabel(nfloat leftPadding, nfloat topPadding, nfloat rightPadding, nfloat bottomPadding)
	{
		LeftPadding = leftPadding;
		TopPadding = topPadding;
		RightPadding = rightPadding;
		BottomPadding = bottomPadding;
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

	/// <inheritdoc/>
	public override CGSize IntrinsicContentSize => new(
		base.IntrinsicContentSize.Width + LeftPadding + RightPadding,
		base.IntrinsicContentSize.Height + TopPadding + BottomPadding);

	/// <inheritdoc/>
	public override void DrawText(CGRect rect)
	{
		var insets = new UIEdgeInsets(TopPadding, LeftPadding, BottomPadding, RightPadding);
		base.DrawText(insets.InsetRect(rect));
	}
}