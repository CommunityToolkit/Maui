using CommunityToolkit.Maui.Core;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Behaviors;

public partial class TouchBehavior
{
	UIGestureRecognizer? touchGesture;
	UIGestureRecognizer? hoverGesture;

	/// <summary>
	/// Attaches the behavior to the platform view.
	/// </summary>
	/// <param name="bindable">Maui Visual Element</param>
	/// <param name="platformView">Native View</param>
	protected override void OnAttachedTo(VisualElement bindable, UIView platformView)
	{
		Element = bindable;

		touchGesture = new TouchUITapGestureRecognizer(this);

		if (((platformView as IVisualNativeElementRenderer)?.Control ?? platformView) is UIButton button)
		{
			button.AllTouchEvents += HandleAllTouchEvents;
			((TouchUITapGestureRecognizer)touchGesture).IsButton = true;
		}

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
		if (((platformView as IVisualNativeElementRenderer)?.Control ?? platformView) is UIButton button)
		{
			button.AllTouchEvents -= HandleAllTouchEvents;
		}

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

		Element = null;
	}

	static void HandleAllTouchEvents(object? sender, EventArgs args)
	{
		if (sender is not UIButton button)
		{
			throw new ArgumentException($"{nameof(sender)} must be Type {nameof(UIButton)}", nameof(sender));
		}
		
		// Prevent Button Highlight
		button.Highlighted = false;
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
				HandleHover(HoverStatus.Exited);
				break;
		}
	}

	partial void PlatformDispose()
	{
		touchGesture?.Dispose();
		touchGesture = null;

		hoverGesture?.Dispose();
		hoverGesture = null;
	}

	sealed class TouchUITapGestureRecognizer : UIGestureRecognizer
	{
		readonly TouchBehavior behavior;

		float? defaultRadius;
		float? defaultShadowRadius;
		float? defaultShadowOpacity;
		CGPoint? startPoint;

		bool isCanceled;

		public TouchUITapGestureRecognizer(TouchBehavior behavior)
		{
			this.behavior = behavior;
			CancelsTouchesInView = false;
			Delegate = new TouchUITapGestureRecognizerDelegate();
		}

		public bool IsButton { get; set; }

		public override async void TouchesBegan(NSSet touches, UIEvent evt)
		{
			base.TouchesBegan(touches, evt);

			if (!behavior.IsEnabled)
			{
				return;
			}

			isCanceled = false;
			startPoint = GetTouchPoint(touches);

			await HandleTouch(TouchStatus.Started, CancellationToken.None, TouchInteractionStatus.Started);
		}

		public override async void TouchesEnded(NSSet touches, UIEvent evt)
		{
			base.TouchesEnded(touches, evt);

			if (!behavior.IsEnabled)
			{
				return;
			}

			await HandleTouch(behavior.CurrentTouchStatus is TouchStatus.Started ? TouchStatus.Completed : TouchStatus.Canceled,
				CancellationToken.None,
				TouchInteractionStatus.Completed);

			isCanceled = true;
		}

		public override async void TouchesCancelled(NSSet touches, UIEvent evt)
		{
			base.TouchesCancelled(touches, evt);

			if (!behavior.IsEnabled)
			{
				return;
			}

			await HandleTouch(TouchStatus.Canceled, CancellationToken.None, TouchInteractionStatus.Completed);

			isCanceled = true;
		}

		public override async void TouchesMoved(NSSet touches, UIEvent evt)
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
					await HandleTouch(TouchStatus.Canceled, CancellationToken.None, TouchInteractionStatus.Completed);
					isCanceled = true;
					return;
				}
			}

			var status = point is not null && View.Bounds.Contains(point.Value)
				? TouchStatus.Started
				: TouchStatus.Canceled;

			if (behavior.CurrentTouchStatus != status)
			{
				await HandleTouch(status, CancellationToken.None);
			}

			if (status is TouchStatus.Canceled)
			{
				isCanceled = true;
			}
		}

		Task HandleTouch(TouchStatus status, CancellationToken token, TouchInteractionStatus? interactionStatus = null)
		{
			if (isCanceled || !behavior.IsEnabled)
			{
				return Task.CompletedTask;
			}

			var canExecuteAction = behavior.CanExecute;

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

			if (behavior.Element is null
				|| (!behavior.ShouldUseNativeAnimation && !IsButton)
				|| (!canExecuteAction && status is TouchStatus.Started))
			{
				return Task.CompletedTask;
			}

			var color = behavior.NativeAnimationColor;
			var radius = behavior.NativeAnimationRadius;
			var shadowRadius = behavior.NativeAnimationShadowRadius;
			var isStarted = status is TouchStatus.Started;

			defaultRadius = (float?)(defaultRadius ?? View.Layer.CornerRadius);
			defaultShadowRadius = (float?)(defaultShadowRadius ?? View.Layer.ShadowRadius);
			defaultShadowOpacity ??= View.Layer.ShadowOpacity;

			var tcs = new TaskCompletionSource<UIViewAnimatingPosition>();
			UIViewPropertyAnimator.CreateRunningPropertyAnimator(.2, 0, UIViewAnimationOptions.AllowUserInteraction,
				() =>
				{
					if (color is null)
					{
						View.Layer.Opacity = isStarted ? 0.5f : (float)behavior.Element.Opacity;
					}
					else
					{
						View.Layer.BackgroundColor = (isStarted ? color : behavior.Element.BackgroundColor).ToCGColor();
					}

					View.Layer.CornerRadius = isStarted ? (nfloat)radius.GetValueOrDefault() : defaultRadius.GetValueOrDefault();

					if (shadowRadius >= 0)
					{
						View.Layer.ShadowRadius = isStarted ? (nfloat)shadowRadius : defaultShadowRadius.GetValueOrDefault();
						View.Layer.ShadowOpacity = isStarted ? 0.7f : defaultShadowOpacity.GetValueOrDefault();
					}
				}, endPos => tcs.SetResult(endPos));

			return tcs.Task.WaitAsync(token);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Delegate.Dispose();
			}

			base.Dispose(disposing);
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
					HandleTouch();
				}

				return true;

				async void HandleTouch()
				{
					await touchGesture.HandleTouch(TouchStatus.Canceled, CancellationToken.None, TouchInteractionStatus.Completed);
					touchGesture.isCanceled = true;
				}
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