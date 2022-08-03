using CommunityToolkit.Maui.Core.Views.OnScreenSize;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.OnScreenSize;

public class AppleScreenDensityHelper_Tests
{
	[Theory]
	[InlineData("iPhone14,2", "iPhone 13 Pro", 390, 844, 460)]
	[InlineData("iPhone10,4", "iPhone 8", 375, 667, 326)]
	public void TryGetPpiWithFallBacks_Should_ReturnTrue_And_CorrectPPI_When_CorrectValuesAreProvided(string deviceModel, string deviceName, double width, double height, int expectedPPI)
	{
		var actualReturn = AppleScreenDensityHelper.TryGetPpiWithFallBacks(deviceModel, deviceName, (width, height), out var actualPPI);

		Assert.Equal(expectedPPI, actualPPI);
		Assert.True(actualReturn);
	}

	[Theory]
	[InlineData("XXXXX", "Samsung Galaxy 7", 190, 544)]
	[InlineData("HARDWARE_ID", "Phone name", 215, 47)]
	public void TryGetPpiWithFallBacks_Should_ReturnFalse_When_InvalidValuesAreProvided(string deviceModel, string deviceName, double width, double height)
	{
		var actualReturn = AppleScreenDensityHelper.TryGetPpiWithFallBacks(deviceModel, deviceName, (width, height), out var actualPPI);

		Assert.False(actualReturn);
	}


}

