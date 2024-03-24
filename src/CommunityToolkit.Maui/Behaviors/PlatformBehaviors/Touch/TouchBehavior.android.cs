using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Android.Widget;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using static System.OperatingSystem;
using AView = Android.Views.View;
using Color = Android.Graphics.Color;
using MColor = Microsoft.Maui.Graphics.Color;
using MView = Microsoft.Maui.Controls.View;
using Trace = System.Diagnostics.Trace;

namespace CommunityToolkit.Maui.Behaviors;

public partial class TouchBehavior
{
	static readonly MColor defaultNativeAnimationColor = MColor.FromRgba(128, 128, 128, 64);

	bool isHoverSupported;
	RippleDrawable? ripple;
	AView? rippleView;
	float startX;
	float startY;
	MColor? rippleColor;
	int? rippleRadius;
	AView? view;
	ViewGroup? viewGroup;

	AccessibilityManager? accessibilityManager;
	AccessibilityListener? accessibilityListener;

	bool IsAccessibilityMode => accessibilityManager is not null
		&& accessibilityManager.IsEnabled
		&& accessibilityManager.IsTouchExplorationEnabled;

	internal bool IsCanceled { get; set; }

	bool IsForegroundRippleWithTapGestureRecognizer => ripple is not null
		&& this.view is not null
		&& ripple.IsAlive()
		&& this.view.IsAlive()
		&& (IsAndroidVersionAtLeast(23) ? this.view.Foreground : this.view.Background) == ripple
		&& element is MView view
		&& view.GestureRecognizers.Any(gesture => gesture is TapGestureRecognizer);

	/// <summary>
	/// Attaches the behavior to the platform view.
	/// </summary>
	/// <param name="bindable">Maui Visual Element</param>
	/// <param name="platformView">Native View</param>
	protected override void OnAttachedTo(VisualElement bindable, AView platformView)
	{
		Element = bindable;
		view = platformView;
		viewGroup = Microsoft.Maui.Platform.ViewExtensions.GetParentOfType<ViewGroup>(platformView);

		platformView.Touch += OnTouch;
		UpdateClickHandler();
		accessibilityManager = platformView.Context?.GetSystemService(Context.AccessibilityService) as AccessibilityManager;

		if (accessibilityManager is not null)
		{
			accessibilityListener = new AccessibilityListener(this);
			accessibilityManager.AddAccessibilityStateChangeListener(accessibilityListener);
			accessibilityManager.AddTouchExplorationStateChangeListener(accessibilityListener);
		}

		if (!IsAndroidVersionAtLeast((int)BuildVersionCodes.Lollipop) || !ShouldUseNativeAnimation)
		{
			return;
		}

		platformView.Clickable = true;
		platformView.LongClickable = true;

		CreateRipple();
		ApplyRipple();

		platformView.LayoutChange += OnLayoutChange;
	}

	/// <summary>
	/// Detaches the behavior from the platform view.
	/// </summary>
	/// <param name="bindable">Maui Visual Element</param>
	/// <param name="platformView">Native View</param>
	protected override void OnDetachedFrom(VisualElement bindable, AView platformView)
	{ 
		view = platformView;

		if (Element is null)
		{
			return;
		}

		try
		{
			if (accessibilityManager is not null && accessibilityListener is not null)
			{
				accessibilityManager.RemoveAccessibilityStateChangeListener(accessibilityListener);
				accessibilityManager.RemoveTouchExplorationStateChangeListener(accessibilityListener);
				accessibilityListener.Dispose();
				accessibilityManager = null;
				accessibilityListener = null;
			}

			RemoveRipple();

			if (view is not null)
			{
				view.LayoutChange -= OnLayoutChange;
				view.Touch -= OnTouch;
				view.Click -= OnClick;
			}

			if (rippleView is not null)
			{
				rippleView.Pressed = false;
				viewGroup?.RemoveView(rippleView);
				rippleView.Dispose();
				rippleView = null;
			}
			
			Element = null;
		}
		catch (ObjectDisposedException)
		{
			// Suppress exception
			Trace.WriteLine("TouchBehavior is already disposed.");
		}

		isHoverSupported = false;
	}

