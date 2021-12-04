using System.Threading.Tasks;
using CommunityToolkit.Maui.Animations;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.Models;
public class SampleScaleAnimation : BaseAnimation
{
	public override async Task Animate(VisualElement? view)
	{
		if (view == null)
			return;
		await view.ScaleTo(1.2, Length, Easing);
		await view.ScaleTo(1, Length, Easing);
	}
}
