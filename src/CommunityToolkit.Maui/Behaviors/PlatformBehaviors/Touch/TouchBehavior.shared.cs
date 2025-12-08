using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// <see cref="PlatformBehavior{TView,TPlatformView}"/> that adds additional actions for user interactions 
/// </summary>
public partial class TouchBehavior : BasePlatformBehavior<VisualElement>
{
	/// <summary>
	/// The visual state for when the <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	public const string UnpressedVisualState = "Unpressed";

	/// <summary>
	/// The visual state for when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	public const string PressedVisualState = "Pressed";

	/// <summary>
	/// The visual state for when the <see cref="HoverState"/> is <see cref="HoverState.Hovered"/>.
	/// </summary>
	public const string HoveredVisualState = "Hovered";

	readonly WeakEventManager weakEventManager = new();
	readonly GestureManager gestureManager = new();

	/// <summary>
	/// Fires when <see cref="CurrentTouchStatus"/> changes.
	/// </summary>
	public event EventHandler<TouchStatusChangedEventArgs> CurrentTouchStatusChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Fires when <see cref="CurrentTouchState"/> changes.
	/// </summary>
	public event EventHandler<TouchStateChangedEventArgs> CurrentTouchStateChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Fires when <see cref="CurrentInteractionStatus"/> changes.
	/// </summary>
	public event EventHandler<TouchInteractionStatusChangedEventArgs> InteractionStatusChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Fires when <see cref="CurrentHoverStatus"/> changes.
	/// </summary>
	public event EventHandler<HoverStatusChangedEventArgs> HoverStatusChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Fires when <see cref="CurrentHoverState"/> changes.
	/// </summary>
	public event EventHandler<HoverStateChangedEventArgs> HoverStateChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Fires when a touch gesture has completed.
	/// </summary>
	public event EventHandler<TouchGestureCompletedEventArgs> TouchGestureCompleted
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Fires when a long press gesture has completed.
	/// </summary>
	public event EventHandler<LongPressCompletedEventArgs> LongPressCompleted
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the behavior is enabled.
	/// </summary>
	[BindableProperty]
	public partial bool IsEnabled { get; set; } = TouchBehaviorDefaults.IsEnabled;

	/// <summary>
	/// Gets or sets a value indicating whether the children of the element should be made input transparent.
	/// </summary>
	[BindableProperty]
	public partial bool ShouldMakeChildrenInputTransparent { get; set; } = TouchBehaviorDefaults.ShouldMakeChildrenInputTransparent;

	/// <summary>
	/// Gets or sets the <see cref="ICommand"/> to invoke when the user has completed a touch gesture.
	/// </summary>
	[BindableProperty]
	public partial ICommand? Command { get; set; }

	/// <summary>
	/// Gets or sets the parameter to pass to the <see cref="Command"/> property.
	/// </summary>
	[BindableProperty]
	public partial object? CommandParameter { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ICommand"/> to invoke when the user has completed a long press.
	/// </summary>
	[BindableProperty]
	public partial ICommand? LongPressCommand { get; set; }

	/// <summary>
	/// Gets or sets the parameter to pass to the <see cref="LongPressCommand"/> property.
	/// </summary>
	[BindableProperty]
	public partial object? LongPressCommandParameter { get; set; }

	/// <summary>
	/// Gets or sets the duration required to trigger the long press gesture.
	/// </summary>
	[BindableProperty]
	public partial int LongPressDuration { get; set; } = TouchBehaviorDefaults.LongPressDuration;

	/// <summary>
	/// Gets the current <see cref="TouchStatus"/> of the behavior.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(RaiseCurrentTouchStatusChanged), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial TouchStatus CurrentTouchStatus { get; set; } = TouchBehaviorDefaults.CurrentTouchStatus;

	/// <summary>
	/// Gets the current <see cref="TouchState"/> of the behavior.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangedMethodName = nameof(RaiseCurrentTouchStateChanged))]
	public partial TouchState CurrentTouchState { get; set; } = TouchBehaviorDefaults.CurrentTouchState;

	/// <summary>
	/// Gets the current <see cref="TouchInteractionStatus"/> of the behavior.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangedMethodName = nameof(RaiseInteractionStatusChanged))]
	public partial TouchInteractionStatus CurrentInteractionStatus { get; set; } = TouchBehaviorDefaults.CurrentInteractionStatus;

	/// <summary>
	/// Gets the current <see cref="HoverStatus"/> of the behavior.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangedMethodName = nameof(RaiseHoverStatusChanged))]
	public partial HoverStatus CurrentHoverStatus { get; set; } = TouchBehaviorDefaults.CurrentHoverStatus;

	/// <summary>
	/// Gets the current <see cref="HoverState"/> of the behavior.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangedMethodName = nameof(RaiseHoverStateChanged))]
	public partial HoverState CurrentHoverState { get; set; } = TouchBehaviorDefaults.CurrentHoverState;

	/// <summary>
	/// Gets or sets the background color of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial Color? DefaultBackgroundColor { get; set; } = TouchBehaviorDefaults.DefaultBackgroundColor;

	/// <summary>
	/// Gets or sets the background color of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial Color? HoveredBackgroundColor { get; set; } = TouchBehaviorDefaults.HoveredBackgroundColor;

	/// <summary>
	/// Gets or sets the background color of the element when the <see cref="TouchState" /> is <see cref="TouchState.Pressed" />.
	/// </summary>
	[BindableProperty]
	public partial Color? PressedBackgroundColor { get; set; } = TouchBehaviorDefaults.PressedBackgroundColor;

	/// <summary>
	/// Gets or sets the opacity of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	[BindableProperty(PropertyChangingMethodName = nameof(HandleDefaultOpacityChanging))]
	public partial double DefaultOpacity { get; set; } = TouchBehaviorDefaults.DefaultOpacity;

