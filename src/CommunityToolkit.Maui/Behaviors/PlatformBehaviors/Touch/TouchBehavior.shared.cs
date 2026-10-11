using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// <see cref="PlatformBehavior{TView,TPlatformView}"/> that adds additional actions for user interactions
/// </summary>
/// <remarks>
/// <para>
/// The behavior animates the opacity, translation and rotation of the element with MAUI's <c>FadeToAsync</c>, <c>TranslateToAsync</c>,
/// <c>RotateToAsync</c>, <c>RotateXToAsync</c> and <c>RotateYToAsync</c>, and its background colour with
/// <see cref="ColorAnimationExtensions.BackgroundColorTo"/>. A state change stops an animation that the app runs with the same method on
/// the element when the state changes that property, as starting the same animation again would; it leaves the app's other animations
/// alone. Removing or disposing the behavior stops only the behavior's own animations.
/// </para>
/// <para>
/// A <see cref="Button"/> or an <see cref="ImageButton"/> keeps its own click handling: its platform button raises <c>Clicked</c> and runs
/// the button's <c>Command</c> for a tap, as without the behavior. On Android, iOS and Mac Catalyst the behavior completes its own gesture
/// (<see cref="TouchGestureCompleted"/>, <see cref="Command"/>) next to it. On Windows the platform button handles the pointer press and
/// release itself and the behavior does not see them, so a tap on a <see cref="Button"/> or an <see cref="ImageButton"/> does not complete
/// the behavior's gesture there.
/// </para>
/// <para>
/// On Android a view has a single touch listener and a single click listener. The behavior uses the touch listener: a <see cref="Button"/>,
/// whose handler raises <c>Pressed</c> and <c>Released</c> from a touch listener of its own, gets them from the behavior instead, while the
/// behavior is attached and after it is removed. The click listener stays the view's (a <see cref="DatePicker"/> or a <see cref="TimePicker"/>
/// opens on a tap); only in touch exploration (TalkBack) mode the behavior completes its gesture through the click listener, and it takes
/// that listener only from a view without one of its own. A <see cref="Button"/> or an <see cref="ImageButton"/> completes the gesture
/// through its <c>Clicked</c> then. Any other view with a click listener of its own (a <see cref="Picker"/>, a <see cref="DatePicker"/> or a
/// <see cref="TimePicker"/>) keeps it: TalkBack's click opens it and does not complete the behavior's gesture.
/// </para>
/// <para>
/// On iOS and Mac Catalyst the behavior's gesture recognizer leaves the touches of the view and of its children to them, the end of a touch
/// included: a control that acts when a touch ends inside it, such as the platform button of a <see cref="Button"/>, an
/// <see cref="ImageButton"/> or a <see cref="CheckBox"/>, does so and takes the next touch. A <see cref="CollectionView"/> selects an item
/// whose template has the behavior when the item is tapped, next to the behavior's own gesture.
/// </para>
/// <para>
/// On Android and Windows the view the behavior is attached to takes the tap for itself: when the view is in the item template of a
/// <see cref="CollectionView"/>, the platform list does not select the item for a tap on the view. On these two platforms the behavior
/// selects the item when it completes its gesture, following the <see cref="SelectableItemsView.SelectionMode"/> of the
/// <see cref="CollectionView"/>: <see cref="SelectionMode.Single"/> makes the item the <see cref="SelectableItemsView.SelectedItem"/>,
/// <see cref="SelectionMode.Multiple"/> adds the item to the <see cref="SelectableItemsView.SelectedItems"/> or removes it from them, and
/// <see cref="SelectionMode.None"/> selects nothing.
/// </para>
/// </remarks>
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
	/// <remarks>
	/// Read the new state from <see cref="TouchStateChangedEventArgs.State"/>. The event is raised while the behavior is still processing the
	/// touch: <see cref="CurrentTouchStatus"/> already reports the same change, but <see cref="CurrentInteractionStatus"/> is updated after the
	/// event (a handler of the release still sees <see cref="TouchInteractionStatus.Started"/>), and by the time an asynchronous handler
	/// continues, the state may have changed again.
	/// </remarks>
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
	/// <remarks>
	/// Read the new status from <see cref="HoverStatusChangedEventArgs.Status"/>. The behavior updates <see cref="CurrentHoverStatus"/> before
	/// <see cref="CurrentHoverState"/>, so a handler of this event still sees the previous <see cref="CurrentHoverState"/>.
	/// </remarks>
	public event EventHandler<HoverStatusChangedEventArgs> HoverStatusChanged
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Fires when <see cref="CurrentHoverState"/> changes.
	/// </summary>
	/// <remarks>
	/// Read the new state from <see cref="HoverStateChangedEventArgs.State"/>. <see cref="CurrentHoverStatus"/> already reports the same change
	/// when the event is raised, but by the time an asynchronous handler continues, the hover may have changed again.
	/// </remarks>
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
	[BindableProperty(PropertyChangedMethodName = nameof(HandleChildrenInputTransparencyChanged))]
	public partial bool IsEnabled { get; set; } = TouchBehaviorDefaults.IsEnabled;

	/// <summary>
	/// Gets or sets a value indicating whether the children of the element should be made input transparent.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(HandleChildrenInputTransparencyChanged))]
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
	/// <remarks>
	/// While <see cref="LongPressCommand"/> is set, a long press does not run <see cref="Command"/>. On a <see cref="Button"/> or an
	/// <see cref="ImageButton"/>, the button's own <c>Clicked</c> and <c>Command</c> follow the platform button, which also clicks when a long
	/// press ends: to tell a tap from a long press, use <see cref="Command"/> for the tap.
	/// </remarks>
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
	/// <remarks>
	/// The duration is in milliseconds. <see cref="Timeout.Infinite"/> (-1) turns the long press off: it is never raised. 0 and any value
	/// below -1 raise the long press as soon as the element is pressed.
	/// </remarks>
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
	/// <remarks>
	/// The colour of a state takes the place of the element's <see cref="VisualElement.BackgroundColor"/> while the state is shown. A state
	/// without a background colour of its own gives the element its own background colour back: a binding, <c>DynamicResource</c>,
	/// <c>AppThemeBinding</c> or <see cref="Style"/> that sets <see cref="VisualElement.BackgroundColor"/> applies again, with its current
	/// value, and a colour that the app set on the element itself is set again. The detach of the behavior while a state colour is shown
	/// (the behavior is removed, or the element is unloaded) gives the element its own background colour back too, and stops the
	/// behavior's colour animation.
	/// <para>
	/// When a state colour takes the place of a colour that the app set on the element itself, the behavior first clears that value and sets
	/// it again, which tells it from a colour that a binding, <c>DynamicResource</c>, <c>AppThemeBinding</c> or <see cref="Style"/> applies:
	/// <see cref="VisualElement.BackgroundColor"/> raises <c>PropertyChanged</c> for the colour the element has without that value and for the
	/// app's colour again, before the state colour. When the app's colour comes back, it raises <c>PropertyChanged</c> once at most.
	/// </para>
	/// <para>
	/// An element without a background colour gets <see cref="Colors.Transparent"/> from the first state update after the attach when that
	/// state has no colour of its own. That colour counts as the element's own: the behavior leaves it on the element.
	/// </para>
	/// <para>
	/// While the behavior animates the background colour, it sets <see cref="VisualElement.BackgroundColor"/> at every frame: a colour that
	/// the app sets on the element itself during the animation does not stay. A binding, <c>DynamicResource</c>, <c>AppThemeBinding</c> or
	/// <see cref="Style"/> keeps its latest value, which applies when the element gets its own colour back.
	/// </para>
	/// <para>
	/// The behavior cannot tell a colour that the app sets on the element while a state colour is shown from its own when it is the same
	/// colour (MAUI raises no change for an equal value): the next state change, or the detach, then treats it as the behavior's colour and
	/// gives the element the app's previous background colour back.
	/// </para>
	/// </remarks>
	[BindableProperty]
	public partial Color? DefaultBackgroundColor { get; set; }

	/// <summary>
	/// Gets or sets the background color of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	/// <remarks>
	/// The colour of a state takes the place of the element's <see cref="VisualElement.BackgroundColor"/> while the state is shown. A state
	/// without a background colour of its own gives the element its own background colour back: a binding, <c>DynamicResource</c>,
	/// <c>AppThemeBinding</c> or <see cref="Style"/> that sets <see cref="VisualElement.BackgroundColor"/> applies again, with its current
	/// value, and a colour that the app set on the element itself is set again. The detach of the behavior while a state colour is shown
	/// (the behavior is removed, or the element is unloaded) gives the element its own background colour back too, and stops the
	/// behavior's colour animation.
	/// <para>
	/// When a state colour takes the place of a colour that the app set on the element itself, the behavior first clears that value and sets
	/// it again, which tells it from a colour that a binding, <c>DynamicResource</c>, <c>AppThemeBinding</c> or <see cref="Style"/> applies:
	/// <see cref="VisualElement.BackgroundColor"/> raises <c>PropertyChanged</c> for the colour the element has without that value and for the
	/// app's colour again, before the state colour. When the app's colour comes back, it raises <c>PropertyChanged</c> once at most.
	/// </para>
	/// <para>
	/// An element without a background colour gets <see cref="Colors.Transparent"/> from the first state update after the attach when that
	/// state has no colour of its own. That colour counts as the element's own: the behavior leaves it on the element.
	/// </para>
	/// <para>
	/// While the behavior animates the background colour, it sets <see cref="VisualElement.BackgroundColor"/> at every frame: a colour that
	/// the app sets on the element itself during the animation does not stay. A binding, <c>DynamicResource</c>, <c>AppThemeBinding</c> or
	/// <see cref="Style"/> keeps its latest value, which applies when the element gets its own colour back.
	/// </para>
	/// <para>
	/// The behavior cannot tell a colour that the app sets on the element while a state colour is shown from its own when it is the same
	/// colour (MAUI raises no change for an equal value): the next state change, or the detach, then treats it as the behavior's colour and
	/// gives the element the app's previous background colour back.
	/// </para>
	/// </remarks>
	[BindableProperty]
	public partial Color? HoveredBackgroundColor { get; set; }

	/// <summary>
	/// Gets or sets the background color of the element when the <see cref="TouchState" /> is <see cref="TouchState.Pressed" />.
	/// </summary>
	/// <remarks>
	/// The colour of a state takes the place of the element's <see cref="VisualElement.BackgroundColor"/> while the state is shown. A state
	/// without a background colour of its own gives the element its own background colour back: a binding, <c>DynamicResource</c>,
	/// <c>AppThemeBinding</c> or <see cref="Style"/> that sets <see cref="VisualElement.BackgroundColor"/> applies again, with its current
	/// value, and a colour that the app set on the element itself is set again. The detach of the behavior while a state colour is shown
	/// (the behavior is removed, or the element is unloaded) gives the element its own background colour back too, and stops the
	/// behavior's colour animation.
	/// <para>
	/// When a state colour takes the place of a colour that the app set on the element itself, the behavior first clears that value and sets
	/// it again, which tells it from a colour that a binding, <c>DynamicResource</c>, <c>AppThemeBinding</c> or <see cref="Style"/> applies:
	/// <see cref="VisualElement.BackgroundColor"/> raises <c>PropertyChanged</c> for the colour the element has without that value and for the
	/// app's colour again, before the state colour. When the app's colour comes back, it raises <c>PropertyChanged</c> once at most.
	/// </para>
	/// <para>
	/// An element without a background colour gets <see cref="Colors.Transparent"/> from the first state update after the attach when that
	/// state has no colour of its own. That colour counts as the element's own: the behavior leaves it on the element.
	/// </para>
	/// <para>
	/// While the behavior animates the background colour, it sets <see cref="VisualElement.BackgroundColor"/> at every frame: a colour that
	/// the app sets on the element itself during the animation does not stay. A binding, <c>DynamicResource</c>, <c>AppThemeBinding</c> or
	/// <see cref="Style"/> keeps its latest value, which applies when the element gets its own colour back.
	/// </para>
	/// <para>
	/// The behavior cannot tell a colour that the app sets on the element while a state colour is shown from its own when it is the same
	/// colour (MAUI raises no change for an equal value): the next state change, or the detach, then treats it as the behavior's colour and
	/// gives the element the app's previous background colour back.
	/// </para>
	/// </remarks>
	[BindableProperty]
	public partial Color? PressedBackgroundColor { get; set; }

	/// <summary>
	/// Gets or sets the opacity of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	[BindableProperty(PropertyChangingMethodName = nameof(HandleDefaultOpacityChanging))]
	public partial double? DefaultOpacity { get; set; }

	/// <summary>
	/// Gets or sets the opacity of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty(PropertyChangingMethodName = nameof(HandleHoveredOpacityChanging))]
	public partial double? HoveredOpacity { get; set; }

	/// <summary>
	/// Gets or sets the opacity of the element when the <see cref="TouchState" /> is <see cref="TouchState.Pressed" />.
	/// </summary>
	[BindableProperty(PropertyChangingMethodName = nameof(HandlePressedOpacityChanging))]
	public partial double? PressedOpacity { get; set; }

	/// <summary>
	/// Gets or sets the scale of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double? DefaultScale { get; set; }

	/// <summary>
	/// Gets or sets the scale of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double? HoveredScale { get; set; }

	/// <summary>
	/// Gets or sets the scale of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double? PressedScale { get; set; }

	/// <summary>
	/// Gets or sets the translation X of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double? DefaultTranslationX { get; set; }

	/// <summary>
	/// Gets or sets the translation X of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double? HoveredTranslationX { get; set; }

	/// <summary>
	/// Gets or sets the translation X of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double? PressedTranslationX { get; set; }

	/// <summary>
	/// Gets or sets the translation Y of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double? DefaultTranslationY { get; set; }

	/// <summary>
	/// Gets or sets the translation Y of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double? HoveredTranslationY { get; set; }

	/// <summary>
	/// Gets or sets the translation Y of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double? PressedTranslationY { get; set; }

	/// <summary>
	/// Gets or sets the rotation of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double? DefaultRotation { get; set; }

	/// <summary>
	/// Gets or sets the rotation of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double? HoveredRotation { get; set; }

	/// <summary>
	/// Gets or sets the rotation of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double? PressedRotation { get; set; }

	/// <summary>
	/// Gets or sets the rotation X of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double? DefaultRotationX { get; set; }

	/// <summary>
	/// Gets or sets the rotation X of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double? HoveredRotationX { get; set; }

	/// <summary>
	/// Gets or sets the rotation X of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double? PressedRotationX { get; set; }

	/// <summary>
	/// Gets or sets the rotation Y of the element when the <see cref="TouchState" /> is <see cref="TouchState.Default" />.
	/// </summary>
	[BindableProperty]
	public partial double? DefaultRotationY { get; set; }

	/// <summary>
	/// Gets or sets the rotation Y of the element when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial double? HoveredRotationY { get; set; }

	/// <summary>
	/// Gets or sets the rotation Y of the element when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial double? PressedRotationY { get; set; }

	/// <summary>
	/// Gets or sets the duration of the animation when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial int? PressedAnimationDuration { get; set; }

	/// <summary>
	/// Gets or sets the easing of the animation when the <see cref="TouchState"/> is <see cref="TouchState.Pressed"/>.
	/// </summary>
	[BindableProperty]
	public partial Easing? PressedAnimationEasing { get; set; }

	/// <summary>
	/// Gets or sets the duration of the animation when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	[BindableProperty]
	public partial int? DefaultAnimationDuration { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="Easing"/> of the animation when <see cref="TouchState"/> is <see cref="TouchState.Default"/>.
	/// </summary>
	[BindableProperty]
	public partial Easing? DefaultAnimationEasing { get; set; }

	/// <summary>
	/// Gets or sets the duration of the animation when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial int? HoveredAnimationDuration { get; set; }

	/// <summary>
	/// Gets or sets the easing of the animation when the <see cref="HoverState" /> is <see cref="HoverState.Hovered" />.
	/// </summary>
	[BindableProperty]
	public partial Easing? HoveredAnimationEasing { get; set; }

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
				gestureManager.Reset(View);

				// Every detach gives the children their InputTransparent back, also the detach of an unload (PlatformBehavior detaches on
				// Unloaded and attaches again on Loaded). On Android MAUI wraps a view in a WrapperView for InputTransparent, so an item that
				// a CollectionView recycles unwraps and re-wraps its children each time. That cost is accepted: a behavior that is removed
				// while its element is unloaded gets no detach, and keeping the children transparent until then would leave them so.
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