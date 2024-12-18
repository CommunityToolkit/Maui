namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// <see cref="UILabel"/> with Left, Top, Right and Bottom Padding
/// </summary>
/// <remarks>
/// Initialize <see cref="PaddedLabel"/>
/// </remarks>
public class PaddedLabel(nfloat leftPadding, nfloat topPadding, nfloat rightPadding, nfloat bottomPadding) : UILabel
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

	/// <inheritdoc/>
	public override CGRect TextRectForBounds(CGRect bounds, nint numberOfLines)
	{
		var insets = new UIEdgeInsets(TopPadding, LeftPadding, BottomPadding, RightPadding);
		return base.TextRectForBounds(insets.InsetRect(bounds), numberOfLines);
	}
}