using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// 
/// </summary>
public partial class TouchBehavior : PlatformBehavior<VisualElement>
{
	/// <summary>
	/// The visual state for when the touch is unpressed.
	/// </summary>
	public const string UnpressedVisualState = "Unpressed";

	/// <summary>
	/// The visual state for when the touch is pressed.
	/// </summary>
	public const string PressedVisualState = "Pressed";

	/// <summary>
	/// The visual state for when the touch is hovered.
	/// </summary>
	public const string HoveredVisualState = "Hovered";

	/// <summary>
	/// Bindable property for <see cref="IsEnabled"/>
	/// </summary>
	public static readonly BindableProperty IsEnabledProperty = BindableProperty.Create(
		nameof(IsEnabled),
		typeof(bool),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.IsEnabled);

	/// <summary>
	/// Bindable property for <see cref="Command"/>
	/// </summary>
	public static readonly BindableProperty CommandProperty = BindableProperty.Create(
		nameof(Command),
		typeof(ICommand),
		typeof(TouchBehavior),
		null);

	/// <summary>
	/// Bindable property for <see cref="ShouldMakeChildrenInputTransparent"/>
	/// </summary>
	public static readonly BindableProperty ShouldMakeChildrenInputTransparentProperty = BindableProperty.Create(
		nameof(ShouldMakeChildrenInputTransparent),
		typeof(bool),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.ShouldMakeChildrenInputTransparent);

	/// <summary>
	/// Bindable property for <see cref="DisallowTouchThreshold"/>
	/// </summary>
	public static readonly BindableProperty DisallowTouchThresholdProperty = BindableProperty.Create(
		nameof(DisallowTouchThreshold),
		typeof(int),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DisallowTouchThreshold);

	/// <summary>
	/// Bindable property for <see cref="ShouldUseNativeAnimation"/>
	/// </summary>
	public static readonly BindableProperty ShouldUseNativeAnimationProperty = BindableProperty.Create(
		nameof(ShouldUseNativeAnimation),
		typeof(bool),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.ShouldUseNativeAnimation);

	/// <summary>
	/// Bindable property for <see cref="NativeAnimationColor"/>
	/// </summary>
	public static readonly BindableProperty NativeAnimationColorProperty = BindableProperty.Create(
		nameof(NativeAnimationColor),
		typeof(Color),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.NativeAnimationColor);

	/// <summary>
	/// Bindable property for <see cref="NativeAnimationRadius"/>
	/// </summary>
	public static readonly BindableProperty NativeAnimationRadiusProperty = BindableProperty.Create(
		nameof(NativeAnimationRadius),
		typeof(int?),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.NativeAnimationRadius);

	/// <summary>
	/// Bindable property for <see cref="NativeAnimationShadowRadius"/>
	/// </summary>
	public static readonly BindableProperty NativeAnimationShadowRadiusProperty = BindableProperty.Create(
		nameof(NativeAnimationShadowRadius),
		typeof(int?),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.NativeAnimationShadowRadius);

	/// <summary>
	/// Bindable property for <see cref="IsNativeAnimationBorderless"/>
	/// </summary>
	public static readonly BindableProperty IsNativeAnimationBorderlessProperty = BindableProperty.Create(
		nameof(IsNativeAnimationBorderless),
		typeof(bool),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.IsNativeAnimationBorderless);

	/// <summary>
	/// Bindable property for <see cref="LongPressCommand"/>
	/// </summary>
	public static readonly BindableProperty LongPressCommandProperty = BindableProperty.Create(
		nameof(LongPressCommand),
		typeof(ICommand),
		typeof(TouchBehavior),
		null);

	/// <summary>
	/// Bindable property for <see cref="CurrentTouchStatus"/>
	/// </summary>
	public static readonly BindableProperty CurrentTouchStatusProperty = BindableProperty.Create(
		nameof(CurrentTouchStatus),
		typeof(TouchStatus),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.CurrentTouchStatus,
		BindingMode.OneWayToSource,
		propertyChanged: static (bindable, _, _) => ((TouchBehavior)bindable).RaiseCurrentTouchStatusChanged());

