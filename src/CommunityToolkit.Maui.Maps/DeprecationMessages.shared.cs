namespace CommunityToolkit.Maui.Maps;

static class DeprecationMessages
{
	// One constant so the attribute text on every public Maps API stays identical; [Obsolete] needs a compile-time constant.
	public const string Maps = "CommunityToolkit.Maui.Maps is deprecated and will be removed in a future release because Bing Maps, which it uses on Windows, was retired; see https://github.com/CommunityToolkit/Maui/issues/3190.";
}
