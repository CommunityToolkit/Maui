using CommunityToolkit.Maui.Core;
using CoreGraphics;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Behaviors;

public partial class TouchBehavior
{
	UIGestureRecognizer? touchGesture;
	UIGestureRecognizer? hoverGesture;

	// The UIButton of a Button or an ImageButton raises Clicked for a tap itself (TouchUpInside): the behavior's recognizer leaves the
	// button's touches to it (TouchUITapGestureRecognizer)
	internal bool IsClickedRaisedByPlatformView => Element?.Handler?.PlatformView is UIButton;

	/// <summary>
	/// Attaches the behavior to the platform view.
	/// </summary>
	/// <param name="bindable">Maui Visual Element</param>
	/// <param name="platformView">Native View</param>
	protected override void OnAttachedTo(VisualElement bindable, UIView platformView)
	{
		base.OnAttachedTo(bindable, platformView);

		Element = bindable;

		touchGesture = new TouchUITapGestureRecognizer(this);

		platformView.AddGestureRecognizer(touchGesture);

		if (OperatingSystem.IsIOSVersionAtLeast(13))
		{
			hoverGesture = new UIHoverGestureRecognizer(OnHover);
			platformView.AddGestureRecognizer(hoverGesture);
		}

		platformView.UserInteractionEnabled = true;
	}

	/// <summary>
	/// Detaches the behavior from the platform view.
	/// </summary>
	/// <param name="bindable">Maui Visual Element</param>
	/// <param name="platformView">Native View</param>
	protected override void OnDetachedFrom(VisualElement bindable, UIView platformView)
	{
		// Element must be cleared before the base class clears View (which Element returns): the Element setter undoes what the attach did
		// to the element (the layout's ChildAdded subscription, the children's InputTransparent) only while it still knows the element.
		Element = null;
		base.OnDetachedFrom(bindable, platformView);

		if (touchGesture is not null)
		{
			platformView.RemoveGestureRecognizer(touchGesture);
			touchGesture?.Dispose();
			touchGesture = null;
		}

		if (hoverGesture is not null)
		{
			platformView.RemoveGestureRecognizer(hoverGesture);
			hoverGesture?.Dispose();
			hoverGesture = null;
		}

		// The attach turned user interaction on. MAUI sets it from InputTransparent (MAUI layouts track it for their hit testing) and, for a
		// view that is not a UIControl, from IsEnabled (whichever changed last), so the element's handler applies them again instead of the
		// value read at attach, which can be out of date: a disabled or input transparent view takes no touches. Another TouchBehavior that is
		// still attached to the element needs user interaction: that one gives it back.
		if (GetOtherAttachedTouchBehavior(bindable, this) is null)
		{
			bindable.Handler?.UpdateValue(nameof(VisualElement.InputTransparent));

			if (!bindable.IsEnabled)
			{
				bindable.Handler?.UpdateValue(nameof(VisualElement.IsEnabled));
			}
		}
	}

	void OnHover()
	{
		if (!IsEnabled)
		{
			return;
		}

		switch (hoverGesture?.State)
		{
			case UIGestureRecognizerState.Began:
			case UIGestureRecognizerState.Changed:
				HandleHover(HoverStatus.Entered);
				break;
			case UIGestureRecognizerState.Ended:
			case UIGestureRecognizerState.Cancelled:
				HandleHover(HoverStatus.Exited);
				break;
		}
	}

	partial void PlatformDispose()
	{
		// The recognizers are removed from the view first: UIKit would keep calling a disposed recognizer for every touch on the view.
		if (touchGesture is not null)
		{
			touchGesture.View?.RemoveGestureRecognizer(touchGesture);
			touchGesture.Dispose();
			touchGesture = null;
		}

		if (hoverGesture is not null)
		{
			hoverGesture.View?.RemoveGestureRecognizer(hoverGesture);
			hoverGesture.Dispose();
			hoverGesture = null;
		}
	}

	sealed class TouchUITapGestureRecognizer : UIGestureRecognizer
	{
		readonly TouchBehavior behavior;

		CGPoint? startPoint;

		bool isCanceled;

		public TouchUITapGestureRecognizer(TouchBehavior behavior)
		{
			this.behavior = behavior;

			// The view gets its touches as without the recognizer. The recognizer never recognizes (it stays Possible), so with UIKit's
			// default it would hold the ended touches back from the view for good: a control that acts when a touch ends inside it (the
			// UIButton of a Button, an ImageButton or a CheckBox) would never see its touch end (no TouchUpInside, no Released) and would
			// not take another touch.
			CancelsTouchesInView = false;
			DelaysTouchesEnded = false;
			Delegate = new TouchUITapGestureRecognizerDelegate();
		}

		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			base.TouchesBegan(touches, evt);