	/// <summary>
	/// Bindable property for <see cref="LongPressDuration"/>
	/// </summary>
	public static readonly BindableProperty LongPressDurationProperty = BindableProperty.Create(
		nameof(LongPressDuration),
		typeof(int),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.LongPressDuration);

	/// <summary>
	/// Bindable property for <see cref="LongPressCommandParameter"/>
	/// </summary>
	public static readonly BindableProperty LongPressCommandParameterProperty = BindableProperty.Create(
		nameof(LongPressCommandParameter),
		typeof(object),
		typeof(TouchBehavior),
		default);

	/// <summary>
	/// Bindable property for <see cref="CurrentHoverStatus"/>
	/// </summary>
	public static readonly BindableProperty CurrentHoverStatusProperty = BindableProperty.Create(
		nameof(CurrentHoverStatus),
		typeof(HoverStatus),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.CurrentHoverStatus,
		BindingMode.OneWayToSource,
		propertyChanged: static (bindable, _, _) => ((TouchBehavior)bindable).RaiseHoverStatusChanged());

	/// <summary>
	/// Bindable property for <see cref="CurrentInteractionStatus"/>
	/// </summary>
	public static readonly BindableProperty CurrentInteractionStatusProperty = BindableProperty.Create(
		nameof(CurrentInteractionStatus),
		typeof(TouchInteractionStatus),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.CurrentInteractionStatus,
		BindingMode.OneWayToSource,
		propertyChanged: static (bindable, _, _) => ((TouchBehavior)bindable).RaiseInteractionStatusChanged());

	/// <summary>
	/// Bindable property for <see cref="CurrentTouchState"/>
	/// </summary>
	public static readonly BindableProperty CurrentTouchStateProperty = BindableProperty.Create(
		nameof(CurrentTouchState),
		typeof(TouchState),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.CurrentTouchState,
		BindingMode.OneWayToSource,
		propertyChanged: static async (bindable, _, _) => await ((TouchBehavior)bindable).RaiseCurrentTouchStateChanged(CancellationToken.None));

	/// <summary>
	/// Bindable property for <see cref="DefaultBackgroundColor"/>
	/// </summary>
	public static readonly BindableProperty DefaultBackgroundColorProperty = BindableProperty.Create(
		nameof(DefaultBackgroundColor),
		typeof(Color),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultBackgroundColor);

	/// <summary>
	/// Bindable property for <see cref="CurrentHoverState"/>
	/// </summary>
	public static readonly BindableProperty CurrentHoverStateProperty = BindableProperty.Create(
		nameof(CurrentHoverState),
		typeof(HoverState),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.CurrentHoverState,
		BindingMode.OneWayToSource,
		propertyChanged: static async (bindable, _, _) => await ((TouchBehavior)bindable).RaiseHoverStateChanged(CancellationToken.None));

