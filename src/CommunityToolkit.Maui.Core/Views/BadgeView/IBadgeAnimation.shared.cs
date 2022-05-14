using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Core.Views.BadgeView;

/// <summary>
/// You can implement this interface to create your own animations to be used on the <see cref="IBadgeView"/>. Create an implementation of <see cref="IBadgeAnimation"/>, assign the implemenatation to the <see cref="IBadgeView.BadgeAnimation"/> and set <see cref="IBadgeView.IsAnimated"/> to true.
/// </summary>
public interface IBadgeAnimation
{
	/// <summary>
	/// With the <see cref="OnAppearing(VisualElement)"/> method you can influence the animation that is used when the <see cref="IBadgeView"/> appears.
	/// </summary>
	/// <param name="badgeView">The <see cref="BadgeView"/> instance on which the animation will be applied</param>
	/// <returns>An awaitable <see cref="Task"/></returns>
	Task OnAppearing(VisualElement badgeView);

	/// <summary>
	/// With the <see cref="OnDisappearing"/> method you can influence the animation that is used when the <see cref="IBadgeView"/> disappears.
	/// </summary>
	/// <param name="badgeView">The <see cref="BadgeView"/> instance on which the animation will be applied</param>
	/// <returns>An awaitable <see cref="Task"/></returns>
	Task OnDisappearing(VisualElement badgeView);
}