using System.Reflection;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Additional;

#region TouchBehavior Tests

public class TouchBehaviorTests
{
	[Fact]
	public void TouchBehavior_DefaultValues_AreCorrect()
	{
		var behavior = new TouchBehavior();

		Assert.True(behavior.IsEnabled);
		Assert.True(behavior.ShouldMakeChildrenInputTransparent);
		Assert.Null(behavior.Command);
		Assert.Null(behavior.CommandParameter);
		Assert.Null(behavior.LongPressCommand);
		Assert.Null(behavior.LongPressCommandParameter);
		Assert.Equal(500, behavior.LongPressDuration);
		Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
		Assert.Equal(TouchState.Default, behavior.CurrentTouchState);
		Assert.Equal(TouchInteractionStatus.Completed, behavior.CurrentInteractionStatus);
		Assert.Equal(HoverStatus.Exited, behavior.CurrentHoverStatus);
		Assert.Equal(HoverState.Default, behavior.CurrentHoverState);
		Assert.Null(behavior.DefaultBackgroundColor);
		Assert.Null(behavior.HoveredBackgroundColor);
		Assert.Null(behavior.PressedBackgroundColor);
		Assert.Null(behavior.DefaultOpacity);
		Assert.Null(behavior.HoveredOpacity);
		Assert.Null(behavior.PressedOpacity);
		Assert.Null(behavior.DefaultScale);
		Assert.Null(behavior.HoveredScale);
		Assert.Null(behavior.PressedScale);
		Assert.Null(behavior.DefaultTranslationX);
		Assert.Null(behavior.HoveredTranslationX);
		Assert.Null(behavior.PressedTranslationX);
		Assert.Null(behavior.DefaultTranslationY);
		Assert.Null(behavior.HoveredTranslationY);
		Assert.Null(behavior.PressedTranslationY);
		Assert.Null(behavior.DefaultRotation);
		Assert.Null(behavior.HoveredRotation);
		Assert.Null(behavior.PressedRotation);
		Assert.Null(behavior.DefaultRotationX);
		Assert.Null(behavior.HoveredRotationX);
		Assert.Null(behavior.PressedRotationX);
		Assert.Null(behavior.DefaultRotationY);
		Assert.Null(behavior.HoveredRotationY);
		Assert.Null(behavior.PressedRotationY);
		Assert.Null(behavior.PressedAnimationDuration);
		Assert.Null(behavior.PressedAnimationEasing);
		Assert.Null(behavior.DefaultAnimationDuration);
		Assert.Null(behavior.DefaultAnimationEasing);
		Assert.Null(behavior.HoveredAnimationDuration);
		Assert.Null(behavior.HoveredAnimationEasing);
		Assert.Equal(0, behavior.DisallowTouchThreshold);
	}

	[Fact]
	public void TouchBehavior_VisualStateConstants_AreCorrect()
	{
		Assert.Equal("Unpressed", TouchBehavior.UnpressedVisualState);
		Assert.Equal("Pressed", TouchBehavior.PressedVisualState);
		Assert.Equal("Hovered", TouchBehavior.HoveredVisualState);
	}