	/// <summary>
	/// Gets or sets the opacity of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(PropertyChangingMethodName = nameof(HandleHoveredOpacityChanging))]
	public partial double HoveredOpacity { get; set; } = TouchBehaviorDefaults.HoveredOpacity;

	/// <summary>
	/// Gets or sets the opacity of the element when the <see cref="TouchState" /> is <see cref="TouchState.Pressed" />.
	/// </summary>
	[BindableProperty(PropertyChangingMethodName = nameof(HandlePressedOpacityChanging))]
	public partial double PressedOpacity { get; set; } = TouchBehaviorDefaults.PressedOpacity;

	/// <summary>
	/// Gets or sets the scale of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double DefaultScale { get; set; } = TouchBehaviorDefaults.DefaultScale;

	/// <summary>
	/// Gets or sets the scale of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double HoveredScale { get; set; } = TouchBehaviorDefaults.HoveredScale;

	/// <summary>
	/// Gets or sets the scale of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double PressedScale { get; set; } = TouchBehaviorDefaults.PressedScale;

	/// <summary>
	/// Gets or sets the translation X of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double DefaultTranslationX { get; set; } = TouchBehaviorDefaults.DefaultTranslationX;

	/// <summary>
	/// Gets or sets the translation X of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double HoveredTranslationX { get; set; } = TouchBehaviorDefaults.HoveredTranslationX;

	/// <summary>
	/// Gets or sets the translation X of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double PressedTranslationX { get; set; } = TouchBehaviorDefaults.PressedTranslationX;

	/// <summary>
	/// Gets or sets the translation Y of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double DefaultTranslationY { get; set; } = TouchBehaviorDefaults.DefaultTranslationY;

	/// <summary>
	/// Gets or sets the translation Y of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double HoveredTranslationY { get; set; } = TouchBehaviorDefaults.HoveredTranslationY;

	/// <summary>
	/// Gets or sets the translation Y of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double PressedTranslationY { get; set; } = TouchBehaviorDefaults.PressedTranslationY;

	/// <summary>
	/// Gets or sets the rotation of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double DefaultRotation { get; set; } = TouchBehaviorDefaults.DefaultRotation;

	/// <summary>
	/// Gets or sets the rotation of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double HoveredRotation { get; set; } = TouchBehaviorDefaults.HoveredRotation;

	/// <summary>
	/// Gets or sets the rotation of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double PressedRotation { get; set; } = TouchBehaviorDefaults.PressedRotation;

	/// <summary>
	/// Gets or sets the rotation X of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double DefaultRotationX { get; set; } = TouchBehaviorDefaults.DefaultRotationX;

	/// <summary>
	/// Gets or sets the rotation X of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double HoveredRotationX { get; set; } = TouchBehaviorDefaults.HoveredRotationX;

	/// <summary>
	/// Gets or sets the rotation X of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double PressedRotationX { get; set; } = TouchBehaviorDefaults.PressedRotationX;

	/// <summary>
	/// Gets or sets the rotation Y of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double DefaultRotationY { get; set; } = TouchBehaviorDefaults.DefaultRotationY;

	/// <summary>
	/// Gets or sets the rotation Y of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double HoveredRotationY { get; set; } = TouchBehaviorDefaults.HoveredRotationY;

	/// <summary>
	/// Gets or sets the rotation Y of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double PressedRotationY { get; set; } = TouchBehaviorDefaults.PressedRotationY;

	/// <summary>
	/// Gets or sets the duration of the animation when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial int PressedAnimationDuration { get; set; } = TouchBehaviorDefaults.PressedAnimationDuration;

	/// <summary>
	/// Gets or sets the easing of the animation when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial Easing? PressedAnimationEasing { get; set; } = TouchBehaviorDefaults.PressedAnimationEasing;

	/// <summary>
	/// Gets or sets the duration of the animation when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	[BindableProperty]
	public partial int DefaultAnimationDuration { get; set; } = TouchBehaviorDefaults.DefaultAnimationDuration;

	/// <summary>
	/// Gets or sets the <see cref="Easing"/> of the animation when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	[BindableProperty]
	public partial Easing? DefaultAnimationEasing { get; set; } = TouchBehaviorDefaults.DefaultAnimationEasing;

	/// <summary>
	/// Gets or sets the duration of the animation when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial int HoveredAnimationDuration { get; set; } = TouchBehaviorDefaults.HoveredAnimationDuration;

	/// <summary>
	/// Gets or sets the easing of the animation when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial Easing? HoveredAnimationEasing { get; set; } = TouchBehaviorDefaults.HoveredAnimationEasing;

	/// <summary>
	/// Gets or sets the threshold for disallowing touch.
	/// </summary>
	[BindableProperty]
	public partial int DisallowTouchThreshold { get; set; } = TouchBehaviorDefaults.DisallowTouchThreshold;

	internal bool CanExecute => IsEnabled
		&& Element?.IsEnabled is true
		&& (Command?.CanExecute(CommandParameter) ?? true);

	internal VisualElement? Element
	{
		get => View;
		set
		{
			if (View is not null)
			{
				gestureManager.Reset();
				SetChildrenInputTransparent(false);
			}
			gestureManager.AbortAnimations(this, CancellationToken.None).SafeFireAndForget<TaskCanceledException>(ex => Trace.WriteLine(ex));
			View = value;

			if (value is not null)
			{
				SetChildrenInputTransparent(ShouldMakeChildrenInputTransparent);
				ForceUpdateState(CancellationToken.None, false).SafeFireAndForget<TaskCanceledException>(ex => Trace.WriteLine(ex));
			}
		}
	}
}