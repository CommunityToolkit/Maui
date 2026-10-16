using System.Runtime.CompilerServices;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using static System.OperatingSystem;
using AView = Android.Views.View;
using MColor = Microsoft.Maui.Graphics.Color;
using Trace = System.Diagnostics.Trace;

namespace CommunityToolkit.Maui.Behaviors;

public partial class TouchBehavior
{
	static readonly MColor defaultNativeAnimationColor = MColor.FromRgba(128, 128, 128, 64);

	// The press and release handling that a Button gets in the place of its handler's own, once per platform view (see ButtonTouchHandler)
	static readonly ConditionalWeakTable<AView, ButtonTouchHandler> buttonTouchHandlers = new();

	bool isHoverSupported;
	bool isClickHandled;
	bool wasClickable;
	bool wasLongClickable;
	float startX;
	float startY;
	AView? view;

	AccessibilityManager? accessibilityManager;
	AccessibilityListener? accessibilityListener;

	internal bool IsCanceled { get; set; }

	bool IsAccessibilityMode => accessibilityManager is not null
		&& accessibilityManager.IsEnabled
		&& accessibilityManager.IsTouchExplorationEnabled;

	// Android clicks a view with a click listener of its own (a Button's or an ImageButton's, which raises the element's Clicked) when a tap
	// ends on it: the behavior only holds the click listener of a view without one (see UpdateClickHandler)
	internal bool IsClickedRaisedByPlatformView => !isClickHandled && view is not null && view.IsPeerAlive() && view.HasOnClickListeners;

	/// <summary>
	/// Attaches the behavior to the platform view.
	/// </summary>
	/// <param name="bindable">Maui Visual Element</param>
	/// <param name="platformView">Native View</param>
	protected override void OnAttachedTo(VisualElement bindable, AView platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		// The attach makes the view clickable (also through the click listener): the detach gives the view these values back. Another
		// TouchBehavior of the element that is attached already made the view clickable: it read the view's own values.
		var otherAttachedBehavior = GetOtherAttachedTouchBehavior(bindable, this);
		wasClickable = otherAttachedBehavior?.wasClickable ?? platformView.Clickable;
		wasLongClickable = otherAttachedBehavior?.wasLongClickable ?? platformView.LongClickable;

		Element = bindable;
		view = platformView;

		platformView.Touch += OnTouch;
		AddButtonTouchHandler(bindable, platformView);
		platformView.GenericMotion += OnGenericMotion;
		platformView.KeyPress += OnKeyPressed;
		SetElementClickedHandler(bindable, true);
		accessibilityManager = platformView.Context?.GetSystemService(Context.AccessibilityService) as AccessibilityManager;

		if (accessibilityManager is not null)
		{
			accessibilityListener = new AccessibilityListener(this);
			accessibilityManager.AddAccessibilityStateChangeListener(accessibilityListener);
			accessibilityManager.AddTouchExplorationStateChangeListener(accessibilityListener);
		}

		// Once the accessibility manager is known: the click listener depends on touch exploration
		UpdateClickHandler();

		if (!IsAndroidVersionAtLeast((int)BuildVersionCodes.Lollipop))
		{
			return;
		}

		platformView.Clickable = true;
		platformView.LongClickable = true;
	}

	/// <summary>
	/// Detaches the behavior from the platform view.
	/// </summary>
	/// <param name="bindable">Maui Visual Element</param>
	/// <param name="platformView">Native View</param>
	protected override void OnDetachedFrom(VisualElement bindable, AView platformView)
	{
		// Element must be cleared before the base class clears View (which Element returns): the Element setter undoes what the attach did
		// to the element (the layout's ChildAdded subscription, the children's InputTransparent) only while it still knows the element.
		Element = null;
		base.OnDetachedFrom(bindable, platformView);

		view = platformView;
		SetElementClickedHandler(bindable, false);

		try
		{
			RemoveAccessibilityListener();

			if (view is not null)
			{
				RemoveListeners(view);

				// Another TouchBehavior that is still attached to the element needs the view to stay clickable: that one gives the values back
				if (GetOtherAttachedTouchBehavior(bindable, this) is null)
				{
					view.Clickable = wasClickable;
					view.LongClickable = wasLongClickable;

					// A handler that maps IsEnabled to Clickable (Picker) applies it again: IsEnabled may have changed while the behavior was attached
					bindable.Handler?.UpdateValue(nameof(VisualElement.IsEnabled));
				}
			}
		}
		catch (ObjectDisposedException)
		{
			// Suppress exception
			Trace.WriteLine("TouchBehavior is already disposed.");
		}

		isHoverSupported = false;
	}

