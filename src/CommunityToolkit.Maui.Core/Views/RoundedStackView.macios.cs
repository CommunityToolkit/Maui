using System.Runtime.InteropServices;
using CoreAnimation;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// A rounded <see cref="UIStackView"/>
/// </summary>
public class RoundedStackView : UIStackView
{
	/// <summary>
	/// Initialize <see cref="RoundedStackView"/>
	/// </summary>
	public RoundedStackView(NFloat leftPadding, NFloat topPadding, NFloat rightPadding, NFloat bottomPadding)
	{
		LeftPadding = leftPadding;
		TopPadding = topPadding;
		RightPadding = rightPadding;
		BottomPadding = bottomPadding;
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

	/// <inheritdoc />
	public override void Draw(CGRect rect)
	{
		ClipsToBounds = true;

		var path = GetRoundedPath(rect, LeftPadding, TopPadding, RightPadding, BottomPadding);
		var maskLayer = new CAShapeLayer
		{
			Frame = rect,
			Path = path
		};

		Layer.Mask = maskLayer;
		Layer.MasksToBounds = true;
	}

	static CGPath? GetRoundedPath(CGRect rect, NFloat left, NFloat top, NFloat right, NFloat bottom)
	{
		var path = new UIBezierPath();
		path.MoveTo(new CGPoint(rect.Width - right, rect.Y));

		path.AddArc(new CGPoint(rect.X + rect.Width - right, rect.Y + right), right, (NFloat)(Math.PI * 1.5), (NFloat)Math.PI * 2, true);
		path.AddLineTo(new CGPoint(rect.Width, rect.Height - bottom));

		path.AddArc(new CGPoint(rect.X + rect.Width - bottom, rect.Y + rect.Height - bottom), bottom, 0, (NFloat)(Math.PI * .5), true);
		path.AddLineTo(new CGPoint(left, rect.Height));

		path.AddArc(new CGPoint(rect.X + left, rect.Y + rect.Height - left), left, (NFloat)(Math.PI * .5), (NFloat)Math.PI, true);
		path.AddLineTo(new CGPoint(rect.X, top));

		path.AddArc(new CGPoint(rect.X + top, rect.Y + top), top, (NFloat)Math.PI, (NFloat)(Math.PI * 1.5), true);

		path.ClosePath();

		return path.CGPath;
	}
}