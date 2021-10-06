using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.UI.Views
{
    /// <summary>
    /// This is the default animation that is used when the <see cref="BadgeView.IsAnimated"/> is set to true.
    /// </summary>
    public class BadgeAnimation : IBadgeAnimation
    {
        protected uint AnimationLength { get; } = 150;

        protected uint Offset { get; } = 24;

        double? translationY;

        /// <summary>
        /// With the <see cref="OnAppearing(View)"/> method you can influence the animation that is used when the <see cref="BadgeView"/> appears.
        /// </summary>
        /// <param name="badgeView">The <see cref="BadgeView"/> instance on which the animation will be applied</param>
        /// <returns>An awaitable <see cref="Task"/></returns>
        public Task OnAppearing(View badgeView)
        {
            if (translationY == null)
                translationY = badgeView.TranslationY;

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
        /// With the <see cref="OnDisappering(View)"/> method you can influence the animation that is used when the <see cref="BadgeView"/> disappears.
        /// </summary>
        /// <param name="badgeView">The <see cref="BadgeView"/> instance on which the animation will be applied</param>
        /// <returns>An awaitable <see cref="Task"/></returns>
        public Task OnDisappering(View badgeView)
        {
            if (translationY == null)
                translationY = badgeView.TranslationY;

            var tcs = new TaskCompletionSource<bool>();

            var disapperingAnimation = new Animation();

            disapperingAnimation.WithConcurrent(
                (f) => badgeView.Opacity = f,
                1, 0);

            disapperingAnimation.WithConcurrent(
                (f) => badgeView.TranslationY = f,
                translationY.Value, translationY.Value + Offset);

            disapperingAnimation.Commit(badgeView, nameof(OnAppearing), length: AnimationLength,
                finished: (v, t) => tcs.SetResult(true));

            return tcs.Task;
        }
    }
}