using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.Tests.Views;

public class PopupOptionsTests
{
	[Fact]
	public void CanBeDismissedByTappingOutsideOfPopup_DefaultValue_ShouldBeTrue()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(PopupOptionsDefaults.CanBeDismissedByTappingOutsideOfPopup, popupOptions.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void CanBeDismissedByTappingOutsideOfPopup_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		popupOptions.CanBeDismissedByTappingOutsideOfPopup = false;
		Assert.False(popupOptions.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void BackgroundColor_DefaultValue_ShouldBeDefaultColor()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(PopupOptionsDefaults.BackgroundColor, popupOptions.BackgroundColor);
	}

	[Fact]
	public void BackgroundColor_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		var color = Colors.Red;
		popupOptions.BackgroundColor = color;
		Assert.Equal(color, popupOptions.BackgroundColor);
	}

	[Fact]
	public void OnTappingOutsideOfPopup_DefaultValue_ShouldBeNull()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(PopupOptionsDefaults.OnTappingOutsideOfPopup, popupOptions.OnTappingOutsideOfPopup);
	}

	[Fact]
	public void OnTappingOutsideOfPopup_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		Action action = () => { };
		popupOptions.OnTappingOutsideOfPopup = action;
		Assert.Equal(action, popupOptions.OnTappingOutsideOfPopup);
	}

	[Fact]
	public void Shape_DefaultValue_ShouldBeNull()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(PopupOptionsDefaults.Shape, popupOptions.Shape);
	}

	[Fact]
	public void Shape_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		IShape shape = new Rectangle();
		popupOptions.Shape = shape;
		Assert.Equal(shape, popupOptions.Shape);
	}

	[Fact]
	public void Margin_DefaultValue_ShouldBeDefaultThickness()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(PopupOptionsDefaults.Margin, popupOptions.Margin);
	}

	[Fact]
	public void Margin_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		var thickness = new Thickness(10);
		popupOptions.Margin = thickness;
		Assert.Equal(thickness, popupOptions.Margin);
	}

	[Fact]
	public void Padding_DefaultValue_ShouldBeDefaultThickness()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(PopupOptionsDefaults.Padding, popupOptions.Padding);
	}

	[Fact]
	public void Padding_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		var thickness = new Thickness(10);
		popupOptions.Padding = thickness;
		Assert.Equal(thickness, popupOptions.Padding);
	}

	[Fact]
	public void VerticalOptions_DefaultValue_ShouldBeDefaultLayoutOptions()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(PopupOptionsDefaults.VerticalOptions, popupOptions.VerticalOptions);
	}

	[Fact]
	public void VerticalOptions_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		var layoutOptions = LayoutOptions.Center;
		popupOptions.VerticalOptions = layoutOptions;
		Assert.Equal(layoutOptions, popupOptions.VerticalOptions);
	}

	[Fact]
	public void HorizontalOptions_DefaultValue_ShouldBeDefaultLayoutOptions()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(PopupOptionsDefaults.HorizontalOptions, popupOptions.HorizontalOptions);
	}

	[Fact]
	public void HorizontalOptions_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		var layoutOptions = LayoutOptions.Center;
		popupOptions.HorizontalOptions = layoutOptions;
		Assert.Equal(layoutOptions, popupOptions.HorizontalOptions);
	}
}
