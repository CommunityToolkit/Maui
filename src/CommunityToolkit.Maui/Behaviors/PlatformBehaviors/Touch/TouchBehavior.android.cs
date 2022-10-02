using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using static System.OperatingSystem;
using AView = Android.Views.View;
using Color = Android.Graphics.Color;
using MColor = Microsoft.Maui.Graphics.Color;
using MView = Microsoft.Maui.Controls.View;

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
	AView? platformView = null;
	ViewGroup? viewGroup;

	readonly bool isAtLeastM = IsAndroidVersionAtLeast((int)BuildVersionCodes.M);

	internal bool IsCanceled { get; set; }

	bool IsForegroundRippleWithTapGestureRecognizer =>
		ripple is not null &&
		platformView is not null &&
		ripple.IsAlive() &&
		platformView.IsAlive() &&
		(isAtLeastM ? platformView.Foreground : platformView.Background) == ripple &&
		element is MView view &&
		view.GestureRecognizers.Any(gesture => gesture is TapGestureRecognizer);

	protected override void OnAttachedTo(VisualElement bindable, AView platformView)
	{
		element = bindable;
		this.platformView = platformView;
		viewGroup = Microsoft.Maui.Platform.ViewExtensions.GetParentOfType<ViewGroup>(platformView);
		if (IsDisabled)
		{
			return;
		}

		platformView.Touch += OnTouch;
		UpdateClickHandler();

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

	protected override void OnDetachedFrom(VisualElement bindable, AView platformView)
	{
		element = bindable;
		this.platformView = platformView;

		if (element is null)
		{
			return;
		}

		try
		{
			//if (accessibilityManager != null && accessibilityListener != null)
			//{
			//	accessibilityManager.RemoveAccessibilityStateChangeListener(accessibilityListener);
			//	accessibilityManager.RemoveTouchExplorationStateChangeListener(accessibilityListener);
			//	accessibilityListener.Dispose();
			//	accessibilityManager = null;
			//	accessibilityListener = null;
			//}

			RemoveRipple();

			if (this.platformView is not null)
			{
				this.platformView.LayoutChange -= OnLayoutChange;
				this.platformView.Touch -= OnTouch;
				this.platformView.Click -= OnClick;
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
		if (sender is not AView view || (viewGroup as IVisualElementRenderer)?.Element is null || rippleView is null)
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
			? platformView?.Foreground
		: platformView?.Background;

		var isBorderLess = NativeAnimationBorderless;
		var isEmptyDrawable = Element is Layout || drawable == null;
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

		if (platformView is not null)
		{
			if (isAtLeastM && platformView.Foreground == ripple)
			{
				platformView.Foreground = null;
			}
			else if (platformView.Background == ripple)
			{
				platformView.Background = null;
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
			ripple.Radius = (int)(platformView?.Context?.Resources?.DisplayMetrics?.Density * NativeAnimationRadius ?? throw new NullReferenceException());
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
		if (!platformView.IsAlive() || platformView is null)
		{
			return;
		}

		platformView.Click -= OnClick;
		//if (IsAccessibilityMode || ((IsAvailable) && (element?.IsEnabled ?? false)))
		if (((IsAvailable) && (element?.IsEnabled ?? false)))
		{
			platformView.Click += OnClick;
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

		if (viewGroup is null && platformView is not null)
		{
			if (IsAndroidVersionAtLeast((int)BuildVersionCodes.M))
			{
				platformView.Foreground = ripple;
			}
			else
			{
				platformView.Background = ripple;
			}

			return;
		}

		if (rippleView is null)
		{
			rippleView = new FrameLayout(viewGroup?.Context ?? platformView?.Context ?? throw new NullReferenceException())
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

		//if (!IsAccessibilityMode)
		//{
		//	return;
		//}

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
			if (platformView?.Pressed ?? false)
			{
				platformView.Pressed = false;
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

		//if (IsAccessibilityMode)
		//{
		//	return;
		//}

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

		var diffX = Math.Abs(e.Event.GetX() - startX) / platformView?.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
		var diffY = Math.Abs(e.Event.GetY() - startY) / platformView?.Context?.Resources?.DisplayMetrics?.Density ?? throw new NullReferenceException();
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
			else if (IsForegroundRippleWithTapGestureRecognizer && platformView is not null)
			{
				ripple?.SetHotspot(x, y);
				platformView.Pressed = true;
			}
		}
		else if (rippleView is null)
		{
			UpdateRipple(Colors.Transparent);
		}
	}
}
