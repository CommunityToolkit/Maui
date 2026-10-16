using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupStyleTests : BaseViewTest
{
	static readonly Color styleBackgroundColor = Colors.Red;
	static readonly Thickness styleMargin = new(5);
	static readonly Thickness stylePadding = new(7);
	static readonly LayoutOptions styleHorizontalOptions = LayoutOptions.Start;
	static readonly LayoutOptions styleVerticalOptions = LayoutOptions.End;
	const bool styleCanBeDismissedByTappingOutsideOfPopup = false;

	[Fact]
	public void ExplicitStyle_ShouldSetPopupProperties()
	{
		// Arrange
		var popup = new StyledPopup();

		// Act
		popup.Style = CreateStyle();

		// Assert
		AssertStyleApplied(popup);
	}

	[Fact]
	public void ImplicitStyle_ShouldSetPopupProperties()
	{
		// Arrange
		var application = Application.Current ?? throw new InvalidOperationException("Application.Current cannot be null");
		application.Resources.Add(CreateStyle());

		// Act
		var popup = new StyledPopup();

		// Assert
		AssertStyleApplied(popup);
	}

	[Fact]
	public void XamlStyle_ShouldSetPopupProperties()
	{
		// Arrange
		const string xaml = """
			<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
			                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
			                    xmlns:mct="clr-namespace:CommunityToolkit.Maui.Views;assembly=CommunityToolkit.Maui">
			    <Style x:Key="PopupStyle" TargetType="mct:Popup" ApplyToDerivedTypes="True">
			        <Setter Property="BackgroundColor" Value="Red" />
			        <Setter Property="Margin" Value="5" />
			        <Setter Property="Padding" Value="7" />
			        <Setter Property="HorizontalOptions" Value="Start" />
			        <Setter Property="VerticalOptions" Value="End" />
			        <Setter Property="CanBeDismissedByTappingOutsideOfPopup" Value="False" />
			    </Style>
			</ResourceDictionary>
			""";

		var resourceDictionary = Microsoft.Maui.Controls.Xaml.Extensions.LoadFromXaml(new ResourceDictionary(), xaml);
		var popup = new StyledPopup();

		// Act
		popup.Style = (Style)resourceDictionary["PopupStyle"];

		// Assert
		AssertStyleApplied(popup);
	}

	[Fact]
	public void Style_ShouldBeDisplayedByPopupPage()
	{
		// Arrange
		var popup = new StyledPopup
		{
			Style = CreateStyle()
		};

		// Act
		var popupPage = new PopupPage(popup, PopupOptions.Empty);

		// Assert
		AssertStyleDisplayed(popupPage, popup);
	}

	[Fact]
	public void StyleAppliedAfterPopupPageIsCreated_ShouldBeDisplayedByPopupPage()
	{
		// Arrange
		var popup = new StyledPopup();
		var popupPage = new PopupPage(popup, PopupOptions.Empty);

		// Act
		popup.Style = CreateStyle();

		// Assert
		AssertStyleDisplayed(popupPage, popup);
	}

	[Fact]
	public void ImplicitStyle_ShouldBeDisplayedWhenPopupIsShown()
	{
		// Arrange
		var application = Application.Current ?? throw new InvalidOperationException("Application.Current cannot be null");
		application.Resources.Add(CreateStyle());

		var navigation = application.Windows[0].Page?.Navigation ?? throw new InvalidOperationException("Unable to locate INavigation");
		var popup = new StyledPopup();

		// Act
		navigation.ShowPopup(popup, PopupOptions.Empty);

		// Assert
		var popupPage = Assert.IsType<PopupPage>(navigation.ModalStack[^1]);
		AssertStyleDisplayed(popupPage, popup);
	}

	[Fact]
	public void LocalValues_ShouldTakePrecedenceOverStyle()
	{
		// Arrange
		var popup = new StyledPopup
		{
			BackgroundColor = Colors.Blue,
			Margin = new Thickness(11),
			Padding = new Thickness(13),
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Start,
			CanBeDismissedByTappingOutsideOfPopup = true
		};

		// Act
		popup.Style = CreateStyle();
		var popupPage = new PopupPage(popup, PopupOptions.Empty);
		var popupBorder = popupPage.Content.PopupBorder;

		// Assert
		Assert.Equal(Colors.Blue, popup.BackgroundColor);
		Assert.Equal(Colors.Blue, popupBorder.BackgroundColor);
		Assert.Equal(new Thickness(11), popupBorder.Margin);
		Assert.Equal(new Thickness(13), popup.Padding);
		Assert.Equal(LayoutOptions.Center, popupBorder.HorizontalOptions);
		Assert.Equal(LayoutOptions.Start, popupBorder.VerticalOptions);
		Assert.True(popup.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void DefaultPopupSettings_ShouldBeRestored_WhenStyleIsRemoved()
	{
		// Arrange
		var popup = new StyledPopup
		{
			Style = CreateStyle()
		};
		var popupPage = new PopupPage(popup, PopupOptions.Empty);
		var popupBorder = popupPage.Content.PopupBorder;

		// Act
		popup.Style = null;

		// Assert
		Assert.Null(popup.BackgroundColor);
		Assert.Equal(Colors.White, popupBorder.BackgroundColor);
		Assert.Equal(new Thickness(30), popupBorder.Margin);
		Assert.Equal(new Thickness(15), popup.Padding);
		Assert.Equal(LayoutOptions.Center, popupBorder.HorizontalOptions);
		Assert.Equal(LayoutOptions.Center, popupBorder.VerticalOptions);
		Assert.True(popup.CanBeDismissedByTappingOutsideOfPopup);
	}

	static Style CreateStyle() => new(typeof(StyledPopup))
	{
		Setters =
		{
			new Setter { Property = VisualElement.BackgroundColorProperty, Value = styleBackgroundColor },
			new Setter { Property = Popup.MarginProperty, Value = styleMargin },
			new Setter { Property = Popup.PaddingProperty, Value = stylePadding },
			new Setter { Property = Popup.HorizontalOptionsProperty, Value = styleHorizontalOptions },
			new Setter { Property = Popup.VerticalOptionsProperty, Value = styleVerticalOptions },
			new Setter { Property = Popup.CanBeDismissedByTappingOutsideOfPopupProperty, Value = styleCanBeDismissedByTappingOutsideOfPopup },
		}
	};

	static void AssertStyleApplied(Popup popup)
	{
		Assert.Equal(styleBackgroundColor, popup.BackgroundColor);
		Assert.Equal(styleMargin, popup.Margin);
		Assert.Equal(stylePadding, popup.Padding);
		Assert.Equal(styleHorizontalOptions, popup.HorizontalOptions);
		Assert.Equal(styleVerticalOptions, popup.VerticalOptions);
		Assert.Equal(styleCanBeDismissedByTappingOutsideOfPopup, popup.CanBeDismissedByTappingOutsideOfPopup);
	}

	static void AssertStyleDisplayed(PopupPage popupPage, Popup popup)
	{
		var popupBorder = popupPage.Content.PopupBorder;

		AssertStyleApplied(popup);
		Assert.Same(popup, popupBorder.Content);
		Assert.Equal(styleBackgroundColor, popupBorder.BackgroundColor);
		Assert.Equal(styleMargin, popupBorder.Margin);
		Assert.Equal(styleHorizontalOptions, popupBorder.HorizontalOptions);
		Assert.Equal(styleVerticalOptions, popupBorder.VerticalOptions);
		Assert.False(popupPage.TryExecuteTapOutsideOfPopupCommand());
	}

	sealed class StyledPopup : Popup;
}