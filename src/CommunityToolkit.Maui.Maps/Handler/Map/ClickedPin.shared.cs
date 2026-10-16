namespace CommunityToolkit.Maui.Maps.Handlers;

// Not Microsoft.Maui.Controls.Maps.Pin, which is an Element: JsonSerializer throws for a payload with properties such as parent or handler
// (abstract and interface types), and the handler reads only the marker id.
class ClickedPin
{
	public object? MarkerId { get; set; }
}