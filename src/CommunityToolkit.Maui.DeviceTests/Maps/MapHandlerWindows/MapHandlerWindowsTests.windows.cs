using System.Reflection;
using CommunityToolkit.Maui.Maps.Handlers;
using Microsoft.Maui.Controls.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Platform;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MapHandlerWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MapHandlerWindowsTests : MapTestBase
{
	static readonly FieldInfo regionToGoField = typeof(MapHandlerWindows).GetField("regionToGo", BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException("MapHandlerWindows has no 'regionToGo' field: the handler changed.");

	static MapSpan? GetRegionToGo(MapHandlerWindows handler) => (MapSpan?)regionToGoField.GetValue(handler);

	public static TheoryData<string?> MissingMapsKeys => new() { null, string.Empty };

	[Theory]
	[MemberData(nameof(MissingMapsKeys))]
	public async Task CreatingThePlatformViewWithoutAMapsKeyThrowsInvalidOperationException(string? key)
	{
		SetupMapBuilder();
		MapHandlerWindows.MapsKey = key;

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<InvalidOperationException>(() => CreateHandler<MapHandlerWindows>(new Map())));

		Assert.Equal("You need to specify a Bing Maps Key", exception.Message);
	}

	[Fact]
	public async Task CreatingThePlatformViewReturnsAMauiWebViewAndBuildsTheMapPageWithTheKey()
	{
		RequireWebView2Runtime();
		SetupMapBuilder("first-key");

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = CreateHandler<MapHandlerWindows>(new Map());

			Assert.IsType<MauiWebView>(handler.PlatformView);
			Assert.NotNull(MapHandlerWindows.MapPage);
			Assert.Contains("src='https://www.bing.com/api/maps/mapcontrol?key=first-key'", MapHandlerWindows.MapPage);
			Assert.Contains("function setMapType(", MapHandlerWindows.MapPage);
			((IElementHandler)handler).DisconnectHandler();
		});
	}

	[Fact]
	public async Task EachPlatformViewBuildsTheMapPageWithTheKeyThatIsSetAtThatTime()
	{
		RequireWebView2Runtime();
		SetupMapBuilder("first-key");

		await InvokeOnMainThreadAsync(() =>
		{
			var first = CreateHandler<MapHandlerWindows>(new Map());
			Assert.Contains("key=first-key'", MapHandlerWindows.MapPage);

			MapHandlerWindows.MapsKey = "second-key";
			var second = CreateHandler<MapHandlerWindows>(new Map());

			Assert.Contains("key=second-key'", MapHandlerWindows.MapPage);
			Assert.DoesNotContain("first-key", MapHandlerWindows.MapPage);
			((IElementHandler)first).DisconnectHandler();
			((IElementHandler)second).DisconnectHandler();
		});
	}

	[Fact]
	public async Task MapTypeMappingRunsNoScriptWithoutAWebView()
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var map = new Map { MapType = MapType.Hybrid };
			var handler = CreateHandler<NoWebViewMapHandler>(map);

			Assert.False(await ScriptRanAsync(MapHandlerWindows.MapMapType(handler, map)));
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ZoomScrollAndTrafficMappingsRunNoScriptWithoutAWebView(bool enabled)
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var map = new Map { IsZoomEnabled = enabled, IsScrollEnabled = enabled, IsTrafficEnabled = enabled };
			var handler = CreateHandler<NoWebViewMapHandler>(map);

			Assert.False(await ScriptRanAsync(MapHandlerWindows.MapIsZoomEnabled(handler, map)));
			Assert.False(await ScriptRanAsync(MapHandlerWindows.MapIsScrollEnabled(handler, map)));
			Assert.False(await ScriptRanAsync(MapHandlerWindows.MapIsTrafficEnabled(handler, map)));
		});
	}

	[Fact]
	public async Task ShowingUserMappingRemovesTheLocationPinWithoutAskingForTheLocationWhenTheUserIsNotShown()
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var map = new Map { IsShowingUser = false };
			var handler = CreateHandler<NoWebViewMapHandler>(map);

			var exception = await Record.ExceptionAsync(() => MapHandlerWindows.MapIsShowingUser(handler, map));

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task PinsMappingRunsNoScriptWithoutAWebView()
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var map = new Map();
			map.Pins.Add(new Pin { Label = "First", Address = "Here", Location = new Location(1, 2) });
			map.Pins.Add(new Pin { Label = "Second", Address = "There", Location = new Location(3, 4) });
			var handler = CreateHandler<NoWebViewMapHandler>(map);

			var exception = await Record.ExceptionAsync(() => MapHandlerWindows.MapPins(handler, map));

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task ElementsMappingDoesNothing()
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var map = new Map();
			var handler = CreateHandler<NoWebViewMapHandler>(map);

			var exception = Record.Exception(() => MapHandlerWindows.MapElements(handler, map));

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task MoveToRegionStoresTheRegionOfAMapHandlerWindows()
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var map = new Map();
			var handler = CreateHandler<NoWebViewMapHandler>(map);
			var region = new MapSpan(new Location(10.5, 20.25), 1, 2);

			await MapHandlerWindows.MapMoveToRegion(handler, map, region);

			Assert.Same(region, GetRegionToGo(handler));
		});
	}

	[Fact]
	public async Task MoveToRegionOfTheMapIsStoredWhenTheHandlerIsCreated()
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var region = new MapSpan(new Location(47.5, -122.25), 0.5, 0.75);
			var map = new Map(region);

			// Map invokes MoveToRegion with its last region when the handler is set, before the page can have loaded.
			var handler = CreateHandler<NoWebViewMapHandler>(map);

			Assert.Same(region, GetRegionToGo(handler));
		});
	}

	public static TheoryData<object?> ArgumentsThatAreNoMapSpan => new() { null, "a string", 42 };

	[Theory]
	[MemberData(nameof(ArgumentsThatAreNoMapSpan))]
	public async Task MoveToRegionIgnoresAnArgumentThatIsNoMapSpan(object? argument)
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var map = new Map();
			var handler = CreateHandler<NoWebViewMapHandler>(map);
			var regionBefore = GetRegionToGo(handler);
			Assert.NotNull(regionBefore);

			var exception = await Record.ExceptionAsync(() => MapHandlerWindows.MapMoveToRegion(handler, map, argument));

			Assert.Null(exception);
			Assert.Same(regionBefore, GetRegionToGo(handler));
		});
	}

	[Fact]
	public async Task MoveToRegionWithAnotherKindOfMapHandlerSendsTheScriptToThePlatformViewOfThatHandler()
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var map = new Map();
			var otherHandler = DispatchProxy.Create<IMapHandler, OtherMapHandlerProxy>();
			var proxy = (OtherMapHandlerProxy)(object)otherHandler;

			// Another IMapHandler has no region to store, and no cast to MapHandlerWindows may fail. The script call reads its platform view once;
			// it is no WebView2, so no script runs.
			var exception = await Record.ExceptionAsync(() => MapHandlerWindows.MapMoveToRegion(otherHandler, map, new MapSpan(new Location(1, 2), 1, 1)));

			Assert.Null(exception);
			Assert.Equal(1, proxy.PlatformViewReads);
		});
	}

	[Fact]
	public async Task ConnectingTheHandlerListensToTheNetworkAndDisconnectingStops()
	{
		RequireWebView2Runtime();
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(0, Network.SubscriberCount);

			var handler = CreateHandler<MapHandlerWindows>(new Map());
			Assert.Equal(1, Network.SubscriberCount);

			((IElementHandler)handler).DisconnectHandler();
			Assert.Equal(0, Network.SubscriberCount);
		});
	}

	[Fact]
	public async Task HandlerWithoutAMauiWebViewDoesNotListenToTheNetwork()
	{
		SetupMapBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = CreateHandler<NoWebViewMapHandler>(new Map());
			Assert.Equal(0, Network.SubscriberCount);

			var exception = Record.Exception(() => ((IElementHandler)handler).DisconnectHandler());

			Assert.Null(exception);
			Assert.Equal(0, Network.SubscriberCount);
		});
	}

	[Fact]
	public async Task MapHandlerAndPlatformViewAreCollectedAfterTheHandlerIsDisconnected()
	{
		SetupMapBuilder();
		WeakReference mapReference = null!, handlerReference = null!, platformViewReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var map = new Map();
			var handler = CreateHandler<NoWebViewMapHandler>(map);
			mapReference = new WeakReference(map);
			handlerReference = new WeakReference(handler);
			platformViewReference = new WeakReference(handler.PlatformView);

			((IElementHandler)handler).DisconnectHandler();
			map.Handler = null;
		});

		await AssertionExtensions.WaitForGC(mapReference, handlerReference, platformViewReference);
	}

	[Fact]
	public async Task MapAndHandlerAreCollectedAfterAHostedHandlerIsDisconnected()
	{
		RequireWebView2Runtime();
		SetupMapBuilder();

		// Only the map and the handler are asserted on: WinUI releases the WebView2 itself on its own schedule.
		var (mapReference, handlerReference) = await HostAndDisconnectAsync();

		await AssertionExtensions.WaitForGC(mapReference, handlerReference);
	}

	async Task<(WeakReference MapReference, WeakReference HandlerReference)> HostAndDisconnectAsync()
	{
		WeakReference mapReference = null!, handlerReference = null!;
		var map = new Map();

		await WithMapPageAsync(map, (handler, _) =>
		{
			mapReference = new WeakReference(map);
			handlerReference = new WeakReference(handler);
			((IElementHandler)handler).DisconnectHandler();
			map.Handler = null;
			return Task.CompletedTask;
		}, loadPage: false);

		return (mapReference, handlerReference);
	}

	// Some mappings are declared to return Task but return the Task<bool> of the script call, whose result says whether a script ran.
	static async Task<bool> ScriptRanAsync(Task mappingTask) => await Assert.IsType<Task<bool>>(mappingTask, exactMatch: false);

	public class OtherMapHandlerProxy : DispatchProxy
	{
		WFrameworkElement? platformView;

		public int PlatformViewReads { get; private set; }

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
		{
			if (targetMethod?.Name != "get_PlatformView")
			{
				throw new NotSupportedException($"{targetMethod?.Name} is not used by the static map mappings.");
			}

			PlatformViewReads++;
			return platformView ??= new WGrid();
		}
	}
}