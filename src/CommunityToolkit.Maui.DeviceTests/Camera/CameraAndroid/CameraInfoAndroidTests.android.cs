using AndroidX.Camera.Core;
using CommunityToolkit.Maui.Core;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;
using ICameraProvider = CommunityToolkit.Maui.Core.ICameraProvider;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraInfoAndroid)]
public class CameraInfoAndroidTests
{
	static CameraInfo Create(string deviceId = "0", string name = "Back Camera", CameraPosition position = CameraPosition.Rear, bool flash = true,
		float minZoom = 1, float maxZoom = 4, IEnumerable<Size>? resolutions = null, CameraSelector? selector = null) =>
		new(name, deviceId, position, flash, minZoom, maxZoom, resolutions ?? [new Size(1, 1), new Size(2, 2)], selector!);

	static CameraSelector SelectorFor(int lensFacing) =>
		new CameraSelector.Builder().RequireLensFacing(lensFacing)!.Build();

	[Fact]
	public void ConstructorKeepsEveryArgument()
	{
		var selector = SelectorFor(CameraSelector.LensFacingFront);

		var info = new CameraInfo("Front Camera", "7", CameraPosition.Front, false, 0.5f, 8, [new Size(640, 480)], selector);

		Assert.Equal("Front Camera", info.Name);
		Assert.Equal("7", info.DeviceId);
		Assert.Equal(CameraPosition.Front, info.Position);
		Assert.False(info.IsFlashSupported);
		Assert.Equal(0.5f, info.MinimumZoomFactor);
		Assert.Equal(8f, info.MaximumZoomFactor);
		Assert.Equal(new[] { new Size(640, 480) }, info.SupportedResolutions);
	}

	[Theory]
	[InlineData(CameraSelector.LensFacingBack)]
	[InlineData(CameraSelector.LensFacingFront)]
	public void CameraSelectorIsTheNativeSelectorThatWasPassedIn(int lensFacing)
	{
		var selector = SelectorFor(lensFacing);

		var info = Create(selector: selector);

		// The lens facing getter of a CameraSelector is internal CameraX API, so only the instance can be compared.
		Assert.Same(selector, info.CameraSelector);
	}

	[Fact]
	public void CameraSelectorIsNullWhenNoneWasPassedIn()
	{
		var info = Create(selector: null);

		Assert.Null(info.CameraSelector);
	}

	[Fact]
	public void SupportedResolutionsIsACopyOfTheSequencePassedIn()
	{
		var resolutions = new List<Size> { new(1, 1), new(2, 2) };
		var info = Create(resolutions: resolutions);

		resolutions.Add(new Size(3, 3));
		resolutions.RemoveAt(0);

		Assert.Equal(new[] { new Size(1, 1), new Size(2, 2) }, info.SupportedResolutions);
	}

	[Fact]
	public void SupportedResolutionsCanBeEmpty()
	{
		var info = Create(resolutions: []);

		Assert.Empty(info.SupportedResolutions);
	}

	[Fact]
	public void EqualsComparesTheDeviceIdOnly()
	{
		var first = Create("1", name: "A", position: CameraPosition.Rear, flash: true, minZoom: 1, maxZoom: 2, resolutions: [new Size(1, 1)]);
		var sameId = Create("1", name: "B", position: CameraPosition.Front, flash: false, minZoom: 3, maxZoom: 9, resolutions: [new Size(9, 9)]);
		var otherId = Create("2", name: "A", position: CameraPosition.Rear, flash: true, minZoom: 1, maxZoom: 2, resolutions: [new Size(1, 1)]);

		Assert.True(first.Equals(sameId));
		Assert.True(((object)first).Equals(sameId));
		Assert.False(first.Equals(otherId));
		Assert.False(((object)first).Equals(otherId));
		Assert.True(first.Equals(first));
	}

	[Fact]
	public void EqualsIsFalseForNullAndForOtherTypes()
	{
		var info = Create("1");

		Assert.False(info.Equals((CameraInfo?)null));
		Assert.False(info.Equals((object?)null));
		Assert.False(info.Equals("1"));
		Assert.False(info.Equals(1));
	}

	[Fact]
	public void GetHashCodeIsTheHashCodeOfTheDeviceId()
	{
		var first = Create("camera-1", name: "A");
		var second = Create("camera-1", name: "B", maxZoom: 9);

		Assert.Equal("camera-1".GetHashCode(), first.GetHashCode());
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.NotEqual(Create("camera-2").GetHashCode(), first.GetHashCode());
	}

	[Fact]
	public void ToStringDescribesTheCameraAndEndsTheResolutionsWithADot()
	{
		var info = new CameraInfo("Back Camera", "0", CameraPosition.Rear, true, 1, 4, [new Size(1920, 1080), new Size(640, 480)], null!);

		Assert.Equal("Camera Info => Name:Back Camera, id:0, position:Rear, hasFlash:True, minZoom:1, maxZoom:4\nresolutions 1920x1080, 640x480. ", info.ToString());
	}

	[Fact]
	public void ToStringOfASingleResolutionEndsWithADot()
	{
		var info = Create(resolutions: [new Size(320, 240)]);

		Assert.EndsWith("resolutions 320x240. ", info.ToString());
	}

	[Fact]
	public void ToStringWithoutResolutionsListsNone()
	{
		var info = Create(resolutions: []);

		Assert.EndsWith("\nresolutions  ", info.ToString());
	}

	[Fact]
	public void ToStringSeparatesRepeatedResolutionsWithCommas()
	{
		var info = Create(resolutions: [new Size(1, 1), new Size(2, 2), new Size(1, 1)]);

		Assert.EndsWith("resolutions 1x1, 2x2, 1x1. ", info.ToString());
	}
}