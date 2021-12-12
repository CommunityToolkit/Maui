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
	public PaddedButton(double leftPadding, double topPadding, double rightPadding, double bottomPadding)
	{
		LeftPadding = leftPadding;
		TopPadding = topPadding;
		RightPadding = rightPadding;
		BottomPadding = bottomPadding;

		ContentEdgeInsets = new UIEdgeInsets((nfloat)topPadding, (nfloat)leftPadding, (nfloat)bottomPadding, (nfloat)rightPadding);
	}

	/// <summary>
	/// Left Padding
	/// </summary>
	public double LeftPadding { get; }

	/// <summary>
	/// Top Padding
	/// </summary>
	public double TopPadding { get; }

	/// <summary>
	/// Right Padding
	/// </summary>
	public double RightPadding { get; }

	/// <summary>
	/// Bottom Padding
	/// </summary>
	public double BottomPadding { get; }
}