	[Fact]
	public void TouchBehavior_SetProperties_UpdatesValues()
	{
		var behavior = new TouchBehavior
		{
			IsEnabled = false,
			ShouldMakeChildrenInputTransparent = false,
			LongPressDuration = 1000,
			DisallowTouchThreshold = 5,
			DefaultBackgroundColor = Colors.Red,
			HoveredBackgroundColor = Colors.Blue,
			PressedBackgroundColor = Colors.Green,
			DefaultOpacity = 0.5,
			HoveredOpacity = 0.7,
			PressedOpacity = 0.9,
			DefaultScale = 1.0,
			HoveredScale = 1.1,
			PressedScale = 0.9,
			DefaultTranslationX = 10,
			HoveredTranslationX = 20,
			PressedTranslationX = 30,
			DefaultTranslationY = 5,
			HoveredTranslationY = 15,
			PressedTranslationY = 25,
			DefaultRotation = 0,
			HoveredRotation = 45,
			PressedRotation = 90,
			DefaultRotationX = 0,
			HoveredRotationX = 10,
			PressedRotationX = 20,
			DefaultRotationY = 0,
			HoveredRotationY = 10,
			PressedRotationY = 20,
			PressedAnimationDuration = 100,
			PressedAnimationEasing = Easing.CubicIn,
			DefaultAnimationDuration = 200,
			DefaultAnimationEasing = Easing.CubicOut,
			HoveredAnimationDuration = 150,
			HoveredAnimationEasing = Easing.Linear,
		};

		Assert.False(behavior.IsEnabled);
		Assert.False(behavior.ShouldMakeChildrenInputTransparent);
		Assert.Equal(1000, behavior.LongPressDuration);
		Assert.Equal(5, behavior.DisallowTouchThreshold);
		Assert.Equal(Colors.Red, behavior.DefaultBackgroundColor);
		Assert.Equal(Colors.Blue, behavior.HoveredBackgroundColor);
		Assert.Equal(Colors.Green, behavior.PressedBackgroundColor);
		Assert.Equal(0.5, behavior.DefaultOpacity);
		Assert.Equal(0.7, behavior.HoveredOpacity);
		Assert.Equal(0.9, behavior.PressedOpacity);
		Assert.Equal(1.0, behavior.DefaultScale);
		Assert.Equal(1.1, behavior.HoveredScale);
		Assert.Equal(0.9, behavior.PressedScale);
		Assert.Equal(10, behavior.DefaultTranslationX);
		Assert.Equal(20, behavior.HoveredTranslationX);
		Assert.Equal(30, behavior.PressedTranslationX);
		Assert.Equal(5, behavior.DefaultTranslationY);
		Assert.Equal(15, behavior.HoveredTranslationY);
		Assert.Equal(25, behavior.PressedTranslationY);
		Assert.Equal(0, behavior.DefaultRotation);
		Assert.Equal(45, behavior.HoveredRotation);
		Assert.Equal(90, behavior.PressedRotation);
		Assert.Equal(0, behavior.DefaultRotationX);
		Assert.Equal(10, behavior.HoveredRotationX);
		Assert.Equal(20, behavior.PressedRotationX);
		Assert.Equal(0, behavior.DefaultRotationY);
		Assert.Equal(10, behavior.HoveredRotationY);
		Assert.Equal(20, behavior.PressedRotationY);
		Assert.Equal(100, behavior.PressedAnimationDuration);
		Assert.Equal(Easing.CubicIn, behavior.PressedAnimationEasing);
		Assert.Equal(200, behavior.DefaultAnimationDuration);
		Assert.Equal(Easing.CubicOut, behavior.DefaultAnimationEasing);
		Assert.Equal(150, behavior.HoveredAnimationDuration);
		Assert.Equal(Easing.Linear, behavior.HoveredAnimationEasing);
	}

	[Fact]
	public void TouchBehavior_Command_CanBeSet()
	{
		var command = new Command(() => { });
		var behavior = new TouchBehavior
		{
			Command = command,
			CommandParameter = "test",
		};

		Assert.Same(command, behavior.Command);
		Assert.Equal("test", behavior.CommandParameter);
	}

	[Fact]
	public void TouchBehavior_LongPressCommand_CanBeSet()
	{
		var command = new Command(() => { });
		var behavior = new TouchBehavior
		{
			LongPressCommand = command,
			LongPressCommandParameter = 42,
		};

		Assert.Same(command, behavior.LongPressCommand);
		Assert.Equal(42, behavior.LongPressCommandParameter);
	}

