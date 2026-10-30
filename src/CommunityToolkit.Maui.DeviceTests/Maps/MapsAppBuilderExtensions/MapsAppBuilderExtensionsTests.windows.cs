using CommunityToolkit.Maui.Maps;
using CommunityToolkit.Maui.Maps.Handlers;
using Microsoft.Maui.Controls.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MapsAppBuilderExtensions)]
[Collection(TestCollections.DisableParallelization)]
public class MapsAppBuilderExtensionsTests : MapTestBase
{
	[Fact]
	public void UseMauiCommunityToolkitMapsReturnsTheSameBuilder()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(MapHandlerWindows));
		var builder = MauiApp.CreateBuilder();

		try
		{
			var returned = builder.UseMauiCommunityToolkitMaps("any-key");

			Assert.Same(builder, returned);
		}
		finally
		{
			snapshot.Restore();
		}
	}

	[Fact]
	public async Task UseMauiCommunityToolkitMapsRegistersMapHandlerWindowsForMap()
	{
		RequireWebView2Runtime();
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(typeof(MapHandlerWindows), MauiContext.Handlers.GetHandlerType(typeof(Map)));

			var handler = new Map().ToHandler(MauiContext);

			Assert.IsType<MapHandlerWindows>(handler);
			Assert.IsType<MauiWebView>(handler.PlatformView);
			((IElementHandler)handler).DisconnectHandler();
		});
	}

	[Fact]
	public void UseMauiCommunityToolkitMapsStoresTheKeyWhenTheAppCreatesItsHandlers()
	{
		SetupMapBuilder("key-of-the-test");

		Assert.Equal("key-of-the-test", MapHandlerWindows.MapsKey);
	}

	[Fact]
	public void TheLastCallToUseMauiCommunityToolkitMapsDecidesTheKey()
	{
		SetupMapBuilder("first-key", builder => builder.UseMauiCommunityToolkitMaps("second-key"));

		Assert.Equal("second-key", MapHandlerWindows.MapsKey);
	}

	[Fact]
	public async Task TheKeyOfTheAppEndsUpInTheMapPageOfTheHandler()
	{
		RequireWebView2Runtime();
		SetupMapBuilder("key-in-the-page");

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = new Map().ToHandler(MauiContext);

			Assert.Contains("mapcontrol?key=key-in-the-page'", MapHandlerWindows.MapPage);
			((IElementHandler)handler).DisconnectHandler();
		});
	}

	public static TheoryData<string?> MissingMapsKeys => new() { null, string.Empty };

	[Theory]
	[MemberData(nameof(MissingMapsKeys))]
	public async Task AnEmptyKeyIsAcceptedHereAndRejectedWhenTheMapIsCreated(string? key)
	{
		SetupMapBuilder(key);

		Assert.Equal(key, MapHandlerWindows.MapsKey);
		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<InvalidOperationException>(() => new Map().ToHandler(MauiContext)));
		Assert.Equal("You need to specify a Bing Maps Key", exception.Message);
	}
}