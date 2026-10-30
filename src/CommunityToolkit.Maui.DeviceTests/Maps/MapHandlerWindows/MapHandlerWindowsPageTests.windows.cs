using System.Globalization;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Maui.Maps.Handlers;
using Microsoft.Maui.Controls.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;
using Microsoft.Maui.Devices.Sensors;
using IMap = Microsoft.Maui.Maps.IMap;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MapHandlerWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MapHandlerWindowsPageTests : MapTestBase
{
	static readonly FieldInfo regionToGoField = typeof(MapHandlerWindows).GetField("regionToGo", BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException("MapHandlerWindows has no 'regionToGo' field: the handler changed.");

	[Fact]
	public async Task PageIsLoadedOnlyWhileTheNetworkIsInternet()
	{
		SetupMapBuilder();

		await WithMapPageAsync(new Map(), async (handler, page) =>
		{
			// The handler loads its page from a string, which WebView2 navigates to as a data: URI.
			var pageNavigations = 0;
			page.WebView.NavigationStarting += (_, args) =>
			{
				if (args.Uri.StartsWith("data:", StringComparison.Ordinal))
				{
					pageNavigations++;
				}
			};

			Assert.False(page.WebView.CoreWebView2.Source.StartsWith("data:", StringComparison.Ordinal));
			Network.Change(NetworkAccess.Local);
			Network.Change(NetworkAccess.ConstrainedInternet);
			Network.Change(NetworkAccess.Unknown);
			Network.Change(NetworkAccess.None);

			var pageLoaded = page.WaitForPageLoadedAsync();
			Network.Change(NetworkAccess.Internet);
			await pageLoaded;

			// WebView2 raises NavigationStarting in the order the navigations were started, so a page that one of the other network
			// states had started to load would have been counted before the page of the Internet state completed.
			Assert.Equal(1, pageNavigations);
			Assert.Equal("function", await page.ExecuteForStringAsync("typeof setMapType"));
			Assert.Equal("function", await page.ExecuteForStringAsync("typeof Microsoft.Maps.Map"));
		}, loadPage: false);
	}

	[Fact]
	public async Task ConnectingWhileTheNetworkIsUpLoadsThePage()
	{
		RequireWebView2Runtime("The WebView2 Runtime is not installed, so the map page cannot be hosted.");
		SetupMapBuilder();
		Network.Change(NetworkAccess.Internet);

		await InvokeOnMainThreadAsync(async () =>
		{
			var map = new Map { WidthRequest = 400, HeightRequest = 300 };
			var handler = CreateHandler<MapHandlerWindows>(map);
			var webView = Assert.IsType<MauiWebView>(handler.PlatformView);
			// The handler starts loading as soon as the WebView2 is ready, so the stand-in has to be installed by then.
			webView.CoreWebView2Initialized += (sender, _) => InstallBingControlStub(sender);
			var page = new LoadedMapPage(webView);
			var pageLoaded = page.WaitForPageLoadedAsync();

			await webView.AttachAndRun(async () =>
			{
				await pageLoaded;

				Assert.Equal("function", await page.ExecuteForStringAsync("typeof setMapType"));
				await page.SettleAsync();
				((IElementHandler)handler).DisconnectHandler();
			}, MauiContext);
		});
	}

	[Fact]
	public async Task PropertiesPinsAndRegionOfTheMapAreSentToThePageWhenItLoads()
	{
		SetupMapBuilder();
		// IsTrafficEnabled is left out: the page only applies it once the Traffic module has loaded (see
		// TrafficEnabledBeforeThePageLoadsIsShownOnceTheTrafficModuleHasLoaded).
		var map = new Map(new MapSpan(new Location(47.5, -122.25), 0.5, 0.75))
		{
			MapType = MapType.Hybrid,
			IsZoomEnabled = false,
			IsScrollEnabled = false
		};
		var spaceNeedle = new Pin { Label = "Space Needle", Address = "Seattle Center", Location = new Location(47.6205, -122.3493) };
		var pikePlace = new Pin { Label = "Pike Place", Address = "Pike Place Market", Location = new Location(47.6097, -122.3425) };
		map.Pins.Add(spaceNeedle);
		map.Pins.Add(pikePlace);

		await WithMapPageAsync(map, async (handler, page) =>
		{
			var log = await page.WaitForLogAsync(entries => Entries(entries, "push").Count >= 2, "The two pins never reached the page.");

			Assert.Contains(Entries(log, "setView"), entry => entry.GetProperty("view").TryGetProperty("mapTypeId", out var type) && type.GetString() == "aerial");
			Assert.Contains(Entries(log, "setView"), entry => HasBoundsCenter(entry, 47.5, -122.25));
			Assert.Contains(Entries(log, "setOptions"), entry => entry.GetProperty("options").TryGetProperty("disableZooming", out var disabled) && disabled.GetBoolean());
			Assert.Contains(Entries(log, "setOptions"), entry => entry.GetProperty("options").TryGetProperty("disablePanning", out var disabled) && disabled.GetBoolean());
			Assert.Equal(["Microsoft.Maps.Traffic"], Entries(log, "module").Select(entry => entry.GetProperty("name").GetString()));
			// The user is not shown, so there is no location pin to remove.
			Assert.Empty(Entries(log, "remove"));

			var clearIndex = log.ToList().FindIndex(entry => entry.Contains("\"kind\":\"clear\"", StringComparison.Ordinal));
			var pushes = Entries(log, "push");
			Assert.True(clearIndex >= 0, "The handler should remove the pins of the page before it adds the pins of the map.");
			Assert.Equal(2, pushes.Count);
			AssertPin(pushes[0], spaceNeedle);
			AssertPin(pushes[1], pikePlace);
		});
	}

	[Theory]
	[InlineData(MapType.Street, "road")]
	[InlineData(MapType.Satellite, "aerial")]
	[InlineData(MapType.Hybrid, "aerial")]
	[InlineData((MapType)99, "road")]
	public async Task MapTypeChangeIsSentToThePageAsARoadOrAerialMap(MapType mapType, string expectedPageType)
	{
		SetupMapBuilder();
		// Starts with a type whose page type differs from the expected one, so that the change is a real change.
		var map = new Map { MapType = expectedPageType == "road" ? MapType.Satellite : MapType.Street };

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.MapType = mapType;

			var log = await page.WaitForLogAsync(entries => Entries(entries, "setView").Count == 1, $"MapType {mapType} never reached the page.");
			Assert.Equal(expectedPageType, Entries(log, "setView")[0].GetProperty("view").GetProperty("mapTypeId").GetString());
			Assert.True(await ScriptRanAsync(MapHandlerWindows.MapMapType(handler, map)));
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ZoomChangeIsSentToThePageAsDisableZooming(bool zoomEnabled)
	{
		SetupMapBuilder();
		var map = new Map { IsZoomEnabled = !zoomEnabled };

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.IsZoomEnabled = zoomEnabled;

			var log = await page.WaitForLogAsync(entries => Entries(entries, "setOptions").Count == 1, "IsZoomEnabled never reached the page.");
			Assert.Equal(!zoomEnabled, Entries(log, "setOptions")[0].GetProperty("options").GetProperty("disableZooming").GetBoolean());
			Assert.True(await ScriptRanAsync(MapHandlerWindows.MapIsZoomEnabled(handler, map)));
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ScrollChangeIsSentToThePageAsDisablePanning(bool scrollEnabled)
	{
		SetupMapBuilder();
		var map = new Map { IsScrollEnabled = !scrollEnabled };

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.IsScrollEnabled = scrollEnabled;

			var log = await page.WaitForLogAsync(entries => Entries(entries, "setOptions").Count == 1, "IsScrollEnabled never reached the page.");
			Assert.Equal(!scrollEnabled, Entries(log, "setOptions")[0].GetProperty("options").GetProperty("disablePanning").GetBoolean());
			Assert.True(await ScriptRanAsync(MapHandlerWindows.MapIsScrollEnabled(handler, map)));
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task TrafficChangeIsSentToThePageAsShowOrHide(bool trafficEnabled)
	{
		SetupMapBuilder();
		var map = new Map { IsTrafficEnabled = !trafficEnabled };

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.IsTrafficEnabled = trafficEnabled;

			var log = await page.WaitForLogAsync(entries => Entries(entries, "traffic").Count == 1, "IsTrafficEnabled never reached the page.");
			Assert.Equal(trafficEnabled, Entries(log, "traffic")[0].GetProperty("visible").GetBoolean());
			Assert.True(await ScriptRanAsync(MapHandlerWindows.MapIsTrafficEnabled(handler, map)));
		});
	}

	[Fact]
	public async Task NotShowingTheUserRemovesTheLocationPinOfThePage()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();
			await page.ExecuteAsync("addLocationPin(1.25, 2.5);");
			Assert.Single(Entries(await page.GetLogAsync(), "push"));

			// Updating the value through the handler needs no location permission, unlike showing the person.
			((IElementHandler)handler).UpdateValue(nameof(IMap.IsShowingUser));

			var log = await page.WaitForLogAsync(entries => Entries(entries, "remove").Count == 1, "The location pin was never removed.");
			var removed = Entries(log, "remove")[0].GetProperty("entity");
			Assert.Equal(1.25, removed.GetProperty("latitude").GetDouble());
			Assert.Equal(2.5, removed.GetProperty("longitude").GetDouble());
		});
	}

	[Fact]
	public async Task PinsAreSentToThePageAgainWhenThePinsChange()
	{
		SetupMapBuilder();
		var first = new Pin { Label = "First", Address = "Here", Location = new Location(1.5, 2.5) };
		var second = new Pin { Label = "Second", Address = "There", Location = new Location(3.5, 4.5) };
		var map = new Map();
		map.Pins.Add(first);

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.WaitForLogAsync(entries => Entries(entries, "push").Count == 1, "The first pin never reached the page.");
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.Pins.Add(second);

			var log = await page.WaitForLogAsync(entries => Entries(entries, "push").Count == 2, "The pins were never sent again.");
			Assert.StartsWith("{\"kind\":\"clear\"", log[0]);
			var pushes = Entries(log, "push");
			AssertPin(pushes[0], first);
			AssertPin(pushes[1], second);

			await page.ClearLogAsync();
			map.Pins.Clear();

			log = await page.WaitForLogAsync(entries => entries.Count >= 1, "The pins were never removed from the page.");
			await page.SettleAsync();
			log = await page.GetLogAsync();
			Assert.Equal(["{\"kind\":\"clear\"}"], log);
		});
	}

	[Fact]
	public async Task PinsThatAreNotMapsPinsAreSentWithAnEmptyMarkerId()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();
			var pin = new PlainMapPin { Label = "Plain", Address = "Nowhere", Location = new Location(5.5, 6.5) };

			// IMap.Pins can hold any IMapPin, but only a Maps Pin has an id.
			await MapHandlerWindows.MapPins(handler, CreateMapWithPins(pin));

			var log = await page.WaitForLogAsync(entries => Entries(entries, "push").Count == 1, "The plain pin never reached the page.");
			var entity = Entries(log, "push")[0].GetProperty("entity");
			Assert.Equal("Plain", entity.GetProperty("title").GetString());
			Assert.Equal("Nowhere", entity.GetProperty("subTitle").GetString());
			Assert.Equal(string.Empty, entity.GetProperty("markerId").GetString());
		});
	}

	[Fact]
	public async Task MoveToRegionIsSentToThePageWithTheCenterAndBothDegrees()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.MoveToRegion(new MapSpan(new Location(10.5, 20.25), 1.5, 2.5));

			var log = await page.WaitForLogAsync(entries => Entries(entries, "setView").Count == 1, "The region never reached the page.");
			var bounds = Entries(log, "setView")[0].GetProperty("view").GetProperty("bounds");
			Assert.Equal(10.5, bounds.GetProperty("center").GetProperty("latitude").GetDouble());
			Assert.Equal(20.25, bounds.GetProperty("center").GetProperty("longitude").GetDouble());
			// Which degrees are the width and which the height is checked by
			// MoveToRegionSendsTheLatitudeDegreesAsTheHeightAndTheLongitudeDegreesAsTheWidth.
			Assert.Equal([1.5, 2.5], new[] { bounds.GetProperty("width").GetDouble(), bounds.GetProperty("height").GetDouble() }.Order());

			// MapMoveToRegion returns a Task that completes once the page has run its script, not the Task<bool> of the script call.
			await page.ClearLogAsync();
			await MapHandlerWindows.MapMoveToRegion(handler, map, new MapSpan(new Location(-5.5, -6.25), 0.5, 0.5));
			Assert.Contains(Entries(await page.GetLogAsync(), "setView"), entry => HasBoundsCenter(entry, -5.5, -6.25));
		});
	}

	[Fact]
	public async Task CoordinatesAreSentWithTheInvariantCulture()
	{
		SetupMapBuilder();
		var map = new Map();
		var pin = new Pin { Label = "Seattle", Address = "WA", Location = new Location(47.6062, -122.3321) };

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();
			var culture = CultureInfo.CurrentCulture;
			// German writes 47,6062: used in the script, that would be two arguments.
			CultureInfo.CurrentCulture = new CultureInfo("de-DE");

			try
			{
				map.Pins.Add(pin);
				var log = await page.WaitForLogAsync(entries => Entries(entries, "push").Count == 1, "The pin never reached the page.");
				var entity = Entries(log, "push")[0].GetProperty("entity");
				Assert.Equal(47.6062, entity.GetProperty("latitude").GetDouble());
				Assert.Equal(-122.3321, entity.GetProperty("longitude").GetDouble());

				await page.ClearLogAsync();
				map.MoveToRegion(new MapSpan(new Location(10.5, 20.25), 1.5, 2.5));
				log = await page.WaitForLogAsync(entries => Entries(entries, "setView").Count == 1, "The region never reached the page.");
				var bounds = Entries(log, "setView")[0].GetProperty("view").GetProperty("bounds");
				Assert.Equal(10.5, bounds.GetProperty("center").GetProperty("latitude").GetDouble());
				Assert.Equal(20.25, bounds.GetProperty("center").GetProperty("longitude").GetDouble());
				// The degrees too: written as 1,5 and 2,5 they would be four arguments, and the page would get 1 and 5.
				Assert.Equal([1.5, 2.5], new[] { bounds.GetProperty("width").GetDouble(), bounds.GetProperty("height").GetDouble() }.Order());
			}
			finally
			{
				CultureInfo.CurrentCulture = culture;
			}
		});
	}

	[Fact]
	public async Task RegionIsSentAgainWhenThePageLoadsAgain()
	{
		SetupMapBuilder();
		var map = new Map(new MapSpan(new Location(47.5, -122.25), 0.5, 0.75));

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.WaitForLogAsync(entries => Entries(entries, "setView").Any(entry => HasBoundsCenter(entry, 47.5, -122.25)), "The first region never reached the page.");
			map.MoveToRegion(new MapSpan(new Location(10.5, 20.25), 1.5, 2.5));
			await page.WaitForLogAsync(entries => Entries(entries, "setView").Any(entry => HasBoundsCenter(entry, 10.5, 20.25)), "The new region never reached the page.");
			await page.SettleAsync();

			var pageLoaded = page.WaitForPageLoadedAsync();
			Network.Change(NetworkAccess.Internet);
			await pageLoaded;

			var log = await page.WaitForLogAsync(entries => Entries(entries, "setView").Any(entry => HasBoundsCenter(entry, 10.5, 20.25)), "The region was not sent to the page that loaded again.");
			Assert.DoesNotContain(Entries(log, "setView"), entry => HasBoundsCenter(entry, 47.5, -122.25));
		});
	}

	[Fact]
	public async Task NoRegionIsSentToThePageWhenNoRegionWasStored()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			// A Map always hands its handler a region, so take it away again.
			regionToGoField.SetValue(handler, null);

			var pageLoaded = page.WaitForPageLoadedAsync();
			Network.Change(NetworkAccess.Internet);
			await pageLoaded;
			await page.SettleAsync();

			var log = await page.GetLogAsync();
			Assert.DoesNotContain(Entries(log, "setView"), entry => entry.GetProperty("view").TryGetProperty("bounds", out _));
			Assert.Contains(Entries(log, "setView"), entry => entry.GetProperty("view").TryGetProperty("mapTypeId", out _));
		}, loadPage: false);
	}

	[Fact]
	public async Task DisconnectedHandlerNoLongerReactsToTheNetworkOrToTheLoadingOfAPage()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			Assert.Equal(1, Network.SubscriberCount);

			((IElementHandler)handler).DisconnectHandler();

			Assert.Equal(0, Network.SubscriberCount);

			// A handler that still listened would send the properties of the map to the page that is loaded again, and fail while doing so:
			// it has no map any more.
			var pageLoaded = page.WaitForPageLoadedAsync();
			page.WebView.NavigateToString(MapHandlerWindows.MapPage ?? throw new InvalidOperationException("The handler never built its map page."));
			await pageLoaded;
			// A handler that still listened would have queued its script calls from its own NavigationCompleted handler, which runs before
			// the one of the test: two rounds of scripts behind them are enough for them to have run.
			await page.SettleAsync();
			await page.SettleAsync();

			var log = await page.GetLogAsync();
			Assert.Equal(["map", "module"], log.Select(entry => JsonDocument.Parse(entry).RootElement.GetProperty("kind").GetString()));
		});
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task ShowingTheUserAddsALocationPinWhenTheLocationIsAllowed()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			// Windows may ask the person to allow the location: allow it for this test.
			map.IsShowingUser = true;

			var log = await page.WaitForLogAsync(entries => Entries(entries, "push").Count == 1, "No location pin reached the page: allow the app to use your location.");
			var entity = Entries(log, "push")[0].GetProperty("entity");
			Assert.InRange(entity.GetProperty("latitude").GetDouble(), -90, 90);
			Assert.InRange(entity.GetProperty("longitude").GetDouble(), -180, 180);
			Assert.Contains(Entries(log, "setView"), entry => entry.GetProperty("view").TryGetProperty("center", out _));
		});
	}

	// The app declares no location capability (Platforms/Windows/Package.appxmanifest), so Windows denies a packaged run the location
	// without asking anybody. An unpackaged run may get a location prompt instead, which nobody answers on CI and which would stay in
	// front of the later tests, so it skips (the packaged leg of the CI matrix runs this test).
	[Fact]
	public async Task ShowingTheUserWithoutLocationAccessAddsNoLocationPin()
	{
		Skip.IfNot(Microsoft.Maui.ApplicationModel.AppInfo.Current.PackagingModel == Microsoft.Maui.ApplicationModel.AppPackagingModel.Packaged,
			"Only a packaged run is denied the location without a prompt (the app declares no location capability); an unpackaged run may make Windows ask a person.");
		SetupMapBuilder();
		var access = await RequestLocationAccessAsync();
		Skip.If(access is null, "Windows did not answer the location access request within 5 seconds: it is probably waiting for a person to answer a location prompt.");
		Skip.If(access == Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed, "Location access is allowed on this machine, so the denied path cannot be forced.");
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.IsShowingUser = true;
			// The mapping of the property is not awaited; the same call, awaited, completes once the location request was answered.
			await MapHandlerWindows.MapIsShowingUser(handler, map);
			await page.SettleAsync();

			var log = await page.GetLogAsync();
			Assert.Empty(Entries(log, "push"));
			Assert.Empty(Entries(log, "setView"));
			Assert.Empty(Entries(log, "remove"));
		});
	}

	[Fact]
	public async Task RemovingThePinsKeepsTheMapCenteredOnTheLocationPin()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ExecuteAsync("addLocationPin(1.25, 2.5);");
			await page.ClearLogAsync();

			await MapHandlerWindows.MapPins(handler, map);

			var log = await page.WaitForLogAsync(entries => Entries(entries, "setView").Count == 1, "The map was never re-centered.");
			var center = Entries(log, "setView")[0].GetProperty("view").GetProperty("center");
			Assert.Equal(1.25, center.GetProperty("latitude").GetDouble());
			Assert.Equal(2.5, center.GetProperty("longitude").GetDouble());
		});
	}

	[Fact]
	public async Task PinWithAnApostropheInItsLabelReachesThePage()
	{
		SetupMapBuilder();
		var map = new Map();
		var pin = new Pin { Label = "Joe's Pizza", Address = "1 Main St", Location = new Location(1.5, 2.5) };

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.Pins.Add(pin);

			var log = await page.WaitForLogAsync(entries => Entries(entries, "push").Count == 1, "The pin never reached the page.");
			Assert.Equal("Joe's Pizza", Entries(log, "push")[0].GetProperty("entity").GetProperty("title").GetString());
		});
	}

	// Text that would break out of the single-quoted addPin('label', 'address', ...) call of MapPins if it were not encoded: a backslash
	// changes the text, or escapes the closing quote when it ends the text; a line break ends the script with a syntax error; and a text
	// that closes the quote runs its own script in the map page, which can post forged messages to the handler through window.chrome.webview.
	// The rows are enum members, so that no line break ends up in the name of a test.
	public enum PinText
	{
		BackslashInTheLabel,
		BackslashAtTheEndOfTheLabel,
		BackslashAtTheEndOfTheAddress,
		LineBreakInTheLabel,
		LineBreakInTheAddress,
		ScriptInTheLabel,
		ScriptInTheAddress
	}

	[Theory]
	[InlineData(PinText.BackslashInTheLabel)]
	[InlineData(PinText.BackslashAtTheEndOfTheLabel)]
	[InlineData(PinText.BackslashAtTheEndOfTheAddress)]
	[InlineData(PinText.LineBreakInTheLabel)]
	[InlineData(PinText.LineBreakInTheAddress)]
	[InlineData(PinText.ScriptInTheLabel)]
	[InlineData(PinText.ScriptInTheAddress)]
	public async Task PinLabelOrAddressWithJavaScriptSyntaxReachesThePageUnchangedAndRunsNoScript(PinText text)
	{
		SetupMapBuilder();
		var (label, address) = GetPinText(text);
		var map = new Map();
		var pin = new Pin { Label = label, Address = address, Location = new Location(1.5, 2.5) };

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.Pins.Add(pin);

			await page.WaitForLogAsync(entries => Entries(entries, "push").Count >= 1, "The pin never reached the page.");
			await page.SettleAsync();
			AssertPin(Assert.Single(Entries(await page.GetLogAsync(), "push")), pin);
			Assert.Equal("undefined", await page.ExecuteForStringAsync("typeof window.__injected"));
		});
	}

	static (string Label, string Address) GetPinText(PinText text) => text switch
	{
		PinText.BackslashInTheLabel => (@"Pike\Place", "1 Main St"),
		PinText.BackslashAtTheEndOfTheLabel => (@"Pike Place\", "1 Main St"),
		PinText.BackslashAtTheEndOfTheAddress => ("Pike Place", @"1 Main St\"),
		PinText.LineBreakInTheLabel => ("Pike\nPlace", "1 Main St"),
		PinText.LineBreakInTheAddress => ("Pike Place", "1 Main St\r\nSeattle"),
		PinText.ScriptInTheLabel => ("x', 0, 0); window.__injected = true; ('", "1 Main St"),
		PinText.ScriptInTheAddress => ("Pike Place", "x', ''); window.__injected = true; ('"),
		_ => throw new ArgumentOutOfRangeException(nameof(text), text, null)
	};

	[Fact]
	public async Task MoveToRegionSendsTheLatitudeDegreesAsTheHeightAndTheLongitudeDegreesAsTheWidth()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			map.MoveToRegion(new MapSpan(new Location(10.5, 20.25), latitudeDegrees: 1.5, longitudeDegrees: 2.5));

			var log = await page.WaitForLogAsync(entries => Entries(entries, "setView").Count == 1, "The region never reached the page.");
			var bounds = Entries(log, "setView")[0].GetProperty("view").GetProperty("bounds");
			Assert.Equal(1.5, bounds.GetProperty("height").GetDouble());
			Assert.Equal(2.5, bounds.GetProperty("width").GetDouble());
		});
	}

	[Fact]
	public async Task TrafficEnabledBeforeThePageLoadsIsShownOnceTheTrafficModuleHasLoaded()
	{
		SetupMapBuilder();
		var map = new Map { IsTrafficEnabled = true };

		// WithMapPageAsync lets the handler send the properties of the map to the loaded page, and only then delivers the Traffic module.
		await WithMapPageAsync(map, async (handler, page) =>
		{
			var log = await page.WaitForLogAsync(entries => Entries(entries, "traffic").Count >= 1, "The traffic of the map was never shown.");
			Assert.Equal([true], Entries(log, "traffic").Select(entry => entry.GetProperty("visible").GetBoolean()));
		});
	}

	[Fact]
	public async Task PinWithoutALocationIsLeftOutAndTheOtherPinsReachThePage()
	{
		SetupMapBuilder();
		var map = new Map();
		var pin = new Pin { Label = "Seattle", Address = "WA", Location = new Location(47.6062, -122.3321) };

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			// The pin without a location comes first, so a mapping that stops at it never sends the other one.
			map.Pins.Add(new Pin { Label = "No location" });
			map.Pins.Add(pin);

			await page.WaitForLogAsync(entries => Entries(entries, "push").Count >= 1, "The pin with a location never reached the page.");
			await page.SettleAsync();
			AssertPin(Assert.Single(Entries(await page.GetLogAsync(), "push")), pin);
		});
	}

	// MapPins used to send the removal on its own and then, after an await, the pins of that later moment: the removals of all three changes
	// ran first and each change then added all three pins.
	[Fact]
	public async Task PinsAddedOneAfterTheOtherAreEachOnTheMapOnce()
	{
		SetupMapBuilder();
		var map = new Map();
		Pin[] pins =
		[
			new Pin { Label = "First", Address = "1", Location = new Location(1, 1) },
			new Pin { Label = "Second", Address = "2", Location = new Location(2, 2) },
			new Pin { Label = "Third", Address = "3", Location = new Location(3, 3) },
		];

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			await page.ClearLogAsync();

			foreach (var pin in pins)
			{
				map.Pins.Add(pin);
			}

			await page.WaitForLogAsync(entries => Entries(entries, "push").Count >= pins.Length, "The pins never reached the page.");
			await page.SettleAsync();

			var log = await page.GetLogAsync();
			var lastClear = -1;
			for (var i = 0; i < log.Count; i++)
			{
				using var entry = JsonDocument.Parse(log[i]);
				if (entry.RootElement.GetProperty("kind").GetString() == "clear")
				{
					lastClear = i;
				}
			}

			var shown = Entries(log.Skip(lastClear + 1), "push");
			Assert.Equal(pins.Length, shown.Count);
			for (var i = 0; i < pins.Length; i++)
			{
				AssertPin(shown[i], pins[i]);
			}
		});
	}

	[Fact]
	public async Task MapChangesAfterTheWebViewWasClosedNeitherFailNorHang()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.SettleAsync();
			page.CloseWebView();

			var exception = await Record.ExceptionAsync(async () =>
			{
				await MapHandlerWindows.MapMapType(handler, map).WaitAsync(pageTimeout);
				await MapHandlerWindows.MapPins(handler, map).WaitAsync(pageTimeout);
				await MapHandlerWindows.MapMoveToRegion(handler, map, new MapSpan(new Location(1.5, 2.5), 1, 1)).WaitAsync(pageTimeout);
				await MapHandlerWindows.MapIsShowingUser(handler, map).WaitAsync(pageTimeout);
			});

			Assert.Null(exception);

			// The property changes go through the same calls without awaiting them: an exception there would be unhandled.
			map.MapType = MapType.Satellite;
			map.Pins.Add(new Pin { Label = "After close", Location = new Location(3.5, 4.5) });
			map.MoveToRegion(new MapSpan(new Location(5.5, 6.5), 1, 1));
		});
	}

	// Null when Windows did not answer within 5 seconds: a location prompt is waiting for a person.
	static async Task<Windows.Devices.Geolocation.GeolocationAccessStatus?> RequestLocationAccessAsync()
	{
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		try
		{
			return await InvokeOnMainThreadAsync(async () => await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync().AsTask(cancellation.Token));
		}
		catch (OperationCanceledException)
		{
			return null;
		}
	}

	static bool HasBoundsCenter(JsonElement setView, double latitude, double longitude) =>
		setView.GetProperty("view").TryGetProperty("bounds", out var bounds)
		&& bounds.GetProperty("center").GetProperty("latitude").GetDouble() == latitude
		&& bounds.GetProperty("center").GetProperty("longitude").GetDouble() == longitude;

	static void AssertPin(JsonElement push, Pin pin)
	{
		var entity = push.GetProperty("entity");
		Assert.Equal(pin.Location.Latitude, entity.GetProperty("latitude").GetDouble());
		Assert.Equal(pin.Location.Longitude, entity.GetProperty("longitude").GetDouble());
		Assert.Equal(pin.Label, entity.GetProperty("title").GetString());
		Assert.Equal(pin.Address, entity.GetProperty("subTitle").GetString());
		Assert.Equal(pin.Id.ToString(), entity.GetProperty("markerId").GetString());
	}

	static async Task<bool> ScriptRanAsync(Task mappingTask) => await Assert.IsType<Task<bool>>(mappingTask, exactMatch: false);

	sealed class PlainMapPin : IMapPin
	{
		public string Address { get; set; } = string.Empty;

		public string Label { get; set; } = string.Empty;

		public Location Location { get; set; } = new(0, 0);

		public object? MarkerId { get; set; }

		public IElementHandler? Handler { get; set; }

		public IElement? Parent => null;

		public bool SendMarkerClick() => false;

		public bool SendInfoWindowClick() => false;
	}

	static IMap CreateMapWithPins(params IMapPin[] pins)
	{
		var map = DispatchProxy.Create<IMap, PinsOnlyMapProxy>();
		((PinsOnlyMapProxy)(object)map).Pins = [.. pins];
		return map;
	}

	public class PinsOnlyMapProxy : DispatchProxy
	{
		public IList<IMapPin> Pins { get; set; } = [];

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
		{
			"get_Pins" => Pins,
			_ => throw new NotSupportedException($"{targetMethod?.Name} is not used by MapHandlerWindows.MapPins.")
		};
	}
}