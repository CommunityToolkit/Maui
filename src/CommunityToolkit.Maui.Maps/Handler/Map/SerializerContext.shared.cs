using System.Text.Json.Serialization;

namespace CommunityToolkit.Maui.Maps.Handlers;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(EventMessage))]
[JsonSerializable(typeof(ClickedPin))]
[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(Bounds))]
[JsonSerializable(typeof(InfoWindow))]
sealed partial class SerializerContext : JsonSerializerContext;