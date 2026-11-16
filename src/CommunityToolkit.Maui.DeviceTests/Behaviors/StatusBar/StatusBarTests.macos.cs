using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class StatusBarTests
{
	const string unsupportedMessage = "MacCatalyst does not currently support changing the macOS status bar color";

	static string UnsupportedStyleMessage => unsupportedMessage;

	[Theory]
	[InlineData(0xFFFF0000u)]
	[InlineData(0x00000000u)]
	public async Task SetColorThrowsNotSupportedException(uint argb)
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<NotSupportedException>(() => StatusBar.SetColor(Color.FromUint(argb))));

		Assert.Equal(unsupportedMessage, exception.Message);
	}

	[Fact]
	public async Task SetColorWithNullThrowsNotSupportedException()
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(static () => Assert.Throws<NotSupportedException>(static () => StatusBar.SetColor(null)));

		Assert.Equal(unsupportedMessage, exception.Message);
	}

	[Theory]
	[InlineData(StatusBarStyle.Default)]
	[InlineData(StatusBarStyle.LightContent)]
	[InlineData(StatusBarStyle.DarkContent)]
	public async Task SetStyleThrowsNotSupportedException(StatusBarStyle style)
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<NotSupportedException>(() => StatusBar.SetStyle(style)));

		Assert.Equal(unsupportedMessage, exception.Message);
	}
}