using Xunit;

namespace CommunityToolkit.Maui.DeviceTests;

public class SmokeTests
{
	[Fact]
	public void ApplicationIsNotNull()
	{
		Assert.NotNull(Application.Current);
	}

	[Fact]
	public void WindowPageIsNotNull()
	{
		Assert.NotNull(Application.Current?.Windows[0].Page);
	}

	[Fact]
	public void DispatcherIsAvailable()
	{
		Assert.NotNull(Application.Current?.Dispatcher);
	}

	[Fact]
	public void MauiAppServicesAreAvailable()
	{
		Assert.NotNull(Application.Current?.Handler?.MauiContext?.Services);
	}
}
