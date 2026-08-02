using Xunit;

namespace CommunityToolkit.Maui.DeviceTests;

public class PlatformDetectionTests
{
	[Fact]
	public void DevicePlatformIsNotUnknown()
	{
		Assert.NotEqual(DevicePlatform.Unknown, DeviceInfo.Platform);
	}

	[Fact]
	public void DeviceIdiomIsNotUnknown()
	{
		Assert.NotEqual(DeviceIdiom.Unknown, DeviceInfo.Idiom);
	}

	[Fact]
	public void OperatingSystemVersionIsPopulated()
	{
		Assert.False(string.IsNullOrWhiteSpace(DeviceInfo.VersionString));
	}

#if ANDROID
	[Fact]
	public void PlatformIsAndroid()
	{
		Assert.Equal(DevicePlatform.Android, DeviceInfo.Platform);
		Assert.True(OperatingSystem.IsAndroid());
	}
#elif IOS
	[Fact]
	public void PlatformIsIOS()
	{
		Assert.Equal(DevicePlatform.iOS, DeviceInfo.Platform);
		Assert.True(OperatingSystem.IsIOS());
	}
#elif MACCATALYST
	[Fact]
	public void PlatformIsMacCatalyst()
	{
		Assert.Equal(DevicePlatform.MacCatalyst, DeviceInfo.Platform);
		Assert.True(OperatingSystem.IsMacCatalyst());
	}
#elif WINDOWS
	[Fact]
	public void PlatformIsWindows()
	{
		Assert.Equal(DevicePlatform.WinUI, DeviceInfo.Platform);
		Assert.True(OperatingSystem.IsWindows());
	}
#endif
}
