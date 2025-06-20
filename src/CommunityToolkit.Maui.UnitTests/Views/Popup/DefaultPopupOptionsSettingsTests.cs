using CommunityToolkit.Maui.Views;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

#pragma warning disable CA1416
public class DefaultPopupOptionsSettingsTests : BaseHandlerTest
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
	public void Popup_SetPopupOptionsDefaultsNotCalled_UsesPopupOptionsDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new Popup(), null);
		var popupBorder = popupPage.Content.PopupBorder;
		
		var tapGestureRecognizer = GetTapOutsideGestureRecognizer(popupPage);
		
		// Assert
		Assert.True(tapGestureRecognizer.Command?.CanExecute(null));
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.BorderStrokeThickness, popupBorder.StrokeThickness);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.BorderStroke, popupBorder.Stroke);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.Shadow, popupBorder.Shadow);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.Shape, popupBorder.StrokeShape);
	}
	
	[Fact]
	public void Popup_SetPopupOptionsNotCalled_PopupOptionsEmptyUsed_UsesPopupOptionsDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var popupBorder = popupPage.Content.PopupBorder;
		
		var tapGestureRecognizer = GetTapOutsideGestureRecognizer(popupPage);
		
		// Assert
		Assert.True(tapGestureRecognizer.Command?.CanExecute(null));
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.BorderStrokeThickness, popupBorder.StrokeThickness);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.BorderStroke, popupBorder.Stroke);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.Shadow, popupBorder.Shadow);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.Shape, popupBorder.StrokeShape);
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
		builder.UseMauiCommunityToolkit(options =>
		{
			options.SetPopupOptionsDefaults(defaultPopupSettings);
		});
		
		var popupPage = new PopupPage(new Popup(), null);
		var popupBorder = popupPage.Content.PopupBorder;
		var tapGestureRecognizer = GetTapOutsideGestureRecognizer(popupPage);
		
		// Act
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() =>
			{
				tapGestureRecognizer.Command?.Execute(null);
			});
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}
		
		// // Assert
		Assert.True(tapGestureRecognizer.Command?.CanExecute(null));
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
		builder.UseMauiCommunityToolkit(options =>
		{
			options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings());
		});
		
		var popupPage = new PopupPage(new Popup(), defaultPopupSettings);
		var popupBorder = popupPage.Content.PopupBorder;
		var tapGestureRecognizer = GetTapOutsideGestureRecognizer(popupPage);
		
		// Act
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() =>
			{
				tapGestureRecognizer.Command?.Execute(null);
			});
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}
		
		// // Assert
		Assert.True(tapGestureRecognizer.Command?.CanExecute(null));
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
		
		var tapGestureRecognizer = GetTapOutsideGestureRecognizer(popupPage);
		
		// Assert
		Assert.True(tapGestureRecognizer.Command?.CanExecute(null));
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.BorderStrokeThickness, popupBorder.StrokeThickness);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.BorderStroke, popupBorder.Stroke);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.Shadow, popupBorder.Shadow);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.Shape, popupBorder.StrokeShape);
	}
	
	[Fact]
	public void View_SetPopupOptionsNotCalled_PopupOptionsEmptyUsed_UsesPopupOptionsDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new View(), PopupOptions.Empty);
		var popupBorder = popupPage.Content.PopupBorder;
		
		var tapGestureRecognizer = GetTapOutsideGestureRecognizer(popupPage);
		
		// Assert
		Assert.True(tapGestureRecognizer.Command?.CanExecute(null));
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.BorderStrokeThickness, popupBorder.StrokeThickness);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.BorderStroke, popupBorder.Stroke);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.Shadow, popupBorder.Shadow);
		Assert.Equal(DefaultPopupOptionsSettings.PopupOptionsDefaults.Shape, popupBorder.StrokeShape);
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
		builder.UseMauiCommunityToolkit(options =>
		{
			options.SetPopupOptionsDefaults(defaultPopupSettings);
		});
		
		var popupPage = new PopupPage(new View(), null);
		var popupBorder = popupPage.Content.PopupBorder;
		var tapGestureRecognizer = GetTapOutsideGestureRecognizer(popupPage);
		
		// Act
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() =>
			{
				tapGestureRecognizer.Command?.Execute(null);
			});
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}
		
		// // Assert
		Assert.True(tapGestureRecognizer.Command?.CanExecute(null));
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
		builder.UseMauiCommunityToolkit(options =>
		{
			options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings());
		});
		
		var popupPage = new PopupPage(new View(), defaultPopupSettings);
		var popupBorder = popupPage.Content.PopupBorder;
		var tapGestureRecognizer = GetTapOutsideGestureRecognizer(popupPage);
		
		// Act
		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() =>
			{
				tapGestureRecognizer.Command?.Execute(null);
			});
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}
		
		// // Assert
		Assert.True(tapGestureRecognizer.Command?.CanExecute(null));
		Assert.True(hasOnTappingOutsideOfPopupExecuted);
		Assert.Equal(defaultPopupSettings.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(defaultPopupSettings.Shadow, popupBorder.Shadow);
		Assert.Equal(defaultPopupSettings.Shape, popupBorder.StrokeShape);
	}
	
	
	static TapGestureRecognizer GetTapOutsideGestureRecognizer(PopupPage popupPage) =>
		(TapGestureRecognizer)popupPage.Content.Children.OfType<BoxView>().Single().GestureRecognizers[0];
}
#pragma warning restore CA1416