	[Fact]
	public void TouchBehavior_Events_CanSubscribeAndUnsubscribe()
	{
		var behavior = new TouchBehavior();
		var touchStatusFired = false;
		var touchStateFired = false;
		var interactionFired = false;
		var hoverStatusFired = false;
		var hoverStateFired = false;
		var gestureFired = false;
		var longPressFired = false;

		EventHandler<TouchStatusChangedEventArgs> touchStatusHandler = (s, e) => touchStatusFired = true;
		EventHandler<TouchStateChangedEventArgs> touchStateHandler = (s, e) => touchStateFired = true;
		EventHandler<TouchInteractionStatusChangedEventArgs> interactionHandler = (s, e) => interactionFired = true;
		EventHandler<HoverStatusChangedEventArgs> hoverStatusHandler = (s, e) => hoverStatusFired = true;
		EventHandler<HoverStateChangedEventArgs> hoverStateHandler = (s, e) => hoverStateFired = true;
		EventHandler<TouchGestureCompletedEventArgs> gestureHandler = (s, e) => gestureFired = true;
		EventHandler<LongPressCompletedEventArgs> longPressHandler = (s, e) => longPressFired = true;

		behavior.CurrentTouchStatusChanged += touchStatusHandler;
		behavior.CurrentTouchStateChanged += touchStateHandler;
		behavior.InteractionStatusChanged += interactionHandler;
		behavior.HoverStatusChanged += hoverStatusHandler;
		behavior.HoverStateChanged += hoverStateHandler;
		behavior.TouchGestureCompleted += gestureHandler;
		behavior.LongPressCompleted += longPressHandler;

		// Unsubscribe
		behavior.CurrentTouchStatusChanged -= touchStatusHandler;
		behavior.CurrentTouchStateChanged -= touchStateHandler;
		behavior.InteractionStatusChanged -= interactionHandler;
		behavior.HoverStatusChanged -= hoverStatusHandler;
		behavior.HoverStateChanged -= hoverStateHandler;
		behavior.TouchGestureCompleted -= gestureHandler;
		behavior.LongPressCompleted -= longPressHandler;

		// Verify no exceptions during subscribe/unsubscribe
		Assert.False(touchStatusFired);
		Assert.False(touchStateFired);
		Assert.False(interactionFired);
		Assert.False(hoverStatusFired);
		Assert.False(hoverStateFired);
		Assert.False(gestureFired);
		Assert.False(longPressFired);
	}
}

#endregion

#region ImageTouchBehavior Tests

public class ImageTouchBehaviorTests
{
	[Fact]
	public void ImageTouchBehavior_DefaultValues_AreCorrect()
	{
		var behavior = new ImageTouchBehavior();

		Assert.Null(behavior.DefaultImageSource);
		Assert.Null(behavior.HoveredImageSource);
		Assert.Null(behavior.PressedImageSource);
		Assert.Null(behavior.DefaultImageAspect);
		Assert.Null(behavior.HoveredImageAspect);
		Assert.Null(behavior.PressedImageAspect);
		Assert.False(behavior.ShouldSetImageOnAnimationEnd);
	}

	[Fact]
	public void ImageTouchBehavior_SetProperties_UpdatesValues()
	{
		var defaultSource = ImageSource.FromFile("default.png");
		var hoveredSource = ImageSource.FromFile("hovered.png");
		var pressedSource = ImageSource.FromFile("pressed.png");

		var behavior = new ImageTouchBehavior
		{
			DefaultImageSource = defaultSource,
			HoveredImageSource = hoveredSource,
			PressedImageSource = pressedSource,
			DefaultImageAspect = Aspect.AspectFit,
			HoveredImageAspect = Aspect.AspectFill,
			PressedImageAspect = Aspect.Fill,
			ShouldSetImageOnAnimationEnd = true,
		};

		Assert.Same(defaultSource, behavior.DefaultImageSource);
		Assert.Same(hoveredSource, behavior.HoveredImageSource);
		Assert.Same(pressedSource, behavior.PressedImageSource);
		Assert.Equal(Aspect.AspectFit, behavior.DefaultImageAspect);
		Assert.Equal(Aspect.AspectFill, behavior.HoveredImageAspect);
		Assert.Equal(Aspect.Fill, behavior.PressedImageAspect);
		Assert.True(behavior.ShouldSetImageOnAnimationEnd);
	}

