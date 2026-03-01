using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// A behavior that adds smooth expand and collapse animations to an <see cref="Views.Expander"/>.
/// </summary>
public partial class ExpanderAnimationBehavior : BaseBehavior<Views.Expander>, IExpansionController
{
	/// <summary>
	/// Gets or sets the easing function used when the expander collapses.
	/// </summary>
	[BindableProperty]
	public partial Easing CollapsingEasing { get; set; } = ExpanderAnimationBehaviorDefaults.CollapsingEasing;

	/// <summary>
	/// Gets or sets the duration, in milliseconds, of the collapse animation.
	/// </summary>
	[BindableProperty]
	public partial uint CollapsingLength { get; set; } = ExpanderAnimationBehaviorDefaults.CollapsingLength;

	/// <summary>
	/// Gets or sets the easing function used when the expander expands.
	/// </summary>
	[BindableProperty]
	public partial Easing ExpandingEasing { get; set; } = ExpanderAnimationBehaviorDefaults.ExpandingEasing;

	/// <summary>
	/// Gets or sets the duration, in milliseconds, of the expansion animation.
	/// </summary>
	[BindableProperty]
	public partial uint ExpandingLength { get; set; } = ExpanderAnimationBehaviorDefaults.ExpandingLength;

	/// <summary>
	/// Attaches the behavior to the specified expander and assigns it as the controller responsible for handling expansion animations.
	/// </summary>
	/// <param name="bindable">The Expander control to which the behavior is being attached to.</param>
	protected override void OnAttachedTo(Views.Expander bindable)
	{
		base.OnAttachedTo(bindable);
		bindable.ExpansionController = this;
	}

	/// <summary>
	/// Detaches the behavior from the specified Expander control and resets its expansion controller to the shared instance.
	/// </summary>
	/// <param name="bindable">The Expander control from which the behavior is being detached from.</param>
	protected override void OnDetachingFrom(Views.Expander bindable)
	{
		base.OnDetachingFrom(bindable);
		bindable.ExpansionController = InstantExpansionController.Instance;
	}

	/// <summary>
	/// Performs the animation that runs when the expander transitions from a collapsed to an expanded state.
	/// </summary>
	/// <param name="expander">The Expander control that is expanding.</param>
	public async Task OnExpandingAsync(Views.Expander expander)
	{
		if (expander.ContentHost is ContentView host && expander.Content is View view)
		{
			var tcs = new TaskCompletionSource();
			var size = view.Measure(host.Width, double.PositiveInfinity);
			var animation = new Animation(v => host.HeightRequest = v, 0, size.Height);
			animation.Commit(expander, "ExpandingAnimation", 16, ExpandingLength, ExpandingEasing, (v, c) => tcs.TrySetResult());
			host.HeightRequest = -1;
			await tcs.Task;
		}
	}

	/// <summary>
	/// Performs the animation that runs when the expander transitions from an expanded to a collapsed state.
	/// </summary>
	/// <param name="expander">The Expander control that is collapsing.</param>
	public async Task OnCollapsingAsync(Views.Expander expander)
	{
		if (expander.ContentHost is ContentView host && expander.Content is View view)
		{
			var tcs = new TaskCompletionSource();
			var size = view.Measure(host.Width, double.PositiveInfinity);
			var animation = new Animation(v => host.HeightRequest = v, size.Height, 0);
			animation.Commit(expander, "CollapsingAnimation", 16, CollapsingLength, CollapsingEasing, (v, c) => tcs.TrySetResult());
			host.HeightRequest = 0;
			await tcs.Task;
		}
	}
}
