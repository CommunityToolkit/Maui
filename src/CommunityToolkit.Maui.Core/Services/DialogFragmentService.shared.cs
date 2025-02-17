using System.ComponentModel;
using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Core.Services;

/// <summary>
/// An Android-specific service to support <see cref="CommunityToolkit.Maui.Core.Platform.StatusBar"/> on modal pages
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[SupportedOSPlatform("Android")]
public sealed partial class DialogFragmentService
{

}