	// A Button keeps the press and release handling that the attach gave it (AddButtonTouchHandler)
	void RemoveListeners(AView platformView)
	{
		platformView.Touch -= OnTouch;
		platformView.GenericMotion -= OnGenericMotion;
		platformView.KeyPress -= OnKeyPressed;

		if (isClickHandled)
		{
			platformView.Click -= OnClick;
			isClickHandled = false;
		}
	}

	// A listener that stays registered after it was disposed makes the next accessibility change call a peer that no longer exists,
	// which takes the process down: it is removed from the AccessibilityManager before it is disposed.
	void RemoveAccessibilityListener()
	{
		if (accessibilityManager is not null && accessibilityListener is not null)
		{
			accessibilityManager.RemoveAccessibilityStateChangeListener(accessibilityListener);
			accessibilityManager.RemoveTouchExplorationStateChangeListener(accessibilityListener);
		}

		accessibilityListener?.Dispose();
		accessibilityListener = null;
		accessibilityManager = null;
	}

	// The click listener carries the gesture in touch exploration (TalkBack) mode only, where the touch listener ignores raw touches; otherwise
	// the view keeps its click listener to itself. Android has one click listener per view, and the behavior's (the shared .NET Click event)
	// would take the place of a listener the view's handler installed for good. So in touch exploration mode the behavior takes the slot
	// only from a view without a click listener of its own, or shares it with another TouchBehavior of the element. A view with one (a
	// Button, an ImageButton, a Picker, a DatePicker, a TimePicker) keeps it, and a Button or ImageButton completes the gesture through
	// Clicked then (OnElementClicked); TalkBack's click on another such view is the view's alone.
	void UpdateClickHandler()
	{
		if (view is null || !view.IsPeerAlive())
		{
			return;
		}

		var shouldHandleClick = IsAccessibilityMode && (isClickHandled || !view.HasOnClickListeners || IsClickHandledByAnotherTouchBehavior());
		if (shouldHandleClick == isClickHandled)
		{
			return;
		}

		if (shouldHandleClick)
		{
			view.Click += OnClick;
		}
		else
		{
			view.Click -= OnClick;
		}

		isClickHandled = shouldHandleClick;
	}

	bool IsClickHandledByAnotherTouchBehavior() =>
		Element?.Behaviors.OfType<TouchBehavior>().Any(other => !ReferenceEquals(other, this) && ReferenceEquals(other.Element, Element) && other.isClickHandled) is true;

	void SetElementClickedHandler(VisualElement element, bool isSubscribed)
	{
		switch (element)
		{
			case Button button:
				button.Clicked -= OnElementClicked;
				if (isSubscribed)
				{
					button.Clicked += OnElementClicked;
				}

				break;

			case ImageButton imageButton:
				imageButton.Clicked -= OnElementClicked;
				if (isSubscribed)
				{
					imageButton.Clicked += OnElementClicked;
				}

				break;
		}
	}

	// In touch exploration mode a Button or an ImageButton keeps its own click listener (UpdateClickHandler): the Clicked it raises when
	// TalkBack activates it completes the gesture
	void OnElementClicked(object? sender, EventArgs e)
	{
		if (!IsEnabled || !IsAccessibilityMode || isClickHandled)
		{
			return;
		}

		IsCanceled = false;
		HandleTouchEnded(TouchStatus.Completed);
	}

	// MAUI's ButtonHandler raises a Button's Pressed and Released from a touch listener of its own (SetOnTouchListener). Android has one touch
	// listener per view, and the behavior's (the shared .NET Touch event) takes its place, which cannot be read back from the view to give it
	// back. The Button gets a handler in the shared Touch event instead, once per view, which does what MAUI's listener did: it raises
	// Pressed and Released while the behavior is attached and after it is removed, next to the handler of a gesture recognizer.
	static void AddButtonTouchHandler(VisualElement element, AView platformView)
	{
		if (element is not IButton button || element.Handler is not ButtonHandler)
		{
			return;
		}

		if (buttonTouchHandlers.TryGetValue(platformView, out var buttonTouchHandler))
		{
			// After the behavior's handler, as on iOS and Mac Catalyst, where the behavior's recognizer sees a touch before the button
			platformView.Touch -= buttonTouchHandler.OnTouch;
		}
		else
		{
			buttonTouchHandler = new ButtonTouchHandler(button);
			buttonTouchHandlers.Add(platformView, buttonTouchHandler);
		}

		platformView.Touch += buttonTouchHandler.OnTouch;
	}

