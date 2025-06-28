using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

#pragma warning disable CA1416
public class DefaultPopupSettingsTests : BaseTest
{
	[Fact]
	public void Popup_SetPopupDefaultsNotCalled_UsesPopupDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var popup = (Popup)(popupPage.Content.PopupBorder.Content ?? throw new InvalidOperationException("Popup cannot be null"));

		// Assert
		Assert.True(popup.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(new Thickness(30), popup.Margin);
		Assert.Equal(new Thickness(15), popup.Padding);
		Assert.Equal(LayoutOptions.Center, popup.HorizontalOptions);
		Assert.Equal(LayoutOptions.Center, popup.VerticalOptions);
		Assert.Equal(Colors.White, popup.BackgroundColor);
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
		builder.UseMauiCommunityToolkit(options => { options.SetPopupDefaults(defaultPopupSettings); });

		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var popup = (Popup)(popupPage.Content.PopupBorder.Content ?? throw new InvalidOperationException("Popup cannot be null"));

		// Act // Assert
		Assert.Equal(defaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup, popup.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(defaultPopupSettings.Margin, popup.Margin);
		Assert.Equal(defaultPopupSettings.Padding, popup.Padding);
		Assert.Equal(defaultPopupSettings.HorizontalOptions, popup.HorizontalOptions);
		Assert.Equal(defaultPopupSettings.VerticalOptions, popup.VerticalOptions);
		Assert.Equal(defaultPopupSettings.BackgroundColor, popup.BackgroundColor);
	}

	[Fact]
	public void View_SetPopupDefaultsNotCalled_UsesPopupDefaults()
	{
		// Arrange
		var popupPage = new PopupPage(new View(), PopupOptions.Empty);
		var popupBorder = popupPage.Content.PopupBorder;
		var popup = (Popup)(popupBorder.Content ?? throw new InvalidOperationException("Popup cannot be null"));

		// Assert
		Assert.True(popup.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(new Thickness(30), popup.Margin);
		Assert.Equal(new Thickness(15), popup.Padding);
		Assert.Equal(LayoutOptions.Center, popupBorder.HorizontalOptions);
		Assert.Equal(LayoutOptions.Center, popupBorder.VerticalOptions);
		Assert.Equal(Colors.White, popup.BackgroundColor);
	}

	[Fact]
	public void View_SetPopupDefaultsCalled_UsesDefaultPopupSettings()
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
		builder.UseMauiCommunityToolkit(options => { options.SetPopupDefaults(defaultPopupSettings); });

		var popupPage = new PopupPage(new View(), PopupOptions.Empty);
		var popupBorder = popupPage.Content.PopupBorder;
		var popup = (Popup)(popupBorder.Content ?? throw new InvalidOperationException("Popup cannot be null"));

		// Act // Assert
		Assert.Equal(defaultPopupSettings.Padding, popup.Padding);
		Assert.Equal(defaultPopupSettings.BackgroundColor, popup.BackgroundColor);
		Assert.Equal(defaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup, popup.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(defaultPopupSettings.Margin, popupBorder.Margin);
		Assert.Equal(defaultPopupSettings.VerticalOptions, popupBorder.VerticalOptions);
		Assert.Equal(defaultPopupSettings.HorizontalOptions, popupBorder.HorizontalOptions);
	}
}
#pragma warning restore CA1416