	[Fact]
	public void ImageTouchBehavior_InheritsFromTouchBehavior()
	{
		var behavior = new ImageTouchBehavior();
		Assert.IsAssignableFrom<TouchBehavior>(behavior);
	}
}

#endregion

#region IconTintColorBehavior Tests

public class IconTintColorBehaviorTests
{
	[Fact]
	public void IconTintColorBehavior_DefaultTintColor_IsNull()
	{
		var behavior = new IconTintColorBehavior();
		Assert.Null(behavior.TintColor);
	}

	[Fact]
	public void IconTintColorBehavior_SetTintColor_UpdatesValue()
	{
		var behavior = new IconTintColorBehavior
		{
			TintColor = Colors.Red,
		};

		Assert.Equal(Colors.Red, behavior.TintColor);
	}

	[Fact]
	public void IconTintColorBehavior_SetTintColorToNull_Works()
	{
		var behavior = new IconTintColorBehavior
		{
			TintColor = Colors.Blue,
		};

		behavior.TintColor = null;
		Assert.Null(behavior.TintColor);
	}

	[Fact]
	public void IconTintColorBehavior_TintColorBindableProperty_ReadsBackCorrectValue()
	{
		var behavior = new IconTintColorBehavior();
		var expectedColor = Colors.Fuchsia;
		behavior.TintColor = expectedColor;

		var appliedColor = behavior.GetValue(IconTintColorBehavior.TintColorProperty) as Color;

		Assert.Equal(expectedColor, appliedColor);
	}
}

#endregion

#region StatusBarApplyOn Enum Tests

public class StatusBarApplyOnTests
{
	[Theory]
	[InlineData(StatusBarApplyOn.OnBehaviorAttachedTo, 0)]
	[InlineData(StatusBarApplyOn.OnPageNavigatedTo, 1)]
	public void StatusBarApplyOn_HasExpectedValues(StatusBarApplyOn applyOn, int expected)
	{
		Assert.Equal(expected, (int)applyOn);
	}

	[Fact]
	public void StatusBarApplyOn_HasTwoValues()
	{
		var values = Enum.GetValues<StatusBarApplyOn>();
		Assert.Equal(2, values.Length);
	}
}

#endregion

#region StatusBarBehavior Tests

public class StatusBarBehaviorTests
{
	[Fact]
	public void StatusBarBehavior_DefaultStatusBarColor_IsTransparent()
	{
		var behavior = new StatusBarBehavior();
		Assert.Equal(Colors.Transparent, behavior.StatusBarColor);
	}

	[Fact]
	public void StatusBarBehavior_DefaultStatusBarStyle_IsDefault()
	{
		var behavior = new StatusBarBehavior();
		Assert.Equal(StatusBarStyle.Default, behavior.StatusBarStyle);
	}

	[Fact]
	public void StatusBarBehavior_DefaultApplyOn_IsOnBehaviorAttachedTo()
	{
		var behavior = new StatusBarBehavior();
		Assert.Equal(StatusBarApplyOn.OnBehaviorAttachedTo, behavior.ApplyOn);
	}

