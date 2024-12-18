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
		SetPadding(leftPadding, topPadding, rightPadding, bottomPadding);
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

	void SetPadding(nfloat leftPadding, nfloat topPadding, nfloat rightPadding, nfloat bottomPadding)
	{
		if (OperatingSystem.IsIOSVersionAtLeast(15) && Configuration is not null)
		{
			Configuration.ContentInsets = new NSDirectionalEdgeInsets(topPadding, leftPadding, bottomPadding, rightPadding);
		}
		else
		{
			ContentEdgeInsets = new UIEdgeInsets(topPadding, leftPadding, bottomPadding, rightPadding);
		}
	}
}