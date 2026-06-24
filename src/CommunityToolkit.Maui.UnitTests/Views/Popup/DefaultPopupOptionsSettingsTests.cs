using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

#pragma warning disable CA1416
public class DefaultPopupOptionsSettingsTests : BaseViewTest
{
	readonly INavigation navigation;

	public DefaultPopupOptionsSettingsTests()
	{
		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		navigation = page.Navigation;
	}

	[Fact]
	public void DefaultPopupOptionsSettings_DefaultConstructor_UsesExpectedDefaults()
	{
		// Arrange
		IPopupOptions defaults = new DefaultPopupOptionsSettings();

		// Assert
		Assert.True(defaults.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Null(defaults.OnTappingOutsideOfPopup);
		Assert.Equal(Colors.Black.WithAlpha(0.3f), defaults.PageOverlayColor);
		Assert.Equal(Colors.LightGray, ((RoundRectangle?)defaults.Shape)?.Stroke);
		Assert.Equal(2, ((RoundRectangle?)defaults.Shape)?.StrokeThickness);
		Assert.Equal(Colors.Black, defaults.Shadow?.Brush);
	}

	[Fact]
	public void DefaultPopupOptionsSettings_WithOverrides_UsesProvidedValues()
	{
		// Arrange
		bool actionInvoked = false;
		var expectedShape = new Ellipse
		{
			Stroke = Colors.Red,
			StrokeThickness = 8
		};
		var expectedShadow = new Shadow
		{
			Brush = Colors.Blue,
			Offset = new Point(1, 2),
			Radius = 3,
			Opacity = 0.4f
		};

		IPopupOptions options = new DefaultPopupOptionsSettings
		{
			CanBeDismissedByTappingOutsideOfPopup = false,
			OnTappingOutsideOfPopup = () => actionInvoked = true,
			PageOverlayColor = Colors.Green,
			Shape = expectedShape,
			Shadow = expectedShadow
		};

		// Act
		options.OnTappingOutsideOfPopup?.Invoke();

		// Assert
		Assert.False(options.CanBeDismissedByTappingOutsideOfPopup);
		Assert.True(actionInvoked);
		Assert.Equal(Colors.Green, options.PageOverlayColor);
		Assert.Same(expectedShape, options.Shape);
		Assert.Same(expectedShadow, options.Shadow);
	}

	[Fact]
	public void Popup_SetPopupOptionsDefaultsNotCalled_UsesPopupOptionsDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new Popup(), null);
		var popupBorder = popupPage.Content.PopupBorder;

		// Assert
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		Assert.Equal(2, popupBorder.StrokeThickness);
		Assert.Equal(Colors.LightGray, popupBorder.Stroke);
		Assert.Equal(Colors.Black.WithAlpha(0.3f), popupPage.BackgroundColor);

		Assert.Equal(Colors.Black, popupBorder.Shadow.Brush);
		Assert.Equal(new(20, 20), popupBorder.Shadow.Offset);
		Assert.Equal(40, popupBorder.Shadow.Radius);
		Assert.Equal(0.8f, popupBorder.Shadow.Opacity);