	[Fact]
	public async Task StatusBarBehavior_SetStatusBarColor_UpdatesValue()
	{
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			var behavior = new StatusBarBehavior
			{
				StatusBarColor = Colors.Fuchsia,
			};

			Assert.Equal(Colors.Fuchsia, behavior.StatusBarColor);
		});
	}

	[Fact]
	public async Task StatusBarBehavior_SetStatusBarStyle_UpdatesValue()
	{
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			var behavior = new StatusBarBehavior
			{
				StatusBarStyle = StatusBarStyle.LightContent,
			};

			Assert.Equal(StatusBarStyle.LightContent, behavior.StatusBarStyle);
		});
	}

	[Fact]
	public void StatusBarBehavior_SetApplyOn_OnPageNavigatedTo_UpdatesValue()
	{
		var behavior = new StatusBarBehavior
		{
			ApplyOn = StatusBarApplyOn.OnPageNavigatedTo,
		};

		Assert.Equal(StatusBarApplyOn.OnPageNavigatedTo, behavior.ApplyOn);
	}

	[Fact]
	public void StatusBarBehavior_IsBasePlatformBehaviorOfPage()
	{
		var behavior = new StatusBarBehavior();
		Assert.IsAssignableFrom<BasePlatformBehavior<Page>>(behavior);
	}

	[Fact]
	public async Task StatusBarBehavior_CanBeAttachedToPage()
	{
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			var page = new ContentPage();
			var behavior = new StatusBarBehavior
			{
				StatusBarColor = Colors.Fuchsia,
			};

			page.Behaviors.Add(behavior);

			var attachedBehavior = page.Behaviors.FirstOrDefault(x => x is StatusBarBehavior);
			Assert.NotNull(attachedBehavior);
			Assert.Same(behavior, attachedBehavior);
		});
	}

	[Fact]
	public async Task StatusBarBehavior_AttachedToPage_ColorIsPreserved()
	{
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			var page = new ContentPage();
			var behavior = new StatusBarBehavior
			{
				StatusBarColor = Colors.Fuchsia,
			};

			page.Behaviors.Add(behavior);

			var attachedBehavior = page.Behaviors.OfType<StatusBarBehavior>().FirstOrDefault();
			Assert.NotNull(attachedBehavior);
			Assert.Equal(Colors.Fuchsia, attachedBehavior.StatusBarColor);
		});
	}

	[Fact]
	public void StatusBarBehavior_CanBeDetachedFromPage()
	{
		var page = new ContentPage();
		var behavior = new StatusBarBehavior();

		page.Behaviors.Add(behavior);
		Assert.Single(page.Behaviors.OfType<StatusBarBehavior>());

		page.Behaviors.Remove(behavior);
		Assert.Empty(page.Behaviors.OfType<StatusBarBehavior>());
	}

	[Fact]
	public async Task StatusBarBehavior_MultipleBehaviors_CanBeAttached()
	{
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			var page = new ContentPage();
			var behavior1 = new StatusBarBehavior { StatusBarColor = Colors.Red };
			var behavior2 = new StatusBarBehavior { StatusBarColor = Colors.Blue };

			page.Behaviors.Add(behavior1);
			page.Behaviors.Add(behavior2);

			var behaviors = page.Behaviors.OfType<StatusBarBehavior>().ToList();
			Assert.Equal(2, behaviors.Count);
			Assert.Equal(Colors.Red, behaviors[0].StatusBarColor);
			Assert.Equal(Colors.Blue, behaviors[1].StatusBarColor);
		});
	}
}

#endregion

#region SetFocusOnEntryCompletedBehavior Tests

public class SetFocusOnEntryCompletedBehaviorTests
{
	[Fact]
	public void SetFocusOnEntryCompletedBehavior_CanBeCreated()
	{
		var behavior = new SetFocusOnEntryCompletedBehavior();
		Assert.NotNull(behavior);
	}

