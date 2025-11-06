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
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.IsEnabled)]
	public partial bool IsEnabled { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the children of the element should be made input transparent.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.ShouldMakeChildrenInputTransparent)]
	public partial bool ShouldMakeChildrenInputTransparent { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ICommand"/> to invoke when the user has completed a touch gesture.
	/// </summary>
	[BindableProperty(DefaultValue = null)]
	public partial ICommand? Command { get; set; }
	
	/// <summary>
	/// Gets or sets the parameter to pass to the <see cref="Command"/> property.
	/// </summary>
	[BindableProperty(DefaultValue = null)]
	public partial object? CommandParameter { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="ICommand"/> to invoke when the user has completed a long press.
	/// </summary>
	[BindableProperty(DefaultValue = null)]
	public partial ICommand? LongPressCommand { get; set; }

	/// <summary>
	/// Gets or sets the parameter to pass to the <see cref="LongPressCommand"/> property.
	/// </summary>
	[BindableProperty(DefaultValue = null)]
	public partial object? LongPressCommandParameter { get; set; }

	/// <summary>
	/// Gets or sets the duration required to trigger the long press gesture.
	/// </summary>
	[BindableProperty(DefaultValue =TouchBehaviorDefaults.LongPressDuration)]
	public partial int LongPressDuration { get; set; }

	/// <summary>
	/// Gets the current <see cref="TouchStatus"/> of the behavior.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.CurrentTouchStatus, PropertyChangedMethodName = nameof(RaiseCurrentTouchStatusChanged), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial TouchStatus CurrentTouchStatus { get; set; }

	/// <summary>
	/// Gets the current <see cref="TouchState"/> of the behavior.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.CurrentTouchState, DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangedMethodName = nameof(RaiseCurrentTouchStateChanged))]
	public partial TouchState CurrentTouchState { get; set; }

	/// <summary>
	/// Gets the current <see cref="TouchInteractionStatus"/> of the behavior.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.CurrentInteractionStatus, DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangedMethodName = nameof(RaiseInteractionStatusChanged))]
	public partial TouchInteractionStatus CurrentInteractionStatus { get; set; }

	/// <summary>
	/// Gets the current <see cref="HoverStatus"/> of the behavior.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.CurrentHoverStatus, DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangedMethodName = nameof(RaiseHoverStatusChanged))]
	public partial HoverStatus CurrentHoverStatus { get; set; }

	/// <summary>
	/// Gets the current <see cref="HoverState"/> of the behavior.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.CurrentHoverState, DefaultBindingMode = BindingMode.OneWayToSource, PropertyChangedMethodName = nameof(RaiseHoverStateChanged))]
	public partial HoverState CurrentHoverState { get; set; }

	/// <summary>
	/// Gets or sets the background color of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultBackgroundColor)]
	public partial Color? DefaultBackgroundColor { get; set; }

	/// <summary>
	/// Gets or sets the background color of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.HoveredBackgroundColor)]
	public partial Color? HoveredBackgroundColor { get; set; }

	/// <summary>
	/// Gets or sets the background color of the element when the <see cref="TouchState" /> is <see cref="TouchState.Pressed" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedBackgroundColor)]
	public partial Color? PressedBackgroundColor { get; set; }

	/// <summary>
	/// Gets or sets the opacity of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultOpacity, PropertyChangingMethodName = nameof(HandleDefaultOpacityChanged))]
	public partial double DefaultOpacity { get; set; }

	/// <summary>
	/// Gets or sets the opacity of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.HoveredOpacity, PropertyChangingMethodName = nameof(HandleHoveredOpacityChanged))]
	public partial double HoveredOpacity { get; set; }

	/// <summary>
	/// Gets or sets the opacity of the element when the <see cref="TouchState" /> is <see cref="TouchState.Pressed" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedOpacity, PropertyChangingMethodName = nameof(HandlePressedOpacityChanged))]
	public partial double PressedOpacity { get; set; }

	/// <summary>
	/// Gets or sets the scale of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultScale)]
	public partial double DefaultScale { get; set; }

	/// <summary>
	/// Gets or sets the scale of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.HoveredScale)]
	public partial double HoveredScale { get; set; }

	/// <summary>
	/// Gets or sets the scale of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedScale)]
	public partial double PressedScale { get; set; }

	/// <summary>
	/// Gets or sets the translation X of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultTranslationX)]
	public partial double DefaultTranslationX { get; set; }

	/// <summary>
	/// Gets or sets the translation X of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.HoveredTranslationX)]
	public partial double HoveredTranslationX { get; set; }

	/// <summary>
	/// Gets or sets the translation X of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedTranslationX)]
	public partial double PressedTranslationX { get; set; }

	/// <summary>
	/// Gets or sets the translation Y of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultTranslationY)]
	public partial double DefaultTranslationY { get; set; }

	/// <summary>
	/// Gets or sets the translation Y of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = 0.0)]
	public partial double HoveredTranslationY { get; set; }

	/// <summary>
	/// Gets or sets the translation Y of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedTranslationY)]
	public partial double PressedTranslationY { get; set; }

	/// <summary>
	/// Gets or sets the rotation of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultRotation)]
	public partial double DefaultRotation { get; set; }

	/// <summary>
	/// Gets or sets the rotation of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.HoveredRotation)]
	public partial double HoveredRotation { get; set; }

	/// <summary>
	/// Gets or sets the rotation of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedRotation)]
	public partial double PressedRotation { get; set; }

	/// <summary>
	/// Gets or sets the rotation X of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultRotationX)]
	public partial double DefaultRotationX { get; set; }

	/// <summary>
	/// Gets or sets the rotation X of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.HoveredRotationX)]
	public partial double HoveredRotationX { get; set; }

	/// <summary>
	/// Gets or sets the rotation X of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedRotationX)]
	public partial double PressedRotationX { get; set; }

	/// <summary>
	/// Gets or sets the rotation Y of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultRotationY)]
	public partial double DefaultRotationY { get; set; }

	/// <summary>
	/// Gets or sets the rotation Y of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.HoveredRotationY)]
	public partial double HoveredRotationY { get; set; }

	/// <summary>
	/// Gets or sets the rotation Y of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedRotationY)]
	public partial double PressedRotationY { get; set; }

	/// <summary>
	/// Gets or sets the duration of the animation when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedAnimationDuration)]
	public partial int PressedAnimationDuration { get; set; }

	/// <summary>
	/// Gets or sets the easing of the animation when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.PressedAnimationEasing)]
	public partial Easing? PressedAnimationEasing { get; set; }

	/// <summary>
	/// Gets or sets the duration of the animation when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultAnimationDuration)]
	public partial int DefaultAnimationDuration { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="Easing"/> of the animation when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DefaultAnimationEasing)]
	public partial Easing? DefaultAnimationEasing { get; set; }

	/// <summary>
	/// Gets or sets the duration of the animation when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.HoveredAnimationDuration)] 
	public partial int HoveredAnimationDuration { get; set; }

	/// <summary>
	/// Gets or sets the easing of the animation when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.HoveredAnimationEasing)]
	public partial Easing? HoveredAnimationEasing { get; set; }

	/// <summary>
	/// Gets or sets the threshold for disallowing touch.
	/// </summary>
	[BindableProperty(DefaultValue = TouchBehaviorDefaults.DisallowTouchThreshold)]
	public partial int DisallowTouchThreshold { get; set; }

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