			if (!behavior.IsEnabled)
			{
				return;
			}

			isCanceled = false;
			startPoint = GetTouchPoint(touches);

			HandleTouch(TouchStatus.Started, TouchInteractionStatus.Started);
		}

		public override void TouchesEnded(NSSet touches, UIEvent evt)
		{
			base.TouchesEnded(touches, evt);

			if (!behavior.IsEnabled)
			{
				return;
			}

			if (isCanceled)
			{
				// The touch was canceled while it moved (it left the view or went past DisallowTouchThreshold) and keeps that status,
				// but it has ended now: the interaction completes, as on the other platforms.
				behavior.HandleUserInteraction(TouchInteractionStatus.Completed);
			}
			else
			{
				HandleTouch(behavior.CurrentTouchStatus is TouchStatus.Started ? TouchStatus.Completed : TouchStatus.Canceled,
					TouchInteractionStatus.Completed);
			}

			isCanceled = true;
		}

		public override void TouchesCancelled(NSSet touches, UIEvent evt)
		{
			base.TouchesCancelled(touches, evt);

			if (!behavior.IsEnabled)
			{
				return;
			}

			if (isCanceled)
			{
				// As in TouchesEnded: a touch that was canceled while it moved keeps that status, and its interaction completes now.
				behavior.HandleUserInteraction(TouchInteractionStatus.Completed);
			}
			else
			{
				HandleTouch(TouchStatus.Canceled, TouchInteractionStatus.Completed);
			}

			isCanceled = true;
		}

		public override void TouchesMoved(NSSet touches, UIEvent evt)
		{
			base.TouchesMoved(touches, evt);

			if (!behavior.IsEnabled)
			{
				return;
			}

			var disallowTouchThreshold = behavior.DisallowTouchThreshold;
			var point = GetTouchPoint(touches);
			if (point is not null && startPoint is not null && disallowTouchThreshold > 0)
			{
				var diffX = Math.Abs(point.Value.X - startPoint.Value.X);
				var diffY = Math.Abs(point.Value.Y - startPoint.Value.Y);
				var maxDiff = Math.Max(diffX, diffY);
				if (maxDiff > disallowTouchThreshold)
				{
					HandleTouch(TouchStatus.Canceled, TouchInteractionStatus.Completed);
					isCanceled = true;
					return;
				}
			}

			var status = point is not null && View.Bounds.Contains(point.Value)
				? TouchStatus.Started
				: TouchStatus.Canceled;

			if (behavior.CurrentTouchStatus != status)
			{
				HandleTouch(status);
			}

			if (status is TouchStatus.Canceled)
			{
				isCanceled = true;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Delegate.Dispose();
			}

			base.Dispose(disposing);
		}

		void HandleTouch(TouchStatus status, TouchInteractionStatus? interactionStatus = null)
		{
			if (isCanceled || !behavior.IsEnabled)
			{
				return;
			}

			if (interactionStatus is TouchInteractionStatus.Started)
			{
				behavior.HandleUserInteraction(TouchInteractionStatus.Started);
				interactionStatus = null;
			}

			behavior.HandleTouch(status);

			if (interactionStatus.HasValue)
			{
				behavior.HandleUserInteraction(interactionStatus.Value);
			}
		}

		CGPoint? GetTouchPoint(NSSet touches)
		{
			return (touches?.AnyObject as UITouch)?.LocationInView(View);
		}

		sealed class TouchUITapGestureRecognizerDelegate : UIGestureRecognizerDelegate
		{
			public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
			{
				if (gestureRecognizer is TouchUITapGestureRecognizer touchGesture && otherGestureRecognizer is UIPanGestureRecognizer &&
					otherGestureRecognizer.State is UIGestureRecognizerState.Began)
				{
					touchGesture.HandleTouch(TouchStatus.Canceled, TouchInteractionStatus.Completed);
					touchGesture.isCanceled = true;
				}

				return true;
			}

			public override bool ShouldReceiveTouch(UIGestureRecognizer recognizer, UITouch touch)
			{
				if (recognizer.View.IsDescendantOfView(touch.View))
				{
					return true;
				}

				return touch.View.IsDescendantOfView(recognizer.View) && (touch.View.GestureRecognizers is null || touch.View.GestureRecognizers.Length == 0);
			}
		}
	}
}