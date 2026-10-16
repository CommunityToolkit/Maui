using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class DrawingViewServiceTests
{
	// Catches: the stroke color is only applied on some Android versions (it used to be applied only from Android 10, API 29, on),
	// or the stroke is not drawn. Runs on the API 36 and the API 28 emulators alike.
	[Fact]
	public async Task StrokeIsDrawnWithItsColorOnEveryAndroidVersion()
	{
		var options = new ImagePointOptions(ToPoints((10, 20), (90, 20)), new Size(100, 40), 8, blue, new SolidPaint(white), new Size(100, 40));

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.ColorAtUnits(image, 50, 20, blue, tolerance: 2);
		DrawingPixelAssert.ColorAtUnits(image, 20, 20, blue, tolerance: 2);
		DrawingPixelAssert.ColorAtUnits(image, 50, 34, white, because: "8 units wide: covers y 16..24");
		DrawingPixelAssert.ColorAtUnits(image, 50, 6, white, because: "8 units wide: covers y 16..24");
	}
}