	[Fact]
	public void SetFocusOnEntryCompletedBehavior_NextElementProperty_Exists()
	{
		var property = typeof(SetFocusOnEntryCompletedBehavior).GetField("NextElementProperty", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(property);
	}

	[Fact]
	public void SetFocusOnEntryCompletedBehavior_SetNextElement_Works()
	{
		var entry1 = new Entry();
		var entry2 = new Entry();

		SetFocusOnEntryCompletedBehavior.SetNextElement(entry1, entry2);
		var result = SetFocusOnEntryCompletedBehavior.GetNextElement(entry1);

		Assert.Same(entry2, result);
	}

	[Fact]
	public void SetFocusOnEntryCompletedBehavior_GetNextElement_DefaultIsNull()
	{
		var entry = new Entry();
		var result = SetFocusOnEntryCompletedBehavior.GetNextElement(entry);
		Assert.Null(result);
	}
}

#endregion

#region BaseAnimation Tests

public partial class BaseAnimationTests
{
	partial class TestAnimation : BaseAnimation
	{
		public override Task Animate(VisualElement view, CancellationToken token = default)
		{
			return Task.CompletedTask;
		}
	}

	partial class TestAnimationWithLength : BaseAnimation
	{
		public TestAnimationWithLength(uint length) : base(length)
		{
		}

		public override Task Animate(VisualElement view, CancellationToken token = default)
		{
			return Task.CompletedTask;
		}
	}

	[Fact]
	public void BaseAnimation_DefaultLength_Is250()
	{
		var animation = new TestAnimation();
		Assert.Equal(250u, animation.Length);
	}

	[Fact]
	public void BaseAnimation_DefaultEasing_IsLinear()
	{
		var animation = new TestAnimation();
		Assert.Equal(Easing.Linear, animation.Easing);
	}

	[Fact]
	public void BaseAnimation_CustomLength_IsRespected()
	{
		var animation = new TestAnimationWithLength(500);
		Assert.Equal(500u, animation.Length);
	}

	[Fact]
	public void BaseAnimation_SetLength_UpdatesValue()
	{
		var animation = new TestAnimation
		{
			Length = 1000,
		};

		Assert.Equal(1000u, animation.Length);
	}

	[Fact]
	public void BaseAnimation_SetEasing_UpdatesValue()
	{
		var animation = new TestAnimation
		{
			Easing = Easing.CubicInOut,
		};

		Assert.Equal(Easing.CubicInOut, animation.Easing);
	}

	[Fact]
	public async Task BaseAnimation_Animate_Completes()
	{
		var animation = new TestAnimation();
		var label = new Label();

		await animation.Animate(label);
		// If we get here without exception, the test passes
		Assert.True(true);
	}
}

#endregion

#region FadeAnimation Additional Tests

public class FadeAnimationAdditionalTests
{
	[Fact]
	public void FadeAnimation_DefaultOpacity_IsFadeAnimationDefault()
	{
		var animation = new FadeAnimation();

		// Default opacity comes from FadeAnimationDefaults.Opacity (0.3)
		Assert.Equal(0.3, animation.Opacity);
	}

	[Fact]
	public void FadeAnimation_SetOpacity_UpdatesValue()
	{
		var animation = new FadeAnimation
		{
			Opacity = 0.5,
		};

		Assert.Equal(0.5, animation.Opacity);
	}

	[Fact]
	public void FadeAnimation_InheritsFromBaseAnimation()
	{
		var animation = new FadeAnimation();
		Assert.IsAssignableFrom<BaseAnimation>(animation);
	}
}

#endregion

#region PopupService Tests

public class PopupServiceTests
{
	[Fact]
	public void PopupService_Constructor_WithServiceProvider_Works()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();

		var popupService = new PopupService(serviceProvider);
		Assert.NotNull(popupService);
	}

	[Fact]
	public void PopupService_ImplementsIPopupService()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();

