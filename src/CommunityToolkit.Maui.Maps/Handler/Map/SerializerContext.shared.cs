using System.Text.Json.Serialization;
using Microsoft.Maui.Controls.Maps;

namespace CommunityToolkit.Maui.Maps.Handlers;

[JsonSerializable(typeof(EventMessage))]
[JsonSerializable(typeof(Pin))]
[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(Bounds))]
[JsonSerializable(typeof(InfoWindow))]
sealed partial class SerializerContext : JsonSerializerContext;