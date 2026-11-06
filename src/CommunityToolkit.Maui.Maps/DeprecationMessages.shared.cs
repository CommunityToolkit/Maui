namespace CommunityToolkit.Maui.Maps;

static class DeprecationMessages
{
	// One constant so the attribute text on every public Maps API stays identical; [Obsolete] needs a compile-time constant.
	public const string Maps = "CommunityToolkit.Maui.Maps is deprecated and will be removed in a future release. Microsoft is retiring Bing Maps for Enterprise, which it uses on Windows. Use the .NET MAUI Map (Microsoft.Maui.Controls.Maps), which supports Windows starting with .NET MAUI 10.0.70. See https://github.com/CommunityToolkit/Maui/issues/3190";
}
