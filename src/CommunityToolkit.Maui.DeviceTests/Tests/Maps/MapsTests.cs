using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Maps;

/// <summary>
/// All Maps handler types (Bounds, Center, EventMessage, EventIdentifier, InfoWindow, SerializerContext)
/// are internal. Per PR #3251, reflection is the sanctioned approach for testing toolkit-owned internals.
/// </summary>
public class MapsReflectionHelper
{
	public static readonly Assembly MapsAssembly = typeof(CommunityToolkit.Maui.Maps.AppBuilderExtensions).Assembly;

	public static Type GetType(string typeName) =>
		MapsAssembly.GetType($"CommunityToolkit.Maui.Maps.Handlers.{typeName}")
		?? throw new InvalidOperationException($"Type CommunityToolkit.Maui.Maps.Handlers.{typeName} not found");

	public static object CreateInstance(string typeName)
	{
		var type = GetType(typeName);
		return Activator.CreateInstance(type, nonPublic: true)
			?? throw new InvalidOperationException($"Failed to create instance of {typeName}");
	}

	public static void SetProperty(object instance, string propertyName, object? value)
	{
		var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
		Assert.NotNull(property);
		property.SetValue(instance, value);
	}

	public static object? GetProperty(object instance, string propertyName)
	{
		var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
		Assert.NotNull(property);
		return property.GetValue(instance);
	}
}

public class BoundsTests
{
	[Fact]
	public void Bounds_DefaultValues()
	{
		var bounds = MapsReflectionHelper.CreateInstance("Bounds");

		Assert.Null(MapsReflectionHelper.GetProperty(bounds, "Center"));
		Assert.Equal(0d, MapsReflectionHelper.GetProperty(bounds, "Width"));
		Assert.Equal(0d, MapsReflectionHelper.GetProperty(bounds, "Height"));
	}

	[Fact]
	public void Bounds_CanSetProperties()
	{
		var center = MapsReflectionHelper.CreateInstance("Center");
		MapsReflectionHelper.SetProperty(center, "Latitude", 47.6062);
		MapsReflectionHelper.SetProperty(center, "Longitude", -122.3321);

		var bounds = MapsReflectionHelper.CreateInstance("Bounds");
		MapsReflectionHelper.SetProperty(bounds, "Center", center);
		MapsReflectionHelper.SetProperty(bounds, "Width", 360d);
		MapsReflectionHelper.SetProperty(bounds, "Height", 180d);

		var retrievedCenter = MapsReflectionHelper.GetProperty(bounds, "Center");
		Assert.NotNull(retrievedCenter);
		Assert.Equal(47.6062, MapsReflectionHelper.GetProperty(retrievedCenter, "Latitude"));
		Assert.Equal(-122.3321, MapsReflectionHelper.GetProperty(retrievedCenter, "Longitude"));
		Assert.Equal(360d, MapsReflectionHelper.GetProperty(bounds, "Width"));
		Assert.Equal(180d, MapsReflectionHelper.GetProperty(bounds, "Height"));
	}
}

public class CenterTests
{
	[Fact]
	public void Center_DefaultValues()
	{
		var center = MapsReflectionHelper.CreateInstance("Center");

		Assert.Equal(0d, MapsReflectionHelper.GetProperty(center, "Latitude"));
		Assert.Equal(0d, MapsReflectionHelper.GetProperty(center, "Longitude"));
		Assert.Equal(0, MapsReflectionHelper.GetProperty(center, "Altitude"));
		Assert.Equal(0, MapsReflectionHelper.GetProperty(center, "AltitudeReference"));
	}

	[Fact]
	public void Center_CanSetCoordinates()
	{
		var center = MapsReflectionHelper.CreateInstance("Center");
		MapsReflectionHelper.SetProperty(center, "Latitude", 51.5074);
		MapsReflectionHelper.SetProperty(center, "Longitude", -0.1278);
		MapsReflectionHelper.SetProperty(center, "Altitude", 11);
		MapsReflectionHelper.SetProperty(center, "AltitudeReference", 1);

		Assert.Equal(51.5074, MapsReflectionHelper.GetProperty(center, "Latitude"));
		Assert.Equal(-0.1278, MapsReflectionHelper.GetProperty(center, "Longitude"));
		Assert.Equal(11, MapsReflectionHelper.GetProperty(center, "Altitude"));
		Assert.Equal(1, MapsReflectionHelper.GetProperty(center, "AltitudeReference"));
	}
}