	static ColorStateList GetColorStateList(MColor? color)
	{
		var animationColor = color;
		animationColor ??= defaultNativeAnimationColor;

		return new ColorStateList([[]], [animationColor.ToAndroid()]);
	}

	void OnLayoutChange(object? sender, AView.LayoutChangeEventArgs e)
	{
		if (sender is not AView view || rippleView is null)
		{
			return;
		}

		rippleView.Right = view.Width;
		rippleView.Bottom = view.Height;
	}

	void CreateRipple()
	{
		RemoveRipple();

		var drawable = IsAndroidVersionAtLeast(23)
			&& viewGroup is null
				? view?.Foreground
				: view?.Background;

		var isBorderLess = IsNativeAnimationBorderless;
		var isEmptyDrawable = Element is Layout || drawable is null;
		var color = NativeAnimationColor;

		if (drawable is RippleDrawable rippleDrawable && rippleDrawable.GetConstantState() is Drawable.ConstantState constantState)
		{
			ripple = (RippleDrawable)constantState.NewDrawable();
		}
		else
		{
			var content = isEmptyDrawable || isBorderLess ? null : drawable;
			var mask = isEmptyDrawable && !isBorderLess ? new ColorDrawable(Color.White) : null;

			ripple = new RippleDrawable(GetColorStateList(color), content, mask);
		}

		UpdateRipple(color);
	}

	void RemoveRipple()
	{
		if (ripple is null)
		{
			return;
		}

		if (view is not null)
		{
			if (IsAndroidVersionAtLeast(23) && view.Foreground == ripple)
			{
				view.Foreground = null;
			}
			else if (view.Background == ripple)
			{
				view.Background = null;
			}
		}

		if (rippleView is not null)
		{
			if (IsAndroidVersionAtLeast(23))
			{
				rippleView.Foreground = null;
			}

			rippleView.Background = null;
		}

		ripple.Dispose();
		ripple = null;
	}

	void UpdateRipple(MColor? color)
	{
		if (!IsEnabled
			|| color?.Equals(rippleColor) is true && NativeAnimationRadius == rippleRadius)
		{
			return;
		}

		rippleColor = color;
		rippleRadius = NativeAnimationRadius;
		ripple?.SetColor(GetColorStateList(color));
		if (IsAndroidVersionAtLeast(23) && ripple is not null)
		{
			ripple.Radius = (int)(view?.Context?.Resources?.DisplayMetrics?.Density * NativeAnimationRadius ?? 1);
		}
	}

	void UpdateClickHandler()
	{
		if (view is null || !view.IsAlive())
		{
			return;
		}

		view.Click -= OnClick;
		if (IsAccessibilityMode || (IsEnabled && (element?.IsEnabled ?? false)))
		{
			view.Click += OnClick;
			return;
		}
	}

	void ApplyRipple()
	{
		if (ripple is null)
		{
			return;
		}

		var isBorderless = IsNativeAnimationBorderless;

		if (viewGroup is null && view is not null)
		{
			if (IsAndroidVersionAtLeast(23))
			{
				view.Foreground = ripple;
			}
			else
			{
				view.Background = ripple;
			}

			return;
		}

		if (rippleView is null)
		{
			rippleView = new FrameLayout(viewGroup?.Context ?? view?.Context ?? throw new NullReferenceException())
			{
				LayoutParameters = new ViewGroup.LayoutParams(-1, -1),
				Clickable = false,
				Focusable = false,
				Enabled = false
			};

			bool rippleAtFront = !(viewGroup?.ChildCount > 0);

			viewGroup?.AddView(rippleView);

			if (!rippleAtFront)
			{
				var child = viewGroup?.GetChildAt(0);
				child?.BringToFront();
			}
		}

		viewGroup?.SetClipChildren(!isBorderless);

		if (isBorderless)
		{
			rippleView.Background = null;

			if (IsAndroidVersionAtLeast(23))
			{
				rippleView.Foreground = ripple;
			}
		}
		else
		{
			if (IsAndroidVersionAtLeast(23))
			{
				rippleView.Foreground = null;
			}

			rippleView.Background = ripple;
		}
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
			viewGroup?.Parent?.RequestDisallowInterceptTouchEvent(false);
		}

