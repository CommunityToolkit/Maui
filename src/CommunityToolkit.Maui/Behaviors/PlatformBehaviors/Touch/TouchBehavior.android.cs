using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Media.Effect;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Android.Widget;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Platform;
using static System.OperatingSystem;
using AView = Android.Views.View;
using Color = Android.Graphics.Color;
using MColor = Microsoft.Maui.Graphics.Color;
using MView = Microsoft.Maui.Controls.View;
using PlatformView = Android.Views.View;
using ParentView = Android.Views.IViewParent;

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
	int rippleRadius = -1;
	AView? view = null;
	ViewGroup? viewGroup;

	AccessibilityManager? accessibilityManager;
	AccessibilityListener? accessibilityListener;

	bool IsAccessibilityMode => accessibilityManager is not null
		&& accessibilityManager.IsEnabled
		&& accessibilityManager.IsTouchExplorationEnabled;

	readonly bool isAtLeastM = IsAndroidVersionAtLeast((int)BuildVersionCodes.M);

	internal bool IsCanceled { get; set; }

	bool IsForegroundRippleWithTapGestureRecognizer =>
		ripple is not null &&
		this.view is not null &&
		ripple.IsAlive() &&
		this.view.IsAlive() &&
		(isAtLeastM ? this.view.Foreground : this.view.Background) == ripple &&
		element is MView view &&
		view.GestureRecognizers.Any(gesture => gesture is TapGestureRecognizer);

	/// <summary>
	/// Attaches the behavior to the platform view.
	/// </summary>
	/// <param name="bindable">Maui Visual Element</param>
	/// <param name="platformView">Native View</param>
	protected override void OnAttachedTo(VisualElement bindable, AView platformView)
	{
		Element = bindable;
		this.view = platformView;
		viewGroup = Microsoft.Maui.Platform.ViewExtensions.GetParentOfType<ViewGroup>(platformView);
		if (IsDisabled)
		{
			return;
		}

		platformView.Touch += OnTouch;
		UpdateClickHandler();
		accessibilityManager = platformView.Context?.GetSystemService(Context.AccessibilityService) as AccessibilityManager;

		if (accessibilityManager is not null)
		{
			accessibilityListener = new AccessibilityListener(this);
			accessibilityManager.AddAccessibilityStateChangeListener(accessibilityListener);
			accessibilityManager.AddTouchExplorationStateChangeListener(accessibilityListener);
		}

		if (!IsAndroidVersionAtLeast((int)BuildVersionCodes.Lollipop) || !NativeAnimation)
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
		element = bindable;
		this.view = platformView;

		if (element is null)
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

			if (this.view is not null)
			{
				this.view.LayoutChange -= OnLayoutChange;
				this.view.Touch -= OnTouch;
				this.view.Click -= OnClick;
			}

			if (rippleView is not null)
			{
				rippleView.Pressed = false;
				viewGroup?.RemoveView(rippleView);
				rippleView.Dispose();
				rippleView = null;
			}
		}
		catch (ObjectDisposedException)
		{
			// Suppress exception
		}
		isHoverSupported = false;
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

		var drawable = isAtLeastM && viewGroup is null
			? view?.Foreground
		: view?.Background;

		var isBorderLess = NativeAnimationBorderless;
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
			if (isAtLeastM && view.Foreground == ripple)
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
			rippleView.Foreground = null;
			rippleView.Background = null;
		}

		ripple.Dispose();
		ripple = null;
	}

	void UpdateRipple(MColor color)
	{
		if (IsDisabled || (color == rippleColor && NativeAnimationRadius == rippleRadius))
		{
			return;
		}

		rippleColor = color;
		rippleRadius = NativeAnimationRadius;
		ripple?.SetColor(GetColorStateList(color));
		if (isAtLeastM && ripple is not null)
		{
			ripple.Radius = (int)(view?.Context?.Resources?.DisplayMetrics?.Density * NativeAnimationRadius ?? throw new NullReferenceException());
		}
	}

	ColorStateList GetColorStateList(MColor? color)
	{
		var animationColor = color;
		animationColor ??= defaultNativeAnimationColor;

		return new ColorStateList(
			new[] { Array.Empty<int>() },
			new[] { (int)animationColor.ToAndroid() });
	}

	void UpdateClickHandler()
	{
		if (view is null || !view.IsAlive())
		{
			return;
		}

		view.Click -= OnClick;
		if (IsAccessibilityMode || ((IsAvailable) && (element?.IsEnabled ?? false)))
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

		var isBorderless = NativeAnimationBorderless;

		if (viewGroup is null && view is not null)
		{
			if (IsAndroidVersionAtLeast((int)BuildVersionCodes.M))
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
				Enabled = false,
			};

			viewGroup?.AddView(rippleView);
			rippleView.BringToFront();
		}

		viewGroup?.SetClipChildren(!isBorderless);

		if (isBorderless)
		{
			rippleView.Background = null;
			rippleView.Foreground = ripple;
		}
		else
		{
			rippleView.Foreground = null;
			rippleView.Background = ripple;
		}
	}

	void OnClick(object? sender, EventArgs args)
	{
		if (IsDisabled)
		{
			return;
		}

		if (!IsAccessibilityMode)
		{
			return;
		}

		IsCanceled = false;
		HandleEnd(TouchStatus.Completed);
	}
	void HandleEnd(TouchStatus status)
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
		if (IsDisabled)
		{
			return;
		}

		if (rippleView != null)
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
		e.Handled = false;

		if (IsDisabled)
		{
			return;
		}

		if (IsAccessibilityMode)
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
				OnTouchMove(sender, e);
				break;
			case MotionEventActions.HoverEnter:
				OnHoverEnter();
				break;
			case MotionEventActions.HoverExit:
				OnHoverExit();
				break;
		}
	}

	void OnTouchDown(AView.TouchEventArgs e)
	{
		_ = e.Event ?? throw new NullReferenceException();

		IsCanceled = false;

		startX = e.Event.GetX();
		startY = e.Event.GetY();

		HandleUserInteraction(TouchInteractionStatus.Started);
		HandleTouch(TouchStatus.Started);

		StartRipple(e.Event.GetX(), e.Event.GetY());

		if (DisallowTouchThreshold > 0)
		{
			viewGroup?.Parent?.RequestDisallowInterceptTouchEvent(true);
		}
	}

	void OnTouchUp()
		=> HandleEnd(Status == TouchStatus.Started ? TouchStatus.Completed : TouchStatus.Canceled);

	void OnTouchCancel()
		=> HandleEnd(TouchStatus.Canceled);

	void OnTouchMove(object? sender, AView.TouchEventArgs e)
	{
		if (IsCanceled || e.Event == null)
		{
			return;
		}

		var diffX = Math.Abs(e.Event.GetX() - startX) / this.view?.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
		var diffY = Math.Abs(e.Event.GetY() - startY) / this.view?.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
		var maxDiff = Math.Max(diffX, diffY);

		var disallowTouchThreshold = DisallowTouchThreshold;
		if (disallowTouchThreshold > 0 && maxDiff > disallowTouchThreshold)
		{
			HandleEnd(TouchStatus.Canceled);
			return;
		}

		if (sender is not AView view)
		{
			return;
		}

		var screenPointerCoords = new Point(view.Left + e.Event.GetX(), view.Top + e.Event.GetY());
		var viewRect = new Rect(view.Left, view.Top, view.Right - view.Left, view.Bottom - view.Top);
		var status = viewRect.Contains(screenPointerCoords) ? TouchStatus.Started : TouchStatus.Canceled;

		if (isHoverSupported && ((status == TouchStatus.Canceled && HoverStatus == HoverStatus.Entered)
			|| (status == TouchStatus.Started && HoverStatus == HoverStatus.Exited)))
		{
			HandleHover(status == TouchStatus.Started ? HoverStatus.Entered : HoverStatus.Exited);
		}

		if (Status != status)
		{
			HandleTouch(status);

			if (status == TouchStatus.Started)
			{
				StartRipple(e.Event.GetX(), e.Event.GetY());
			}

			if (status == TouchStatus.Canceled)
			{
				EndRipple();
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
		if (IsDisabled || !NativeAnimation)
		{
			return;
		}

		if (CanExecute)
		{
			UpdateRipple(NativeAnimationColor);
			if (rippleView is not null)
			{
				rippleView.Enabled = true;
				rippleView.BringToFront();
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

	sealed class AccessibilityListener : Java.Lang.Object,
											 AccessibilityManager.IAccessibilityStateChangeListener,
											 AccessibilityManager.ITouchExplorationStateChangeListener
	{
		TouchBehavior? platformTouchEffect;

		internal AccessibilityListener(TouchBehavior platformTouchEffect)
			=> this.platformTouchEffect = platformTouchEffect;

		public void OnAccessibilityStateChanged(bool enabled)
			=> platformTouchEffect?.UpdateClickHandler();

		public void OnTouchExplorationStateChanged(bool enabled)
			=> platformTouchEffect?.UpdateClickHandler();

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

static class JavaObjectExtensions
{
	public static bool IsDisposed(this Java.Lang.Object obj)
		=> obj.Handle == IntPtr.Zero;

	public static bool IsAlive(this Java.Lang.Object obj)
		=> obj != null && !obj.IsDisposed();

	public static bool IsDisposed(this global::Android.Runtime.IJavaObject obj)
		=> obj.Handle == IntPtr.Zero;

	public static bool IsAlive(this global::Android.Runtime.IJavaObject obj)
		=> obj != null && !obj.IsDisposed();
}