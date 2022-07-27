using System;
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

	[Theory]
	[InlineData("iPhone5,2", 326)]
	[InlineData("iPhone12,3", 458)]
	public void TryGetPpiByDeviceModel_Should_ReturnTrue_And_CorrectPPI_When_DeviceModelIsKnown(string appleDeviceModel, int expectedPPI)
	{
		var actualReturn = AppleScreenDensityHelper.TryGetPpiByDeviceModel(appleDeviceModel, out var actualPPI);

		Assert.Equal(expectedPPI, actualPPI);
		Assert.True(actualReturn);
	}

	[Theory]
	[InlineData("X86_64")]
	[InlineData("arm64")]
	[InlineData("i386")]
	[InlineData("unkown-device-model")]
	public void TryGetPpiByDeviceModel_Should_ReturnFalse_When_DeviceModelIsUnkown(string appleDeviceModel)
	{
		bool expected = false;

		var actual = AppleScreenDensityHelper.TryGetPpiByDeviceModel(appleDeviceModel, out var actualPPI);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("iPhone 11 Pro Max", 458)]
	[InlineData("iPhone 13 PRO MAX", 458)]
	[InlineData("iPhone 13 Pro", 460)]
	[InlineData("IPHONE 8 PLUS", 401)]
	[InlineData("iPhone 6 Plus", 401)]
	public void TryGetPpiByDeviceModel_Should_ReturnTrue_And_CorrectPPI_When_DeviceNameIsKnown(string appleDeviceName, int expectedPPI)
	{
		var actualReturn = AppleScreenDensityHelper.TryGetPpiByDeviceName(appleDeviceName, out var actualPPI);

		Assert.Equal(expectedPPI, actualPPI);
		Assert.True(actualReturn);
	}

	[Theory]
	[InlineData("iPhone old")]
	[InlineData("iPhone master blaster")]
	[InlineData("Null")]
	public void TryGetPpiByDeviceModel_Should_ReturnFalse_When_DeviceNameIsUnkown(string appleDeviceModel)
	{
		bool expected = false;

		var actual = AppleScreenDensityHelper.TryGetPpiByDeviceModel(appleDeviceModel, out var actualPPI);

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData(320, 480, 163)]
	[InlineData(428, 926, 458)]
	[InlineData(834, 1194, 264)]
	[InlineData(1024, 1366, 264)]
	public void TryGetPpiByScreenDimensions_Should_ReturnTrue_And_CorrectPPI_When_DeviceSizeIsFound(double width, double height, int expectedPPI)
	{
		var actualReturn = AppleScreenDensityHelper.TryGetPpiByScreenDimensions((width, height), out var actualPPI);

		Assert.Equal(expectedPPI, actualPPI);
		Assert.True(actualReturn);
	}

	[Theory]
	[InlineData(1000, 100)]
	[InlineData(90000, 400)]
	[InlineData(0, 0)]
	public void TryGetPpiByScreenDimensions_Should_ReturnFalse_When_DeviceSizeIsNotFound(double width, double height)
	{
		var actualReturn = AppleScreenDensityHelper.TryGetPpiByScreenDimensions((width, height), out var actualPPI);

		Assert.False(actualReturn);
	}
}

