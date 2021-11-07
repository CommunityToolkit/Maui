using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// The <see cref="FlipHorizontalAnimation"/> is an animation that flips a <see cref="View"/> vertically
/// </summary>
public class FlipVerticalAnimation : RotateAnimation
{
	///<inheritdoc/>
	protected override double DefaultRotation { get; set; } = 90;

	///<inheritdoc/>
	public override async Task Animate(View? view)
	{
		if (view != null)
		{
			await view.RotateXTo(Rotation, Duration, Easing);
			await view.RotateXTo(0, Duration, Easing);
		}
	}
}