	/// <summary>
	/// Bindable property for <see cref="CommandParameter"/>
	/// </summary>
	public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
		nameof(CommandParameter),
		typeof(object),
		typeof(TouchBehavior),
		default);

	/// <summary>
	/// Bindable property for <see cref="DefaultScale"/>
	/// </summary>
	public static readonly BindableProperty DefaultScaleProperty = BindableProperty.Create(
		nameof(DefaultScale),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultScale);

	/// <summary>
	/// Bindable property for <see cref="PressedOpacity"/>
	/// </summary>
	public static readonly BindableProperty PressedOpacityProperty = BindableProperty.Create(
		nameof(PressedOpacity),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedOpacity);

	/// <summary>
	/// Bindable property for <see cref="HoveredOpacity"/>
	/// </summary>
	public static readonly BindableProperty HoveredOpacityProperty = BindableProperty.Create(
		nameof(HoveredOpacity),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.HoveredOpacity);

	/// <summary>
	/// Bindable property for <see cref="DefaultOpacity"/>
	/// </summary>
	public static readonly BindableProperty DefaultOpacityProperty = BindableProperty.Create(
		nameof(DefaultOpacity),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultOpacity);

	/// <summary>
	/// Bindable property for <see cref="PressedScale"/>
	/// </summary>
	public static readonly BindableProperty PressedScaleProperty = BindableProperty.Create(
		nameof(PressedScale),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedScale);

	/// <summary>
	/// Bindable property for <see cref="HoveredScale"/>
	/// </summary>
	public static readonly BindableProperty HoveredScaleProperty = BindableProperty.Create(
		nameof(HoveredScale),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.HoveredScale);

	/// <summary>
	/// Bindable property for <see cref="PressedBackgroundColor"/>
	/// </summary>
	public static readonly BindableProperty PressedBackgroundColorProperty = BindableProperty.Create(
		nameof(PressedBackgroundColor),
		typeof(Color),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedBackgroundColor);

	/// <summary>
	/// Bindable property for <see cref="HoveredBackgroundColor"/>
	/// </summary>
	public static readonly BindableProperty HoveredBackgroundColorProperty = BindableProperty.Create(
		nameof(HoveredBackgroundColor),
		typeof(Color),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.HoveredBackgroundColor);

	/// <summary>
	/// Bindable property for <see cref="PressedTranslationX"/>
	/// </summary>
	public static readonly BindableProperty PressedTranslationXProperty = BindableProperty.Create(
		nameof(PressedTranslationX),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedTranslationX);

	/// <summary>
	/// Bindable property for <see cref="HoveredTranslationX"/>
	/// </summary>
	public static readonly BindableProperty HoveredTranslationXProperty = BindableProperty.Create(
		nameof(HoveredTranslationX),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.HoveredTranslationX);

	/// <summary>
	/// Bindable property for <see cref="DefaultTranslationX"/>
	/// </summary>
	public static readonly BindableProperty DefaultTranslationXProperty = BindableProperty.Create(
		nameof(DefaultTranslationX),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultTranslationX);

	/// <summary>
	/// Bindable property for <see cref="PressedRotationX"/>
	/// </summary>
	public static readonly BindableProperty PressedRotationXProperty = BindableProperty.Create(
		nameof(PressedRotationX),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedRotationX);

	/// <summary>
	/// Bindable property for <see cref="HoveredRotationX"/>
	/// </summary>
	public static readonly BindableProperty HoveredRotationXProperty = BindableProperty.Create(
		nameof(HoveredRotationX),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.HoveredRotationX);

	/// <summary>
	/// Bindable property for <see cref="DefaultRotationX"/>
	/// </summary>
	public static readonly BindableProperty DefaultRotationXProperty = BindableProperty.Create(
		nameof(DefaultRotationX),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultRotationX);

	/// <summary>
	/// Bindable property for <see cref="PressedRotation"/>
	/// </summary>
	public static readonly BindableProperty PressedRotationProperty = BindableProperty.Create(
		nameof(PressedRotation),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedRotation);

	/// <summary>
	/// Bindable property for <see cref="AnimationEasing"/>
	/// </summary>
	public static readonly BindableProperty AnimationEasingProperty = BindableProperty.Create(
		nameof(AnimationEasing),
		typeof(Easing),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.AnimationEasing);

	/// <summary>
	/// Bindable property for <see cref="AnimationDuration"/>
	/// </summary>
	public static readonly BindableProperty AnimationDurationProperty = BindableProperty.Create(
		nameof(AnimationDuration),
		typeof(int),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.AnimationDuration);

	/// <summary>
	/// Bindable property for <see cref="PressedRotationY"/>
	/// </summary>
	public static readonly BindableProperty PressedRotationYProperty = BindableProperty.Create(
		nameof(PressedRotationY),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedRotationY);

	/// <summary>
	/// Bindable property for <see cref="DefaultRotationY"/>
	/// </summary>
	public static readonly BindableProperty DefaultRotationYProperty = BindableProperty.Create(
		nameof(DefaultRotationY),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultRotationY);

	/// <summary>
	/// Bindable property for <see cref="HoveredRotation"/>
	/// </summary>
	public static readonly BindableProperty HoveredRotationProperty = BindableProperty.Create(
		nameof(HoveredRotation),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.HoveredRotation);

	/// <summary>
	/// Bindable property for <see cref="DefaultRotation"/>
	/// </summary>
	public static readonly BindableProperty DefaultRotationProperty = BindableProperty.Create(
		nameof(DefaultRotation),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultRotation);

	/// <summary>
	/// Bindable property for <see cref="PressedTranslationY"/>
	/// </summary>
	public static readonly BindableProperty PressedTranslationYProperty = BindableProperty.Create(
		nameof(PressedTranslationY),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedTranslationY);

	/// <summary>
	/// Bindable property for <see cref="RepeatAnimationCount"/>
	/// </summary>
	public static readonly BindableProperty RepeatAnimationCountProperty = BindableProperty.Create(
		nameof(RepeatAnimationCount),
		typeof(int),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.RepeatAnimationCount);

	/// <summary>
	/// Bindable property for <see cref="HoveredAnimationEasing"/>
	/// </summary>
	public static readonly BindableProperty HoveredAnimationEasingProperty = BindableProperty.Create(
		nameof(HoveredAnimationEasing),
		typeof(Easing),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.HoveredAnimationEasing);

	/// <summary>
	/// Bindable property for <see cref="HoveredRotationY"/>
	/// </summary>
	public static readonly BindableProperty HoveredRotationYProperty = BindableProperty.Create(
		nameof(HoveredRotationY),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.HoveredRotationY);

	/// <summary>
	/// Bindable property for <see cref="DefaultTranslationY"/>
	/// </summary>
	public static readonly BindableProperty DefaultTranslationYProperty = BindableProperty.Create(
		nameof(DefaultTranslationY),
		typeof(double),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultTranslationY);

	/// <summary>
	/// Bindable property for <see cref="PressedAnimationDuration"/>
	/// </summary>
	public static readonly BindableProperty PressedAnimationDurationProperty = BindableProperty.Create(
		nameof(PressedAnimationDuration),
		typeof(int),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedAnimationDuration);

	/// <summary>
	/// Bindable property for <see cref="HoveredAnimationDuration"/>
	/// </summary>
	public static readonly BindableProperty HoveredAnimationDurationProperty = BindableProperty.Create(
		nameof(HoveredAnimationDuration),
		typeof(int),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.HoveredAnimationDuration);

	/// <summary>
	/// Bindable property for <see cref="DefaultAnimationEasing"/>
	/// </summary>
	public static readonly BindableProperty DefaultAnimationEasingProperty = BindableProperty.Create(
		nameof(DefaultAnimationEasing),
		typeof(Easing),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultAnimationEasing);

	/// <summary>
	/// Bindable property for <see cref="DefaultAnimationDuration"/>
	/// </summary>
	public static readonly BindableProperty DefaultAnimationDurationProperty = BindableProperty.Create(
		nameof(DefaultAnimationDuration),
		typeof(int),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.DefaultAnimationDuration);

	/// <summary>
	/// Bindable property for <see cref="PressedAnimationEasing"/>
	/// </summary>
	public static readonly BindableProperty PressedAnimationEasingProperty = BindableProperty.Create(
		nameof(PressedAnimationEasing),
		typeof(Easing),
		typeof(TouchBehavior),
		TouchBehaviorDefaults.PressedAnimationEasing);

	readonly WeakEventManager weakEventManager = new();
	readonly GestureManager gestureManager = new();
	
	VisualElement? element;

	/// <summary>
	/// Occurs when the touch status changes.
	/// </summary>
	public event EventHandler<TouchStatusChangedEventArgs> CurrentTouchStatusChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Occurs when the touch state changes.
	/// </summary>
	public event EventHandler<TouchStateChangedEventArgs> CurrentTouchStateChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Occurs when the touch interaction status changes.
	/// </summary>
	public event EventHandler<TouchInteractionStatusChangedEventArgs> InteractionStatusChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Occurs when the hover status changes.
	/// </summary>
	public event EventHandler<HoverStatusChangedEventArgs> HoverStatusChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Occurs when the hover state changes.
	/// </summary>
	public event EventHandler<HoverStateChangedEventArgs> HoverStateChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Occurs when a touch gesture is completed.
	/// </summary>
	public event EventHandler<TouchGestureCompletedEventArgs> TouchGestureCompleted
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Occurs when a long press gesture is completed.
	/// </summary>
	public event EventHandler<LongPressCompletedEventArgs> LongPressCompleted
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the touch is available.
	/// </summary>
	public bool IsEnabled
	{
		get => (bool)GetValue(IsEnabledProperty);
		set => SetValue(IsEnabledProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the children of the element should be made input transparent.
	/// </summary>
	public bool ShouldMakeChildrenInputTransparent
	{
		get => (bool)GetValue(ShouldMakeChildrenInputTransparentProperty);
		set => SetValue(ShouldMakeChildrenInputTransparentProperty, value);
	}

	/// <summary>
	/// Gets or sets the command to invoke when the user has completed a touch gesture.
	/// </summary>
	public ICommand? Command
	{
		get => (ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	/// <summary>
	/// Gets or sets the command to invoke when the user has completed a long press.
	/// </summary>
	public ICommand? LongPressCommand
	{
		get => (ICommand?)GetValue(LongPressCommandProperty);
		set => SetValue(LongPressCommandProperty, value);
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the <see cref="Command"/> property.
	/// </summary>
	public object? CommandParameter
	{
		get => (object?)GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
	}

	/// <summary>
	/// Gets or sets the parameter to pass to the <see cref="LongPressCommand"/> property.
	/// </summary>
	public object? LongPressCommandParameter
	{
		get => (object?)GetValue(LongPressCommandParameterProperty);
		set => SetValue(LongPressCommandParameterProperty, value);
	}

	/// <summary>
	/// Gets or sets the duration of the long press.
	/// </summary>
	public int LongPressDuration
	{
		get => (int)GetValue(LongPressDurationProperty);
		set => SetValue(LongPressDurationProperty, value);
	}

	/// <summary>
	/// Gets the current status of the touch.
	/// </summary>
	public TouchStatus CurrentTouchStatus
	{
		get => (TouchStatus)GetValue(CurrentTouchStatusProperty);
		set => SetValue(CurrentTouchStatusProperty, value);
	}

	/// <summary>
	/// Gets the current state of the touch.
	/// </summary>
	public TouchState CurrentTouchState
	{
		get => (TouchState)GetValue(CurrentTouchStateProperty);
		set => SetValue(CurrentTouchStateProperty, value);
	}

	/// <summary>
	/// Gets the current interaction status of the touch.
	/// </summary>
	public TouchInteractionStatus CurrentInteractionStatus
	{
		get => (TouchInteractionStatus)GetValue(CurrentInteractionStatusProperty);
		set => SetValue(CurrentInteractionStatusProperty, value);
	}

	/// <summary>
	/// Gets the current hover status of the touch.
	/// </summary>
	public HoverStatus CurrentHoverStatus
	{
		get => (HoverStatus)GetValue(CurrentHoverStatusProperty);
		set => SetValue(CurrentHoverStatusProperty, value);
	}

	/// <summary>
	/// Gets the current hover state of the touch.
	/// </summary>
	public HoverState CurrentHoverState
	{
		get => (HoverState)GetValue(CurrentHoverStateProperty);
		set => SetValue(CurrentHoverStateProperty, value);
	}

	/// <summary>
	/// Gets or sets the background color of the element when when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	public Color? DefaultBackgroundColor
	{
		get => (Color?)GetValue(DefaultBackgroundColorProperty);
		set => SetValue(DefaultBackgroundColorProperty, value);
	}

	/// <summary>
	/// Gets or sets the background color of the element when the touch is in the hovered state.
	/// </summary>
	public Color? HoveredBackgroundColor
	{
		get => (Color?)GetValue(HoveredBackgroundColorProperty);
		set => SetValue(HoveredBackgroundColorProperty, value);
	}

	/// <summary>
	/// Gets or sets the background color of the element when the touch is in the pressed state.
	/// </summary>
	public Color? PressedBackgroundColor
	{
		get => (Color?)GetValue(PressedBackgroundColorProperty);
		set => SetValue(PressedBackgroundColorProperty, value);
	}

	/// <summary>
	/// Gets or sets the opacity of the element when when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public double DefaultOpacity
	{
		get => (double)GetValue(DefaultOpacityProperty);
		set
		{
			switch (value)
			{
				case < 0:
					throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(DefaultOpacity)} must be greater than 0");
				case > 1:
					throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(DefaultOpacity)} must be less than 1");
				default:
					SetValue(DefaultOpacityProperty, value);
					break;
			}
		}
	}

	/// <summary>
	/// Gets or sets the opacity of the element when the touch is in the hovered state.
	/// </summary>
	public double HoveredOpacity
	{
		get => (double)GetValue(HoveredOpacityProperty);
		set
		{
			switch (value)
			{
				case < 0:
					throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(HoveredOpacity)} must be greater than 0");
				case > 1:
					throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(HoveredOpacity)} must be less than 1");
				default:
					SetValue(HoveredOpacityProperty, value);
					break;
			}
		}
	}

	/// <summary>
	/// Gets or sets the opacity of the element when the touch is in the pressed state.
	/// </summary>
	public double PressedOpacity
	{
		get => (double)GetValue(PressedOpacityProperty);
		set
		{
			switch (value)
			{
				case < 0:
					throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(PressedOpacity)} must be greater than 0");
				case > 1:
					throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(PressedOpacity)} must be less than 1");
				default:
					SetValue(PressedOpacityProperty, value);
					break;
			}
		}
	}

	/// <summary>
	/// Gets or sets the scale of the element when when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	public double DefaultScale
	{
		get => (double)GetValue(DefaultScaleProperty);
		set => SetValue(DefaultScaleProperty, value);
	}

	/// <summary>
	/// Gets or sets the scale of the element when the touch is in the hovered state.
	/// </summary>
	public double HoveredScale
	{
		get => (double)GetValue(HoveredScaleProperty);
		set => SetValue(HoveredScaleProperty, value);
	}

	/// <summary>
	/// Gets or sets the scale of the element when the touch is in the pressed state.
	/// </summary>
	public double PressedScale
	{
		get => (double)GetValue(PressedScaleProperty);
		set => SetValue(PressedScaleProperty, value);
	}

	/// <summary>
	/// Gets or sets the translation X of the element when when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	public double DefaultTranslationX
	{
		get => (double)GetValue(DefaultTranslationXProperty);
		set => SetValue(DefaultTranslationXProperty, value);
	}

	/// <summary>
	/// Gets or sets the translation X of the element when the touch is in the hovered state.
	/// </summary>
	public double HoveredTranslationX
	{
		get => (double)GetValue(HoveredTranslationXProperty);
		set => SetValue(HoveredTranslationXProperty, value);
	}

	/// <summary>
	/// Gets or sets the translation X of the element when the touch is in the pressed state.
	/// </summary>
	public double PressedTranslationX
	{
		get => (double)GetValue(PressedTranslationXProperty);
		set => SetValue(PressedTranslationXProperty, value);
	}

	/// <summary>
	/// Gets or sets the translation Y of the element when when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	public double DefaultTranslationY
	{
		get => (double)GetValue(DefaultTranslationYProperty);
		set => SetValue(DefaultTranslationYProperty, value);
	}

	/// <summary>
	/// Bindable property for <see cref="HoveredTranslationY"/>
	/// </summary>
	public static readonly BindableProperty HoveredTranslationYProperty = BindableProperty.Create(
		nameof(HoveredTranslationY),
		typeof(double),
		typeof(TouchBehavior),
		0.0);

	/// <summary>
	/// Gets or sets the translation Y of the element when the touch is in the hovered state.
	/// </summary>
	public double HoveredTranslationY
	{
		get => (double)GetValue(HoveredTranslationYProperty);
		set => SetValue(HoveredTranslationYProperty, value);
	}

	/// <summary>
	/// Gets or sets the translation Y of the element when the touch is in the pressed state.
	/// </summary>
	public double PressedTranslationY
	{
		get => (double)GetValue(PressedTranslationYProperty);
		set => SetValue(PressedTranslationYProperty, value);
	}

	/// <summary>
	/// Gets or sets the rotation of the element when when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	public double DefaultRotation
	{
		get => (double)GetValue(DefaultRotationProperty);
		set => SetValue(DefaultRotationProperty, value);
	}

	/// <summary>
	/// Gets or sets the rotation of the element when the touch is in the hovered state.
	/// </summary>
	public double HoveredRotation
	{
		get => (double)GetValue(HoveredRotationProperty);
		set => SetValue(HoveredRotationProperty, value);
	}

	/// <summary>
	/// Gets or sets the rotation of the element when the touch is in the pressed state.
	/// </summary>
	public double PressedRotation
	{
		get => (double)GetValue(PressedRotationProperty);
		set => SetValue(PressedRotationProperty, value);
	}

	/// <summary>
	/// Gets or sets the rotation X of the element when when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	public double DefaultRotationX
	{
		get => (double)GetValue(DefaultRotationXProperty);
		set => SetValue(DefaultRotationXProperty, value);
	}

	/// <summary>
	/// Gets or sets the rotation X of the element when the touch is in the hovered state.
	/// </summary>
	public double HoveredRotationX
	{
		get => (double)GetValue(HoveredRotationXProperty);
		set => SetValue(HoveredRotationXProperty, value);
	}

	/// <summary>
	/// Gets or sets the rotation X of the element when the touch is in the pressed state.
	/// </summary>
	public double PressedRotationX
	{
		get => (double)GetValue(PressedRotationXProperty);
		set => SetValue(PressedRotationXProperty, value);
	}

	/// <summary>
	/// Gets or sets the rotation Y of the element when when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	public double DefaultRotationY
	{
		get => (double)GetValue(DefaultRotationYProperty);
		set => SetValue(DefaultRotationYProperty, value);
	}

	/// <summary>
	/// Gets or sets the rotation Y of the element when the touch is in the hovered state.
	/// </summary>
	public double HoveredRotationY
	{
		get => (double)GetValue(HoveredRotationYProperty);
		set => SetValue(HoveredRotationYProperty, value);
	}

	/// <summary>
	/// Gets or sets the rotation Y of the element when the touch is in the pressed state.
	/// </summary>
	public double PressedRotationY
	{
		get => (double)GetValue(PressedRotationYProperty);
		set => SetValue(PressedRotationYProperty, value);
	}

	/// <summary>
	/// Gets or sets the duration
	/// </summary>
	public int AnimationDuration
	{
		get => (int)GetValue(AnimationDurationProperty);
		set => SetValue(AnimationDurationProperty, value);
	}

	/// <summary>
	/// Gets or sets the easing of the animation.
	/// </summary>
	public Easing? AnimationEasing
	{
		get => (Easing?)GetValue(AnimationEasingProperty);
		set => SetValue(AnimationEasingProperty, value);
	}

	/// <summary>
	/// Gets or sets the duration of the pressed animation.
	/// </summary>
	public int PressedAnimationDuration
	{
		get => (int)GetValue(PressedAnimationDurationProperty);
		set => SetValue(PressedAnimationDurationProperty, value);
	}

	/// <summary>
	/// Gets or sets the easing of the pressed animation.
	/// </summary>
	public Easing? PressedAnimationEasing
	{
		get => (Easing?)GetValue(PressedAnimationEasingProperty);
		set => SetValue(PressedAnimationEasingProperty, value);
	}

	/// <summary>
	/// Gets or sets the duration of the animation when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	public int DefaultAnimationDuration
	{
		get => (int)GetValue(DefaultAnimationDurationProperty);
		set => SetValue(DefaultAnimationDurationProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="Easing"/> of the animation when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	public Easing? DefaultAnimationEasing
	{
		get => (Easing?)GetValue(DefaultAnimationEasingProperty);
		set => SetValue(DefaultAnimationEasingProperty, value);
	}

	/// <summary>
	/// Gets or sets the duration of the hovered animation.
	/// </summary>
	public int HoveredAnimationDuration
	{
		get => (int)GetValue(HoveredAnimationDurationProperty);
		set => SetValue(HoveredAnimationDurationProperty, value);
	}

	/// <summary>
	/// Gets or sets the easing of the hovered animation.
	/// </summary>
	public Easing? HoveredAnimationEasing
	{
		get => (Easing?)GetValue(HoveredAnimationEasingProperty);
		set => SetValue(HoveredAnimationEasingProperty, value);
	}

	/// <summary>
	/// Gets or sets the number of times the element should pulse.
	/// </summary>
	public int RepeatAnimationCount
	{
		get => (int)GetValue(RepeatAnimationCountProperty);
		set => SetValue(RepeatAnimationCountProperty, value);
	}

	/// <summary>
	/// Gets or sets the threshold for disallowing touch.
	/// </summary>
	public int DisallowTouchThreshold
	{
		get => (int)GetValue(DisallowTouchThresholdProperty);
		set => SetValue(DisallowTouchThresholdProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the animation should be native.
	/// </summary>
	public bool ShouldUseNativeAnimation
	{
		get => (bool)GetValue(ShouldUseNativeAnimationProperty);
		set => SetValue(ShouldUseNativeAnimationProperty, value);
	}

	/// <summary>
	/// Gets or sets the color of the native animation.
	/// </summary>
	public Color? NativeAnimationColor
	{
		get => (Color?)GetValue(NativeAnimationColorProperty);
		set => SetValue(NativeAnimationColorProperty, value);
	}

	/// <summary>
	/// Gets or sets the radius of the native animation.
	/// </summary>
	public int? NativeAnimationRadius
	{
		get => (int?)GetValue(NativeAnimationRadiusProperty);
		set => SetValue(NativeAnimationRadiusProperty, value);
	}

	/// <summary>
	/// Gets or sets the shadow radius of the native animation.
	/// </summary>
	public int? NativeAnimationShadowRadius
	{
		get => (int?)GetValue(NativeAnimationShadowRadiusProperty);
		set => SetValue(NativeAnimationShadowRadiusProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the native animation should be borderless.
	/// </summary>
	public bool IsNativeAnimationBorderless
	{
		get => (bool)GetValue(IsNativeAnimationBorderlessProperty);
		set => SetValue(IsNativeAnimationBorderlessProperty, value);
	}

	internal bool CanExecute => IsEnabled
		&& Element?.IsEnabled is true
		&& (Command?.CanExecute(CommandParameter) ?? true);

	internal VisualElement? Element
	{
		get => element;
		set
		{
			if (element is not null)
			{
				gestureManager.Reset();
				SetChildrenInputTransparent(false);
			}
			gestureManager.AbortAnimations(this, CancellationToken.None).SafeFireAndForget<TaskCanceledException>(ex => Trace.WriteLine(ex));
			element = value;

			if (value is not null)
			{
				SetChildrenInputTransparent(ShouldMakeChildrenInputTransparent);
				ForceUpdateState(CancellationToken.None, false).SafeFireAndForget<TaskCanceledException>(ex => Trace.WriteLine(ex));
			}
		}
	}
}