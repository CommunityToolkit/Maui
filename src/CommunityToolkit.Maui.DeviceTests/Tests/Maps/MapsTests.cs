using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using CommunityToolkit.Maui.Maps.Handlers;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Maps;

[Trait("Category", "ExpectedFailure")]
public class BoundsTests
{
	[Fact]
	public void Bounds_DefaultValues()
	{
		var bounds = new Bounds();

		Assert.Null(bounds.Center);
		Assert.Equal(0d, bounds.Width);
		Assert.Equal(0d, bounds.Height);
	}

	[Fact]
	public void Bounds_CanSetProperties()
	{
		var center = new Center
		{
			Latitude = 47.6062,
			Longitude = -122.3321,
		};

		var bounds = new Bounds
		{
			Center = center,
			Width = 360d,
			Height = 180d,
		};

		Assert.NotNull(bounds.Center);
		Assert.Equal(47.6062, bounds.Center.Latitude);
		Assert.Equal(-122.3321, bounds.Center.Longitude);
		Assert.Equal(360d, bounds.Width);
		Assert.Equal(180d, bounds.Height);
	}
}

[Trait("Category", "ExpectedFailure")]
public class CenterTests
{
	[Fact]
	public void Center_DefaultValues()
	{
		var center = new Center();

		Assert.Equal(0d, center.Latitude);
		Assert.Equal(0d, center.Longitude);
		Assert.Equal(0, center.Altitude);
		Assert.Equal(0, center.AltitudeReference);
	}

	[Fact]
	public void Center_CanSetCoordinates()
	{
		var center = new Center
		{
			Latitude = 51.5074,
			Longitude = -0.1278,
			Altitude = 11,
			AltitudeReference = 1,
		};

		Assert.Equal(51.5074, center.Latitude);
		Assert.Equal(-0.1278, center.Longitude);
		Assert.Equal(11, center.Altitude);
		Assert.Equal(1, center.AltitudeReference);
	}
}

[Trait("Category", "ExpectedFailure")]
public class EventIdentifierEnumTests
{
	[Fact]
	public void EventIdentifier_HasExpectedValues()
	{
		var names = Enum.GetNames(typeof(EventIdentifier));

		Assert.Contains("Unknown", names);
		Assert.Contains("InfoWindowClicked", names);
		Assert.Contains("BoundsChanged", names);
		Assert.Contains("MapClicked", names);
		Assert.Contains("PinClicked", names);
	}

	[Fact]
	public void EventIdentifier_UnknownIsZero()
	{
		Assert.Equal(0, (int)EventIdentifier.Unknown);
	}

	[Fact]
	public void EventIdentifier_InfoWindowClickedIsOne()
	{
		Assert.Equal(1, (int)EventIdentifier.InfoWindowClicked);
	}

	[Fact]
	public void EventIdentifier_BoundsChangedIsTwo()
	{
		Assert.Equal(2, (int)EventIdentifier.BoundsChanged);
	}

	[Fact]
	public void EventIdentifier_MapClickedIsThree()
	{
		Assert.Equal(3, (int)EventIdentifier.MapClicked);
	}

	[Fact]
	public void EventIdentifier_PinClickedIsFour()
	{
		Assert.Equal(4, (int)EventIdentifier.PinClicked);
	}
}

[Trait("Category", "ExpectedFailure")]
public class EventMessageTests
{
	[Fact]
	public void EventMessage_CanBeCreated()
	{
		var message = new EventMessage
		{
			Id = "event-123",
			Payload = "test payload",
		};

		Assert.Equal("event-123", message.Id);
		Assert.Equal("test payload", message.Payload);
	}

	[Fact]
	public void EventMessage_DefaultId_IsEmpty()
	{
		var message = new EventMessage();

		Assert.Equal(string.Empty, message.Id);
	}

	[Fact]
	public void EventMessage_NullPayload()
	{
		var message = new EventMessage
		{
			Id = "event-456",
			Payload = null,
		};

		Assert.Null(message.Payload);
	}
}

