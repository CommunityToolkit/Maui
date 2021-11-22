using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for Microsoft.Maui.Graphics.Color animations
/// </summary>
public static class ColorAnimationExtensions
{
	/// <summary>
	/// Animates the BackgroundColor of a VisualElement to the given color
	/// </summary>
	/// <param name="element"></param>
	/// <param name="color">The target color to animate the VisualElement's BackgroundColor to</param>
	/// <param name="rate">The time, in milliseconds, between the frames of the animation</param>
	/// <param name="length">The duration, in milliseconds, of the animation</param>
	/// <param name="easing">The easing function to be used in the animation</param>
	/// <returns>Value indicating if the animation completed successfully or not</returns>
	public static Task<bool> ColorTo(this VisualElement element, Color color, uint rate = 16u, uint length = 250u, Easing? easing = null)
	{
		ArgumentNullException.ThrowIfNull(element);

		var animationCompletionSource = new TaskCompletionSource<bool>();

		try
		{
			new Animation
			{ 
				{ 0, 1, RedTransformAnimation(element, color.Red) },
				{ 0, 1, GreenTransformAnimation(element, color.Green) },
				{ 0, 1, BlueTransformAnimation(element, color.Blue) },
				{ 0, 1, AlphaTransformAnimation(element, color.Alpha) },
			}
			.Commit(element, nameof(ColorTo), rate, length, easing, (d, b) => animationCompletionSource.SetResult(true));
		}
		catch (ArgumentException aex)
		{
			//When creating an Animation too early in the lifecycle of the Page, i.e. in the OnAppearing method,
			//the Page might not have an 'IAnimationManager' yet, resulting in an ArgumentException.
			System.Diagnostics.Debug.WriteLine($"{aex.GetType().Name} thrown in {typeof(ColorAnimationExtensions).FullName}: {aex.Message}");
			animationCompletionSource.SetResult(false);
		}

		return animationCompletionSource.Task;
	}

	private static Animation RedTransformAnimation(VisualElement element, float targetRed)
		=> new (v => element.BackgroundColor = element.BackgroundColor.WithRed(v),
			element.BackgroundColor.Red, targetRed);

	private static Animation GreenTransformAnimation(VisualElement element, float targetGreen)
		=> new (v => element.BackgroundColor = element.BackgroundColor.WithGreen(v), 
			element.BackgroundColor.Green, targetGreen);

	private static Animation BlueTransformAnimation(VisualElement element, float targetBlue)
		=> new (v => element.BackgroundColor = element.BackgroundColor.WithBlue(v),
			element.BackgroundColor.Blue, targetBlue);

	private static Animation AlphaTransformAnimation(VisualElement element, float targetAlpha)
		=> new (v => element.BackgroundColor = element.BackgroundColor.WithAlpha(v),
			element.BackgroundColor.Alpha, targetAlpha);
}
