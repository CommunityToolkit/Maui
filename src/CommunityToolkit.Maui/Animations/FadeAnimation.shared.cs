using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Animations;

/// <summary>
/// Animation that will fade the supplied view to the specified <see cref="Opacity"/>
/// and then back to its original <see cref="Opacity"/>.
/// </summary>
public partial class FadeAnimation() : BaseAnimation(FadeAnimationDefaults.Length)
{
	/// <summary>
	/// Gets or sets the opacity to fade to before returning to the elements current <see cref="VisualElement.Opacity"/>.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial double Opacity { get; set; } = FadeAnimationDefaults.Opacity;

	/// <inheritdoc />
	public override async Task Animate(VisualElement view, CancellationToken token = default)
	{
		ArgumentNullException.ThrowIfNull(view);
		token.ThrowIfCancellationRequested();

		var originalOpacity = view.Opacity;

		await view.FadeToAsync(Opacity, Length, Easing).WaitAsync(token);
		await view.FadeToAsync(originalOpacity, Length, Easing).WaitAsync(token);
	}
}