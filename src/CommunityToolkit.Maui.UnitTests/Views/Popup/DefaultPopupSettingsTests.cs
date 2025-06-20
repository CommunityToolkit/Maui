using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

#pragma warning disable CA1416
public class DefaultPopupSettingsTests
{
	[Fact]
	public void Popup_SetPopupDefaultsNotCalled_UsesPopupDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var popup = (Popup)(popupPage.Content.PopupBorder.Content ?? throw new InvalidOperationException("Popup cannot be null")); 
		
		// Assert
		Assert.Equal(DefaultPopupSettings.PopupDefaults.CanBeDismissedByTappingOutsideOfPopup, popup.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(DefaultPopupSettings.PopupDefaults.Margin, popup.Margin);
		Assert.Equal(DefaultPopupSettings.PopupDefaults.Padding, popup.Padding);
		Assert.Equal(DefaultPopupSettings.PopupDefaults.HorizontalOptions, popup.HorizontalOptions);
		Assert.Equal(DefaultPopupSettings.PopupDefaults.VerticalOptions, popup.VerticalOptions);
		Assert.Equal(DefaultPopupSettings.PopupDefaults.BackgroundColor, popup.BackgroundColor);
	}
	
	[Fact]
	public void Popup_SetPopupDefaultsCalled_UsesDefaultPopupSettings()
	{
		// Arrange
		var defaultPopupSettings = new DefaultPopupSettings
		{
			CanBeDismissedByTappingOutsideOfPopup = true,
			BackgroundColor = Colors.Orange,
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Start,
			Margin = 72,
			Padding = 4
		};
		
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiCommunityToolkit(options =>
		{
			options.SetPopupDefaults(defaultPopupSettings);
		});
		
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var popup = (Popup)(popupPage.Content.PopupBorder.Content ?? throw new InvalidOperationException("Popup cannot be null")); 
		
		// Act // Assert
		Assert.Equal(Options.DefaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup, popup.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(Options.DefaultPopupSettings.Margin, popup.Margin);
		Assert.Equal(Options.DefaultPopupSettings.Padding, popup.Padding);
		Assert.Equal(Options.DefaultPopupSettings.HorizontalOptions, popup.HorizontalOptions);
		Assert.Equal(Options.DefaultPopupSettings.VerticalOptions, popup.VerticalOptions);
		Assert.Equal(Options.DefaultPopupSettings.BackgroundColor, popup.BackgroundColor);
	}
}
#pragma warning restore CA1416