		var popupService = new PopupService(serviceProvider);
		Assert.IsAssignableFrom<IPopupService>(popupService);
	}

	[Fact]
	public void PopupService_ShowPopup_NullPage_ThrowsArgumentNullException()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();
		var popupService = new PopupService(serviceProvider);

		var thrown = false;
		try
		{
			popupService.ShowPopup<Popup>(default(Page)!);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}

	[Fact]
	public void PopupService_ShowPopup_NullNavigation_ThrowsArgumentNullException()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();
		var popupService = new PopupService(serviceProvider);

		var thrown = false;
		try
		{
			popupService.ShowPopup<Popup>(default(INavigation)!);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}

	[Fact]
	public void PopupService_ShowPopup_NullShell_ThrowsArgumentNullException()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();
		var popupService = new PopupService(serviceProvider);

		var thrown = false;
		try
		{
			popupService.ShowPopup<Popup>(default(Shell)!);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}

	[Fact]
	public async Task PopupService_ShowPopupAsync_NullPage_ThrowsArgumentNullException()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();
		var popupService = new PopupService(serviceProvider);

		var thrown = false;
		try
		{
			await popupService.ShowPopupAsync<Popup>(default(Page)!);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}

	[Fact]
	public async Task PopupService_ShowPopupAsync_NullNavigation_ThrowsArgumentNullException()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();
		var popupService = new PopupService(serviceProvider);

		var thrown = false;
		try
		{
			await popupService.ShowPopupAsync<Popup>(default(INavigation)!);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}

	[Fact]
	public async Task PopupService_ShowPopupAsync_NullShell_ThrowsArgumentNullException()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();
		var popupService = new PopupService(serviceProvider);

		var thrown = false;
		try
		{
			await popupService.ShowPopupAsync<Popup>(default(Shell)!, null, null, CancellationToken.None);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}

	[Fact]
	public async Task PopupService_ShowPopupAsync_CancellationToken_ThrowsOperationCanceledException()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		var serviceProvider = services.BuildServiceProvider();
		var popupService = new PopupService(serviceProvider);

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var navigation = new Page().Navigation;
		var thrown = false;
		try
		{
			await popupService.ShowPopupAsync<Popup>(navigation, null, cts.Token);
		}
		catch (OperationCanceledException)
		{
			thrown = true;
		}

		Assert.True(thrown);
	}
}

#endregion

#region ImageResourceConverter Tests

public class ImageResourceConverterTests
{
	[Fact]
	public void ImageResourceConverter_ConvertFrom_Null_ReturnsNull()
	{
		var converter = new ImageResourceConverter();
		var result = converter.ConvertFrom(null);
		Assert.Null(result);
	}

	[Fact]
	public void ImageResourceConverter_DefaultConvertReturnValue_IsNull()
	{
		var converter = new ImageResourceConverter();
		Assert.Null(converter.DefaultConvertReturnValue);
	}

	[Fact]
	public void ImageResourceConverter_InheritsFromBaseConverterOneWay()
	{
		var converter = new ImageResourceConverter();
		Assert.IsAssignableFrom<BaseConverterOneWay<string?, ImageSource?>>(converter);
	}
}

#endregion

#region Options Tests

public class OptionsTests
{
	[Fact]
	public void Options_SetShouldSuppressExceptionsInConverters_Works()
	{
		var options = CreateOptions();
		options.SetShouldSuppressExceptionsInConverters(true);

		var value = GetInternalStaticProperty<bool>("CommunityToolkit.Maui.Options", "ShouldSuppressExceptionsInConverters");
		Assert.True(value);

		options.SetShouldSuppressExceptionsInConverters(false);
		value = GetInternalStaticProperty<bool>("CommunityToolkit.Maui.Options", "ShouldSuppressExceptionsInConverters");
		Assert.False(value);
	}

	[Fact]
	public void Options_SetShouldSuppressExceptionsInAnimations_Works()
	{
		var options = CreateOptions();
		options.SetShouldSuppressExceptionsInAnimations(true);

		var value = GetInternalStaticProperty<bool>("CommunityToolkit.Maui.Options", "ShouldSuppressExceptionsInAnimations");
		Assert.True(value);

		options.SetShouldSuppressExceptionsInAnimations(false);
		value = GetInternalStaticProperty<bool>("CommunityToolkit.Maui.Options", "ShouldSuppressExceptionsInAnimations");
		Assert.False(value);
	}