		HandleTouch(status);

		HandleUserInteraction(TouchInteractionStatus.Completed);

		EndRipple();
	}

	void EndRipple()
	{
		if (!IsEnabled)
		{
			return;
		}

		if (rippleView is not null)
		{
			if (rippleView.Pressed)
			{
				rippleView.Pressed = false;
				rippleView.Enabled = false;
			}
		}
		else if (IsForegroundRippleWithTapGestureRecognizer)
		{
			if (view?.Pressed ?? false)
			{
				view.Pressed = false;
			}
		}
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

	void OnTouchDown(AView.TouchEventArgs touchEventArgs)
	{
		ArgumentNullException.ThrowIfNull(touchEventArgs.Event);

		IsCanceled = false;

		startX = touchEventArgs.Event.GetX();
		startY = touchEventArgs.Event.GetY();

		HandleUserInteraction(TouchInteractionStatus.Started);
		HandleTouch(TouchStatus.Started);

		StartRipple(touchEventArgs.Event.GetX(), touchEventArgs.Event.GetY());

		if (DisallowTouchThreshold > 0)
		{
			viewGroup?.Parent?.RequestDisallowInterceptTouchEvent(true);
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

		var diffX = Math.Abs(touchEventArgs.Event.GetX() - startX) / this.view?.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
		var diffY = Math.Abs(touchEventArgs.Event.GetY() - startY) / this.view?.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
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

			switch (status)
			{
				case TouchStatus.Started:
					StartRipple(touchEventArgs.Event.GetX(), touchEventArgs.Event.GetY());
					break;
				case TouchStatus.Canceled:
					EndRipple();
					break;
				case TouchStatus.Completed:
					break;
				default:
					throw new NotSupportedException($"{nameof(TouchStatus)} {status} not yet supported");
			}
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

	void StartRipple(float x, float y)
	{
		if (!IsEnabled || !ShouldUseNativeAnimation)
		{
			return;
		}

		if (CanExecute)
		{
			UpdateRipple(NativeAnimationColor);
			if (rippleView is not null)
			{
				rippleView.Enabled = true;
				ripple?.SetHotspot(x, y);
				rippleView.Pressed = true;
			}
			else if (IsForegroundRippleWithTapGestureRecognizer && view is not null)
			{
				ripple?.SetHotspot(x, y);
				view.Pressed = true;
			}
		}
		else if (rippleView is null)
		{
			UpdateRipple(Colors.Transparent);
		}
	}

	partial void PlatformDispose()
	{
		ripple?.Dispose();
		ripple = null;

		rippleView?.Dispose();
		rippleView = null;

		view?.Dispose();
		view = null;

		viewGroup?.Dispose();
		viewGroup = null;

		accessibilityListener?.Dispose();
		accessibilityListener = null;

		accessibilityManager?.Dispose();
		accessibilityManager = null;
	}

	sealed class AccessibilityListener : Java.Lang.Object,
		AccessibilityManager.IAccessibilityStateChangeListener,
		AccessibilityManager.ITouchExplorationStateChangeListener
	{
		TouchBehavior? platformTouchEffect;

		internal AccessibilityListener(TouchBehavior platformTouchEffect)
		{
			this.platformTouchEffect = platformTouchEffect;
		}

		public void OnAccessibilityStateChanged(bool enabled)
		{
			platformTouchEffect?.UpdateClickHandler();
		}

		public void OnTouchExplorationStateChanged(bool enabled)
		{
			platformTouchEffect?.UpdateClickHandler();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				platformTouchEffect = null;
			}

			base.Dispose(disposing);
		}
	}
}