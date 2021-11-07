using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// The <see cref="FlipHorizontalAnimation"/> is an animation that flips a <see cref="View"/> horizontally
/// </summary>
public class FlipHorizontalAnimation : RotateAnimation
{
	/// <inheritdoc/>
	protected override double DefaultRotation { get; set; } = 90;

	/// <inheritdoc/>
	protected override uint DefaultDuration { get; set; } = 300;

	/// <inheritdoc/>
	public override async Task Animate(View? view)
	{
		if (view != null)
		{
			await view.RotateYTo(Rotation, Duration, Easing);
			await view.RotateYTo(0, Duration, Easing);
		}
	}
}
