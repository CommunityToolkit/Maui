using CoreAnimation;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// A rounded <see cref="UIStackView"/>
/// </summary>
public class RoundedStackView : UIStackView
{
	/// <summary>
	/// Initialize <see cref="RoundedStackView"/>
	/// </summary>
	public RoundedStackView(nfloat leftPadding, nfloat topPadding, nfloat rightPadding, nfloat bottomPadding)
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

	static CGPath? GetRoundedPath(CGRect rect, nfloat left, nfloat top, nfloat right, nfloat bottom)
	{
		var path = new UIBezierPath();
		path.MoveTo(new CGPoint(rect.Width - right, rect.Y));

		path.AddArc(new CGPoint((float)rect.X + rect.Width - right, (float)rect.Y + right), (nfloat)right, (float)(Math.PI * 1.5), (float)Math.PI * 2, true);
		path.AddLineTo(new CGPoint(rect.Width, rect.Height - bottom));

		path.AddArc(new CGPoint((float)rect.X + rect.Width - bottom, (float)rect.Y + rect.Height - bottom), (nfloat)bottom, 0, (float)(Math.PI * .5), true);
		path.AddLineTo(new CGPoint(left, rect.Height));

		path.AddArc(new CGPoint((float)rect.X + left, (float)rect.Y + rect.Height - left), (nfloat)left, (float)(Math.PI * .5), (float)Math.PI, true);
		path.AddLineTo(new CGPoint(rect.X, top));

		path.AddArc(new CGPoint((float)rect.X + top, (float)rect.Y + top), (nfloat)top, (float)Math.PI, (float)(Math.PI * 1.5), true);

		path.ClosePath();

		return path.CGPath;
	}
}