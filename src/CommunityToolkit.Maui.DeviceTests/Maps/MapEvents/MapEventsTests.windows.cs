using System.Text.Json;
using CommunityToolkit.Maui.Maps.Handlers;
using Microsoft.Maui.Controls.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;
using Microsoft.Maui.Devices.Sensors;

namespace CommunityToolkit.Maui.DeviceTests;

// These tests need no device, but the unit tests do not reference the Maps package, which is only part of this app on Windows.
// The JSON below is what the map page posts (window.chrome.webview.postMessage, as WebView2 serializes it), so these tests pin the
// contract between the page and the handler.
[Category(TestCategory.MapEvents)]
public class MapEventsTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public void EventMessageStartsWithAnEmptyIdAndNoPayload()
	{
		var message = new EventMessage();

		Assert.Equal(string.Empty, message.Id);
		Assert.Null(message.Payload);
	}

	[Fact]
	public void EventMessageKeepsTheIdAndThePayloadItIsGiven()
	{
		var payload = new object();

		var message = new EventMessage { Id = "MapClicked", Payload = payload };

		Assert.Equal("MapClicked", message.Id);
		Assert.Same(payload, message.Payload);
	}

	[Fact]
	public void EventIdentifierHasUnknownAndTheFourEventsOfThePage()
	{
		Assert.Equal(["Unknown", "InfoWindowClicked", "BoundsChanged", "MapClicked", "PinClicked"], Enum.GetNames<EventIdentifier>());
		Assert.Equal(0, (int)EventIdentifier.Unknown);
	}

	[Theory]
	[InlineData("InfoWindowClicked", "InfoWindowClicked")]
	[InlineData("BoundsChanged", "BoundsChanged")]
	[InlineData("MapClicked", "MapClicked")]
	[InlineData("PinClicked", "PinClicked")]
	[InlineData("Unknown", "Unknown")]
	[InlineData("pinclicked", "PinClicked")]
	[InlineData("MAPCLICKED", "MapClicked")]
	// Enum.TryParse ignores the white space around the name, and it takes the number of a member as well.
	[InlineData(" PinClicked ", "PinClicked")]
	[InlineData("3", "MapClicked")]
	public void EventIdentifierIsParsedFromTheIdOfTheMessageIgnoringCase(string id, string expectedName)
	{
		Assert.True(Enum.TryParse<EventIdentifier>(id, true, out var identifier));
		Assert.Equal(expectedName, identifier.ToString());
	}

	[Theory]
	[InlineData("")]
	[InlineData("Bogus")]
	[InlineData("Pin Clicked")]
	[InlineData("Pin.Clicked")]
	public void EventIdentifierIsNotParsedFromAnUnknownId(string id)
	{
		Assert.False(Enum.TryParse<EventIdentifier>(id, true, out _));
	}

	[Fact]
	public void SerializerContextReadsAnEventMessageAndKeepsThePayloadAsJson()
	{
		const string json = """{"id":"MapClicked","payload":{"latitude":1.5,"longitude":2.5}}""";

		var message = JsonSerializer.Deserialize(json, SerializerContext.Default.EventMessage);

		Assert.NotNull(message);
		Assert.Equal("MapClicked", message.Id);
		// The handler reads the payload as text and deserializes it again, once it knows the type from the id.
		Assert.Equal("""{"latitude":1.5,"longitude":2.5}""", message.Payload?.ToString());
	}

	[Fact]
	public void SerializerContextMatchesPropertyNamesIgnoringCase()
	{
		const string json = """{"ID":"MapClicked","PAYLOAD":{"Latitude":1.5,"LONGITUDE":2.5}}""";

		var message = JsonSerializer.Deserialize(json, SerializerContext.Default.EventMessage);
		var location = JsonSerializer.Deserialize(message?.Payload?.ToString() ?? string.Empty, SerializerContext.Default.Location);

		Assert.Equal("MapClicked", message?.Id);
		Assert.Equal(1.5, location?.Latitude);
		Assert.Equal(2.5, location?.Longitude);
	}

	[Theory]
	[InlineData("""{"id":"MapClicked"}""")]
	[InlineData("""{"id":"MapClicked","payload":null}""")]
	public void SerializerContextReadsAMessageWithoutAPayloadAsNoPayload(string json)
	{
		var message = JsonSerializer.Deserialize(json, SerializerContext.Default.EventMessage);

		Assert.NotNull(message);
		Assert.Null(message.Payload);
		Assert.Null(message.Payload?.ToString());
	}

	[Fact]
	public void SerializerContextReadsAMessageWithoutAnIdAsAnEmptyId()
	{
		var message = JsonSerializer.Deserialize("""{"payload":{"latitude":1,"longitude":2}}""", SerializerContext.Default.EventMessage);

		Assert.NotNull(message);
		Assert.Equal(string.Empty, message.Id);
	}

	[Theory]
	[InlineData("\"MapClicked\"")]
	[InlineData("7")]
	[InlineData("[1,2]")]
	public void SerializerContextThrowsJsonExceptionForAMessageThatIsNotAnObject(string json)
	{
		// WebView2 hands the handler whatever the page posted, and the handler's DeserializeOrDefault catches only JsonException: anything else
		// would escape its async void event handler.
		Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, SerializerContext.Default.EventMessage));
	}

	[Fact]
	public void SerializerContextReadsNullAsNoMessage()
	{
		Assert.Null(JsonSerializer.Deserialize("null", SerializerContext.Default.EventMessage));
	}

	[Fact]
	public void SerializerContextWritesAnEventMessageWithTheNamesOfItsProperties()
	{
		var json = JsonSerializer.Serialize(new EventMessage { Id = "PinClicked", Payload = 7 }, SerializerContext.Default.EventMessage);

		Assert.Equal("""{"Id":"PinClicked","Payload":7}""", json);
	}

	[Fact]
	public void SerializerContextReadsTheBoundsOfTheMap()
	{
		const string json = """{"center":{"latitude":10.5,"longitude":20.5,"altitude":0,"altitudeReference":-1},"width":3,"height":2,"other":"ignored"}""";

		var bounds = JsonSerializer.Deserialize(json, SerializerContext.Default.Bounds);

		Assert.NotNull(bounds);
		Assert.NotNull(bounds.Center);
		Assert.Equal(10.5, bounds.Center.Latitude);
		Assert.Equal(20.5, bounds.Center.Longitude);
		Assert.Equal(0, bounds.Center.Altitude);
		Assert.Equal(-1, bounds.Center.AltitudeReference);
		Assert.Equal(3, bounds.Width);
		Assert.Equal(2, bounds.Height);
	}

	[Fact]
	public void SerializerContextReadsBoundsWithoutACenter()
	{
		var bounds = JsonSerializer.Deserialize("""{"center":null,"width":3,"height":2}""", SerializerContext.Default.Bounds);

		Assert.NotNull(bounds);
		Assert.Null(bounds.Center);
		Assert.Equal(3, bounds.Width);
		Assert.Equal(2, bounds.Height);
	}

	[Fact]
	public void SerializerContextReadsALocationAndIgnoresWhatItDoesNotKnow()
	{
		var location = JsonSerializer.Deserialize("""{"latitude":47.6205,"longitude":-122.3493,"altitudeReference":-1}""", SerializerContext.Default.Location);

		Assert.NotNull(location);
		Assert.Equal(47.6205, location.Latitude);
		Assert.Equal(-122.3493, location.Longitude);
	}

	[Fact]
	public void SerializerContextReadsTheMarkerIdOfAClickedPin()
	{
		// The page also posts the label, address and location of the pin: the handler reads only the marker id.
		const string json = """{"label":"Space Needle","address":"Seattle Center","location":{"latitude":47.6205,"longitude":-122.3493,"altitude":0,"altitudeReference":-1},"markerId":"4d1a9c1e-0f4f-4d7c-9d0b-4d3f5b5d9f10"}""";

		var clickedPin = JsonSerializer.Deserialize(json, SerializerContext.Default.ClickedPin);

		Assert.NotNull(clickedPin);
		// The marker id arrives as JSON: the handler only ever uses its text.
		Assert.Equal("4d1a9c1e-0f4f-4d7c-9d0b-4d3f5b5d9f10", clickedPin.MarkerId?.ToString());
	}

	[Fact]
	public void SerializerContextReadsAClickedPinWithoutAMarkerIdAsNoMarkerId()
	{
		var clickedPin = JsonSerializer.Deserialize("""{"label":"Space Needle"}""", SerializerContext.Default.ClickedPin);

		Assert.NotNull(clickedPin);
		Assert.Null(clickedPin.MarkerId);
		Assert.Null(clickedPin.MarkerId?.ToString());
	}

	// A script in the page can post any payload. These hold properties that JSON cannot give a Microsoft.Maui.Controls.Maps.Pin (an Element):
	// JsonSerializer threw for them while the handler read the payload into a Pin, inside its async void WebMessageReceived handler.
	[Theory]
	[InlineData("""{"markerId":"4d1a9c1e","parent":{}}""")]
	[InlineData("""{"markerId":"4d1a9c1e","handler":{}}""")]
	[InlineData("""{"markerId":"4d1a9c1e","automationId":"a","automationId":"b"}""")]
	public void SerializerContextReadsTheMarkerIdOfAClickedPinWhateverElseThePayloadHolds(string json)
	{
		var clickedPin = JsonSerializer.Deserialize(json, SerializerContext.Default.ClickedPin);

		Assert.NotNull(clickedPin);
		Assert.Equal("4d1a9c1e", clickedPin.MarkerId?.ToString());
	}

	[Theory]
	[InlineData("""{"infoWindowMarkerId":"4d1a9c1e"}""", "4d1a9c1e")]
	[InlineData("""{"InfoWindowMarkerId":"4D1A9C1E"}""", "4D1A9C1E")]
	[InlineData("""{}""", "")]
	public void SerializerContextReadsTheMarkerIdOfAnInfoWindow(string json, string expectedMarkerId)
	{
		var infoWindow = JsonSerializer.Deserialize(json, SerializerContext.Default.InfoWindow);

		Assert.NotNull(infoWindow);
		Assert.Equal(expectedMarkerId, infoWindow.InfoWindowMarkerId);
	}

	[Fact]
	public void SerializerContextKnowsTheTypesOfTheMessageAndOfItsPayloads()
	{
		Assert.NotNull(SerializerContext.Default.GetTypeInfo(typeof(EventMessage)));
		Assert.NotNull(SerializerContext.Default.GetTypeInfo(typeof(ClickedPin)));
		Assert.NotNull(SerializerContext.Default.GetTypeInfo(typeof(Location)));
		Assert.NotNull(SerializerContext.Default.GetTypeInfo(typeof(Bounds)));
		Assert.NotNull(SerializerContext.Default.GetTypeInfo(typeof(InfoWindow)));
		// The handler never deserializes other types, and it reads a clicked pin into ClickedPin, not into the MAUI Pin.
		Assert.Null(SerializerContext.Default.GetTypeInfo(typeof(Map)));
		Assert.Null(SerializerContext.Default.GetTypeInfo(typeof(Pin)));
	}
}