	[Fact]
	public void Options_SetShouldSuppressExceptionsInBehaviors_Works()
	{
		var options = CreateOptions();
		options.SetShouldSuppressExceptionsInBehaviors(true);

		var value = GetInternalStaticProperty<bool>("CommunityToolkit.Maui.Options", "ShouldSuppressExceptionsInBehaviors");
		Assert.True(value);

		options.SetShouldSuppressExceptionsInBehaviors(false);
		value = GetInternalStaticProperty<bool>("CommunityToolkit.Maui.Options", "ShouldSuppressExceptionsInBehaviors");
		Assert.False(value);
	}

	static Options CreateOptions()
	{
		var type = typeof(Options);
		var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		Assert.NotNull(constructor);
		return (Options)constructor.Invoke(null);
	}

	static T GetInternalStaticProperty<T>(string typeName, string propertyName)
	{
		var assembly = typeof(Options).Assembly;
		var type = assembly.GetType(typeName);
		Assert.NotNull(type);

		var property = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
		Assert.NotNull(property);

		var value = property.GetValue(null);
		Assert.NotNull(value);
		return (T)value;
	}
}

#endregion

#region NavigationBar Tests (Android-specific, test shared API)

public class NavigationBarTests
{
	[Fact]
	public void NavigationBar_ColorProperty_Exists()
	{
		var type = typeof(CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar);
		var property = type.GetField("ColorProperty", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(property);
	}

	[Fact]
	public void NavigationBar_StyleProperty_Exists()
	{
		var type = typeof(CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar);
		var property = type.GetField("StyleProperty", BindingFlags.Public | BindingFlags.Static);
		Assert.NotNull(property);
	}

	[Fact]
	public void NavigationBar_GetColor_ReturnsDefault()
	{
		var page = new Page();
		var color = CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar.GetColor(page);
		Assert.NotNull(color);
	}

	[Fact]
	public void NavigationBar_SetColor_UpdatesValue()
	{
		var page = new Page();
		CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar.SetColor(page, Colors.Red);
		var color = CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar.GetColor(page);
		Assert.Equal(Colors.Red, color);
	}

	[Fact]
	public void NavigationBar_GetStyle_ReturnsDefault()
	{
		var page = new Page();
		var style = CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar.GetStyle(page);
		Assert.Equal(NavigationBarStyle.Default, style);
	}

	[Fact]
	public void NavigationBar_SetStyle_UpdatesValue()
	{
		var page = new Page();
		CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar.SetStyle(page, NavigationBarStyle.LightContent);
		var style = CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar.GetStyle(page);
		Assert.Equal(NavigationBarStyle.LightContent, style);
	}
}

#endregion

#region BasePlatformBehavior Tests

public class BasePlatformBehaviorTests
{
	[Fact]
	public void TouchBehavior_IsBasePlatformBehavior()
	{
		var behavior = new TouchBehavior();
		Assert.IsAssignableFrom<BasePlatformBehavior<VisualElement>>(behavior);
	}

	[Fact]
	public void IconTintColorBehavior_IsBasePlatformBehavior()
	{
		var behavior = new IconTintColorBehavior();
		Assert.IsAssignableFrom<BasePlatformBehavior<View>>(behavior);
	}
}

#endregion

#region PopupService Registration Tests

public class PopupServiceRegistrationTests
{
	[Fact]
	public void PopupService_CanBeResolvedFromServiceProvider()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		services.AddSingleton<IPopupService, PopupService>();
		var serviceProvider = services.BuildServiceProvider();

		var popupService = serviceProvider.GetService<IPopupService>();
		Assert.NotNull(popupService);
		Assert.IsType<PopupService>(popupService);
	}
}

#endregion
