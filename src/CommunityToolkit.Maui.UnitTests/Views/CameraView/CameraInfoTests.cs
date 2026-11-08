using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class CameraInfoTests
{
	[Fact]
	public void ToString_DescribesTheCameraAndListsItsResolutions()
	{
		var cameraInfo = CreateCameraInfo([new Size(1920, 1080), new Size(640, 480)]);

		Assert.Equal("Camera Info => Name:Back Camera, id:0, position:Rear, hasFlash:True, minZoom:1, maxZoom:4\nresolutions 1920x1080, 640x480. ", cameraInfo.ToString());
	}

	[Fact]
	public void ToString_RepeatedResolution_IsSeparatedWithACommaLikeEveryOtherResolution()
	{
		var cameraInfo = CreateCameraInfo([new Size(1, 1), new Size(2, 2), new Size(1, 1)]);

		Assert.EndsWith("\nresolutions 1x1, 2x2, 1x1. ", cameraInfo.ToString());
	}

	[Fact]
	public void ToString_WithoutResolutions_ListsNone()
	{
		var cameraInfo = CreateCameraInfo([]);

		Assert.EndsWith("\nresolutions  ", cameraInfo.ToString());
	}

	static CameraInfo CreateCameraInfo(IEnumerable<Size> supportedResolutions) =>
		new("Back Camera", "0", CameraPosition.Rear, true, 1.0f, 4.0f, supportedResolutions);
}