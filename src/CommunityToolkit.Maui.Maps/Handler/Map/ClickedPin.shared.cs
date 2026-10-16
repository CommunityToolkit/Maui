namespace CommunityToolkit.Maui.Maps.Handlers;

// The payload of the page's PinClicked message, of which the handler reads only the marker id. Not Microsoft.Maui.Controls.Maps.Pin: that
// is an Element, and JsonSerializer throws for a payload with properties such as parent or handler (abstract and interface types).
class ClickedPin
{
	public object? MarkerId { get; set; }
}