[Trait("Category", "ExpectedFailure")]
public class InfoWindowTests
{
	[Fact]
	public void InfoWindow_CanBeCreated()
	{
		var infoWindow = new InfoWindow
		{
			InfoWindowMarkerId = "marker-789",
		};

		Assert.Equal("marker-789", infoWindow.InfoWindowMarkerId);
	}

	[Fact]
	public void InfoWindow_DefaultMarkerId_IsEmpty()
	{
		var infoWindow = new InfoWindow();

		Assert.Equal(string.Empty, infoWindow.InfoWindowMarkerId);
	}
}

[Trait("Category", "ExpectedFailure")]
public class SerializerContextTests
{
	static JsonTypeInfo GetTypeInfo(string typeName)
	{
		return typeName switch
		{
			"EventMessage" => SerializerContext.Default.EventMessage,
			"Bounds" => SerializerContext.Default.Bounds,
			"InfoWindow" => SerializerContext.Default.InfoWindow,
			_ => throw new InvalidOperationException($"Unknown type: {typeName}"),
		};
	}

	[Fact]
	public void SerializerContext_CanSerializeEventMessage()
	{
		var message = new EventMessage
		{
			Id = "test-id",
			Payload = "test-payload",
		};

		var json = JsonSerializer.Serialize(message, GetTypeInfo("EventMessage"));

		Assert.Contains("test-id", json);
		Assert.Contains("test-payload", json);
	}

	[Fact]
	public void SerializerContext_CanDeserializeEventMessage()
	{
		var json = """{"Id":"deserialized-id","Payload":"deserialized-payload"}""";

		var message = JsonSerializer.Deserialize(json, GetTypeInfo("EventMessage"));

		Assert.NotNull(message);
		var eventMessage = (EventMessage)message;
		Assert.Equal("deserialized-id", eventMessage.Id);

		// Payload is typed as object?, so System.Text.Json deserializes it as a JsonElement.
		Assert.NotNull(eventMessage.Payload);
		Assert.Equal("deserialized-payload", eventMessage.Payload.ToString());
	}

	[Fact]
	public void SerializerContext_CanSerializeBounds()
	{
		var center = new Center
		{
			Latitude = 40.7128,
			Longitude = -74.0060,
		};

		var bounds = new Bounds
		{
			Center = center,
			Width = 100d,
			Height = 50d,
		};

		var json = JsonSerializer.Serialize(bounds, GetTypeInfo("Bounds"));

		Assert.Contains("40.7128", json);
	}

	[Fact]
	public void SerializerContext_CanDeserializeBounds()
	{
		var json = """{"Center":{"Latitude":35.6762,"Longitude":139.6503,"Altitude":0,"AltitudeReference":0},"Width":200,"Height":100}""";

		var bounds = JsonSerializer.Deserialize(json, GetTypeInfo("Bounds"));

		Assert.NotNull(bounds);
		var boundsObj = (Bounds)bounds;
		Assert.NotNull(boundsObj.Center);
		Assert.Equal(35.6762, boundsObj.Center.Latitude);
		Assert.Equal(139.6503, boundsObj.Center.Longitude);
		Assert.Equal(200d, boundsObj.Width);
		Assert.Equal(100d, boundsObj.Height);
	}

	[Fact]
	public void SerializerContext_CanSerializeInfoWindow()
	{
		var infoWindow = new InfoWindow
		{
			InfoWindowMarkerId = "marker-abc",
		};

		var json = JsonSerializer.Serialize(infoWindow, GetTypeInfo("InfoWindow"));

		Assert.Contains("marker-abc", json);
	}

	[Fact]
	public void SerializerContext_RoundTrip_EventMessage()
	{
		var original = new EventMessage
		{
			Id = "round-trip",
			Payload = 42,
		};

		var typeInfo = GetTypeInfo("EventMessage");
		var json = JsonSerializer.Serialize(original, typeInfo);
		var deserialized = JsonSerializer.Deserialize(json, typeInfo);

		Assert.NotNull(deserialized);
		var message = (EventMessage)deserialized;
		Assert.Equal("round-trip", message.Id);
	}

	[Fact]
	public void SerializerContext_IsJsonSerializerContext()
	{
		Assert.IsAssignableFrom<JsonSerializerContext>(SerializerContext.Default);
	}
}
