using System.Runtime.InteropServices;
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
	public PaddedButton(NFloat leftPadding, NFloat topPadding, NFloat rightPadding, NFloat bottomPadding)
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
	public NFloat LeftPadding { get; }

	/// <summary>
	/// Top Padding
	/// </summary>
	public NFloat TopPadding { get; }

	/// <summary>
	/// Right Padding
	/// </summary>
	public NFloat RightPadding { get; }

	/// <summary>
	/// Bottom Padding
	/// </summary>
	public NFloat BottomPadding { get; }
}