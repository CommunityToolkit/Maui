using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for Microsoft.Maui.Graphics.Color animations
/// </summary>
public static partial class ColorAnimationExtensions
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
	public static Task<bool> BackgroundColorTo(this VisualElement element, Color color, uint rate = 16u, uint length = 250u, Easing? easing = null)
	{
		ArgumentNullException.ThrowIfNull(element);
		ArgumentNullException.ThrowIfNull(color);

		//Although BackgroundColor is defined as not-nullable, it CAN be null
		//If null => set it to Transparent as Animation will crash on null BackgroundColor
		element.BackgroundColor ??= Colors.Transparent;

		var animationCompletionSource = new TaskCompletionSource<bool>();

		try
		{
			new Animation
			{
				{ 0, 1, GetRedTransformAnimation(element, color.Red) },
				{ 0, 1, GetGreenTransformAnimation(element, color.Green) },
				{ 0, 1, GetBlueTransformAnimation(element, color.Blue) },
				{ 0, 1, GetAlphaTransformAnimation(element, color.Alpha) },
			}
			.Commit(element, nameof(BackgroundColorTo), rate, length, easing, (d, b) => animationCompletionSource.SetResult(true));
		}
		catch (ArgumentException aex)
		{
			//When creating an Animation too early in the lifecycle of the Page, i.e. in the OnAppearing method,
			//the Page might not have an 'IAnimationManager' yet, resulting in an ArgumentException.
			System.Diagnostics.Debug.WriteLine($"{aex.GetType().Name} thrown in {typeof(ColorAnimationExtensions).FullName}: {aex.Message}");
			animationCompletionSource.SetResult(false);
		}

		return animationCompletionSource.Task;


		static Animation GetRedTransformAnimation(VisualElement element, float targetRed) =>
			new(v => element.BackgroundColor = element.BackgroundColor.WithRed(v), element.BackgroundColor.Red, targetRed);

		static Animation GetGreenTransformAnimation(VisualElement element, float targetGreen) =>
			new(v => element.BackgroundColor = element.BackgroundColor.WithGreen(v), element.BackgroundColor.Green, targetGreen);

		static Animation GetBlueTransformAnimation(VisualElement element, float targetBlue) =>
			new(v => element.BackgroundColor = element.BackgroundColor.WithBlue(v), element.BackgroundColor.Blue, targetBlue);

		static Animation GetAlphaTransformAnimation(VisualElement element, float targetAlpha) =>
			new(v => element.BackgroundColor = element.BackgroundColor.WithAlpha((float)v), element.BackgroundColor.Alpha, targetAlpha);
	}
}