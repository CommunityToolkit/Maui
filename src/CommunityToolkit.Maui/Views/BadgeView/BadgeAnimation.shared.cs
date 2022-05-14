using CommunityToolkit.Maui.Core.Views.BadgeView;

namespace CommunityToolkit.Maui.Views.BadgeView;

/// <summary>
/// This is the default animation that is used when the <see cref="BadgeView.IsAnimated"/> is set to true.
/// </summary>
public class BadgeAnimation : IBadgeAnimation
{
	/// <summary>
	/// Gets the length of the animation.
	/// </summary>
	/// <value>The length of the animation.</value>
	protected uint AnimationLength { get; } = 150;

	/// <summary>
	/// Gets the animation offset.
	/// </summary>
	/// <value>The offset value.</value>
	protected uint Offset { get; } = 24;

	double? translationY;

	/// <summary>
	/// With the <see cref="OnAppearing(VisualElement)"/> method you can influence the animation that is used when the <see cref="BadgeView"/> appears.
	/// </summary>
	/// <param name="badgeView">The <see cref="BadgeView"/> instance on which the animation will be applied</param>
	/// <returns>An awaitable <see cref="Task"/></returns>
	public Task OnAppearing(VisualElement badgeView)
	{
		translationY ??= badgeView.TranslationY;

		var tcs = new TaskCompletionSource<bool>();

		var appearingAnimation = new Animation();

		appearingAnimation.WithConcurrent(
			(f) => badgeView.Opacity = f,
			0, 1, Easing.CubicOut);

		appearingAnimation.WithConcurrent(
			(f) => badgeView.TranslationY = f,
			translationY.Value + Offset, translationY.Value);

		appearingAnimation.Commit(badgeView, nameof(OnAppearing), length: AnimationLength,
			finished: (v, t) => tcs.SetResult(true));

		return tcs.Task;
	}

	/// <summary>
	/// With the <see cref="OnDisappearing(VisualElement)"/> method you can influence the animation that is used when the <see cref="BadgeView"/> disappears.
	/// </summary>
	/// <param name="badgeView">The <see cref="BadgeView"/> instance on which the animation will be applied</param>
	/// <returns>An awaitable <see cref="Task"/></returns>
	public Task OnDisappearing(VisualElement badgeView)
	{
		translationY ??= badgeView.TranslationY;

		var tcs = new TaskCompletionSource<bool>();

		var disappearingAnimation = new Animation();

		disappearingAnimation.WithConcurrent(
			(f) => badgeView.Opacity = f,
			1, 0);

		disappearingAnimation.WithConcurrent(
			(f) => badgeView.TranslationY = f,
			translationY.Value, translationY.Value + Offset);

		disappearingAnimation.Commit(badgeView, nameof(OnAppearing), length: AnimationLength,
			finished: (v, t) => tcs.SetResult(true));

		return tcs.Task;
	}
}