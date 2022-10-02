using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using CommunityToolkit.Maui.Core;
using static System.OperatingSystem;
using AView = Android.Views.View;
using Color = Android.Graphics.Color;
using MColor = Microsoft.Maui.Graphics.Color;
using MView = Microsoft.Maui.Controls.View;

namespace CommunityToolkit.Maui.Behaviors;
public partial class TouchBehavior
{
	bool isHoverSupported;
	RippleDrawable? ripple;
	AView? rippleView;
	float startX;
	float startY;
	MColor? rippleColor;
	int rippleRadius = -1;
	AView? platformView = null;

	internal bool IsCanceled { get; set; }

	bool IsForegroundRippleWithTapGestureRecognizer =>
		ripple is not null &&
		platformView is not null &&
		ripple.IsAlive() &&
		platformView.IsAlive() &&
		(IsAndroidVersionAtLeast((int)BuildVersionCodes.M) ? platformView.Foreground : platformView.Background) == ripple &&
		element is MView view &&
		view.GestureRecognizers.Any(gesture => gesture is TapGestureRecognizer);

	protected override void OnAttachedTo(VisualElement bindable, AView platformView)
	{
		element = bindable;
		this.platformView = platformView;

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

	void OnLayoutChange(object? sender, AView.LayoutChangeEventArgs e)
	{
		throw new NotImplementedException();
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
			GetGroup(platformView)?.Parent?.RequestDisallowInterceptTouchEvent(false);
		}

		effect?.HandleTouch(status);

		effect?.HandleUserInteraction(TouchInteractionStatus.Completed);

		EndRipple();
	}

	void OnTouch(object? sender, AView.TouchEventArgs e)
	{
		throw new NotImplementedException();
	}


	ViewGroup? GetGroup(AView? view)
	{
		return view is null ? null : Microsoft.Maui.Platform.ViewExtensions.GetParentOfType<ViewGroup>(view);
	}
}
