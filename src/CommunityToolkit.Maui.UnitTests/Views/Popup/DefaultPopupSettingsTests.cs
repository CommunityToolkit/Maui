#pragma warning disable CA1416
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.UnitTests.Services;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class DefaultPopupSettingsTests : BaseViewTest
{
	[Fact]
	public void DefaultPopupSettings_DefaultConstructor_UsesExpectedDefaults()
	{
		// Arrange
		var settings = new DefaultPopupSettings();

		// Assert
		Assert.True(settings.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(new Thickness(30), settings.Margin);
		Assert.Equal(new Thickness(15), settings.Padding);
		Assert.Equal(LayoutOptions.Center, settings.HorizontalOptions);
		Assert.Equal(LayoutOptions.Center, settings.VerticalOptions);
		Assert.Equal(Colors.White, settings.BackgroundColor);
		Assert.Null(settings.Initialize);
	}

	[Fact]
	public void DefaultPopupSettings_WithOverrides_UsesProvidedValues()
	{
		// Arrange
		var settings = new DefaultPopupSettings
		{
			CanBeDismissedByTappingOutsideOfPopup = false,
			BackgroundColor = Colors.Orange,
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Start,
			Margin = 72,
			Padding = 4
		};

		// Assert
		Assert.False(settings.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(Colors.Orange, settings.BackgroundColor);
		Assert.Equal(LayoutOptions.End, settings.HorizontalOptions);
		Assert.Equal(LayoutOptions.Start, settings.VerticalOptions);
		Assert.Equal(new Thickness(72), settings.Margin);
		Assert.Equal(new Thickness(4), settings.Padding);
	}

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

	[Theory]
	[InlineData(false, false, AppTheme.Light)]
	[InlineData(false, true, AppTheme.Light)]
	[InlineData(true, false, AppTheme.Light)]
	[InlineData(true, true, AppTheme.Light)]
	[InlineData(false, false, AppTheme.Dark)]
	[InlineData(false, true, AppTheme.Dark)]
	[InlineData(true, false, AppTheme.Dark)]
	[InlineData(true, true, AppTheme.Dark)]
	public void Initialize_AppThemeBinding_UpdatesPopupAndBorder(bool useContentView, bool useGenericPopup, AppTheme initialTheme)
	{
		// Arrange
		var application = Application.Current ?? throw new InvalidOperationException("Application cannot be null");
		application.UserAppTheme = initialTheme;
		View view = useContentView ? new ContentView() : useGenericPopup ? new Popup<string>() : new Popup();
		view.BackgroundColor = Colors.Orange;
		Popup? initializedPopup = null;
		var initializationCount = 0;
		new Options().SetPopupDefaults(new DefaultPopupSettings
		{
			Initialize = popup =>
			{
				initializedPopup = popup;
				initializationCount++;
				Assert.Equal(Colors.Orange, popup.BackgroundColor);
				if (useContentView)
				{
					Assert.Same(view, popup.Content);
				}
				else
				{
					Assert.Same(view, popup);
				}

				popup.SetAppThemeColor(VisualElement.BackgroundColorProperty, Colors.White, Colors.Black);
			}
		});

		// Act
		var popupPage = useGenericPopup ? new PopupPage<string>(view, null) : new PopupPage(view, null);
		CreateViewHandler<MockPageHandler>(popupPage);
		application.OpenWindow(new Window { Page = popupPage });
		var border = popupPage.Content.PopupBorder;
		var popup = Assert.IsType<Popup>(border.Content, exactMatch: false);

		// Assert
		Assert.Same(initializedPopup, popup);
		Assert.Equal(1, initializationCount);
		Assert.Equal(initialTheme == AppTheme.Dark ? Colors.Black : Colors.White, popup.BackgroundColor);
		Assert.Equal(popup.BackgroundColor, border.BackgroundColor);

		application.UserAppTheme = initialTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
		Assert.Equal(initialTheme == AppTheme.Dark ? Colors.White : Colors.Black, popup.BackgroundColor);
		Assert.Equal(popup.BackgroundColor, border.BackgroundColor);

		application.UserAppTheme = initialTheme;
		Assert.Equal(initialTheme == AppTheme.Dark ? Colors.Black : Colors.White, popup.BackgroundColor);
		Assert.Equal(popup.BackgroundColor, border.BackgroundColor);
		Assert.Equal(1, initializationCount);
	}

	[Fact]
	public void Initialize_RepeatedPresentation_InvokesCallbackForEachPresentation()
	{
		// Arrange
		var initializationCount = 0;
		new Options().SetPopupDefaults(new DefaultPopupSettings
		{
			Initialize = _ => initializationCount++
		});
		var popup = new Popup();

		// Act
		var firstPopupPage = new PopupPage(popup, null);
		var secondPopupPage = new PopupPage(popup, null);

		// Assert
		Assert.Equal(2, initializationCount);
		Assert.Same(popup, secondPopupPage.Content.PopupBorder.Content);
		Assert.NotSame(firstPopupPage, secondPopupPage);
	}

	[Fact]
	public void Initialize_CallbackThrows_PropagatesExceptionWithoutParentingPopup()
	{
		// Arrange
		var expectedException = new InvalidOperationException("Popup initialization failed");
		new Options().SetPopupDefaults(new DefaultPopupSettings
		{
			Initialize = _ => throw expectedException
		});
		var popup = new Popup();

		// Act
		var exception = Assert.Throws<InvalidOperationException>(() => new PopupPage(popup, null));

		// Assert
		Assert.Same(expectedException, exception);
		Assert.Null(popup.Parent);
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
		var popupPage = new PopupPage(new MockView(), PopupOptions.Empty);
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

		var popupPage = new PopupPage(new MockView(), PopupOptions.Empty);
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

	[Fact(Timeout = (int)TestDuration.Medium)]
	public void PopupService_View_SetPopupDefaultsCalled_UsesDefaultPopupSettings()
	{
		// Arrange
		var defaultPopupSettings = new DefaultPopupSettings
		{
			CanBeDismissedByTappingOutsideOfPopup = false,
			BackgroundColor = Colors.Orange,
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Start,
			Margin = 72,
			Padding = 4
		};

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var builder = MauiApp.CreateBuilder();
		builder.UseMauiCommunityToolkit(options => { options.SetPopupDefaults(defaultPopupSettings); });

		// Act
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
		popupService.ShowPopup<CustomButton>(page.Navigation);

		if (Application.Current.Windows[0].Page is not Shell { CurrentPage: PopupPage popupPage })
		{
			Assert.Fail("Popup page not found");
			throw new InvalidOperationException("Popup page not found");
		}

		var popupBorder = popupPage.Content.PopupBorder;
		var popup = (Popup)(popupBorder.Content ?? throw new InvalidOperationException("PopupBorder Content cannot be null"));
		var underlyingContentView = (ContentView)popup;

		// Assert
		Assert.Equal(defaultPopupSettings.BackgroundColor, popup.BackgroundColor);
		Assert.Equal(defaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup, popup.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(defaultPopupSettings.Margin, popupBorder.Margin);
		Assert.Equal(defaultPopupSettings.VerticalOptions, popupBorder.VerticalOptions);
		Assert.Equal(defaultPopupSettings.HorizontalOptions, popupBorder.HorizontalOptions);
		Assert.Equal(defaultPopupSettings.Padding, popup.Padding);
		Assert.Equal(defaultPopupSettings.Padding, underlyingContentView.Padding);
		Assert.Equal(Thickness.Zero, underlyingContentView.Margin);
		Assert.Equal(LayoutOptions.Fill, underlyingContentView.HorizontalOptions);
		Assert.Equal(LayoutOptions.Fill, underlyingContentView.VerticalOptions);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public void PopupService_Popup_SetPopupDefaultsCalled_UsesDefaultPopupSettings()
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

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var builder = MauiApp.CreateBuilder();
		builder.UseMauiCommunityToolkit(options => { options.SetPopupDefaults(defaultPopupSettings); });

		// Act
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
		popupService.ShowPopup<MockPopup>(page.Navigation);

		if (Application.Current.Windows[0].Page is not Shell { CurrentPage: PopupPage popupPage })
		{
			Assert.Fail("Popup page not found");
			throw new InvalidOperationException("Popup page not found");
		}

		var popupBorder = popupPage.Content.PopupBorder;
		var popup = (Popup)(popupBorder.Content ?? throw new InvalidOperationException("PopupBorder Content cannot be null"));
		var underlyingContentView = (ContentView)popup;

		// Assert
		Assert.Equal(defaultPopupSettings.BackgroundColor, popup.BackgroundColor);
		Assert.Equal(defaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup, popup.CanBeDismissedByTappingOutsideOfPopup);
		Assert.Equal(defaultPopupSettings.Margin, popupBorder.Margin);
		Assert.Equal(defaultPopupSettings.VerticalOptions, popupBorder.VerticalOptions);
		Assert.Equal(defaultPopupSettings.HorizontalOptions, popupBorder.HorizontalOptions);
		Assert.Equal(defaultPopupSettings.Padding, popup.Padding);
		Assert.Equal(defaultPopupSettings.Padding, underlyingContentView.Padding);
		Assert.Equal(Thickness.Zero, underlyingContentView.Margin);
		Assert.Equal(LayoutOptions.Fill, underlyingContentView.HorizontalOptions);
		Assert.Equal(LayoutOptions.Fill, underlyingContentView.VerticalOptions);
	}
}
#pragma warning restore CA1416