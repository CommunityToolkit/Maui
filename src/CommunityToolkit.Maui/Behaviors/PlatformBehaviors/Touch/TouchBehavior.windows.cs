using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;

namespace CommunityToolkit.Maui.Behaviors;
public partial class TouchBehavior
{
	const string pointerDownAnimationKey = "PointerDownAnimation";

	const string pointerUpAnimationKey = "PointerUpAnimation";

	bool isPressed;

	bool isIntentionalCaptureLoss;

	Storyboard? pointerDownStoryboard;

	Storyboard? pointerUpStoryboard;

	protected override void OnAttachedTo(VisualElement bindable, FrameworkElement platformView)
	{
		Element = bindable;
		if (NativeAnimation)
		{
			if (string.IsNullOrEmpty(platformView.Name))
			{
				platformView.Name = Guid.NewGuid().ToString();
			}

			if (platformView.Resources.ContainsKey(pointerDownAnimationKey))
			{
				pointerDownStoryboard = (Storyboard)platformView.Resources[pointerDownAnimationKey];
			}
			else
			{
				pointerDownStoryboard = new();
				var downThemeAnimation = new PointerDownThemeAnimation();

				Storyboard.SetTargetName(downThemeAnimation, platformView.Name);
				pointerDownStoryboard.Children.Add(downThemeAnimation);
				platformView.Resources.Add(new KeyValuePair<object, object>(pointerDownAnimationKey, pointerDownStoryboard));
			}

			if (platformView.Resources.ContainsKey(pointerUpAnimationKey))
			{
				pointerUpStoryboard = (Storyboard)platformView.Resources[pointerUpAnimationKey];
			}
			else
			{
				pointerUpStoryboard = new();
				var upThemeAnimation = new PointerUpThemeAnimation();

				Storyboard.SetTargetName(upThemeAnimation, platformView.Name);

				pointerUpStoryboard.Children.Add(upThemeAnimation);

				platformView.Resources.Add(new KeyValuePair<object, object>(pointerUpAnimationKey, pointerUpStoryboard));
			}

			platformView.PointerPressed += OnPointerPressed;
			platformView.PointerReleased += OnPointerReleased;
			platformView.PointerCanceled += OnPointerCanceled;
			platformView.PointerExited += OnPointerExited;
			platformView.PointerEntered += OnPointerEntered;
			platformView.PointerCaptureLost += OnPointerCaptureLost;
		}
	}

	protected override void OnDetachedFrom(VisualElement bindable, FrameworkElement platformView)
	{
		if (IsDisabled)
		{
			return;
		}

		Element = null;

		platformView.PointerPressed -= OnPointerPressed;
		platformView.PointerReleased -= OnPointerReleased;
		platformView.PointerCanceled -= OnPointerCanceled;
		platformView.PointerExited -= OnPointerExited;
		platformView.PointerEntered -= OnPointerEntered;
		platformView.PointerCaptureLost -= OnPointerCaptureLost;

		isPressed = false;
	}



	void OnPointerEntered(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || IsDisabled)
		{
			return;
		}

		HandleHover(HoverStatus.Entered);

		if (isPressed)
		{
			HandleTouch(TouchStatus.Started);
			AnimateTilt(pointerDownStoryboard);
		}
	}

	void OnPointerExited(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || IsDisabled)
		{
			return;
		}

		if (isPressed)
		{
			HandleTouch(TouchStatus.Canceled);
			AnimateTilt(pointerUpStoryboard);
		}

		HandleHover(HoverStatus.Exited);
	}

	void OnPointerCanceled(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || IsDisabled)
		{
			return;
		}

		isPressed = false;

		HandleTouch(TouchStatus.Canceled);
		HandleUserInteraction(TouchInteractionStatus.Completed);
		HandleHover(HoverStatus.Exited);

		AnimateTilt(pointerUpStoryboard);
	}

	void OnPointerCaptureLost(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || IsDisabled)
		{
			return;
		}

		if (isIntentionalCaptureLoss)
		{
			return;
		}

		isPressed = false;

		if (Status != TouchStatus.Canceled)
		{
			HandleTouch(TouchStatus.Canceled);
		}

		HandleUserInteraction(TouchInteractionStatus.Completed);

		if (HoverStatus != HoverStatus.Exited)
		{
			HandleHover(HoverStatus.Exited);
		}

		AnimateTilt(pointerUpStoryboard);
	}

	void OnPointerReleased(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || IsDisabled)
		{
			return;
		}

		if (isPressed && (HoverStatus == HoverStatus.Entered))
		{
			HandleTouch(TouchStatus.Completed);
			AnimateTilt(pointerUpStoryboard);
		}
		else if (HoverStatus != HoverStatus.Exited)
		{
			HandleTouch(TouchStatus.Canceled);
			AnimateTilt(pointerUpStoryboard);
		}

		HandleUserInteraction(TouchInteractionStatus.Completed);

		isPressed = false;
		isIntentionalCaptureLoss = true;
	}

	void OnPointerPressed(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || IsDisabled || sender is not FrameworkElement container)
		{
			return;
		}

		isPressed = true;
		container.CapturePointer(e.Pointer);

		HandleUserInteraction(TouchInteractionStatus.Started);
		HandleTouch(TouchStatus.Started);

		AnimateTilt(pointerDownStoryboard);

		isIntentionalCaptureLoss = false;
	}

	void AnimateTilt(Storyboard? storyboard)
	{
		if (storyboard is not null && Element is not null && NativeAnimation)
		{
			try
			{
				storyboard.Stop();
				storyboard.Begin();
			}
			catch
			{
				// Suppress
			}
		}
	}
}
