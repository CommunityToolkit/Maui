namespace CommunityToolkit.Maui.Views.BadgeView;

/// <summary>
/// You can implement this interface to create your own animations to be used on the <see cref="BadgeView"/>. Create an implementation of <see cref="IBadgeAnimation"/>, assign the implemenatation to the <see cref="BadgeView.BadgeAnimation"/> and set <see cref="BadgeView.IsAnimated"/> to true.
/// </summary>
public interface IBadgeAnimation
{
	/// <summary>
	/// With the <see cref="OnAppearing(View)"/> method you can influence the animation that is used when the <see cref="BadgeView"/> appears.
	/// </summary>
	/// <param name="badgeView">The <see cref="BadgeView"/> instance on which the animation will be applied</param>
	/// <returns>An awaitable <see cref="Task"/></returns>
	Task OnAppearing(View badgeView);

	/// <summary>
	/// With the <see cref="OnDisappearing"/> method you can influence the animation that is used when the <see cref="BadgeView"/> disappears.
	/// </summary>
	/// <param name="badgeView">The <see cref="BadgeView"/> instance on which the animation will be applied</param>
	/// <returns>An awaitable <see cref="Task"/></returns>
	Task OnDisappearing(View badgeView);
}