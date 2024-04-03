using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using static System.OperatingSystem;
using AView = Android.Views.View;
using MColor = Microsoft.Maui.Graphics.Color;
using Trace = System.Diagnostics.Trace;

namespace CommunityToolkit.Maui.Behaviors;

public partial class TouchBehavior
{
	static readonly MColor defaultNativeAnimationColor = MColor.FromRgba(128, 128, 128, 64);

	bool isHoverSupported;
	float startX;
	float startY;
	AView? view;
	ViewGroup? viewGroup;

	AccessibilityManager? accessibilityManager;
	AccessibilityListener? accessibilityListener;

	bool IsAccessibilityMode => accessibilityManager is not null
		&& accessibilityManager.IsEnabled
		&& accessibilityManager.IsTouchExplorationEnabled;

	internal bool IsCanceled { get; set; }

	/// <summary>
	/// Attaches the behavior to the platform view.
	/// </summary>
	/// <param name="bindable">Maui Visual Element</param>
	/// <param name="platformView">Native View</param>
	protected override void OnAttachedTo(VisualElement bindable, AView platformView)
	{
		base.OnAttachedTo(bindable, platformView);

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
		base.OnDetachedFrom(bindable, platformView);

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

			if (view is not null)
			{
				view.Touch -= OnTouch;
				view.Click -= OnClick;
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

	void UpdateClickHandler()
	{
		if (view is null || !view.IsAlive())
		{
			return;
		}

		view.Click -= OnClick;
		if (IsAccessibilityMode || (IsEnabled && (Element?.IsEnabled ?? false)))
		{
			view.Click += OnClick;
			return;
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
		TouchBehavior? platformTouchBehavior;

		internal AccessibilityListener(TouchBehavior platformTouchBehavior)
		{
			this.platformTouchBehavior = platformTouchBehavior;
		}

		public void OnAccessibilityStateChanged(bool enabled)
		{
			platformTouchBehavior?.UpdateClickHandler();
		}

		public void OnTouchExplorationStateChanged(bool enabled)
		{
			platformTouchBehavior?.UpdateClickHandler();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				platformTouchBehavior = null;
			}

			base.Dispose(disposing);
		}
	}
}