public class EventIdentifierEnumTests
{
	static readonly Type eventIdentifierType = MapsReflectionHelper.GetType("EventIdentifier");

	[Fact]
	public void EventIdentifier_HasExpectedValues()
	{
		var names = Enum.GetNames(eventIdentifierType);

		Assert.Contains("Unknown", names);
		Assert.Contains("InfoWindowClicked", names);
		Assert.Contains("BoundsChanged", names);
		Assert.Contains("MapClicked", names);
		Assert.Contains("PinClicked", names);
	}

	[Fact]
	public void EventIdentifier_UnknownIsZero()
	{
		var unknownValue = Enum.Parse(eventIdentifierType, "Unknown");

		Assert.Equal(0, (int)unknownValue);
	}

	[Fact]
	public void EventIdentifier_InfoWindowClickedIsOne()
	{
		var value = Enum.Parse(eventIdentifierType, "InfoWindowClicked");

		Assert.Equal(1, (int)value);
	}

	[Fact]
	public void EventIdentifier_BoundsChangedIsTwo()
	{
		var value = Enum.Parse(eventIdentifierType, "BoundsChanged");

		Assert.Equal(2, (int)value);
	}

	[Fact]
	public void EventIdentifier_MapClickedIsThree()
	{
		var value = Enum.Parse(eventIdentifierType, "MapClicked");

		Assert.Equal(3, (int)value);
	}

	[Fact]
	public void EventIdentifier_PinClickedIsFour()
	{
		var value = Enum.Parse(eventIdentifierType, "PinClicked");

		Assert.Equal(4, (int)value);
	}
}

public class EventMessageTests
{
	[Fact]
	public void EventMessage_CanBeCreated()
	{
		var message = MapsReflectionHelper.CreateInstance("EventMessage");
		MapsReflectionHelper.SetProperty(message, "Id", "event-123");
		MapsReflectionHelper.SetProperty(message, "Payload", "test payload");

		Assert.Equal("event-123", MapsReflectionHelper.GetProperty(message, "Id"));
		Assert.Equal("test payload", MapsReflectionHelper.GetProperty(message, "Payload"));
	}

	[Fact]
	public void EventMessage_DefaultId_IsEmpty()
	{
		var message = MapsReflectionHelper.CreateInstance("EventMessage");

		Assert.Equal(string.Empty, MapsReflectionHelper.GetProperty(message, "Id"));
	}

	[Fact]
	public void EventMessage_NullPayload()
	{
		var message = MapsReflectionHelper.CreateInstance("EventMessage");
		MapsReflectionHelper.SetProperty(message, "Id", "event-456");
		MapsReflectionHelper.SetProperty(message, "Payload", null);

		Assert.Null(MapsReflectionHelper.GetProperty(message, "Payload"));
	}
}

public class InfoWindowTests
{
	[Fact]
	public void InfoWindow_CanBeCreated()
	{
		var infoWindow = MapsReflectionHelper.CreateInstance("InfoWindow");
		MapsReflectionHelper.SetProperty(infoWindow, "InfoWindowMarkerId", "marker-789");

		Assert.Equal("marker-789", MapsReflectionHelper.GetProperty(infoWindow, "InfoWindowMarkerId"));
	}

	[Fact]
	public void InfoWindow_DefaultMarkerId_IsEmpty()
	{
		var infoWindow = MapsReflectionHelper.CreateInstance("InfoWindow");

		Assert.Equal(string.Empty, MapsReflectionHelper.GetProperty(infoWindow, "InfoWindowMarkerId"));
	}
}

public class SerializerContextTests
{
	static readonly Type serializerContextType = MapsReflectionHelper.GetType("SerializerContext");

	static readonly object defaultContext = serializerContextType
		.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
		?? throw new InvalidOperationException("SerializerContext.Default not found");

