using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Behaviors;

// The Android version is declared here, in the Android build only, as StatusBar does for itself. A list of platforms on the shared declaration would make the
// compiler treat the calls from this class to the StatusBar of the platform being built as reachable on the other listed platforms, where that StatusBar does
// not exist, and report CA1416 for each of them.
[SupportedOSPlatform("android23.0")]
public partial class StatusBarBehavior
{
}