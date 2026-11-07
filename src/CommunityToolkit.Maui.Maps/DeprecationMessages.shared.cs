namespace CommunityToolkit.Maui.Maps;

static class DeprecationMessages
{
	// One constant so the attribute text on every public Maps API stays identical; [Obsolete] needs a compile-time constant.
	public const string Maps = "CommunityToolkit.Maui.Maps is deprecated and will be removed in a future release. Call UseMauiMaps() from .NET MAUI's Microsoft.Maui.Controls.Maps instead; it supports Windows since .NET MAUI 10, where it needs an Azure Maps key. See https://github.com/CommunityToolkit/Maui/issues/3190";
}