	static JsonSerializerContext DefaultContext => (JsonSerializerContext)defaultContext;

	static JsonTypeInfo GetTypeInfo(string typeName)
	{
		var property = serializerContextType.GetProperty(typeName, BindingFlags.Public | BindingFlags.Instance);
		Assert.NotNull(property);
		var value = property.GetValue(DefaultContext);
		Assert.NotNull(value);
		return (JsonTypeInfo)value;
	}

	[Fact]
	public void SerializerContext_CanSerializeEventMessage()
	{
		var message = MapsReflectionHelper.CreateInstance("EventMessage");
		MapsReflectionHelper.SetProperty(message, "Id", "test-id");
		MapsReflectionHelper.SetProperty(message, "Payload", "test-payload");

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
		Assert.Equal("deserialized-id", MapsReflectionHelper.GetProperty(message, "Id"));

		// Payload is typed as object?, so System.Text.Json deserializes it as a JsonElement.
		var payload = MapsReflectionHelper.GetProperty(message, "Payload");
		Assert.NotNull(payload);
		Assert.Equal("deserialized-payload", payload.ToString());
	}

	[Fact]
	public void SerializerContext_CanSerializeBounds()
	{
		var center = MapsReflectionHelper.CreateInstance("Center");
		MapsReflectionHelper.SetProperty(center, "Latitude", 40.7128);
		MapsReflectionHelper.SetProperty(center, "Longitude", -74.0060);

		var bounds = MapsReflectionHelper.CreateInstance("Bounds");
		MapsReflectionHelper.SetProperty(bounds, "Center", center);
		MapsReflectionHelper.SetProperty(bounds, "Width", 100d);
		MapsReflectionHelper.SetProperty(bounds, "Height", 50d);

		var json = JsonSerializer.Serialize(bounds, GetTypeInfo("Bounds"));

		Assert.Contains("40.7128", json);
	}

	[Fact]
	public void SerializerContext_CanDeserializeBounds()
	{
		var json = """{"Center":{"Latitude":35.6762,"Longitude":139.6503,"Altitude":0,"AltitudeReference":0},"Width":200,"Height":100}""";

		var bounds = JsonSerializer.Deserialize(json, GetTypeInfo("Bounds"));

		Assert.NotNull(bounds);
		var center = MapsReflectionHelper.GetProperty(bounds, "Center");
		Assert.NotNull(center);
		Assert.Equal(35.6762, MapsReflectionHelper.GetProperty(center, "Latitude"));
		Assert.Equal(139.6503, MapsReflectionHelper.GetProperty(center, "Longitude"));
		Assert.Equal(200d, MapsReflectionHelper.GetProperty(bounds, "Width"));
		Assert.Equal(100d, MapsReflectionHelper.GetProperty(bounds, "Height"));
	}

	[Fact]
	public void SerializerContext_CanSerializeInfoWindow()
	{
		var infoWindow = MapsReflectionHelper.CreateInstance("InfoWindow");
		MapsReflectionHelper.SetProperty(infoWindow, "InfoWindowMarkerId", "marker-abc");

		var json = JsonSerializer.Serialize(infoWindow, GetTypeInfo("InfoWindow"));

		Assert.Contains("marker-abc", json);
	}

	[Fact]
	public void SerializerContext_RoundTrip_EventMessage()
	{
		var original = MapsReflectionHelper.CreateInstance("EventMessage");
		MapsReflectionHelper.SetProperty(original, "Id", "round-trip");
		MapsReflectionHelper.SetProperty(original, "Payload", 42);

		var typeInfo = GetTypeInfo("EventMessage");
		var json = JsonSerializer.Serialize(original, typeInfo);
		var deserialized = JsonSerializer.Deserialize(json, typeInfo);

		Assert.NotNull(deserialized);
		Assert.Equal("round-trip", MapsReflectionHelper.GetProperty(deserialized, "Id"));
	}

	[Fact]
	public void SerializerContext_IsJsonSerializerContext()
	{
		Assert.IsAssignableFrom<JsonSerializerContext>(DefaultContext);
	}
}
