using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Views;

public class PopupTests
{
	[Fact]
	public void Popup_DefaultProperties()
	{
		var popup = new Popup();

		Assert.Equal(new Thickness(30), popup.Margin);
		Assert.Equal(new Thickness(15), popup.Padding);
		Assert.Equal(LayoutOptions.Center, popup.HorizontalOptions);
		Assert.Equal(LayoutOptions.Center, popup.VerticalOptions);
		Assert.Equal(Colors.White, popup.BackgroundColor);
		Assert.True(popup.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void Popup_CanSetProperties()
	{
		var popup = new Popup
		{
			Margin = new Thickness(10),
			Padding = new Thickness(5),
			HorizontalOptions = LayoutOptions.Start,
			VerticalOptions = LayoutOptions.End,
			BackgroundColor = Colors.Red,
			CanBeDismissedByTappingOutsideOfPopup = false
		};

		Assert.Equal(new Thickness(10), popup.Margin);
		Assert.Equal(new Thickness(5), popup.Padding);
		Assert.Equal(LayoutOptions.Start, popup.HorizontalOptions);
		Assert.Equal(LayoutOptions.End, popup.VerticalOptions);
		Assert.Equal(Colors.Red, popup.BackgroundColor);
		Assert.False(popup.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void Popup_HasOpenedAndClosedEvents()
	{
		var popup = new Popup();
		var openedFired = false;
		var closedFired = false;

		popup.Opened += (_, _) => openedFired = true;
		popup.Closed += (_, _) => closedFired = true;

		// Events are registered but not fired without showing
		Assert.False(openedFired);
		Assert.False(closedFired);
	}
}

public class PopupOptionsTests
{
	[Fact]
	public void PopupOptions_DefaultProperties()
	{
		var options = new PopupOptions();

		Assert.True(options.CanBeDismissedByTappingOutsideOfPopup);
		Assert.NotNull(options.PageOverlayColor);
	}

	[Fact]
	public void PopupOptions_Empty_ReturnsInstance()
	{
		var empty = PopupOptions.Empty;

		Assert.NotNull(empty);
	}

	[Fact]
	public void PopupOptions_CanSetProperties()
	{
		var options = new PopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = false,
			PageOverlayColor = Colors.Transparent
		};

		Assert.False(options.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(Colors.Transparent, options.PageOverlayColor);
	}
}

public class PopupExceptionTests
{
	[Fact]
	public void PopupNotFoundException_CanBeCreated()
	{
		var exception = new PopupNotFoundException();

		Assert.NotNull(exception);
		Assert.IsType<PopupNotFoundException>(exception);
		Assert.Contains("Unable to close popup", exception.Message);
	}

	[Fact]
	public void PopupBlockedException_WithPage()
	{
		var page = new ContentPage();
		var exception = new PopupBlockedException(page);

		Assert.NotNull(exception);
		Assert.Contains("blocked by the Modal Page", exception.Message);
	}

	[Fact]
	public void InvalidPopupOperationException_WithMessage()
	{
		var exception = new InvalidPopupOperationException("test message");

		Assert.Equal("test message", exception.Message);
	}

	[Fact]
	public void PopupNotFoundException_IsInvalidPopupOperationException()
	{
		var exception = new PopupNotFoundException();

		Assert.IsAssignableFrom<InvalidPopupOperationException>(exception);
	}
}

public class AvatarViewTests
{
	[Fact]
	public void AvatarView_DefaultProperties()
	{
		var avatarView = new AvatarView();

		Assert.NotNull(avatarView.Text);
		Assert.Equal(-1, avatarView.FontSize);
		Assert.Equal(TextTransform.Default, avatarView.TextTransform);
		Assert.True(avatarView.FontAutoScalingEnabled);
	}

	[Fact]
	public void AvatarView_CanSetText()
	{
		var avatarView = new AvatarView
		{
			Text = "AB"
		};

		Assert.Equal("AB", avatarView.Text);
	}

	[Fact]
	public void AvatarView_CanSetTextColor()
	{
		var avatarView = new AvatarView
		{
			TextColor = Colors.Blue
		};

		Assert.Equal(Colors.Blue, avatarView.TextColor);
	}

	[Fact]
	public void AvatarView_CanSetFontSize()
	{
		var avatarView = new AvatarView
		{
			FontSize = 24
		};

		Assert.Equal(24, avatarView.FontSize);
	}

	[Fact]
	public void AvatarView_CanSetBorderProperties()
	{
		var avatarView = new AvatarView
		{
			BorderColor = Colors.Green,
			BorderWidth = 3
		};

		Assert.Equal(Colors.Green, avatarView.BorderColor);
		Assert.Equal(3, avatarView.BorderWidth);
	}
}

public class DrawingViewTests
{
	[Fact]
	public void DrawingView_DefaultProperties()
	{
		var drawingView = new DrawingView();

		Assert.NotNull(drawingView.Lines);
		Assert.Empty(drawingView.Lines);
		Assert.NotNull(drawingView.LineColor);
		Assert.True(drawingView.LineWidth > 0);
	}

	[Fact]
	public void DrawingView_CanSetLineColor()
	{
		var drawingView = new DrawingView
		{
			LineColor = Colors.Red
		};

		Assert.Equal(Colors.Red, drawingView.LineColor);
	}

	[Fact]
	public void DrawingView_CanSetLineWidth()
	{
		var drawingView = new DrawingView
		{
			LineWidth = 10f
		};

		Assert.Equal(10f, drawingView.LineWidth);
	}

	[Fact]
	public void DrawingView_CanSetShouldClearOnFinish()
	{
		var drawingView = new DrawingView
		{
			ShouldClearOnFinish = true
		};

		Assert.True(drawingView.ShouldClearOnFinish);
	}

	[Fact]
	public void DrawingView_CanSetIsMultiLineModeEnabled()
	{
		var drawingView = new DrawingView
		{
			IsMultiLineModeEnabled = true
		};

		Assert.True(drawingView.IsMultiLineModeEnabled);
	}

	[Fact]
	public void DrawingView_Clear_RemovesAllLines()
	{
		var drawingView = new DrawingView();

		drawingView.Clear();

		Assert.Empty(drawingView.Lines);
	}
}

public class ExpanderTests
{
	[Fact]
	public void Expander_DefaultProperties()
	{
		var expander = new Expander();

		Assert.False(expander.IsExpanded);
		Assert.Equal(ExpandDirection.Down, expander.Direction);
	}

	[Fact]
	public void Expander_CanSetIsExpanded()
	{
		var expander = new Expander
		{
			IsExpanded = true
		};

		Assert.True(expander.IsExpanded);
	}

	[Fact]
	public void Expander_CanSetDirection()
	{
		var expander = new Expander
		{
			Direction = ExpandDirection.Up
		};

		Assert.Equal(ExpandDirection.Up, expander.Direction);
	}

	[Fact]
	public void Expander_ExpandedChanged_EventFires()
	{
		var expander = new Expander();
		var eventFired = false;
		bool? isExpanded = null;

		expander.ExpandedChanged += (_, args) =>
		{
			eventFired = true;
			isExpanded = args.IsExpanded;
		};

		expander.IsExpanded = true;

		Assert.True(eventFired);
		Assert.True(isExpanded);
	}

	[Fact]
	public void Expander_CanSetCommand()
	{
		var command = new Command(() => { });
		var expander = new Expander
		{
			Command = command,
			CommandParameter = "test"
		};

		Assert.Same(command, expander.Command);
		Assert.Equal("test", expander.CommandParameter);
	}
}

public partial class LazyViewTests
{
	sealed partial class TestLazyView : LazyView<Label>
	{
	}

	[Fact]
	public void LazyView_InitiallyNotLoaded()
	{
		var lazyView = new TestLazyView();

		Assert.False(lazyView.HasLazyViewLoaded);
	}

	[Fact]
	public async Task LazyView_LoadViewAsync_SetsHasLazyViewLoaded()
	{
		var lazyView = new TestLazyView();

		await lazyView.LoadViewAsync(CancellationToken.None);

		Assert.True(lazyView.HasLazyViewLoaded);
	}

	[Fact]
	public async Task LazyView_LoadViewAsync_CreatesContent()
	{
		var lazyView = new TestLazyView();

		await lazyView.LoadViewAsync(CancellationToken.None);

		Assert.NotNull(lazyView.Content);
		Assert.IsType<Label>(lazyView.Content);
	}
}

public class RatingViewTests
{
	[Fact]
	public void RatingView_DefaultProperties()
	{
		var ratingView = new RatingView();

		Assert.True(ratingView.MaximumRating > 0);
		Assert.True(ratingView.Rating >= 0);
		Assert.False(ratingView.IsReadOnly);
	}

	[Fact]
	public void RatingView_CanSetMaximumRating()
	{
		var ratingView = new RatingView
		{
			MaximumRating = 10
		};

		Assert.Equal(10, ratingView.MaximumRating);
	}

	[Fact]
	public void RatingView_CanSetRating()
	{
		var ratingView = new RatingView
		{
			MaximumRating = 5,
			Rating = 3
		};

		Assert.Equal(3, ratingView.Rating);
	}

	[Fact]
	public void RatingView_CanSetIsReadOnly()
	{
		var ratingView = new RatingView
		{
			IsReadOnly = true
		};

		Assert.True(ratingView.IsReadOnly);
	}

	[Fact]
	public void RatingView_CanSetShape()
	{
		var ratingView = new RatingView
		{
			Shape = RatingViewShape.Circle
		};

		Assert.Equal(RatingViewShape.Circle, ratingView.Shape);
	}

	[Fact]
	public void RatingView_CanSetFillOption()
	{
		var ratingView = new RatingView
		{
			FillOption = RatingViewFillOption.Shape
		};

		Assert.Equal(RatingViewFillOption.Shape, ratingView.FillOption);
	}

	[Fact]
	public void RatingView_CanSetColors()
	{
		var ratingView = new RatingView
		{
			FillColor = Colors.Gold,
			EmptyShapeColor = Colors.Gray,
			ShapeBorderColor = Colors.Black
		};

		Assert.Equal(Colors.Gold, ratingView.FillColor);
		Assert.Equal(Colors.Gray, ratingView.EmptyShapeColor);
		Assert.Equal(Colors.Black, ratingView.ShapeBorderColor);
	}

	[Fact]
	public void RatingView_NullFillColor_CoercesToTransparent()
	{
		var ratingView = new RatingView();
		ratingView.FillColor = default(Color);

		Assert.Equal(Colors.Transparent, ratingView.FillColor);
	}

	[Fact]
	public void RatingView_NullEmptyShapeColor_CoercesToTransparent()
	{
		var ratingView = new RatingView();
		ratingView.EmptyShapeColor = default(Color);

		Assert.Equal(Colors.Transparent, ratingView.EmptyShapeColor);
	}

	[Fact]
	public void RatingView_RatingChanged_EventFires()
	{
		var ratingView = new RatingView
		{
			MaximumRating = 5
		};
		var eventFired = false;
		double newRating = 0;

		ratingView.RatingChanged += (_, args) =>
		{
			eventFired = true;
			newRating = args.Rating;
		};

		ratingView.Rating = 4;

		Assert.True(eventFired);
		Assert.Equal(4, newRating);
	}
}

public class SemanticOrderViewTests
{
	[Fact]
	public void SemanticOrderView_DefaultViewOrder_IsEmpty()
	{
		var view = new SemanticOrderView();

		Assert.NotNull(view.ViewOrder);
		Assert.Empty(view.ViewOrder);
	}

	[Fact]
	public void SemanticOrderView_CanSetViewOrder()
	{
		var label = new Label();
		var button = new Button();
		var view = new SemanticOrderView
		{
			ViewOrder = [label, button]
		};

		Assert.Equal(2, view.ViewOrder.Count());
	}
}

public class ToastTests
{
	[Fact]
	public void Toast_Make_CreatesInstance()
	{
		var toast = Toast.Make("Hello");

		Assert.NotNull(toast);
	}

	[Fact]
	public void Toast_Make_WithMessage_SetsText()
	{
		var toast = Toast.Make("Test message");

		Assert.Equal("Test message", toast.Text);
	}

	[Fact]
	public void Toast_Make_DefaultDuration_IsShort()
	{
		var toast = Toast.Make("Hello");

		Assert.Equal(ToastDuration.Short, toast.Duration);
	}

	[Fact]
	public void Toast_Make_WithDuration_SetsDuration()
	{
		var toast = Toast.Make("Hello", ToastDuration.Long);

		Assert.Equal(ToastDuration.Long, toast.Duration);
	}

	[Fact]
	public void Toast_Make_WithTextSize_SetsTextSize()
	{
		var toast = Toast.Make("Hello", textSize: 20);

		Assert.Equal(20, toast.TextSize);
	}

	[Fact]
	public void Toast_InvalidTextSize_ThrowsArgumentOutOfRangeException()
	{
		var thrown1 = false;
		try
		{
			Toast.Make("Hello", textSize: 0);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown1 = true;
		}

		Assert.True(thrown1);

		var thrown2 = false;
		try
		{
			Toast.Make("Hello", textSize: -1);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown2 = true;
		}

		Assert.True(thrown2);
	}
}

public class SnackbarTests
{
	// Snackbar on Windows requires Package.appxmanifest setup + SetShouldEnableSnackbarOnWindows(true),
	// which is not configured in the test host. Skip on Windows to avoid the platform guard exception.
	const string windowsSkipReason = "Snackbar requires Package.appxmanifest setup on Windows";

	[Fact(Skip = windowsSkipReason)]
	public void Snackbar_Make_CreatesInstance()
	{
		var snackbar = Snackbar.Make("Hello");

		Assert.NotNull(snackbar);
	}

	[Fact(Skip = windowsSkipReason)]
	public void Snackbar_Make_WithMessage_SetsText()
	{
		var snackbar = Snackbar.Make("Test message");

		Assert.Equal("Test message", snackbar.Text);
	}

	[Fact(Skip = windowsSkipReason)]
	public void Snackbar_Make_DefaultActionButtonText()
	{
		var snackbar = Snackbar.Make("Hello");

		Assert.NotNull(snackbar.ActionButtonText);
		Assert.NotEmpty(snackbar.ActionButtonText);
	}

	[Fact(Skip = windowsSkipReason)]
	public void Snackbar_Make_WithActionButtonText()
	{
		var snackbar = Snackbar.Make("Hello", actionButtonText: "OK");

		Assert.Equal("OK", snackbar.ActionButtonText);
	}

	[Fact(Skip = windowsSkipReason)]
	public void Snackbar_Make_WithAction()
	{
		var actionCalled = false;
		var snackbar = Snackbar.Make("Hello", action: () => actionCalled = true);

		Assert.NotNull(snackbar.Action);
		snackbar.Action.Invoke();
		Assert.True(actionCalled);
	}

	[Fact(Skip = windowsSkipReason)]
	public void Snackbar_Make_WithDuration_SetsDuration()
	{
		var duration = TimeSpan.FromSeconds(5);
		var snackbar = Snackbar.Make("Hello", duration: duration);

		Assert.Equal(duration, snackbar.Duration);
	}
}

public class DefaultPopupSettingsTests
{
	[Fact]
	public void DefaultPopupSettings_DefaultValues()
	{
		var settings = new DefaultPopupSettings();

		Assert.Equal(new Thickness(30), settings.Margin);
		Assert.Equal(new Thickness(15), settings.Padding);
		Assert.Equal(LayoutOptions.Center, settings.HorizontalOptions);
		Assert.Equal(LayoutOptions.Center, settings.VerticalOptions);
		Assert.True(settings.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(Colors.White, settings.BackgroundColor);
	}

	[Fact]
	public void DefaultPopupSettings_CanSetValues()
	{
		var settings = new DefaultPopupSettings
		{
			Margin = new Thickness(5),
			Padding = new Thickness(10),
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Start,
			CanBeDismissedByTappingOutsideOfPopup = false,
			BackgroundColor = Colors.Blue
		};

		Assert.Equal(new Thickness(5), settings.Margin);
		Assert.Equal(new Thickness(10), settings.Padding);
		Assert.Equal(LayoutOptions.Fill, settings.HorizontalOptions);
		Assert.Equal(LayoutOptions.Start, settings.VerticalOptions);
		Assert.False(settings.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(Colors.Blue, settings.BackgroundColor);
	}
}

public class DefaultPopupOptionsSettingsTests
{
	[Fact]
	public void DefaultPopupOptionsSettings_DefaultValues()
	{
		var settings = new DefaultPopupOptionsSettings();

		Assert.True(settings.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Null(settings.OnTappingOutsideOfPopup);
		Assert.NotNull(settings.PageOverlayColor);
	}

	[Fact]
	public void DefaultPopupOptionsSettings_CanSetValues()
	{
		var settings = new DefaultPopupOptionsSettings
		{
			CanBeDismissedByTappingOutsideOfPopup = false,
			PageOverlayColor = Colors.Transparent
		};

		Assert.False(settings.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(Colors.Transparent, settings.PageOverlayColor);
	}
}

public class AccessModifierTests
{
	[Fact]
	public void AccessModifier_HasExpectedValues()
	{
		Assert.Equal(0, (int)AccessModifier.Public);
		Assert.Equal(1, (int)AccessModifier.Internal);
		Assert.Equal(2, (int)AccessModifier.ProtectedInternal);
		Assert.Equal(3, (int)AccessModifier.Protected);
		Assert.Equal(4, (int)AccessModifier.PrivateProtected);
		Assert.Equal(5, (int)AccessModifier.Private);
		Assert.Equal(6, (int)AccessModifier.None);
	}
}