using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Media.Imaging;
using WinBrush = Microsoft.UI.Xaml.Media.Brush;
using WinColor = Windows.UI.Color;
using WinGradientStop = Microsoft.UI.Xaml.Media.GradientStop;
using WinImageBrush = Microsoft.UI.Xaml.Media.ImageBrush;
using WinLinearGradientBrush = Microsoft.UI.Xaml.Media.LinearGradientBrush;
using WinPanel = Microsoft.UI.Xaml.Controls.Panel;
using WinSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace CommunityToolkit.Maui.DeviceTests;

// BrushExtensions (Windows only): ToColor turns a WinUI SolidColorBrush into the MAUI Color of its color (alpha included) and
// returns null for every other kind of brush.
[Category(TestCategory.BrushExtensions)]
[Collection(TestCollections.DisableParallelization)]
public class BrushExtensionsTests : CommunityToolkitHandlerTestBase
{
	[Theory]
	[InlineData(255, 12, 34, 56)]
	[InlineData(128, 255, 0, 0)]
	[InlineData(0, 0, 0, 0)]
	[InlineData(255, 255, 255, 255)]
	[InlineData(1, 254, 128, 3)]
	public Task SolidColorBrushConvertsToTheMauiColorOfItsColor(int alpha, int red, int green, int blue) =>
		InvokeOnMainThreadAsync(() =>
		{
			WinBrush brush = new WinSolidColorBrush(WinColor.FromArgb((byte)alpha, (byte)red, (byte)green, (byte)blue));

			var color = brush.ToColor();

			Assert.NotNull(color);
			Assert.Equal(Color.FromRgba(red, green, blue, alpha), color);
		});

	[Fact]
	public Task NullBrushConvertsToNull() =>
		InvokeOnMainThreadAsync(() =>
		{
			WinBrush? brush = null;

			// An extension method can be called on null: the brush is not a SolidColorBrush, so there is no color.
			Assert.Null(brush!.ToColor());
		});

	[Fact]
	public Task LinearGradientBrushConvertsToNull() =>
		InvokeOnMainThreadAsync(() =>
		{
			var brush = new WinLinearGradientBrush();
			brush.GradientStops.Add(new WinGradientStop { Color = WinColor.FromArgb(255, 255, 0, 0), Offset = 0 });
			brush.GradientStops.Add(new WinGradientStop { Color = WinColor.FromArgb(255, 0, 0, 255), Offset = 1 });

			Assert.Null(brush.ToColor());
		});

	[Fact]
	public Task ImageBrushConvertsToNull() =>
		InvokeOnMainThreadAsync(() =>
		{
			var brush = new WinImageBrush { ImageSource = new BitmapImage() };

			Assert.Null(brush.ToColor());
		});

	[Fact]
	public async Task BackgroundBrushOfAMauiViewConvertsToTheMauiBackgroundColor()
	{
		SetupBuilder();
		var grid = new Grid { BackgroundColor = Colors.Red };

		var handler = await CreateHandlerAsync<LayoutHandler>(grid);

		await InvokeOnMainThreadAsync(() =>
		{
			var panel = Assert.IsType<WinPanel>(handler.PlatformView, exactMatch: false);

			var color = panel.Background.ToColor();

			Assert.Equal(Colors.Red, color);
		});
	}
}