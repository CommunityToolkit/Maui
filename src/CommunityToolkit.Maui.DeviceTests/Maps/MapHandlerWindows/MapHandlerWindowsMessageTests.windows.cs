using System.Net;
using System.Text.Json;
using Microsoft.Maui.Controls.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.MapHandlerWindows)]
[Collection(TestCollections.DisableParallelization)]
public class MapHandlerWindowsMessageTests : MapTestBase
{
	const string hideInfoWindowEntry = "{\"kind\":\"infobox\",\"options\":{\"visible\":false}}";

	[Fact]
	public async Task BoundsChangedMessageSetsTheVisibleRegionOfTheMap()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (_, page) =>
		{
			Assert.Null(map.VisibleRegion);

			await page.RaiseMapEventAsync("window.__renderMap({ center: { latitude: 10.5, longitude: 20.5, altitude: 0, altitudeReference: -1 }, width: 3, height: 2 });");

			var region = map.VisibleRegion;
			Assert.NotNull(region);
			Assert.Equal(10.5, region.Center.Latitude);
			Assert.Equal(20.5, region.Center.Longitude);
			// The height of the bounds is the latitude, the width the longitude.
			Assert.Equal(2, region.LatitudeDegrees);
			Assert.Equal(3, region.LongitudeDegrees);
		});
	}

	[Fact]
	public async Task BoundsChangedMessageWithoutACenterKeepsTheVisibleRegion()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (_, page) =>
		{
			await page.RaiseMapEventAsync("window.__renderMap({ center: { latitude: 10.5, longitude: 20.5, altitude: 0, altitudeReference: -1 }, width: 3, height: 2 });");
			var region = map.VisibleRegion;
			Assert.NotNull(region);

			await page.RaiseMapEventAsync("window.__renderMap({ center: null, width: 9, height: 8 });");

			Assert.Same(region, map.VisibleRegion);
		});
	}

	[Fact]
	public async Task MapClickedMessageRaisesTheMapClickedEventOnlyForPrimaryClicks()
	{
		SetupMapBuilder();
		var map = new Map();
		var clicks = new List<Location>();
		map.MapClicked += (_, args) => clicks.Add(args.Location);

		await WithMapPageAsync(map, async (_, page) =>
		{
			// The page does not report a click of another button, so nothing is posted for it.
			await page.ExecuteAsync("window.__clickMap(9.5, 9.5, false);");
			await page.RaiseMapEventAsync("window.__clickMap(1.5, 2.5, true);");

			var click = Assert.Single(clicks);
			Assert.Equal(1.5, click.Latitude);
			Assert.Equal(2.5, click.Longitude);
		});
	}

	[Theory]
	[InlineData("{ id: 'mapclicked', payload: { latitude: 1.5, longitude: 2.5 } }")]
	[InlineData("{ id: 'MAPCLICKED', payload: { latitude: 1.5, longitude: 2.5 } }")]
	[InlineData("{ ID: 'MapClicked', Payload: { Latitude: 1.5, Longitude: 2.5 } }")]
	public async Task MessageIdsAndPropertyNamesAreMatchedIgnoringCase(string message)
	{
		SetupMapBuilder();
		var map = new Map();
		var clicks = new List<Location>();
		map.MapClicked += (_, args) => clicks.Add(args.Location);

		await WithMapPageAsync(map, async (_, page) =>
		{
			await page.PostMessageAsync(message);

			var click = Assert.Single(clicks);
			Assert.Equal(1.5, click.Latitude);
			Assert.Equal(2.5, click.Longitude);
		});
	}

	[Theory]
	[InlineData("null")]
	[InlineData("{ id: 'Bogus', payload: { latitude: 1.5, longitude: 2.5 } }")]
	[InlineData("{ id: '', payload: { latitude: 1.5, longitude: 2.5 } }")]
	[InlineData("{ id: 'Unknown', payload: { latitude: 1.5, longitude: 2.5 } }")]
	[InlineData("{ id: '99', payload: { latitude: 1.5, longitude: 2.5 } }")]
	[InlineData("{ id: 'MapClicked' }")]
	[InlineData("{ id: 'MapClicked', payload: null }")]
	[InlineData("{ id: 'MapClicked', payload: '' }")]
	[InlineData("{ id: 'MapClicked', payload: '   ' }")]
	[InlineData("{ id: 'MapClicked', payload: 'null' }")]
	[InlineData("{ id: 'BoundsChanged', payload: { center: null } }")]
	public async Task MessageThatCannotBeActedOnIsIgnored(string message)
	{
		SetupMapBuilder();
		await AssertMessageIsIgnoredAsync(message);
	}

	// Valid JSON (WebView2 serializes whatever a script posts) of another shape than the page posts: a message that is no object, or a
	// payload that the type the handler reads it into cannot hold (Center.Altitude is an int, InfoWindowMarkerId a string).
	[Theory]
	[InlineData("'MapClicked'")]
	[InlineData("''")]
	[InlineData("{ id: 'MapClicked', payload: 5 }")]
	[InlineData("{ id: 'BoundsChanged', payload: [1, 2] }")]
	[InlineData("{ id: 'BoundsChanged', payload: { center: { latitude: 1, longitude: 2, altitude: 12.5 }, width: 1, height: 1 } }")]
	[InlineData("{ id: 'PinClicked', payload: 'x' }")]
	[InlineData("{ id: 'InfoWindowClicked', payload: 5 }")]
	[InlineData("{ id: 'InfoWindowClicked', payload: { infoWindowMarkerId: 7 } }")]
	public async Task MessageOfAnotherShapeIsIgnoredAndTheHandlerKeepsListening(string message)
	{
		SetupMapBuilder();
		await AssertMessageIsIgnoredAsync(message);
	}

	[Theory]
	[InlineData("PinClicked")]
	[InlineData("InfoWindowClicked")]
	public async Task ClickOnAPinThatIsInThePinsTwiceRaisesItsEventOnceAndTheHandlerKeepsListening(string messageId)
	{
		SetupMapBuilder();
		var pin = new Pin { Label = "Twice", Address = "Here", Location = new Location(1.5, 2.5) };
		var map = new Map();
		map.Pins.Add(pin);
		map.Pins.Add(pin);
		var markerClicks = 0;
		var infoWindowClicks = 0;
		pin.MarkerClicked += (_, _) => markerClicks++;
		pin.InfoWindowClicked += (_, _) => infoWindowClicks++;
		var mapClicks = new List<Location>();
		map.MapClicked += (_, args) => mapClicks.Add(args.Location);

		await WithMapPageAsync(map, async (_, page) =>
		{
			await WaitForPinsAsync(page);
			var payload = messageId == "PinClicked"
				? $"{{ label: 'Twice', address: 'Here', location: {{ latitude: 1.5, longitude: 2.5 }}, markerId: '{pin.Id}' }}"
				: $"{{ infoWindowMarkerId: '{pin.Id}' }}";

			await page.PostMessageAsync($"{{ id: '{messageId}', payload: {payload} }}");
			await page.SettleAsync();

			Assert.Equal(messageId == "PinClicked" ? 1 : 0, markerClicks);
			Assert.Equal(messageId == "InfoWindowClicked" ? 1 : 0, infoWindowClicks);

			await page.PostMessageAsync("{ id: 'MapClicked', payload: { latitude: 7.5, longitude: 8.5 } }");
			Assert.Single(mapClicks);
		});
	}

	[Fact]
	public async Task PinClickedMessageRaisesMarkerClickedOfThePinWithThatId()
	{
		SetupMapBuilder();
		var (map, first, second) = CreateMapWithTwoPins();
		var firstClicks = 0;
		var secondClicks = 0;
		var infoWindowClicks = 0;
		first.MarkerClicked += (_, _) => firstClicks++;
		second.MarkerClicked += (_, _) => secondClicks++;
		first.InfoWindowClicked += (_, _) => infoWindowClicks++;

		await WithMapPageAsync(map, async (_, page) =>
		{
			await WaitForPinsAsync(page);

			await page.RaiseMapEventAsync($"window.__clickPin('{first.Id}');");

			Assert.Equal(1, firstClicks);
			Assert.Equal(0, secondClicks);
			Assert.Equal(0, infoWindowClicks);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PinClickedMessageHidesTheInfoWindowOnlyWhenThePinAsksFor(bool hideInfoWindow)
	{
		SetupMapBuilder();
		var (map, first, _) = CreateMapWithTwoPins();
		first.MarkerClicked += (_, args) => args.HideInfoWindow = hideInfoWindow;

		await WithMapPageAsync(map, async (_, page) =>
		{
			await WaitForPinsAsync(page);

			await page.RaiseMapEventAsync($"window.__clickPin('{first.Id}');");
			await page.SettleAsync();

			var infoboxEntries = Entries(await page.GetLogAsync(), "infobox");
			// The page opens its info window for the pin, and the handler hides it again only when the event asked for it
			// (HideInfoWindow is false unless a handler sets it).
			Assert.True(infoboxEntries[0].GetProperty("options").GetProperty("visible").GetBoolean());
			Assert.Equal("First", infoboxEntries[0].GetProperty("options").GetProperty("title").GetString());
			Assert.Equal(hideInfoWindow ? 2 : 1, infoboxEntries.Count);
		});
	}

	// Bing shows the title of its info window as text, but renders the description as HTML (the stand-in shows them the same way). The page passes
	// the label of the clicked pin as the title and encodes the address for the description, so that the info window shows both as they are
	// written and no markup in them runs script in the map page (which can post messages to the handler).
	// The rows are enum members, so that no markup ends up in the name of a test.
	public enum InfoWindowText
	{
		PlainText,
		MarkupInTheLabel,
		MarkupInTheAddress,
		EveryCharacterThatHtmlEncodes
	}

	const string scriptMarkup = "<img src=x onerror=\"window.__injected = (window.__injected || 0) + 1\">";

	[Theory]
	[InlineData(InfoWindowText.PlainText)]
	[InlineData(InfoWindowText.MarkupInTheLabel)]
	[InlineData(InfoWindowText.MarkupInTheAddress)]
	[InlineData(InfoWindowText.EveryCharacterThatHtmlEncodes)]
	public async Task PinClickShowsTheLabelAndTheAddressAsTextInTheInfoWindow(InfoWindowText text)
	{
		SetupMapBuilder();
		var (label, address) = GetInfoWindowText(text);
		var pin = new Pin { Label = label, Address = address, Location = new Location(1.5, 2.5) };
		var map = new Map();
		map.Pins.Add(pin);

		await WithMapPageAsync(map, async (_, page) =>
		{
			await page.WaitForLogAsync(entries => Entries(entries, "push").Count >= 1, "The pin never reached the page.");

			await page.RaiseMapEventAsync($"window.__clickPin('{pin.Id}');");
			await page.SettleAsync();

			var options = Entries(await page.GetLogAsync(), "infobox")[0].GetProperty("options");
			Assert.True(options.GetProperty("visible").GetBoolean());

			Assert.Equal(label, options.GetProperty("title").GetString());
			Assert.Equal(WebUtility.HtmlEncode(address), options.GetProperty("description").GetString());

			Assert.Equal(label + address, await GetInfoWindowTextAsync(page));

			if (text is InfoWindowText.MarkupInTheLabel or InfoWindowText.MarkupInTheAddress)
			{
				// A control: the same markup inserted into the page as HTML, counting in window.__control instead, does run script here. Its error
				// event comes after the one the info window's text would raise, so once it has run, script in the info window would have run too.
				await page.ExecuteAsync($"document.body.insertAdjacentHTML('beforeend', {JsonSerializer.Serialize((label + address).Replace("__injected", "__control", StringComparison.Ordinal))});");

				await AssertHelpers.AssertEventually(async () => await page.ExecuteForStringAsync("String(window.__control)") is "1", timeout: 10000, interval: 50,
					message: "The markup inserted as HTML ran no script either, so this page cannot show that the info window runs none.");
				Assert.Equal("undefined", await page.ExecuteForStringAsync("typeof window.__injected"));
			}
		});
	}

	static (string Label, string Address) GetInfoWindowText(InfoWindowText text) => text switch
	{
		InfoWindowText.PlainText => ("Pike Place", "1 Main St"),
		InfoWindowText.MarkupInTheLabel => ($"Pike Place {scriptMarkup}", "1 Main St"),
		InfoWindowText.MarkupInTheAddress => ("Pike Place", $"{scriptMarkup} 1 Main St"),
		InfoWindowText.EveryCharacterThatHtmlEncodes => ("Tom & Jerry's \"Diner\" <b>", "<i>1 & 2</i> 'Main' St > &amp;"),
		_ => throw new ArgumentOutOfRangeException(nameof(text), text, null)
	};

	// Null when the description holds an element, so markup that became elements never equals the expected text.
	static Task<string?> GetInfoWindowTextAsync(LoadedMapPage page) =>
		page.ExecuteForStringAsync("(function () { var infobox = document.getElementById('__infobox'); return infobox.lastChild.childElementCount === 0 ? infobox.textContent : null; })()");

	[Fact]
	public async Task PinClickedMessageForAnUnknownPinHidesTheInfoWindow()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (_, page) =>
		{
			await page.PostMessageAsync("{ id: 'PinClicked', payload: { label: 'Gone', address: 'Nowhere', location: { latitude: 1.5, longitude: 2.5 }, markerId: 'a-pin-the-map-never-had' } }");

			var log = await page.WaitForLogAsync(entries => entries.Contains(hideInfoWindowEntry), "The info window was not hidden for a pin that the map does not have.");
			Assert.Single(log, hideInfoWindowEntry);
		});
	}

	// A script in the page can post any payload, also as JSON text, which can hold a property twice. The handler reads only the marker id of a
	// clicked pin, so properties that JSON cannot give a MAUI Pin (an Element) neither stop the click nor escape the handler: parent and handler
	// have an abstract and an interface type, and AutomationId can be set only once.
	[Theory]
	[InlineData("{ markerId: 'MARKER_ID', parent: {} }")]
	[InlineData("{ markerId: 'MARKER_ID', handler: {} }")]
	[InlineData("""'{"markerId":"MARKER_ID","automationId":"a","automationId":"b"}'""")]
	public async Task PinClickedMessageWithAnotherPropertyRaisesMarkerClickedOfThePinWithThatIdAndTheHandlerKeepsListening(string payload)
	{
		SetupMapBuilder();
		var (map, first, second) = CreateMapWithTwoPins();
		var firstClicks = 0;
		var secondClicks = 0;
		first.MarkerClicked += (_, _) => firstClicks++;
		second.MarkerClicked += (_, _) => secondClicks++;
		var mapClicks = new List<Location>();
		map.MapClicked += (_, args) => mapClicks.Add(args.Location);

		await WithMapPageAsync(map, async (_, page) =>
		{
			await WaitForPinsAsync(page);

			await page.PostMessageAsync($"{{ id: 'PinClicked', payload: {payload.Replace("MARKER_ID", first.Id.ToString(), StringComparison.Ordinal)} }}");
			await page.SettleAsync();

			Assert.Equal(1, firstClicks);
			Assert.Equal(0, secondClicks);

			await page.PostMessageAsync("{ id: 'MapClicked', payload: { latitude: 7.5, longitude: 8.5 } }");
			Assert.Single(mapClicks);
		});
	}

	[Theory]
	[InlineData("{ id: 'PinClicked', payload: { label: 'First', address: 'Here', location: { latitude: 1.5, longitude: 2.5 }, markerId: '' } }")]
	[InlineData("{ id: 'PinClicked', payload: { label: 'First', address: 'Here', location: { latitude: 1.5, longitude: 2.5 }, markerId: null } }")]
	[InlineData("{ id: 'PinClicked', payload: { label: 'First', address: 'Here', location: { latitude: 1.5, longitude: 2.5 } } }")]
	public async Task PinClickedMessageWithoutAMarkerIdDoesNothing(string message)
	{
		SetupMapBuilder();
		var (map, first, second) = CreateMapWithTwoPins();
		var clicks = 0;
		first.MarkerClicked += (_, _) => clicks++;
		second.MarkerClicked += (_, _) => clicks++;

		await WithMapPageAsync(map, async (_, page) =>
		{
			await WaitForPinsAsync(page);

			await page.PostMessageAsync(message);
			await page.SettleAsync();

			Assert.Equal(0, clicks);
			Assert.Empty(Entries(await page.GetLogAsync(), "infobox"));
		});
	}

	[Fact]
	public async Task ClickOnAnEntityThatIsNotAPushpinSendsNoMessageAndKeepsTheInfoWindowClosed()
	{
		SetupMapBuilder();
		var (map, first, second) = CreateMapWithTwoPins();
		var clicks = 0;
		first.MarkerClicked += (_, _) => clicks++;
		second.MarkerClicked += (_, _) => clicks++;

		await WithMapPageAsync(map, async (_, page) =>
		{
			await WaitForPinsAsync(page);
			var messages = 0;
			page.WebView.WebMessageReceived += (_, _) => messages++;

			// Bing raises click for every kind of entity: the page only reacts to a pushpin.
			Assert.Equal(1, await page.ExecuteForCountAsync($"window.__clickPin('{first.Id}', 'polyline');"));
			await PostOrderingMessageAsync(page);

			Assert.Equal(1, messages);
			Assert.Equal(0, clicks);
			Assert.Empty(Entries(await page.GetLogAsync(), "infobox"));
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InfoWindowClickedMessageRaisesInfoWindowClickedOfThePinWhoseInfoWindowIsOpen(bool hideInfoWindow)
	{
		SetupMapBuilder();
		var (map, first, second) = CreateMapWithTwoPins();
		var firstInfoWindowClicks = 0;
		var secondInfoWindowClicks = 0;
		var markerClicks = 0;
		first.InfoWindowClicked += (_, args) =>
		{
			firstInfoWindowClicks++;
			args.HideInfoWindow = hideInfoWindow;
		};
		second.InfoWindowClicked += (_, _) => secondInfoWindowClicks++;
		first.MarkerClicked += (_, _) => markerClicks++;

		await WithMapPageAsync(map, async (_, page) =>
		{
			await WaitForPinsAsync(page);
			// The info window of the page opens when its pin is clicked, and it remembers which pin that was.
			await page.RaiseMapEventAsync($"window.__clickPin('{first.Id}');");

			await page.RaiseMapEventAsync("window.__clickInfobox();");
			await page.SettleAsync();

			Assert.Equal(1, markerClicks);
			Assert.Equal(1, firstInfoWindowClicks);
			Assert.Equal(0, secondInfoWindowClicks);
			Assert.Equal(hideInfoWindow, (await page.GetLogAsync()).Contains(hideInfoWindowEntry));
		});
	}

	[Fact]
	public async Task ClickOnTheInfoWindowWhileItIsClosedSendsNoMessage()
	{
		SetupMapBuilder();
		var (map, first, second) = CreateMapWithTwoPins();
		var clicks = 0;
		first.InfoWindowClicked += (_, _) => clicks++;
		second.InfoWindowClicked += (_, _) => clicks++;

		await WithMapPageAsync(map, async (_, page) =>
		{
			await WaitForPinsAsync(page);
			var messages = 0;
			page.WebView.WebMessageReceived += (_, _) => messages++;

			// The info window of the page starts closed: a click that still reaches it means that the person pressed its close button.
			Assert.Equal(1, await page.ExecuteForCountAsync("window.__clickInfobox();"));
			await PostOrderingMessageAsync(page);

			Assert.Equal(1, messages);
			Assert.Equal(0, clicks);
		});
	}

	[Fact]
	public async Task InfoWindowClickedMessageForAnUnknownPinHidesTheInfoWindow()
	{
		SetupMapBuilder();
		var map = new Map();

		await WithMapPageAsync(map, async (_, page) =>
		{
			await page.PostMessageAsync("{ id: 'InfoWindowClicked', payload: { infoWindowMarkerId: 'a-pin-the-map-never-had' } }");

			var log = await page.WaitForLogAsync(entries => entries.Contains(hideInfoWindowEntry), "The info window was not hidden for a pin that the map does not have.");
			Assert.Single(log, hideInfoWindowEntry);
		});
	}

	[Theory]
	[InlineData("{ id: 'InfoWindowClicked', payload: { infoWindowMarkerId: '' } }")]
	[InlineData("{ id: 'InfoWindowClicked', payload: { infoWindowMarkerId: null } }")]
	[InlineData("{ id: 'InfoWindowClicked', payload: { } }")]
	public async Task InfoWindowClickedMessageWithoutAMarkerIdDoesNothing(string message)
	{
		SetupMapBuilder();
		var (map, first, second) = CreateMapWithTwoPins();
		var clicks = 0;
		first.InfoWindowClicked += (_, _) => clicks++;
		second.InfoWindowClicked += (_, _) => clicks++;

		await WithMapPageAsync(map, async (_, page) =>
		{
			await WaitForPinsAsync(page);

			await page.PostMessageAsync(message);
			await page.SettleAsync();

			Assert.Equal(0, clicks);
			Assert.Empty(Entries(await page.GetLogAsync(), "infobox"));
		});
	}

	[Fact]
	public async Task DisconnectedHandlerIgnoresTheMessagesOfThePage()
	{
		SetupMapBuilder();
		var map = new Map();
		var clicks = new List<Location>();
		map.MapClicked += (_, args) => clicks.Add(args.Location);

		await WithMapPageAsync(map, async (handler, page) =>
		{
			await page.RaiseMapEventAsync("window.__clickMap(1.5, 2.5, true);");
			Assert.Single(clicks);

			((IElementHandler)handler).DisconnectHandler();

			// A handler that still listened would find no map any more, and fail.
			await page.RaiseMapEventAsync("window.__clickMap(3.5, 4.5, true);");
			await page.SettleAsync();

			Assert.Single(clicks);
		});
	}

	async Task AssertMessageIsIgnoredAsync(string message)
	{
		var map = new Map();
		var clicks = new List<Location>();
		map.MapClicked += (_, args) => clicks.Add(args.Location);

		await WithMapPageAsync(map, async (_, page) =>
		{
			await page.PostMessageAsync(message);
			await page.SettleAsync();

			Assert.Empty(clicks);
			Assert.Null(map.VisibleRegion);
			Assert.Empty(Entries(await page.GetLogAsync(), "infobox"));

			await page.PostMessageAsync("{ id: 'MapClicked', payload: { latitude: 7.5, longitude: 8.5 } }");
			Assert.Single(clicks);
		});
	}

	static (Map Map, Pin First, Pin Second) CreateMapWithTwoPins()
	{
		var first = new Pin { Label = "First", Address = "Here", Location = new Location(1.5, 2.5) };
		var second = new Pin { Label = "Second", Address = "There", Location = new Location(3.5, 4.5) };
		var map = new Map();
		map.Pins.Add(first);
		map.Pins.Add(second);
		return (map, first, second);
	}

	// A message the handler ignores (the JSON null), posted after a script that might have made the page post a message of its own. The
	// WebView2 delivers messages in the order they were posted, so when this one has arrived, an earlier message has arrived too.
	static Task PostOrderingMessageAsync(LoadedMapPage page) => page.PostMessageAsync("null");

	static Task<IReadOnlyList<string>> WaitForPinsAsync(LoadedMapPage page) =>
		page.WaitForLogAsync(entries => Entries(entries, "push").Count >= 2, "The pins never reached the page.");
}