	void OnKeyPressed(object? sender, AView.KeyEventArgs e)
	{
		if (e.KeyCode == Keycode.Space && e.Event?.Action == KeyEventActions.Up)
		{
			IsCanceled = false;
			HandleTouchEnded(TouchStatus.Completed);
		}

		// This ensures that native focus navigation and keyboard input flow are preserved
		e.Handled = false;
	}

	void OnClick(object? sender, EventArgs args)
	{
		if (!IsEnabled)
		{
			return;
		}

		if (!IsAccessibilityMode)
		{
			return;
		}

		IsCanceled = false;
		HandleTouchEnded(TouchStatus.Completed);
	}

	void HandleTouchEnded(TouchStatus status)
	{
		if (IsCanceled)
		{
			return;
		}

		IsCanceled = true;
		if (DisallowTouchThreshold > 0)
		{
			view?.Parent?.RequestDisallowInterceptTouchEvent(false);
		}

		HandleTouch(status);

		HandleUserInteraction(TouchInteractionStatus.Completed);
	}


	void OnTouch(object? sender, AView.TouchEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		e.Handled = false;

		if (!IsEnabled || IsAccessibilityMode)
		{
			return;
		}

		switch (e.Event?.ActionMasked)
		{
			case MotionEventActions.Down:
				OnTouchDown(e);
				break;
			case MotionEventActions.Up:
				OnTouchUp();
				break;
			case MotionEventActions.Cancel:
				OnTouchCancel();
				break;
			case MotionEventActions.Move:
				OnTouchMove((AView)sender, e);
				break;
			case MotionEventActions.HoverEnter:
				OnHoverEnter();
				break;
			case MotionEventActions.HoverExit:
				OnHoverExit();
				break;
		}
	}

	// Android delivers a mouse or stylus hover as a generic motion event (dispatchGenericMotionEvent), never to the touch listener. The view
	// hands every hover event to its generic motion listener unless an OnHoverListener consumes it; the hover listener itself is not used
	// because MAUI sets and clears it for PointerGestureRecognizer (a view has a single one).
	void OnGenericMotion(object? sender, AView.GenericMotionEventArgs e)
	{
		e.Handled = false;

		if (!IsEnabled || IsAccessibilityMode)
		{
			return;
		}

		switch (e.Event?.ActionMasked)
		{
			case MotionEventActions.HoverEnter:
				OnHoverEnter();
				break;
			case MotionEventActions.HoverExit:
				OnHoverExit();
				break;
		}
	}

	void OnTouchDown(AView.TouchEventArgs touchEventArgs)
	{
		ArgumentNullException.ThrowIfNull(touchEventArgs.Event);

		IsCanceled = false;

		startX = touchEventArgs.Event.GetX();
		startY = touchEventArgs.Event.GetY();

		HandleUserInteraction(TouchInteractionStatus.Started);
		HandleTouch(TouchStatus.Started);

		if (DisallowTouchThreshold > 0)
		{
			// The request travels up from the view it is sent to, so it goes to the view's own parent.
			view?.Parent?.RequestDisallowInterceptTouchEvent(true);
		}
	}

	void OnTouchUp()
		=> HandleTouchEnded(CurrentTouchStatus is TouchStatus.Started ? TouchStatus.Completed : TouchStatus.Canceled);

	void OnTouchCancel()
		=> HandleTouchEnded(TouchStatus.Canceled);

