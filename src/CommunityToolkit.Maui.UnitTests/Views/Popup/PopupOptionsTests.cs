using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupOptionsTests : BaseTest
{
	[Fact]
	public void CanBeDismissedByTappingOutsideOfPopup_DefaultValue_ShouldBeTrue()
	{
		var popupOptions = new PopupOptions();
		Assert.True(popupOptions.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void CanBeDismissedByTappingOutsideOfPopup_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		popupOptions.CanBeDismissedByTappingOutsideOfPopup = false;
		Assert.False(popupOptions.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void Shadow_DefaultValue_ShouldBeTrue()
	{
		var popupOptions = new PopupOptions();

		Assert.Equal(Colors.Black, popupOptions.Shadow?.Brush);
		Assert.Equal(new(20, 20), popupOptions.Shadow?.Offset);
		Assert.Equal(40, popupOptions.Shadow?.Radius);
		Assert.Equal(0.8f, popupOptions.Shadow?.Opacity);
	}

	[Fact]
	public void Shadow_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		popupOptions.Shadow = null;
		Assert.Null(popupOptions.Shadow);
	}

	[Fact]
	public void PageOverlayColor_DefaultValue_ShouldBeDefaultColor()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(Colors.Black.WithAlpha(0.3f), popupOptions.PageOverlayColor);
	}

	[Fact]
	public void PageOverlayColor_SetValue_ShouldBeUpdated()
	{
		var popupOptions = new PopupOptions();
		var color = Colors.Red;
		popupOptions.PageOverlayColor = color;
		Assert.Equal(color, popupOptions.PageOverlayColor);
	}

	[Fact]
	public void BorderStroke_DefaultValue_ShouldBeDefaultStroke()
	{
		var popupOptions = new PopupOptions();
		Assert.Equal(Colors.LightGray, popupOptions.Shape?.Stroke);
	}

	[Fact]
	public void OnTappingOutsideOfPopup_DefaultValue_ShouldBeNull()
	{
		var popupOptions = new PopupOptions();
		Assert.Null(popupOptions.OnTappingOutsideOfPopup);
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
		Assert.Equal(new CornerRadius(20, 20, 20, 20), ((RoundRectangle?)popupOptions.Shape)?.CornerRadius);
		Assert.Equal(2, ((RoundRectangle?)popupOptions.Shape)?.StrokeThickness);
		Assert.Equal(Colors.LightGray, ((RoundRectangle?)popupOptions.Shape)?.Stroke);
	}

	[Fact]
	public void CanBeDismissedByTappingOutsideOfPopup_ShouldReturnTrue()
	{
		var options = new MockPopupOptions { CanBeDismissedByTappingOutsideOfPopup = true };
		Assert.True(options.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void CanBeDismissedByTappingOutsideOfPopup_ShouldReturnFalse()
	{
		var options = new MockPopupOptions { CanBeDismissedByTappingOutsideOfPopup = false };
		Assert.False(options.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void BorderStroke_ShouldReturnCorrectColor()
	{
		var color = Colors.Red;
		var options = new MockPopupOptions
		{
			Shape = new Ellipse
			{
				Stroke = color
			}
		};
		Assert.Equal(color, options.Shape.Stroke);
	}

	[Fact]
	public void PageOverylayColor_ShouldReturnCorrectColor()
	{
		var color = Colors.Red;
		var options = new MockPopupOptions { PageOverlayColor = color };
		Assert.Equal(color, options.PageOverlayColor);
	}

	[Fact]
	public void OnTappingOutsideOfPopup_ShouldInvokeAction()
	{
		bool actionInvoked = false;
		var options = new MockPopupOptions { OnTappingOutsideOfPopup = () => actionInvoked = true };
		options.OnTappingOutsideOfPopup?.Invoke();
		Assert.True(actionInvoked);
	}

	[Fact]
	public void Shape_ShouldReturnCorrectShape()
	{
		var shape = new Rectangle();
		var options = new MockPopupOptions { Shape = shape };
		Assert.Equal(shape, options.Shape);
	}

	[Fact]
	public void Margin_ShouldReturnCorrectThickness()
	{
		var margin = new Thickness(10);
		var options = new MockPopupOptions { Margin = margin };
		Assert.Equal(margin, options.Margin);
	}

	[Fact]
	public void Padding_ShouldReturnCorrectThickness()
	{
		var padding = new Thickness(5);
		var options = new MockPopupOptions { Padding = padding };
		Assert.Equal(padding, options.Padding);
	}

	[Fact]
	public void VerticalOptions_ShouldReturnCorrectLayoutOptions()
	{
		var verticalOptions = LayoutOptions.Center;
		var options = new MockPopupOptions { VerticalOptions = verticalOptions };
		Assert.Equal(verticalOptions, options.VerticalOptions);
	}

	[Fact]
	public void HorizontalOptions_ShouldReturnCorrectLayoutOptions()
	{
		var horizontalOptions = LayoutOptions.End;
		var options = new MockPopupOptions { HorizontalOptions = horizontalOptions };
		Assert.Equal(horizontalOptions, options.HorizontalOptions);
	}

	sealed class MockPopupOptions : IPopupOptions
	{
		public bool CanBeDismissedByTappingOutsideOfPopup { get; set; }
		public Color PageOverlayColor { get; set; } = Colors.Transparent;
		public Action? OnTappingOutsideOfPopup { get; set; }
		public Shape? Shape { get; set; }
		public Thickness Margin { get; set; }
		public Thickness Padding { get; set; }
		public LayoutOptions VerticalOptions { get; set; }
		public LayoutOptions HorizontalOptions { get; set; }
		public Shadow? Shadow { get; set; } = null;
	}
}