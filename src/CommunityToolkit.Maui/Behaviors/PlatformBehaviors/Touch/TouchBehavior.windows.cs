using System.Diagnostics;
using CommunityToolkit.Maui.Core;
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

	/// <inheritdoc/>
	protected override void OnAttachedTo(VisualElement bindable, FrameworkElement platformView)
	{
		Element = bindable;
		if (ShouldUseNativeAnimation)
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

	/// <inheritdoc/>
	protected override void OnDetachedFrom(VisualElement bindable, FrameworkElement platformView)
	{
		if (!IsEnabled)
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



	async void OnPointerEntered(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || !IsEnabled)
		{
			return;
		}

		await HandleHover(HoverStatus.Entered, CancellationToken.None);

		if (isPressed)
		{
			await HandleTouch(TouchStatus.Started, CancellationToken.None);
			AnimateTilt(pointerDownStoryboard);
		}
	}

	async void OnPointerExited(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || !IsEnabled)
		{
			return;
		}

		if (isPressed)
		{
			await HandleTouch(TouchStatus.Canceled, CancellationToken.None);
			AnimateTilt(pointerUpStoryboard);
		}

		await HandleHover(HoverStatus.Exited, CancellationToken.None);
	}

	async void OnPointerCanceled(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || !IsEnabled)
		{
			return;
		}

		isPressed = false;

		await HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		HandleUserInteraction(TouchInteractionStatus.Completed);
		await HandleHover(HoverStatus.Exited, CancellationToken.None	);

		AnimateTilt(pointerUpStoryboard);
	}

	async void OnPointerCaptureLost(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || !IsEnabled)
		{
			return;
		}

		if (isIntentionalCaptureLoss)
		{
			return;
		}

		isPressed = false;

		if (CurrentTouchStatus is not TouchStatus.Canceled)
		{
			await HandleTouch(TouchStatus.Canceled, CancellationToken.None);
		}

		HandleUserInteraction(TouchInteractionStatus.Completed);

		if (CurrentHoverStatus is not HoverStatus.Exited)
		{
			await HandleHover(HoverStatus.Exited, CancellationToken.None);
		}

		AnimateTilt(pointerUpStoryboard);
	}

	async void OnPointerReleased(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || !IsEnabled)
		{
			return;
		}

		if (isPressed && (CurrentHoverStatus is HoverStatus.Entered))
		{
			await HandleTouch(TouchStatus.Completed, CancellationToken.None);
			AnimateTilt(pointerUpStoryboard);
		}
		else if (CurrentHoverStatus is not HoverStatus.Exited)
		{
			await HandleTouch(TouchStatus.Canceled, CancellationToken.None);
			AnimateTilt(pointerUpStoryboard);
		}

		HandleUserInteraction(TouchInteractionStatus.Completed);

		isPressed = false;
		isIntentionalCaptureLoss = true;
	}

	async void OnPointerPressed(object? sender, PointerRoutedEventArgs e)
	{
		if (Element is null || !IsEnabled || sender is not FrameworkElement container)
		{
			return;
		}

		isPressed = true;
		container.CapturePointer(e.Pointer);

		HandleUserInteraction(TouchInteractionStatus.Started);
		await HandleTouch(TouchStatus.Started, CancellationToken.None);

		AnimateTilt(pointerDownStoryboard);

		isIntentionalCaptureLoss = false;
	}

	void AnimateTilt(Storyboard? storyboard)
	{
		if (storyboard is not null && Element is not null && ShouldUseNativeAnimation)
		{
			try
			{
				storyboard.Stop();
				storyboard.Begin();
			}
			catch(Exception e)
			{
				Trace.WriteLine(e);
			}
		}
	}
}