	void OnTouchMove(AView view, AView.TouchEventArgs touchEventArgs)
	{
		if (IsCanceled || touchEventArgs.Event is null)
		{
			return;
		}

		var diffX = Math.Abs(touchEventArgs.Event.GetX() - startX) / this.view?.Context?.Resources?.DisplayMetrics?.Density ?? throw new InvalidOperationException("Context cannot be null");
		var diffY = Math.Abs(touchEventArgs.Event.GetY() - startY) / this.view?.Context?.Resources?.DisplayMetrics?.Density ?? throw new InvalidOperationException("Context cannot be null");
		var maxDiff = Math.Max(diffX, diffY);

		var disallowTouchThreshold = DisallowTouchThreshold;
		if (disallowTouchThreshold > 0 && maxDiff > disallowTouchThreshold)
		{
			HandleTouchEnded(TouchStatus.Canceled);
			return;
		}

		var screenPointerCoords = new Point(view.Left + touchEventArgs.Event.GetX(), view.Top + touchEventArgs.Event.GetY());
		var viewRect = new Rect(view.Left, view.Top, view.Right - view.Left, view.Bottom - view.Top);
		var status = viewRect.Contains(screenPointerCoords) ? TouchStatus.Started : TouchStatus.Canceled;

		if (isHoverSupported && ((status is TouchStatus.Canceled && CurrentHoverStatus is HoverStatus.Entered)
			|| (status is TouchStatus.Started && CurrentHoverStatus is HoverStatus.Exited)))
		{
			HandleHover(status is TouchStatus.Started ? HoverStatus.Entered : HoverStatus.Exited);
		}

		if (CurrentTouchStatus != status)
		{
			HandleTouch(status);
		}
	}

	void OnHoverEnter()
	{
		isHoverSupported = true;
		HandleHover(HoverStatus.Entered);
	}

	void OnHoverExit()
	{
		isHoverSupported = true;
		HandleHover(HoverStatus.Exited);
	}

	partial void PlatformDispose()
	{
		// A disposed behavior takes no more input: its listeners leave the view (unless somebody else released the view's wrapper, through
		// which they could be removed).
		if (view is not null && view.IsPeerAlive())
		{
			RemoveListeners(view);
		}

		if (Element is not null)
		{
			SetElementClickedHandler(Element, false);
		}

		// The views and the AccessibilityManager belong to MAUI and to the system (their managed wrappers are shared): the behavior only
		// drops its references to them.
		view = null;

		RemoveAccessibilityListener();
	}

	sealed class ButtonTouchHandler(IButton button)
	{
		readonly WeakReference<IButton> buttonReference = new(button);

		public void OnTouch(object? sender, AView.TouchEventArgs e)
		{
			buttonReference.TryGetTarget(out var button);

			// As MAUI's listener, the handler leaves the touch to the view, so that the button still clicks. MAUI's gesture handling decides
			// that itself where it shares this listener: it listens to the outermost platform view of a view with gesture recognizers, which
			// is the button's own view unless the button has a container view (a Shadow, a Clip or InputTransparent gives it one).
			if (!IsTouchDecidedByMauiGestureHandling(button))
			{
				e.Handled = false;
			}

			switch (e.Event?.ActionMasked)
			{
				case MotionEventActions.Down:
					button?.Pressed();
					break;

				case MotionEventActions.Up:
				case MotionEventActions.Cancel:
					button?.Released();
					break;
			}
		}

		// MAUI's GesturePlatformManager listens to the touches of handler.ToPlatform(), the container view of a view that has one
		static bool IsTouchDecidedByMauiGestureHandling(IButton? button) =>
			button is Microsoft.Maui.Controls.View { GestureRecognizers.Count: > 0, Handler: IPlatformViewHandler { ContainerView: null } };
	}

	sealed class AccessibilityListener : Java.Lang.Object,
		AccessibilityManager.IAccessibilityStateChangeListener,
		AccessibilityManager.ITouchExplorationStateChangeListener
	{
		readonly WeakReference<TouchBehavior?> platformTouchBehaviorReference;

		internal AccessibilityListener(TouchBehavior platformTouchBehavior)
		{
			platformTouchBehaviorReference = new(platformTouchBehavior);
		}

		public void OnAccessibilityStateChanged(bool enabled)
		{
			if (platformTouchBehaviorReference.TryGetTarget(out var platformTouchBehavior))
			{
				platformTouchBehavior.UpdateClickHandler();
			}
		}

		public void OnTouchExplorationStateChanged(bool enabled)
		{
			if (platformTouchBehaviorReference.TryGetTarget(out var platformTouchBehavior))
			{
				platformTouchBehavior.UpdateClickHandler();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				platformTouchBehaviorReference.SetTarget(null);
			}

			base.Dispose(disposing);
		}
	}
}