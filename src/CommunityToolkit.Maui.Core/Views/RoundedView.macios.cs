using CoreAnimation;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// A rounded <see cref="UIView"/>
/// </summary>
/// <remarks>
/// Initialize <see cref="RoundedView"/>
/// </remarks>
public class RoundedView(nfloat leftPadding, nfloat topPadding, nfloat rightPadding, nfloat bottomPadding) : UIView
{
	/// <summary>
	/// Left Padding
	/// </summary>
	public nfloat LeftPadding { get; } = leftPadding;

	/// <summary>
	/// Top Padding
	/// </summary>
	public nfloat TopPadding { get; } = topPadding;

	/// <summary>
	/// Right Padding
	/// </summary>
	public nfloat RightPadding { get; } = rightPadding;

	/// <summary>
	/// Bottom Padding
	/// </summary>
	public nfloat BottomPadding { get; } = bottomPadding;

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

	/// <inheritdoc />
	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		Draw(Frame);
	}

	static CGPath? GetRoundedPath(CGRect rect, nfloat left, nfloat top, nfloat right, nfloat bottom)
	{
		var path = new UIBezierPath();
		path.MoveTo(new CGPoint(rect.Width - right, rect.Y));

		path.AddArc(new CGPoint(rect.X + rect.Width - right, rect.Y + right), right, (nfloat)(Math.PI * 1.5), (nfloat)Math.PI * 2, true);
		path.AddLineTo(new CGPoint(rect.Width, rect.Height - bottom));

		path.AddArc(new CGPoint(rect.X + rect.Width - bottom, rect.Y + rect.Height - bottom), bottom, 0, (nfloat)(Math.PI * .5), true);
		path.AddLineTo(new CGPoint(left, rect.Height));

		path.AddArc(new CGPoint(rect.X + left, rect.Y + rect.Height - left), left, (nfloat)(Math.PI * .5), (nfloat)Math.PI, true);
		path.AddLineTo(new CGPoint(rect.X, top));

		path.AddArc(new CGPoint(rect.X + top, rect.Y + top), top, (nfloat)Math.PI, (nfloat)(Math.PI * 1.5), true);

		path.ClosePath();

		return path.CGPath;
	}
}