		Assert.Equal(new CornerRadius(20, 20, 20, 20), ((RoundRectangle?)popupBorder.StrokeShape)?.CornerRadius);
		Assert.Equal(2, ((RoundRectangle?)popupBorder.StrokeShape)?.StrokeThickness);
		Assert.Equal(Colors.LightGray, ((RoundRectangle?)popupBorder.StrokeShape)?.Stroke);
	}

	[Fact]
	public void Popup_SetPopupOptionsNotCalled_PopupOptionsEmptyUsed_UsesPopupOptionsDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var popupBorder = popupPage.Content.PopupBorder;

		// Assert
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		Assert.Equal(2, popupBorder.StrokeThickness);
		Assert.Equal(Colors.LightGray, popupBorder.Stroke);
		Assert.Equal(Colors.Black.WithAlpha(0.3f), popupPage.BackgroundColor);

		Assert.Equal(Colors.Black, popupBorder.Shadow.Brush);
		Assert.Equal(new(20, 20), popupBorder.Shadow.Offset);
		Assert.Equal(40, popupBorder.Shadow.Radius);
		Assert.Equal(0.8f, popupBorder.Shadow.Opacity);

		Assert.Equal(new CornerRadius(20, 20, 20, 20), ((RoundRectangle?)popupBorder.StrokeShape)?.CornerRadius);
		Assert.Equal(2, ((RoundRectangle?)popupBorder.StrokeShape)?.StrokeThickness);
		Assert.Equal(Colors.LightGray, ((RoundRectangle?)popupBorder.StrokeShape)?.Stroke);
	}

	[Fact]
	public void Popup_SetPopupDefaultsCalled_UsesDefaultPopupOptionsSettings()
	{
		// Arrange
		var hasOnTappingOutsideOfPopupExecuted = false;

		var defaultPopupSettings = new DefaultPopupOptionsSettings
		{
			CanBeDismissedByTappingOutsideOfPopup = true,
			OnTappingOutsideOfPopup = () => hasOnTappingOutsideOfPopupExecuted = true,
			PageOverlayColor = Colors.Orange,
			Shadow = null,
			Shape = null
		};

		var builder = MauiApp.CreateBuilder();
		builder.UseMauiCommunityToolkit(options => { options.SetPopupOptionsDefaults(defaultPopupSettings); });

		var popupPage = new PopupPage(new Popup(), null);
		var popupBorder = popupPage.Content.PopupBorder;

		// Act
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		// Assert
		Assert.True(hasOnTappingOutsideOfPopupExecuted);
		Assert.Equal(defaultPopupSettings.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(defaultPopupSettings.Shadow, popupBorder.Shadow);
		Assert.Equal(defaultPopupSettings.Shape, popupBorder.StrokeShape);
	}

	[Fact]
	public void Popup_SetPopupDefaultsCalled_PopupOptionsOverridden_UsesProvidedPopupOptionsSettings()
	{
		// Arrange
		var hasOnTappingOutsideOfPopupExecuted = false;

		var defaultPopupSettings = new DefaultPopupOptionsSettings
		{
			CanBeDismissedByTappingOutsideOfPopup = true,
			OnTappingOutsideOfPopup = () => hasOnTappingOutsideOfPopupExecuted = true,
			PageOverlayColor = Colors.Orange,
			Shadow = null,
			Shape = null
		};

		var builder = MauiApp.CreateBuilder();
		builder.UseMauiCommunityToolkit(options => { options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings()); });

		var popupPage = new PopupPage(new Popup(), defaultPopupSettings);
		var popupBorder = popupPage.Content.PopupBorder;

		// Act
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		// // Assert
		Assert.True(hasOnTappingOutsideOfPopupExecuted);
		Assert.Equal(defaultPopupSettings.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(defaultPopupSettings.Shadow, popupBorder.Shadow);
		Assert.Equal(defaultPopupSettings.Shape, popupBorder.StrokeShape);
	}

	[Fact]
	public void View_SetPopupOptionsDefaultsNotCalled_UsesPopupOptionsDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new View(), null);
		var popupBorder = popupPage.Content.PopupBorder;

		// Assert
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		Assert.Equal(2, popupBorder.StrokeThickness);
		Assert.Equal(Colors.LightGray, popupBorder.Stroke);
		Assert.Equal(Colors.Black.WithAlpha(0.3f), popupPage.BackgroundColor);

		Assert.Equal(Colors.Black, popupBorder.Shadow.Brush);
		Assert.Equal(new(20, 20), popupBorder.Shadow.Offset);
		Assert.Equal(40, popupBorder.Shadow.Radius);
		Assert.Equal(0.8f, popupBorder.Shadow.Opacity);

		Assert.Equal(new CornerRadius(20, 20, 20, 20), ((RoundRectangle?)popupBorder.StrokeShape)?.CornerRadius);
		Assert.Equal(2, ((RoundRectangle?)popupBorder.StrokeShape)?.StrokeThickness);
		Assert.Equal(Colors.LightGray, ((RoundRectangle?)popupBorder.StrokeShape)?.Stroke);
	}

	[Fact]
	public void View_SetPopupOptionsNotCalled_PopupOptionsEmptyUsed_UsesPopupOptionsDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new View(), PopupOptions.Empty);
		var popupBorder = popupPage.Content.PopupBorder;

		// Assert
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}
		Assert.Equal(2, popupBorder.StrokeThickness);
		Assert.Equal(Colors.LightGray, popupBorder.Stroke);
		Assert.Equal(Colors.Black.WithAlpha(0.3f), popupPage.BackgroundColor);

		Assert.Equal(Colors.Black, popupBorder.Shadow.Brush);
		Assert.Equal(new(20, 20), popupBorder.Shadow.Offset);
		Assert.Equal(40, popupBorder.Shadow.Radius);
		Assert.Equal(0.8f, popupBorder.Shadow.Opacity);

		Assert.Equal(new CornerRadius(20, 20, 20, 20), ((RoundRectangle?)popupBorder.StrokeShape)?.CornerRadius);
		Assert.Equal(2, ((RoundRectangle?)popupBorder.StrokeShape)?.StrokeThickness);
		Assert.Equal(Colors.LightGray, ((RoundRectangle?)popupBorder.StrokeShape)?.Stroke);
	}

	[Fact]
	public void View_SetPopupDefaultsCalled_UsesDefaultPopupOptionsSettings()
	{
		// Arrange
		var hasOnTappingOutsideOfPopupExecuted = false;

		var defaultPopupSettings = new DefaultPopupOptionsSettings
		{
			CanBeDismissedByTappingOutsideOfPopup = true,
			OnTappingOutsideOfPopup = () => hasOnTappingOutsideOfPopupExecuted = true,
			PageOverlayColor = Colors.Orange,
			Shadow = null,
			Shape = null
		};

		var builder = MauiApp.CreateBuilder();
		builder.UseMauiCommunityToolkit(options => { options.SetPopupOptionsDefaults(defaultPopupSettings); });

		var popupPage = new PopupPage(new View(), null);
		var popupBorder = popupPage.Content.PopupBorder;

		// Act
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		// Assert
		Assert.True(hasOnTappingOutsideOfPopupExecuted);
		Assert.Equal(defaultPopupSettings.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(defaultPopupSettings.Shadow, popupBorder.Shadow);
		Assert.Equal(defaultPopupSettings.Shape, popupBorder.StrokeShape);
	}

	[Fact]
	public void View_SetPopupDefaultsCalled_PopupOptionsOverridden_UsesProvidedPopupOptionsSettings()
	{
		// Arrange
		var hasOnTappingOutsideOfPopupExecuted = false;

		var defaultPopupSettings = new DefaultPopupOptionsSettings
		{
			CanBeDismissedByTappingOutsideOfPopup = true,
			OnTappingOutsideOfPopup = () => hasOnTappingOutsideOfPopupExecuted = true,
			PageOverlayColor = Colors.Orange,
			Shadow = null,
			Shape = null
		};

		var builder = MauiApp.CreateBuilder();
		builder.UseMauiCommunityToolkit(options => { options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings()); });

		var popupPage = new PopupPage(new View(), defaultPopupSettings);
		var popupBorder = popupPage.Content.PopupBorder;

		// // Assert
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		Assert.True(hasOnTappingOutsideOfPopupExecuted);
		Assert.Equal(defaultPopupSettings.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(defaultPopupSettings.Shadow, popupBorder.Shadow);
		Assert.Equal(defaultPopupSettings.Shape, popupBorder.StrokeShape);
	}
}
#pragma warning restore CA1416